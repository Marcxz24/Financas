using Financas.Api.DTOs.Metas;
using Financas.Api.DTOs.MetasGasto;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar operações de metas de gasto.
    /// Expõe endpoints para criação, listagem, resumo, atualização e remoção de metas.
    /// Todas as operações são autenticadas e vinculadas ao usuário logado.
    /// </summary>
    [ApiController]
    [Route("api/metas-gasto")]
    public class MetasGastoController : ControllerBase
    {
        private readonly MetaGastoService _metaGastoService;

        public MetasGastoController(MetaGastoService metaGastoService)
        {
            _metaGastoService = metaGastoService;
        }

        /// <summary>
        /// Cria uma nova meta de gasto para o usuário autenticado.
        /// </summary>
        [HttpPost("criar-meta-gasto")]
        [Authorize]
        public async Task<ActionResult<MetaGastoResponseDTO>> CriarMetaGasto([FromBody] CriarMetaGastoDTO dto)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var meta = await _metaGastoService.CriarMetaGasto(dto, usuarioId);

                return CreatedAtAction(nameof(GetMetasGasto), new { id = meta.Id }, meta);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retorna todas as metas de gasto do usuário autenticado.
        /// </summary>
        [HttpGet("listar-metas-gasto")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MetaGastoListagemDTO>>> GetMetasGasto()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var metas = await _metaGastoService.GetMetasGasto(usuarioId);

                return Ok(metas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retorna um resumo das metas de gasto do usuário autenticado, ideal para dashboards e cards.
        /// </summary>
        [HttpGet("resumo-metas-gasto")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MetaGastoResumoDTO>>> GetResumoMetasGasto()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var resumo = await _metaGastoService.GetResumoMetasGasto(usuarioId);

                return Ok(resumo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Atualiza uma meta de gasto existente do usuário autenticado.
        /// </summary>
        [HttpPatch("atualizar-meta-gasto/{id}")]
        [Authorize]
        public async Task<ActionResult<MetaGastoResponseDTO>> AtualizarMetaGasto([FromBody] AtualizarMetaGastoDTO dto, int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var metaAtualizada = await _metaGastoService.AtualizarMetaGasto(dto, id, usuarioId);

                return Ok(metaAtualizada);
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

        /// <summary>
        /// Remove uma meta de gasto do usuário autenticado.
        /// </summary>
        [HttpDelete("deletar-meta-gasto/{id}")]
        [Authorize]
        public async Task<ActionResult> DeletarMetaGasto(int id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _metaGastoService.DeletarMetaGasto(id, usuarioId);

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