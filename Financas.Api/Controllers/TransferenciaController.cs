using Financas.Api.DTOs.Transferencia;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    [ApiController]
    [Route("api/transferencias")]
    public class TransferenciaController : ControllerBase
    {
        private readonly TransferenciaService _transferenciaService;

        /// <summary>
        /// Construtor que recebe o serviço via Injeção de Dependência.
        /// </summary>
        public TransferenciaController(TransferenciaService transferenciaService)
        {
            _transferenciaService = transferenciaService;
        }

        /// <summary>
        /// Endpoint para criação de uma transferência entre contas bancárias.
        /// Retorna 201 (Created) em caso de sucesso.
        /// </summary>
        [HttpPost("criar-transferencia")]
        [Authorize] // Garante que apenas usuários logados acessem
        public async Task<ActionResult<TransferenciaResponseDTO>> CriarTransferencia([FromBody] TransferenciaRequestDTO dto)
        {
            try
            {
                // Extrai o ID do usuário diretamente das Claims do Token JWT
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var transferencia = await _transferenciaService.CriarTransferencia(dto, usuarioId);

                return StatusCode(StatusCodes.Status201Created, transferencia);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); // 404 se alguma conta não existir
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 se alguma conta não pertencer ao usuário
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lista todas as transferências cadastradas pelo usuário autenticado.
        /// </summary>
        /// <returns>Lista de transferências.</returns>
        [HttpGet("listar-transferencias")]
        [Authorize]
        public async Task<ActionResult<List<TransferenciaResponseDTO>>> ListarTransferencias()
        {
            try
            {
                // Extrai o ID do usuário diretamente das Claims do Token JWT
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var transferencias = await _transferenciaService.ListarTransferencias(usuarioId);

                return Ok(transferencias);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Edita uma transferência existente, estornando os saldos antigos e aplicando os novos valores.
        /// </summary>
        [HttpPut("editar-transferencia/{id}")]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDTO>> EditarTransferencia([FromBody] TransferenciaRequestDTO dto, int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var transferenciaAtualizada = await _transferenciaService.EditarTransferencia(dto, id, usuarioId);

                return Ok(transferenciaAtualizada);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); // 404 se a transferência ou alguma conta não existir
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 se a transferência ou alguma conta não pertencer ao usuário
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Remove uma transferência, estornando os valores para as contas de origem e destino.
        /// Retorna 204 (No Content) em caso de sucesso.
        /// </summary>
        [HttpDelete("excluir-transferencia/{id}")]
        [Authorize]
        public async Task<ActionResult> ExcluirTransferencia(int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _transferenciaService.ExcluirTransferencia(id, usuarioId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}