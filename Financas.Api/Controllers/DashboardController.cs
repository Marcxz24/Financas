using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    // Define que todos os endpoints desta controller exigem autenticação via Token
    [Authorize]
    // Habilita comportamentos específicos de API, como a validação automática de ModelState
    [ApiController]
    // Define a rota base da API como 'api/dashboard' (baseado no nome da classe)
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        // Injeção de dependência do serviço de Dashboard
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // Garante que o endpoint exige autorização (redundante devido ao atributo na classe, mas reforça a segurança)
        [Authorize]
        // Define que este é um método GET acessível via 'api/dashboard/resumo-mensal'
        [HttpGet("resumo-mensal")]
        public async Task<IActionResult> GetResumoMensal([FromQuery] int? mes, [FromQuery] int? ano, [FromQuery] int? contaBancariaId)
        {
            // Extrai o ID do usuário autenticado a partir das Claims do Token JWT
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Valida se o ID do usuário foi encontrado nas Claims
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { mensagem = "Usuário não autenticado." });

            // Converte o ID do usuário para inteiro
            int usuarioId = int.Parse(userIdClaim);

            // Define os filtros: se mês ou ano não forem informados na query, utiliza a data atual do servidor
            int mesFiltro = mes ?? DateTime.Now.Month;
            int anoFiltro = ano ?? DateTime.Now.Year;

            // Chama o serviço para buscar os dados financeiros consolidados do período e usuário
            var resumo = await _dashboardService.GetResumoMensalAsync(mesFiltro, anoFiltro, usuarioId, contaBancariaId);

            // Retorna o resumo financeiro com status 200 OK
            return Ok(resumo);
        }
    }
}