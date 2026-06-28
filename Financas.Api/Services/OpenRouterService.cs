using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável exclusivamente pela comunicação HTTP com a API da OpenRouter.
    /// Segue o princípio de Responsabilidade Única (SRP): nenhuma regra de negócio
    /// financeiro é executada aqui — apenas o transporte da requisição e resposta HTTP.
    /// Toda configuração é lida do appsettings.json via IConfiguration,
    /// sem nenhum valor fixo (hardcoded) no código.
    /// </summary>
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenRouterService> _logger;

        // Constantes de rota e cabeçalhos da API OpenRouter
        private const string ChatCompletionsEndpoint = "https://openrouter.ai/api/v1/chat/completions";

        /// <summary>
        /// Construtor com injeção de dependência de HttpClient, IConfiguration e ILogger.
        /// O HttpClient é registrado via AddHttpClient no Program.cs para suporte
        /// a pooling de conexões e configuração centralizada.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP injetado pelo IHttpClientFactory.</param>
        /// <param name="configuration">Acesso às configurações do appsettings.json.</param>
        /// <param name="logger">Logger estruturado para rastreabilidade das requisições.</param>
        public OpenRouterService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenRouterService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envia uma requisição de Chat Completion para a API da OpenRouter e retorna
        /// o texto gerado pelo modelo de linguagem. Toda a lógica de montagem de
        /// contexto financeiro e prompt é responsabilidade do IAService.
        /// Este método recebe apenas strings prontas para envio.
        /// </summary>
        /// <param name="systemPrompt">
        /// Instrução de sistema que define o papel e as regras do assistente de IA.
        /// Separado do prompt do usuário conforme boas práticas de engenharia de prompt.
        /// </param>
        /// <param name="userPrompt">
        /// Prompt completo do usuário já enriquecido com o contexto financeiro
        /// e a pergunta original, montado pelo IAService.
        /// </param>
        /// <returns>
        /// Texto da resposta gerada pelo modelo de IA.
        /// </returns>
        /// <exception cref="HttpRequestException">
        /// Lançada quando a API da OpenRouter retorna erro HTTP (4xx/5xx).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Lançada quando a resposta da API não contém conteúdo válido.
        /// </exception>
        public async Task<string> EnviarMensagemAsync(string systemPrompt, string userPrompt)
        {
            // Lê configurações do appsettings.json — zero valores hardcoded
            var apiKey = _configuration["OpenRouter:ApiKey"]
                ?? throw new InvalidOperationException("OpenRouter:ApiKey não configurada no appsettings.");

            var modelo = _configuration["OpenRouter:Modelo"]
                ?? throw new InvalidOperationException("OpenRouter:Modelo não configurado no appsettings.");

            var appUrl = _configuration["App:BaseUrl"] ?? string.Empty;

            _logger.LogInformation(
                "[OpenRouterService] Iniciando requisição ao modelo {Modelo}. Tamanho do prompt: {Tamanho} chars.",
                modelo,
                userPrompt.Length);

            // Monta o corpo da requisição no formato Chat Completions da OpenRouter/OpenAI
            var requestBody = new
            {
                model = modelo,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                },
                max_tokens = ObterMaxTokens(),
                temperature = ObterTemperature()
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Configura os cabeçalhos exigidos pela OpenRouter
            // (Authorization, HTTP-Referer e X-Title são necessários para uso correto da API)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            if (!string.IsNullOrWhiteSpace(appUrl))
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", appUrl);

            _httpClient.DefaultRequestHeaders.Add("X-Title", "Financas AI Assistant");

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(ChatCompletionsEndpoint, content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "[OpenRouterService] Falha na comunicação HTTP com a OpenRouter. Modelo: {Modelo}",
                    modelo);
                throw;
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[OpenRouterService] API retornou erro {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    responseBody);

                throw new HttpRequestException(
                    $"OpenRouter retornou status {(int)response.StatusCode}. " +
                    $"Verifique a chave de API e as configurações no appsettings.");
            }

            // Desserializa a resposta no formato padrão Chat Completions
            var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponseDTO>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var texto = openRouterResponse?
                .Choices?
                .FirstOrDefault()?
                .Message?
                .Content;

            if (string.IsNullOrWhiteSpace(texto))
            {
                _logger.LogWarning(
                    "[OpenRouterService] Resposta da OpenRouter não continha conteúdo válido. Body: {Body}",
                    responseBody);

                throw new InvalidOperationException(
                    "A IA não retornou uma resposta válida. Tente novamente em instantes.");
            }

            _logger.LogInformation(
                "[OpenRouterService] Resposta recebida com sucesso. Tamanho: {Tamanho} chars.",
                texto.Length);

            return texto.Trim();
        }

        /// <summary>
        /// Retorna o nome do modelo configurado no appsettings para uso nos metadados da resposta.
        /// Utilizado pelo IAService para preencher o campo ModeloUtilizado do RespostaIADTO.
        /// </summary>
        public string ObterNomeModelo()
        {
            return _configuration["OpenRouter:Modelo"] ?? "modelo-nao-configurado";
        }

        // ── Helpers privados ──────────────────────────────────────────────────

        /// <summary>
        /// Lê o limite de tokens da configuração, com fallback seguro de 1500.
        /// Valor configurável via appsettings para evitar custos inesperados.
        /// </summary>
        private int ObterMaxTokens()
        {
            var valor = _configuration["OpenRouter:MaxTokens"];
            return int.TryParse(valor, out var tokens) ? tokens : 1500;
        }

        /// <summary>
        /// Lê a temperatura do modelo da configuração, com fallback de 0.7.
        /// Valores mais baixos = respostas mais determinísticas e conservadoras.
        /// Valores mais altos = respostas mais criativas (não recomendado para finanças).
        /// </summary>
        private double ObterTemperature()
        {
            var valor = _configuration["OpenRouter:Temperature"];
            return double.TryParse(valor,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var temp)
                ? temp
                : 0.7;
        }

        // ── Classes internas de desserialização da resposta da OpenRouter ────

        /// <summary>
        /// Representa o envelope de resposta da API Chat Completions da OpenRouter.
        /// Estrutura baseada na especificação oficial da API (compatível com OpenAI).
        /// </summary>
        private class OpenRouterResponseDTO
        {
            [JsonPropertyName("choices")]
            public List<OpenRouterChoiceDTO>? Choices { get; set; }
        }

        /// <summary>
        /// Representa uma escolha (geração) retornada pelo modelo de IA.
        /// </summary>
        private class OpenRouterChoiceDTO
        {
            [JsonPropertyName("message")]
            public OpenRouterMessageDTO? Message { get; set; }
        }

        /// <summary>
        /// Representa a mensagem gerada pelo assistente de IA.
        /// </summary>
        private class OpenRouterMessageDTO
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
