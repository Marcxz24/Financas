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
        // Instancia o objeto da mensagem utilizando a biblioteca MimeKit.
        var mensagem = new MimeMessage();

        // Define o remetente com base no endereço configurado no sistema.
        mensagem.From.Add(MailboxAddress.Parse(_configuration["Email:From"]!));

        // Adiciona o endereço do usuário que receberá a mensagem.
        mensagem.To.Add(MailboxAddress.Parse(destinatario));

        // Define o título do e-mail.
        mensagem.Subject = assunto;

        // Define o conteúdo como HTML, permitindo o uso de links e formatação visual.
        mensagem.Body = new TextPart("html") { Text = corpo };

        // Inicializa o cliente SMTP do MailKit para realizar a conexão.
        using var smtp = new SmtpClient();

        // Estabelece conexão com o servidor de e-mail usando criptografia TLS (Segurança).
        await smtp.ConnectAsync(
            _configuration["Email:Host"]!,
            int.Parse(_configuration["Email:Port"]!),
            SecureSocketOptions.StartTls
        );

        // Realiza a autenticação com as credenciais do servidor de e-mail.
        await smtp.AuthenticateAsync(
            _configuration["Email:Username"]!,
            _configuration["Email:Password"]!
        );

        // Dispara o envio da mensagem de forma assíncrona.
        await smtp.SendAsync(mensagem);

        // Encerra a conexão com o servidor de e-mail de forma limpa.
        await smtp.DisconnectAsync(true);
    }

    public async Task EnviarEmailAsync02(string destinatario, string assunto, string corpo)
    {
        try
        {
            var mensagem = new MimeMessage();

            mensagem.From.Add(MailboxAddress.Parse(_configuration["Email:From"]));
            mensagem.To.Add(MailboxAddress.Parse(destinatario));
            mensagem.Subject = assunto;

            mensagem.Body = new TextPart("html")
            {
                Text = corpo
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _configuration["Email:Host"],
                int.Parse(_configuration["Email:Port"]),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]
            );

            await smtp.SendAsync(mensagem);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
            throw;
        }
    }
}