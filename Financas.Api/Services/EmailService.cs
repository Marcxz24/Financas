using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class EmailService
{
    // Interface para acessar as chaves de configuração (Host, Porta, Senha) do appsettings.json.
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
    {
        try
        {
            Console.WriteLine("[EMAIL] Iniciando envio.");

            // Instancia o objeto da mensagem utilizando a biblioteca MimeKit.
            var mensagem = new MimeMessage();

            Console.WriteLine("[EMAIL] Montando mensagem.");

            // Define o remetente com base no endereço configurado no sistema.
            mensagem.From.Add(MailboxAddress.Parse(_configuration["Email:From"]!));

            // Adiciona o endereço do usuário que receberá a mensagem.
            mensagem.To.Add(MailboxAddress.Parse(destinatario));

            // Define o título do e-mail.
            mensagem.Subject = assunto;

            // Define o conteúdo como HTML.
            mensagem.Body = new TextPart("html")
            {
                Text = corpo
            };

            Console.WriteLine($"[EMAIL] Destinatário: {destinatario}");
            Console.WriteLine($"[EMAIL] Host SMTP: {_configuration["Email:Host"]}");
            Console.WriteLine($"[EMAIL] Porta SMTP: {_configuration["Email:Port"]}");
            Console.WriteLine($"[EMAIL] Usuário SMTP: {_configuration["Email:Username"]}");

            // Inicializa o cliente SMTP do MailKit.
            using var smtp = new SmtpClient
            {
                Timeout = 30000 // 30 segundos
            };

            Console.WriteLine("[EMAIL] Antes ConnectAsync...");

            await smtp.ConnectAsync(
                _configuration["Email:Host"]!,
                int.Parse(_configuration["Email:Port"]!),
                SecureSocketOptions.StartTls
            );

            Console.WriteLine("[EMAIL] ConnectAsync executado com sucesso.");

            Console.WriteLine("[EMAIL] Antes AuthenticateAsync...");

            await smtp.AuthenticateAsync(
                _configuration["Email:Username"]!,
                _configuration["Email:Password"]!
            );

            Console.WriteLine("[EMAIL] AuthenticateAsync executado com sucesso.");

            Console.WriteLine("[EMAIL] Antes SendAsync...");

            await smtp.SendAsync(mensagem);

            Console.WriteLine("[EMAIL] SendAsync executado com sucesso.");

            Console.WriteLine("[EMAIL] Antes DisconnectAsync...");

            await smtp.DisconnectAsync(true);

            Console.WriteLine("[EMAIL] DisconnectAsync executado com sucesso.");
            Console.WriteLine("[EMAIL] Processo finalizado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[EMAIL] ERRO DURANTE ENVIO");
            Console.WriteLine($"[EMAIL] Tipo: {ex.GetType().Name}");
            Console.WriteLine($"[EMAIL] Mensagem: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"[EMAIL] InnerException: {ex.InnerException.Message}");
            }

            Console.WriteLine($"[EMAIL] StackTrace: {ex.StackTrace}");

            throw;
        }
    }
}