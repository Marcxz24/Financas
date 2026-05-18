using Financas.Api.DTOs.Usuario;
using Financas.Api.Entities;
using Financas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Transactions;

namespace Financas.Api.Controllers
{
    // O controlador de usuários é responsável por lidar com as requisições relacionadas aos usuários, como criação e recuperação de usuários. Ele utiliza o serviço de usuário para realizar as operações necessárias e retornar as respostas adequadas para o cliente.
    [ApiController]
    [Route("api/usuarios")]
    public class UsuarioController : ControllerBase
    {
        // O controlador de usuários depende do serviço de usuário para realizar as operações relacionadas aos usuários. A injeção de dependência é utilizada para fornecer uma instância do serviço de usuário ao controlador, permitindo que ele interaja com a lógica de negócios e o banco de dados para criar e recuperar usuários.
        private readonly UsuarioService _usuarioService;

        // O controlador de usuários depende do serviço de autenticação para lidar com as operações relacionadas à autenticação dos usuários, como login e geração de tokens JWT. A injeção de dependência é utilizada para fornecer uma instância do serviço de autenticação ao controlador, permitindo que ele interaja com a lógica de negócios relacionada à autenticação e acesse as funcionalidades necessárias para autenticar os usuários e gerar tokens JWT.
        private readonly AuthService _authService;

        // O construtor do controlador recebe o serviço de usuário via injeção de dependência, permitindo que o controlador interaja com a lógica de negócios e o banco de dados para criar e recuperar usuários.
        public UsuarioController(UsuarioService userService, AuthService authService)
        {
            _usuarioService = userService;
            _authService = authService;
        }

        // O método GetUsuarios é responsável por lidar com as requisições GET para recuperar a lista de usuários. Ele chama o serviço de usuário para obter os usuários e retorna a resposta adequada para o cliente.
        [HttpGet("listar-usuarios")]
        [Authorize] // O atributo [Authorize] é utilizado para proteger o endpoint de recuperação de usuários, garantindo que apenas usuários autenticados possam acessar essa funcionalidade. Ele verifica se o usuário está autenticado antes de permitir o acesso ao método GetUsuarios, garantindo a segurança dos dados dos usuários.
        public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetUsuarios()
        {
            // O método GetUsuarios é responsável por lidar com as requisições GET para recuperar a lista de usuários. Ele chama o serviço de usuário para obter os usuários e retorna a resposta adequada para o cliente.
            var usuarios = await _usuarioService.GetUsuario();
            return Ok(usuarios);
        }

        // O método CriarUsuario é responsável por lidar com as requisições POST para criar um novo usuário. Ele recebe os dados do usuário no corpo da requisição, chama o serviço de usuário para criar o usuário e retorna a resposta adequada para o cliente.
        [HttpPost("criar-usuario")]
        public async Task<ActionResult<UsuarioResponseDTO>> CriarUsuario([FromBody] CriarUsuarioDTO dto)
        {
            // O método CriarUsuario é responsável por lidar com as requisições POST para criar um novo usuário. Ele recebe os dados do usuário no corpo da requisição, chama o serviço de usuário para criar o usuário e retorna a resposta adequada para o cliente.
            try
            {
                // O método CriarUsuario é responsável por lidar com as requisições POST para criar um novo usuário. Ele recebe os dados do usuário no corpo da requisição, chama o serviço de usuário para criar o usuário e retorna a resposta adequada para o cliente.
                var usuario = await _usuarioService.CriarUsuario(dto);
                return Ok(usuario);
            }
            // O método CriarUsuario é responsável por lidar com as requisições POST para criar um novo usuário. Ele recebe os dados do usuário no corpo da requisição, chama o serviço de usuário para criar o usuário e retorna a resposta adequada para o cliente. Se ocorrer algum erro durante a criação do usuário, ele captura a exceção e retorna uma resposta de erro apropriada para o cliente.
            catch (Exception ex)
            {
                // O método CriarUsuario é responsável por lidar com as requisições POST para criar um novo usuário. Ele recebe os dados do usuário no corpo da requisição, chama o serviço de usuário para criar o usuário e retorna a resposta adequada para o cliente. Se ocorrer algum erro durante a criação do usuário, ele captura a exceção e retorna uma resposta de erro apropriada para o cliente.
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDTO dto)
        {
            try
            {
                // Chama o AuthService para validar as credenciais e gerar o token JWT
                var token = await _authService.Login(dto);

                return Ok(token);
            }
            catch (UnauthorizedAccessException ex)
            {
                // E-mail não confirmado
                return Unauthorized(ex.Message);
            }
            catch
            {
                // Credenciais inválidas
                return Unauthorized("Usuário ou senha inválidos");
            }
        }

        [HttpGet("confirmar-email")] // Define a rota específica para a confirmação de e-mail.
        public async Task<ActionResult> ConfirmarEmail([FromQuery] string token)
        {
            try
            {
                // 1. Processamento: Envia o token extraído da URL para a lógica de negócio no Service.
                await _usuarioService.ConfirmarEmail(token);

                // 2. Sucesso: Retorna status 200 (OK) confirmando que a conta foi ativada.
                return Ok("Email confirmado com sucesso!");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 3. Erro de autorização: Retorna status 401 (Unauthorized) se o token for inválido ou expirado.
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // 4. Outros erros: Retorna status 400 (Bad Request) para quaisquer outros erros que possam ocorrer.
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("alterar-senha")] // Define o verbo HTTP PATCH para atualização parcial de dados do usuário.
        [Authorize] // Bloqueia o acesso de usuários não autenticados, exigindo um Token JWT válido.
        public async Task<ActionResult> AlterarSenha([FromBody] AtualizarSenhaDTO dto)
        {
            try
            {
                // 1. Identificação Segura: Extrai o ID do usuário diretamente das Claims do Token.
                // Isso impede que um usuário tente alterar a senha de outra conta.
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 2. Execução: Envia os dados de senha e o ID validado para a camada de serviço.
                await _usuarioService.AtualizarSenha(dto, userId);

                // 3. Sucesso: Retorna status 200 (OK) com uma mensagem de confirmação.
                return Ok("Senha atualizada com sucesso!");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 4. Erro de Autenticação (401): Capturado caso haja falha específica de permissão.
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // 5. Erro de Negócio (400): Capturado caso a senha atual esteja incorreta ou o DTO seja inválido.
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("alterar-email")] // Utiliza o verbo PATCH pois representa uma modificação parcial no recurso do usuário.
        [Authorize] // Exige que a requisição contenha um Token JWT válido, bloqueando acessos anônimos.
        public async Task<ActionResult> AlterarEmail([FromBody] AtualizarEmailDTO dto)
        {
            try
            {
                // 1. Identificação Blindada: Extrai o ID do usuário diretamente das Claims do Token.
                // Isso garante que a requisição atuará apenas sobre a conta de quem está logado.
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // 2. Orquestração: Envia o DTO (com a senha atual e o novo e-mail) e o ID para o Service processar.
                await _usuarioService.AtualizarEmail(dto, userId);

                // 3. Resposta Positiva: Retorna o status 200 (OK) sinalizando que o processo foi iniciado sem erros.
                return Ok("E-mail atualizado com sucesso!");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 4. Falha de Autenticação (401): Disparado caso ocorra algum bloqueio de segurança.
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // 5. Falha de Regra de Negócio (400): Disparado se a senha estiver errada ou o e-mail já existir.
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("esqueci-senha")] // Define o verbo POST para iniciar o processo de recuperação (criação de recurso temporário de token).
        public async Task<ActionResult> SolicitarRedefinicaoSenha([FromBody] EsqueciSenhaDTO dto)
        {
            try
            {
                // 1. Início do Fluxo: Delega ao serviço a validação do e-mail e geração do token de segurança.
                await _usuarioService.SolicitarRedefinicaoSenha(dto);

                // 2. Resposta Positiva: Retorna status 200 (OK), confirmando que a instrução de recuperação foi enviada.
                return Ok("E-mail de redefinição enviado com sucesso!");
            }
            catch (Exception ex)
            {
                // 3. Falha de Processamento: Retorna status 400 (Bad Request) caso o e-mail não seja encontrado ou ocorra erro no SMTP.
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("redefinir-senha")] // Define o verbo POST para processar a alteração final da senha no servidor.
        public async Task<ActionResult> RedefinirSenha([FromBody] RedefinirSenhaDTO dto)
        {
            try
            {
                // 1. Consumação do Token: Envia o DTO com o token e a nova senha para validação e persistência no banco.
                await _usuarioService.RedefinirSenha(dto);

                // 2. Resposta Positiva: Retorna status 200 (OK) após a senha ser criptografada e o token invalidado com sucesso.
                return Ok("Senha redefinida com sucesso!");
            }
            catch (Exception ex)
            {
                // 3. Falha de Validação: Retorna status 400 (Bad Request) se o token estiver expirado ou for inválido.
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Endpoint para autenticação de usuários via provedor de identidade externo (Google OAuth 2.0).
        /// </summary>
        /// <param name="dto">Objeto contendo o Identity Token gerado pelo Google.</param>
        /// <returns>Retorna o token JWT de acesso nativo do sistema em caso de sucesso.</returns>
        /// <response code="200">Retorna o token gerado com sucesso.</response>
        /// <response code="401">Caso o token do Google seja inválido, malformado ou expirado.</response>
        [HttpPost("google")]
        public async Task<IActionResult> LoginComGoogle([FromBody] GoogleLoginDTO dto)
        {
            try
            {
                // Valida se o token foi enviado na requisição
                if (string.IsNullOrEmpty(dto.Token))
                {
                    return BadRequest("O token do Google é obrigatório.");
                }

                // Chama o método de serviço estruturado anteriormente
                // (Substitua '_usuarioService' pela instância correta do seu serviço de autenticação)
                string tokenSistema = await _authService.LoginComGoogle(dto.Token);

                // Retorna o JWT nativo com status HTTP 200 OK
                return Ok(tokenSistema);
            }
            catch (Exception ex)
            {
                // Retorna o erro de autenticação com status HTTP 401 Unauthorized de forma limpa
                return Unauthorized(new { mensagem = ex.Message });
            }
        }
    }
}
