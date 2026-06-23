using Financas.Api.DTOs.Metas;
using Financas.Api.DTOs.MetasGasto;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar operações de metas financeiras.
    /// Suporta metas do tipo Despesa (baseadas em lançamentos) e Patrimônio (baseadas em saldo de conta).
    /// Todas as operações são autenticadas e isoladas por usuário.
    /// </summary>
    [ApiController]
    [Route("api/metas-gasto")]
    [Authorize]
    public class MetasGastoController : ControllerBase
    {
        private readonly MetaGastoService _metaGastoService;

        public MetasGastoController(MetaGastoService metaGastoService)
        {
            _metaGastoService = metaGastoService;
        }

        /// <summary>
        /// Cria uma nova meta financeira para o usuário autenticado.
        /// </summary>
        [HttpPost("criar-meta-gasto")]
        public async Task<ActionResult<MetaGastoResponseDTO>> CriarMetaGasto([FromBody] CriarMetaGastoDTO dto)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                var meta = await _metaGastoService.CriarMetaGasto(dto, usuarioId);

                return CreatedAtAction(nameof(GetMetaGastoPorId), new { id = meta.Id }, meta);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retorna todas as metas financeiras do usuário autenticado.
        /// </summary>
        [HttpGet("listar-metas-gasto")]
        public async Task<ActionResult<IEnumerable<MetaGastoListagemDTO>>> GetMetasGasto()
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                var metas = await _metaGastoService.GetMetasGasto(usuarioId);
                return Ok(metas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retorna o detalhe completo de uma meta específica do usuário autenticado.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MetaGastoResponseDTO>> GetMetaGastoPorId(int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                var meta = await _metaGastoService.GetMetaGastoPorId(id, usuarioId);
                return Ok(meta);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retorna um resumo das metas do usuário autenticado, ideal para dashboards e cards.
        /// </summary>
        [HttpGet("resumo-metas-gasto")]
        public async Task<ActionResult<IEnumerable<MetaGastoResumoDTO>>> GetResumoMetasGasto()
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                var resumo = await _metaGastoService.GetResumoMetasGasto(usuarioId);
                return Ok(resumo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Atualiza uma meta financeira existente do usuário autenticado.
        /// </summary>
        [HttpPatch("atualizar-meta-gasto/{id:int}")]
        public async Task<ActionResult<MetaGastoResponseDTO>> AtualizarMetaGasto([FromBody] AtualizarMetaGastoDTO dto, int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
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
        /// Remove uma meta financeira do usuário autenticado.
        /// </summary>
        [HttpDelete("deletar-meta-gasto/{id:int}")]
        public async Task<ActionResult> DeletarMetaGasto(int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
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

        // ── Extração centralizada do ID do usuário ────────────────────────────────

        /// <summary>
        /// Extrai e converte o identificador do usuário autenticado a partir das claims do token JWT.
        /// Centralizado para evitar repetição e facilitar manutenção futura.
        /// </summary>
        private int ObterUsuarioId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}