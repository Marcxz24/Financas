using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Financas.Api.Services
{
    // Serviço responsável por enviar e-mails usando a API do Resend
    public class EmailService
    {
        // IHttpClientFactory: gerencia criação de HttpClient de forma correta (evita leaks e melhora performance)
        private readonly IHttpClientFactory _httpClientFactory;

        // IConfiguration: acessa appsettings.json / variáveis de ambiente
        private readonly IConfiguration _configuration;

        // Chave de autenticação da API do Resend
        private readonly string _apiKey;

        // E-mail remetente configurado no Resend
        private readonly string _from;

        // Construtor: injeta dependências necessárias pelo ASP.NET Core (DI)
        public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;

            // Lê a API Key do Resend no appsettings ou variável de ambiente
            _apiKey = _configuration["Resend:ApiKey"]
                ?? throw new Exception("Resend API Key não configurada");

            // Lê o e-mail remetente configurado
            _from = _configuration["Resend:From"]
                ?? throw new Exception("From do Resend não configurado.");
        }

        // Método principal responsável por enviar o e-mail
        public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
        {
            // Cria um HttpClient a partir da factory (forma correta no ASP.NET Core)
            var client = _httpClientFactory.CreateClient();

            // Define a URL base da API do Resend
            client.BaseAddress = new Uri("https://api.resend.com");

            // Adiciona autenticação Bearer Token (API Key do Resend)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            // Monta o payload (dados do e-mail) no formato esperado pela API
            var payload = new
            {
                from = _from,                     // remetente
                to = new[] { destinatario },      // destinatário (array porque pode enviar múltiplos)
                subject = assunto,                // assunto do e-mail
                html = corpo                     // corpo em HTML
            };

            // Converte o objeto para JSON
            var json = JsonSerializer.Serialize(payload);

            // Cria o conteúdo da requisição HTTP (JSON + encoding UTF8)
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Faz a requisição POST para o endpoint de envio de e-mail
            var response = await client.PostAsync("/emails", content);

            // Se a resposta não for sucesso (200-299), trata como erro
            if (!response.IsSuccessStatusCode)
            {
                // Lê o corpo da resposta de erro da API
                var body = await response.Content.ReadAsStringAsync();

                // Lança exceção com detalhes do erro (ajuda debug em produção)
                throw new Exception($"Erro ao enviar e-mail via Resend: {response.StatusCode} - {body}");
            }
        }
    }
}