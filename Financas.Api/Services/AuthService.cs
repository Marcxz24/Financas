using Financas.Api.Data;
using Financas.Api.DTOs.Usuario;
using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        /// <exception cref="Exception">Lançada caso o token do Google seja corrompido, expirado, possua assinatura inválida ou falte correspondência de Audience.</exception>
        public async Task<string> LoginComGoogle(string googleToken)
        {
            try
            {
                // 1. Instancia as configurações de validação criptográfica do token do Google.
                // Restringe a validação ao Client ID da aplicação (Audience) para mitigar ataques de personificação (Token Substitution).
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { _configuration["Google:ClientId"]! }
                };

                // 2. Executa a validação assíncrona do JWT (verifica assinatura, expiração (exp) e emissor legítimo do Google).
                // Em caso de sucesso, deserializa e extrai o Payload com as informações de perfil do usuário.
                Google.Apis.Auth.GoogleJsonWebSignature.Payload payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(googleToken, settings);

                string email = payload.Email;
                string nome = payload.Name;

                // 3. Consulta a persistência buscando a existência do usuário pelo identificador único alternativo (E-mail).
                var usuario = await _FinancasDbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email);

                // 4. Fluxo de Provisionamento Automático (Just-In-Time Provisioning):
                // Caso o usuário não possua registro local, uma nova conta é instanciada e persistida de forma transparente.
                if (usuario == null)
                {
                    usuario = new Usuario
                    {
                        Username = nome,
                        Email = email,
                        // Define uma credencial randômica forte (GUID criptográfico) antes de aplicar o algoritmo de derivação de chave (KDF).
                        // Isso anula vetores de ataque por força bruta ou dicionário no fluxo de autenticação tradicional por senha.
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        // O e-mail é implicitamente considerado verificado dado que a autenticação ocorreu através de um Identity Provider (IdP) confiável.
                        EmailConfirmado = true
                    };

                    _FinancasDbContext.Usuarios.Add(usuario);
                    await _FinancasDbContext.SaveChangesAsync();
                }

                // 5. Garantia de Consistência Cadastral:
                // Caso o usuário exista localmente, mas com status de validação pendente, o IdP externo atua como autoridade de validação.
                if (!usuario.EmailConfirmado)
                {
                    usuario.EmailConfirmado = true;
                    await _FinancasDbContext.SaveChangesAsync();
                }

                // 6. Geração do Token de Acesso Nativo da Aplicação (App JWT):
                // Constrói a identidade baseada em Claims (Alegações) de segurança para o contexto de segurança local (User Context).
                var claims = new[]
                {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Username)
        };

                // Instancia a chave simétrica a partir do segredo armazenado na configuração da aplicação.
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
                );

                // Define as credenciais de assinatura utilizando criptografia de chave simétrica com o algoritmo HMAC SHA-256.
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Configura as propriedades do envelope JWT (Tempo de vida estrito de 2 horas, Emissor e Escopo).
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds
                );

                // 7. Serializa o objeto JwtSecurityToken em sua representação compacta de string (Header.Payload.Signature).
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                // Captura falhas de validação de assinatura, tokens expirados ou estruturalmente malformados do Google.
                throw new Exception("Token do Google inválido ou expirado.", ex);
            }
        }
    }
}
