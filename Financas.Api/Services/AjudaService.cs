using Financas.Api.Data;
using Financas.Api.DTOs.Ajuda;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    public class AjudaService
    {
        // Dependências injetadas: contexto do banco de dados e serviço de envio de e-mails
        private readonly FinancasDbContext _financasDbContext;
        private readonly EmailService _emailService;

        // Construtor para inicialização das dependências via Injeção de Dependência
        public AjudaService(FinancasDbContext financasDbContext, EmailService emailService)
        {
            _financasDbContext = financasDbContext;
            _emailService = emailService;
        }

        // Método responsável por processar o envio de um chamado de suporte
        public async Task EnviarChamado(EnviarChamadoDTO dto, int usuarioId)
        {
            // Busca o usuário no banco de dados pelo ID fornecido para obter informações de contato
            var usuario = await _financasDbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            // Validação: garante que o usuário existe no banco antes de prosseguir
            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // Define o assunto do e-mail combinando um prefixo padrão com o assunto enviado no DTO
            var assuntoEmail = $"Novo chamado de ajuda: {dto.Assunto}";

            // Constrói o corpo do e-mail em formato HTML, utilizando interpolação de strings
            // Inclui dados do remetente (usuário) e detalhes do problema relatado
            var corpoEmail = $@"
            <h2>Novo Chamado de Suporte</h2>
            <hr>
            <p>
                <strong>Usuário:</strong>
                {usuario.Username}
            </p>
            <p>
                <strong>E-mail:</strong>
                {usuario.Email}
            </p>
            <p>
                <strong>Data:</strong>
                {DateTime.Now:dd/MM/yyyy HH:mm}
            </p>
            <hr>
            <p>
                <strong>Assunto:</strong>
                {dto.Assunto}
            </p>
            <p>
                <strong>Descrição:</strong>
            </p>
            <div style='padding:10px;border:1px solid #ddd;border-radius:5px'>
                {dto.Descricao.Replace(Environment.NewLine, "<br>")}
            </div>
        ";

            // Define o destinatário fixo que receberá o chamado (suporte técnico)
            var emailSuporte = "marco.antonio.dev24@gmail.com";

            // Invoca o serviço de e-mail para realizar o envio assíncrono para o endereço de suporte
            await _emailService.EnviarEmailAsync(emailSuporte, assuntoEmail, corpoEmail);
        }
    }
}