using Financas.Api.DTOs.Cofrinho;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento dos cofrinhos do usuário.
    /// Disponibiliza operações de cadastro, consulta, atualização, exclusão
    /// e movimentações financeiras entre contas bancárias e cofrinhos.
    /// </summary>
    [ApiController]
    [Route("api/cofrinhos")]
    public class CofrinhoController : ControllerBase
    {
        /// <summary>
        /// Serviço responsável por concentrar todas as regras de negócio relacionadas
        /// ao gerenciamento dos cofrinhos.
        /// </summary>
        private readonly CofrinhoService _cofrinhoService;

        /// <summary>
        /// Injeta a dependência do serviço responsável pelas operações dos cofrinhos.
        /// </summary>
        public CofrinhoController(CofrinhoService cofrinhoService)
        {
            _cofrinhoService = cofrinhoService;
        }

        /// <summary>
        /// Realiza o cadastro de um novo cofrinho para o usuário autenticado.
        /// </summary>
        [HttpPost("criar")]
        [Authorize]
        public async Task<ActionResult<CofrinhoResponseDTO>> CriarCofrinho([FromBody] CriarCofrinhoDTO dto)
        {
            try
            {
                // Obtém o identificador do usuário autenticado presente no JWT.
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Encaminha a criação para a camada de serviço.
                var resultado = await _cofrinhoService.CriarCofrinho(dto, usuarioId);

                // Retorna HTTP 201 juntamente com a localização do recurso criado.
                return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
            }
            catch (Exception ex)
            {
                // Retorna erro de validação ou regra de negócio.
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza os dados cadastrais de um cofrinho pertencente ao usuário autenticado.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<CofrinhoResponseDTO>> AtualizarCofrinho([FromBody] AtualizarCofrinhoDTO dto, int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var resultado = await _cofrinhoService.AtualizarCofrinho(dto, id, usuarioId);

                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                // O cofrinho informado não foi localizado.
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // O recurso existe, porém não pertence ao usuário autenticado.
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Remove um cofrinho pertencente ao usuário autenticado.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> ExcluirCofrinho(int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _cofrinhoService.ExcluirCofrinho(id, usuarioId);

                // Exclusão realizada com sucesso.
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Retorna todos os cofrinhos cadastrados pelo usuário autenticado.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CofrinhoResponseDTO>>> ObterCofrinhos()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                return Ok(await _cofrinhoService.ObterCofrinhosUsuario(usuarioId));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Obtém os dados de um cofrinho específico pertencente ao usuário autenticado.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CofrinhoResponseDTO>> ObterPorId(int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                return Ok(await _cofrinhoService.ObterPorId(id, usuarioId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Retorna apenas o saldo atual do cofrinho informado.
        /// </summary>
        [HttpGet("saldo/{id}")]
        [Authorize]
        public async Task<ActionResult<SaldoCofrinhoDTO>> ObterSaldo(int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                return Ok(await _cofrinhoService.ObterSaldo(id, usuarioId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Transfere recursos de uma conta bancária para um cofrinho.
        /// A movimentação altera simultaneamente o saldo da conta e do cofrinho.
        /// </summary>
        [HttpPost("transferir-para-cofrinho")]
        [Authorize]
        public async Task<ActionResult<CofrinhoResponseDTO>> TransferirContaParaCofrinho([FromBody] TransferenciaCofrinhoDTO dto)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                return Ok(await _cofrinhoService.TransferirContaParaCofrinho(dto, usuarioId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Realiza o resgate de valores do cofrinho para uma conta bancária.
        /// A operação reduz o saldo do cofrinho e credita o valor na conta informada.
        /// </summary>
        [HttpPost("resgatar")]
        [Authorize]
        public async Task<ActionResult<CofrinhoResponseDTO>> TransferirCofrinhoParaConta([FromBody] TransferenciaCofrinhoDTO dto)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                return Ok(await _cofrinhoService.TransferirCofrinhoParaConta(dto, usuarioId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
