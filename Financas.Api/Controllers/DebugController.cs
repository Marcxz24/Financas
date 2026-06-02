using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;

namespace Financas.Api.Controllers
{
    [ApiController] // Define que esta classe é um controlador de API, habilitando comportamentos de validação automática
    [Route("api/debug")] // Define a rota base para os endpoints de diagnóstico
    public class DebugController : ControllerBase
    {
        // Endpoint para verificar se o ambiente de hospedagem possui conectividade externa com o servidor SMTP do Gmail
        [HttpGet("smtp-test")]
        public async Task<IActionResult> TesteSmtp()
        {
            try
            {
                // Inicializa um cliente TCP para realizar uma conexão de baixo nível (socket)
                using var client = new TcpClient();

                // Tenta estabelecer conexão com o servidor SMTP do Google na porta 587 (porta padrão para TLS/STARTTLS)
                await client.ConnectAsync("smtp.gmail.com", 587);

                // Se o fluxo chegar aqui, a rede está permitindo a saída para o servidor de e-mail
                return Ok("CONEXÃO SMTP OK (Render consegue acessar Gmail)");
            }
            catch (Exception ex)
            {
                // Se houver falha (timeout, firewall ou bloqueio), retorna erro detalhando a causa (ex. "Connection refused")
                return BadRequest(new { message = "FALHA NA CONEXÃO SMTP", error = ex.Message });
            }
        }
    }
}
