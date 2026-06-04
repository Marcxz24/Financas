using System.Text;
using System.Text.Json;

namespace Financas.Api.Services
{
    // Serviço responsável por enviar e-mails usando a API REST do Brevo
    public class EmailService
    {
        private readonly HttpClient _httpClient;

        // Chave de autenticação da API do Brevo
        private readonly string _apiKey;

        // E-mail remetente configurado no Brevo
        private readonly string _fromEmail;

        // Construtor: injeta dependências necessárias pelo ASP.NET Core (DI)
        public EmailService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;

            // Lê a API Key do Brevo no appsettings ou variável de ambiente
            _apiKey = configuration["Brevo:ApiKey"]
                ?? throw new InvalidOperationException("Brevo API Key não configurada.");

            // Lê o e-mail remetente configurado
            _fromEmail = configuration["Brevo:FromEmail"]
                ?? throw new InvalidOperationException("Brevo FromEmail não configurado.");
        }

        // Método principal responsável por enviar o e-mail
        public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
                throw new ArgumentException("Destinatário não informado.");

            if (string.IsNullOrWhiteSpace(assunto))
                throw new ArgumentException("Assunto não informado.");

            if (string.IsNullOrWhiteSpace(corpo))
                throw new ArgumentException("Corpo do e-mail não informado.");

            var payload = new
            {
                sender = new
                {
                    name = "Finanças",
                    email = _fromEmail
                },

                to = new[]
                {
                new
                {
                    email = destinatario
                }
            },

                subject = assunto,

                htmlContent = corpo
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

            request.Headers.Add("api-key", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erro ao enviar e-mail via Brevo: {response.StatusCode} - {responseBody}");
            }
        }
    }
}