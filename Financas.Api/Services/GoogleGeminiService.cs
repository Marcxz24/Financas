using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável exclusivamente pela comunicação HTTP com a API do
    /// Google Gemini (Generative Language API).
    /// Segue o princípio de Responsabilidade Única (SRP): nenhuma regra de negócio
    /// financeiro é executada aqui — apenas o transporte da requisição e resposta HTTP.
    /// Toda configuração é lida do appsettings.json via IConfiguration,
    /// sem nenhum valor fixo (hardcoded) no código.
    /// </summary>
    public class GoogleGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleGeminiService> _logger;

        // Endpoint base da API do Gemini. O nome do modelo e a operação
        // (generateContent) são adicionados dinamicamente na URL, conforme
        // exigido pela API do Google.
        private const string BaseEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";

        public GoogleGeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleGeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envia uma requisição de geração de conteúdo para a API do Gemini e retorna
        /// o texto gerado pelo modelo de linguagem. Toda a lógica de montagem de
        /// contexto financeiro e prompt é responsabilidade do IAService.
        /// Este método recebe apenas strings prontas para envio.
        /// </summary>
        /// <param name="systemPrompt">
        /// Instrução de sistema que define o papel e as regras do assistente de IA.
        /// Enviada separadamente do prompt do usuário via "system_instruction",
        /// conforme o formato exigido pela API do Gemini.
        /// </param>
        /// <param name="userPrompt">
        /// Prompt completo do usuário já enriquecido com o contexto financeiro
        /// e a pergunta original, montado pelo IAService.
        /// </param>
        /// <returns>
        /// Texto da resposta gerada pelo modelo de IA.
        /// </returns>
        /// <exception cref="HttpRequestException">
        /// Lançada quando ocorre falha de comunicação HTTP com a API do Gemini.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Lançada quando a resposta da API não contém conteúdo válido.
        /// </exception>
        public async Task<string> EnviarMensagemAsync(string systemPrompt, string userPrompt)
        {
            // Lê configurações do appsettings.json — zero valores hardcoded
            var apiKey = _configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey não configurada no appsettings.");

            var modelo = _configuration["Gemini:Modelo"]
                ?? throw new InvalidOperationException("Gemini:Modelo não configurado no appsettings.");

            _logger.LogInformation(
                "[GoogleGeminiService] Iniciando requisição ao modelo {Modelo}. Tamanho do prompt: {Tamanho} chars.",
                modelo,
                userPrompt.Length);

            // Monta o corpo da requisição no formato exigido pela API do Gemini.
            // Diferente do padrão OpenAI/OpenRouter, o Gemini separa a instrução
            // de sistema ("system_instruction") do conteúdo do usuário ("contents"),
            // e cada trecho de texto é encapsulado em um objeto "parts".
            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = ObterMaxTokens(),
                    temperature = ObterTemperature()
                }
            };

            var json = JsonSerializer.Serialize(requestBody,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = $"{BaseEndpoint}/{modelo}:generateContent";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            // A API do Gemini autentica via header "x-goog-api-key",
            // diferente do esquema Bearer usado pela OpenRouter.
            request.Headers.Add("x-goog-api-key", apiKey);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "[GoogleGeminiService] Falha na comunicação HTTP com o Gemini. Modelo: {Modelo}",
                    modelo);
                throw;
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[GoogleGeminiService] API retornou erro {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    responseBody);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        // O Gemini pode retornar tanto 401 quanto 403
                        // para chave de API ausente, inválida ou sem permissão.
                        throw new Exception(
                            "Falha de autenticação no Gemini. Verifique a API Key.");

                    case HttpStatusCode.TooManyRequests:
                        throw new Exception(
                            "O modelo de IA está temporariamente indisponível ou atingiu o limite de requisições. Tente novamente em alguns instantes.");

                    case HttpStatusCode.BadRequest:
                        throw new Exception(
                            "A requisição enviada para o Gemini é inválida.");

                    case HttpStatusCode.NotFound:
                        throw new Exception(
                            $"Modelo '{modelo}' não encontrado. Verifique o nome do modelo configurado.");

                    default:
                        throw new Exception(
                            $"Gemini retornou {(int)response.StatusCode}.");
                }
            }

            // Desserializa a resposta no formato padrão da API do Gemini
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDTO>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var texto = geminiResponse?
                .Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

            if (string.IsNullOrWhiteSpace(texto))
            {
                _logger.LogWarning(
                    "[GoogleGeminiService] Resposta do Gemini não continha conteúdo válido. Body: {Body}",
                    responseBody);

                throw new InvalidOperationException(
                    "A IA não retornou uma resposta válida. Tente novamente em instantes.");
            }

            _logger.LogInformation(
                "[GoogleGeminiService] Resposta recebida com sucesso. Tamanho: {Tamanho} chars.",
                texto.Length);

            return texto.Trim();
        }

        /// <summary>
        /// Retorna o nome do modelo configurado no appsettings para uso nos metadados da resposta.
        /// Utilizado pelo IAService para preencher o campo ModeloUtilizado do RespostaIADTO.
        /// </summary>
        public string ObterNomeModelo()
        {
            return _configuration["Gemini:Modelo"] ?? "modelo-nao-configurado";
        }

        // ── Helpers privados ──────────────────────────────────────────────────

        /// <summary>
        /// Lê o limite de tokens de saída da configuração, com fallback seguro de 1500.
        /// Valor configurável via appsettings para evitar custos inesperados.
        /// </summary>
        private int ObterMaxTokens()
        {
            var valor = _configuration["Gemini:MaxTokens"];
            return int.TryParse(valor, out var tokens) ? tokens : 1500;
        }

        /// <summary>
        /// Lê a temperatura do modelo da configuração, com fallback de 0.7.
        /// Valores mais baixos = respostas mais determinísticas e conservadoras.
        /// Valores mais altos = respostas mais criativas (não recomendado para finanças).
        /// </summary>
        private double ObterTemperature()
        {
            var valor = _configuration["Gemini:Temperature"];
            return double.TryParse(valor,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var temp)
                ? temp
                : 0.7;
        }

        // ── Classes internas de desserialização da resposta do Gemini ────────

        /// <summary>
        /// Representa o envelope de resposta da API generateContent do Gemini.
        /// Estrutura baseada na especificação oficial da Generative Language API.
        /// </summary>
        private class GeminiResponseDTO
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidateDTO>? Candidates { get; set; }
        }

        /// <summary>
        /// Representa uma candidata (geração) retornada pelo modelo de IA.
        /// </summary>
        private class GeminiCandidateDTO
        {
            [JsonPropertyName("content")]
            public GeminiContentDTO? Content { get; set; }
        }

        /// <summary>
        /// Representa o conteúdo gerado pelo modelo, composto por uma ou mais "parts".
        /// </summary>
        private class GeminiContentDTO
        {
            [JsonPropertyName("parts")]
            public List<GeminiPartDTO>? Parts { get; set; }
        }

        /// <summary>
        /// Representa um fragmento de texto gerado pelo assistente de IA.
        /// </summary>
        private class GeminiPartDTO
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private int ObterThinkingBudget()
        {
            var valor = _configuration["Gemini:ThinkingBudget"];

            return int.TryParse(valor, out var budget)
                ? budget
                : 0;
        }
    }
}