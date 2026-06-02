using Financas.Api.DTOs.Ajuda;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    [ApiController] // Define que esta classe é um controlador de API, habilitando comportamentos como validação automática de modelos
    [Route("api/ajuda")] // Define o prefixo da rota para todas as ações deste controlador
    [Authorize] // Restringe o acesso aos métodos desta classe apenas a usuários autenticados
    public class AjudaController : ControllerBase
    {
        // Dependência do serviço que contém a lógica de negócio para manipulação de chamados
        private readonly AjudaService _ajudaService;

        private readonly EmailService _emailService;

        // Construtor que realiza a Injeção de Dependência do AjudaService
        public AjudaController(AjudaService ajudaService, EmailService emailService)
        {
            _ajudaService = ajudaService;
            _emailService = emailService;
        }

        // Endpoint para receber e processar a abertura de um novo chamado de suporte
        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarChamado([FromBody] EnviarChamadoDTO dto)
        {
            try
            {
                // Extrai o ID do usuário autenticado a partir dos claims do token JWT
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Verifica se o ID está presente; caso contrário, retorna erro de não autorizado
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { mensagem = "Usuário não autenticado." });

                // Converte o ID do usuário de string para inteiro
                int usuarioId = int.Parse(userIdClaim);

                // Invoca a camada de serviço para processar o envio do chamado passando os dados e o ID do autor
                await _ajudaService.EnviarChamado(dto, usuarioId);

                // Retorna status 200 (OK) com mensagem de sucesso
                return Ok(new { mensagem = "Chamado enviado com sucesso." });
            }
            catch (Exception ex)
            {
                // Captura qualquer erro inesperado e retorna status 400 (BadRequest) com a mensagem da exceção
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("teste-email")]
        public async Task<IActionResult> TesteEmail()
        {
            await _emailService.EnviarEmailAsync02(
                "SEU_EMAIL_PARA_TESTE@gmail.com",
                "Teste SMTP Gmail",
                "<h1>Funcionou 🎉</h1><p>Email enviado com sucesso.</p>"
            );

            return Ok("E-mail enviado");
        }
    }
}