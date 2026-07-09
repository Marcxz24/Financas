using Financas.Api.DTOs.CartaoCredito;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciar as operações relacionadas aos cartões de crédito.
    /// </summary>  
    [ApiController]
    [Route("api/cartoes-credito")]
    public class CartaoCreditoController : ControllerBase
    {
        /// <summary>
        /// Serviço para gerenciar as operações relacionadas aos cartões de crédito.
        /// </summary>
        private readonly CartaoCreditoService _cartaoCreditoService;

        /// <summary>
        /// Construtor para injetar o serviço de cartões de crédito.
        /// </summary>
        public CartaoCreditoController(CartaoCreditoService cartaoCreditoService)
        {
            _cartaoCreditoService = cartaoCreditoService;
        }

        /// <summary>
        /// Endpoint para criar um novo cartão de crédito.
        /// </summary>
        [HttpPost("criar-cartao-credito")]
        [Authorize]
        public async Task<ActionResult<CartaoCreditoResponseDTO>> CriarCartaoCredito([FromBody] CriarCartaoCreditoDTO dto)
        {
            // 1. Extração de Identidade: Recupera o 'Subject' (ID) do usuário autenticado.
            // O método FindFirstValue busca nas Claims do Token o identificador único do usuário logado.
            try
            {
                // 2. Delegação: Transfere os dados recebidos no corpo da requisição (JSON) 
                // para o serviço, injetando o contexto do usuário para garantir o vínculo correto.
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 3. Processamento: Chama o serviço para criar o cartão de crédito.
                var cartaoCredito = await _cartaoCreditoService.CriarCartaoCredito(dto, usuarioId);

                // 4. Resposta REST Padrão (201 Created):
                // 'CreatedAtAction' gera uma resposta com status 201 e inclui no cabeçalho 'Location' 
                // a URL onde o novo recurso pode ser acessado, além de retornar o objeto criado no corpo.
                return CreatedAtAction(nameof(GetCartaoCredito), new { id = cartaoCredito.Id }, cartaoCredito);
            }
            catch (Exception ex)
            {
                // 5. Fallback de Erro: Captura falhas de negócio ou infraestrutura e as traduz 
                // em um status 400 (Bad Request), expondo a mensagem de exceção no corpo da resposta.
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Endpoint para listar os cartões de crédito do usuário.
        /// </summary>
        [HttpGet("listar-cartoes-credito")]
        [Authorize]
        public async Task<ActionResult<List<CartaoCreditoResponseDTO>>> GetCartaoCredito()
        {
            // 1. Extração de Identidade: Recupera o 'Subject' (ID) do usuário autenticado.
            try
            {
                // 2. Captura de Contexto: Extrai o ID do usuário do Token JWT assinado.
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 3. Processamento: Solicita ao serviço a lista de cartões de crédito.
                var cartaoCredito = await _cartaoCreditoService.GetCartaoCredito(usuarioId);

                // 4. Resposta de Sucesso: Retorna o status 200 (OK) com a lista serializada em JSON.
                return Ok(cartaoCredito);
            }
            catch (KeyNotFoundException ex)
            {
                // 5. Erro de Identificação (404): Capturado se o ID fornecido não existir no banco.
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                // 6. Erro de Permissão (403): Capturado se o cartão existir, mas pertencer a outro usuário.
                return Forbid();
            }
            catch (Exception ex)
            {
                // 5. Fallback de Erro: Captura falhas de negócio ou infraestrutura e as traduz 
                // em um status 400 (Bad Request), expondo a mensagem de exceção no corpo da resposta.
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Endpoint para atualizar um cartão de crédito específico.
        /// </summary>
        [HttpPatch("atualizar-cartao-credito/{id}")]
        [Authorize]
        public async Task<ActionResult<CartaoCreditoResponseDTO>> AtualizarCartaoCredito(int id, [FromBody] AtualizarCartaoCreditoDTO dto)
        {
            // 1. Extração de Identidade: Recupera o 'Subject' (ID) do usuário autenticado.
            try
            {
                // 2. Captura de Contexto: Extrai o ID do usuário do Token JWT assinado.
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 3. Processamento: Solicita ao serviço para atualizar o cartão de crédito.
                var cartaoCreditoAtualizado = await _cartaoCreditoService.AtualizarCartaoCredito(dto, id, usuarioId);

                // 4. Resposta de Sucesso: Retorna o status 200 (OK) com o objeto já modificado.
                return Ok(cartaoCreditoAtualizado);
            }
            catch (KeyNotFoundException ex)
            {
                // 5. Erro de Identificação (404): Capturado se o ID fornecido não existir no banco.
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                // 6. Erro de Permissão (403): Capturado se o cartão existir, mas pertencer a outro usuário.
                return Forbid();
            }
            catch (Exception ex)
            {
                // 7. Erro Genérico (400): Captura outras falhas (ex: validações de banco ou servidor).
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Endpoint para deletar um cartão de crédito específico.
        /// </summary>
        [HttpDelete("deletar-cartao-credito/{id}")]
        [Authorize]
        public async Task<ActionResult> DeletarCartaoCredito(int id)
        {
            // 1. Extração de Identidade: Recupera o 'Subject' (ID) do usuário autenticado.
            try
            {
                // 2. Captura de Contexto: Extrai o ID do usuário do Token JWT assinado.
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 3. Processamento: Solicita ao serviço para deletar o cartão de crédito.
                await _cartaoCreditoService.DeletarCartao(id, usuarioId);

                // 4. Resposta de Sucesso: Retorna o status 204 (No Content).
                return NoContent();
            }
            // 5. Erro de Identificação (404): Capturado se o ID fornecido não existir no banco.
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            // 6. Erro de Permissão (403): Capturado se o cartão existir, mas pertencer a outro usuário.
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}