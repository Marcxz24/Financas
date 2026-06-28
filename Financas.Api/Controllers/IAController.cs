using Financas.Api.DTOs.IA;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Financas.Api.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints do módulo de Inteligência Artificial.
    /// Segue o princípio de responsabilidade única (SRP): apenas roteia as requisições,
    /// extrai o ID do usuário do token JWT e delega ao IAService.
    /// Nenhuma lógica de negócio ou comunicação com a OpenRouter ocorre aqui.
    /// </summary>
    [ApiController]
    [Route("api/ia")]
    [Authorize]
    public class IAController : ControllerBase
    {
        private readonly IAService _iaService;

        /// <summary>
        /// Construtor com injeção de dependência do IAService.
        /// </summary>
        /// <param name="iaService">Serviço orquestrador do módulo de IA.</param>
        public IAController(IAService iaService)
        {
            _iaService = iaService;
        }

        /// <summary>
        /// Endpoint principal para envio de perguntas ao assistente financeiro com IA.
        /// Recebe a pergunta do usuário, monta o contexto financeiro completo internamente
        /// e retorna a resposta gerada pelo modelo de linguagem da OpenRouter.
        /// A comunicação com a IA ocorre exclusivamente pelo back-end — o Front-end
        /// jamais acessa a OpenRouter diretamente.
        /// </summary>
        /// <param name="dto">DTO contendo apenas a pergunta do usuário.</param>
        /// <returns>
        /// 200 OK com a resposta da IA encapsulada no RespostaIADTO.
        /// 400 Bad Request para perguntas inválidas ou erros de processamento.
        /// 401 Unauthorized se o token JWT estiver ausente ou inválido.
        /// 404 Not Found se o usuário autenticado não existir no banco.
        /// </returns>
        [HttpPost("perguntar")]
        public async Task<ActionResult<RespostaIADTO>> Perguntar([FromBody] PerguntaIADTO dto)
        {
            try
            {
                // Extrai o ID do usuário autenticado a partir das Claims do Token JWT
                var usuarioId = ObterUsuarioId();

                // Delega ao IAService toda a lógica de contexto + IA + resposta
                var resposta = await _iaService.ProcessarPerguntaAsync(dto, usuarioId);

                return Ok(resposta);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                // Erro de comunicação com a OpenRouter
                return BadRequest(new
                {
                    mensagem = "Não foi possível conectar ao serviço de IA no momento. " +
                               "Tente novamente em instantes.",
                    detalhe = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                // Configuração ausente ou resposta inválida da IA
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                // Erro inesperado — log feito no IAService via ILogger
                return BadRequest(new
                {
                    mensagem = "Ocorreu um erro inesperado ao processar sua pergunta. " +
                               "Tente novamente em instantes.",
                    detalhe = ex.Message
                });
            }
        }

        // ── Extração centralizada do ID do usuário ────────────────────────────

        /// <summary>
        /// Extrai e converte o identificador do usuário autenticado a partir das claims do token JWT.
        /// Centralizado para evitar repetição e facilitar manutenção futura.
        /// Padrão idêntico ao utilizado no MetasGastoController.
        /// </summary>
        private int ObterUsuarioId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
