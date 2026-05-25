using Financas.Api.Data;
using Financas.Api.DTOs.Usuario;
using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Financas.Api.Services
{
    public class AuthService
    {
        // O serviço de autenticação é responsável por lidar com a lógica de negócios relacionada à autenticação dos usuários, como login e geração de tokens JWT.
        private readonly FinancasDbContext _FinancasDbContext;

        // O construtor do serviço recebe o contexto do banco de dados e a configuração da aplicação via injeção de dependência, permitindo que o serviço interaja com a base de dados para realizar operações relacionadas à autenticação e acesse as configurações necessárias para gerar tokens JWT.
        private readonly IConfiguration _configuration;

        // O construtor do serviço recebe o contexto do banco de dados e a configuração da aplicação via injeção de dependência, permitindo que o serviço interaja com a base de dados para realizar operações relacionadas à autenticação e acesse as configurações necessárias para gerar tokens JWT.
        public AuthService(FinancasDbContext dbContext, IConfiguration configuration)
        {
            _FinancasDbContext = dbContext;
            _configuration = configuration;
        }

        // O método Login é responsável por autenticar um usuário com base nas credenciais fornecidas (email e senha) e, se a autenticação for bem-sucedida, gerar e retornar um token JWT que pode ser usado para acessar recursos protegidos na aplicação.
        public async Task<string> Login(LoginDTO dto)
        {

            var usuario = await _FinancasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            var senhaValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.Password);

            if (!senhaValida)
                throw new Exception("Senha inválida");

            if (!usuario.EmailConfirmado)
                throw new UnauthorizedAccessException("Email não confirmado");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Username)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Realiza a autenticação ou o provisionamento automático de um usuário utilizando um Identity Token do Google (OAuth 2.0).
        /// </summary>
        /// <param name="googleToken">O token JWT enviado pelo front-end, gerado após a autenticação bem-sucedida no Google Identity Services.</param>
        /// <returns>Uma string representando o token JWT nativo da aplicação para autorização de requisições subsequentes.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Lançada caso o token do Google seja inválido, expirado ou incompatível com a aplicação.
        /// </exception>
        public async Task<string> LoginComGoogle(string googleToken)
        {
            try
            {
                // Validação defensiva: Verifica se o token recebido do frontend não é nulo ou vazio
                if (string.IsNullOrWhiteSpace(googleToken))
                    throw new UnauthorizedAccessException("Token Google não informado.");

                // Instancia o HttpClient para consultar o endpoint oficial do Google
                using var httpClient = new HttpClient();

                // Realiza a validação oficial do token junto ao Google: O Google responde se o token é legítimo
                var response = await httpClient.GetAsync(
                    $"https://oauth2.googleapis.com/tokeninfo?id_token={googleToken}"
                );

                // Token inválido ou expirado: Se a resposta não for 200 OK, encerra o processo
                if (!response.IsSuccessStatusCode)
                    throw new UnauthorizedAccessException("Token Google inválido ou expirado.");

                // Lê o payload (os dados do usuário) retornado pelo Google em formato JSON
                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);

                var root = document.RootElement;

                // Extrai os campos identificadores do usuário enviados pelo Google
                string? audience = root.GetProperty("aud").GetString();
                string? issuer = root.GetProperty("iss").GetString();
                string? email = root.GetProperty("email").GetString();
                string? nome = root.GetProperty("name").GetString();

                // Validação de integridade mínima: Garante que os dados necessários não vieram vazios
                if (
                    string.IsNullOrWhiteSpace(audience) ||
                    string.IsNullOrWhiteSpace(issuer) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(nome)
                )
                {
                    throw new UnauthorizedAccessException("Payload Google inválido.");
                }

                // Valida se o token pertence à aplicação correta: Compara o ID do cliente configurado no sistema com o 'aud' do token
                if (audience != _configuration["Google:ClientId"])
                    throw new UnauthorizedAccessException("ClientId inválido.");

                // Valida emissor oficial do Google: Garante que o token realmente veio dos servidores do Google
                if (
                    issuer != "https://accounts.google.com" &&
                    issuer != "accounts.google.com"
                )
                {
                    throw new UnauthorizedAccessException("Issuer Google inválido.");
                }

                // Consulta a persistência buscando a existência do usuário pelo identificador único alternativo (E-mail).
                var usuario = await _FinancasDbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email);

                // Fluxo de Provisionamento Automático (Just-In-Time Provisioning): Se o usuário é novo, cria o registro automaticamente
                if (usuario == null)
                {
                    usuario = new Usuario
                    {
                        Username = nome,
                        Email = email,

                        // Define uma credencial randômica forte para inutilizar login tradicional por senha (força o uso do Google)
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),

                        // Usuário autenticado pelo Google já é considerado validado (e-mail verificado pela fonte de origem)
                        EmailConfirmado = true,

                        DataCadastro = DateTime.UtcNow
                    };

                    _FinancasDbContext.Usuarios.Add(usuario);

                    await _FinancasDbContext.SaveChangesAsync();
                }

                // Garante consistência caso o usuário já exista localmente, mas ainda não tivesse marcado o e-mail como confirmado
                if (!usuario.EmailConfirmado)
                {
                    usuario.EmailConfirmado = true;

                    await _FinancasDbContext.SaveChangesAsync();
                }

                // Claims do JWT interno da aplicação: Define os dados que estarão gravados dentro do seu token (ID, Email, Nome)
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Name, usuario.Username)
                };

                // Chave simétrica do JWT: Busca o segredo configurado no appsettings para assinar o token
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
                );

                // Credenciais de assinatura: Define o algoritmo de segurança (HmacSha256)
                var creds = new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

                // Criação do JWT da aplicação: Configura emissor, audiência, duração (2 horas) e as claims do usuário
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds
                );

                // Serializa o objeto JWT para uma string legível que será enviada ao Frontend
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (UnauthorizedAccessException)
            {
                // Repassa falhas controladas de autenticação sem expor detalhes internos
                throw;
            }
            catch (Exception ex)
            {
                // Retorna mensagem segura para a camada superior
                throw new Exception("Falha interna ao processar autenticação Google.", ex);
            }
        }
    }
}