using Financas.Api.Data;
using Financas.Api.DTOs.Usuario;
using Financas.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Financas.Api.Services
{
    public class UsuarioService
    {
        // O serviço de usuário é responsável por lidar com a lógica de negócios relacionada aos usuários, como criação e recuperação de usuários.
        private readonly FinancasDbContext _financasDbContext;

        // Referência ao serviço especializado no envio de e-mails via protocolo SMTP.
        private readonly EmailService _emailService;

        // Interface que permite acessar as configurações do sistema, como chaves e URLs, definidas no appsettings.json.
        private readonly IConfiguration _configuration;

        // O construtor do serviço recebe o contexto do banco de dados via injeção de dependência, permitindo que o serviço interaja com a base de dados para realizar operações relacionadas aos usuários.
        public UsuarioService(FinancasDbContext dbContext, EmailService emailService, IConfiguration configuration)
        {
            _financasDbContext = dbContext;
            _emailService = emailService;
            _configuration = configuration;
        }

        // O método CriarUsuario é responsável por criar um novo usuário. Ele verifica se o e-mail fornecido já está em uso, e se não estiver, cria um novo registro de usuário no banco de dados. O método retorna um DTO de resposta contendo as informações do usuário criado.
        public async Task<UsuarioResponseDTO> CriarUsuario(CriarUsuarioDTO dto)
        {
            // Antes de criar um novo usuário, o método verifica se o e-mail fornecido já existe no banco de dados. Se o e-mail já estiver em uso, uma exceção é lançada para informar que o e-mail não pode ser utilizado.
            var emailExistente = await _financasDbContext.Usuarios
                .AnyAsync(u => u.Email == dto.Email);

            // Se o e-mail já estiver em uso, uma exceção é lançada para informar que o e-mail não pode ser utilizado.
            if (emailExistente)
            {
                throw new InvalidOperationException("Este E-mail já está em uso.");
            }

            // Para garantir a segurança dos dados, a senha fornecida pelo usuário é hashada usando a biblioteca BCrypt antes de ser armazenada no banco de dados. Isso ajuda a proteger as senhas dos usuários em caso de acesso não autorizado ao banco de dados.
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Se o e-mail não estiver em uso, um novo objeto Usuario é criado com as informações fornecidas no DTO. A senha é armazenada diretamente, mas em um cenário real, ela deve ser hashada para garantir a segurança dos dados.
            var usuario = new Usuario
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = senhaHash, // A senha é armazenada de forma segura utilizando hash com BCrypt
                DataCadastro = DateTime.UtcNow
            };

            var token = Guid.NewGuid().ToString("N");

            usuario.TokenConfirmacao = token;
            usuario.TokenExpiracao = DateTime.UtcNow.AddHours(24);

            // O novo usuário é adicionado ao contexto do banco de dados e as alterações são salvas. Após a criação do usuário, um DTO de resposta é retornado contendo as informações do usuário criado, como o ID, username e e-mail.
            _financasDbContext.Usuarios.Add(usuario);
            await _financasDbContext.SaveChangesAsync();

            var baseUrl = _configuration["App:BaseUrl"];
            var linkConfirmacao = $"{baseUrl}/api/usuarios/confirmar-email?token={token}";

            var corpo = $@"
                <h2>Bem-vindo ao Site de Finanças!</h2>
                <p>Clique no link para ativar sua conta:</p>
                <a href='{linkConfirmacao}'>Ativar Conta</a>
                <p>Link válido por 24 horas.</p>
                ";

            await _emailService.EnviarEmailAsync(usuario.Email, "Confirmação de E-mail - Finanças", corpo);

            // Após a criação do usuário, um DTO de resposta é retornado contendo as informações do usuário criado, como o ID, username e e-mail.
            return new UsuarioResponseDTO
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Email = usuario.Email
            };
        }

        // O método GetUsuario é responsável por recuperar a lista de usuários do banco de dados. Ele consulta a tabela de usuários, converte os registros em uma lista de DTOs de resposta e retorna essa lista para o cliente.
        public async Task<List<UsuarioResponseDTO>> GetUsuario()
        {
            var usuarios = await _financasDbContext.Usuarios.ToListAsync();

            return usuarios.Select(u => new UsuarioResponseDTO
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email
            }).ToList();
        }

        /// <summary>
        /// Valida o token de segurança e ativa a conta do usuário ou confirma a alteração de um novo endereço de e-mail.
        /// </summary>
        public async Task ConfirmarEmail(string token)
        {
            // 1. Busca: Localiza o usuário através do token recebido, garantindo a unicidade da operação.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.TokenConfirmacao == token);

            // 2. Validação: Verifica se o token existe e se ainda está dentro do prazo de validade (UTC).
            if (usuario == null || usuario.TokenExpiracao < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Token de confirmação inválido ou expirado.");
            }

            // 3. Ativação: Altera o status da conta para confirmado e limpa os campos de segurança.
            if (usuario.EmailPendente != null)
            {
                usuario.Email = usuario.EmailPendente; // Atualiza o e-mail para o novo endereço pendente.
                usuario.EmailPendente = null; // Limpa o campo de e-mail pendente após a confirmação.
            }

            // 4. Ativação: Marca a conta como confirmada e limpa os campos de segurança.
            usuario.EmailConfirmado = true;
            usuario.TokenConfirmacao = null; // Remove o token para impedir reuso (Segurança).
            usuario.TokenExpiracao = null;

            // 5. Persistência: Atualiza o registro no banco de dados de forma assíncrona.
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Altera a senha do usuário autenticado após validar a titularidade por meio da senha atual.
        /// </summary>
        public async Task AtualizarSenha(AtualizarSenhaDTO dto, int usuarioId)
        {
            // 1. Busca: Localiza o usuário no banco de dados através do ID extraído do Token JWT.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            // 2. Verificação de Existência: Garante que o usuário logado ainda possui um registro ativo.
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuário não encontrado.");
            }

            // 3. Validação de Segurança: Compara a senha atual enviada com o Hash armazenado no banco.
            // O BCrypt.Verify é essencial para descriptografar e validar o hash com segurança.
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, usuario.Password))
            {
                throw new InvalidOperationException("Senha atual incorreta.");
            }

            // 4. Criptografia: Gera um novo Salt e Hash para a nova senha antes de salvar.
            var novaSenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // 5. Atualização: Substitui o hash antigo pelo novo e persiste as mudanças no MySQL.
            usuario.Password = novaSenhaHash;
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Inicia o fluxo de troca de e-mail, validando a senha e enviando um link de confirmação para o novo endereço.
        /// </summary>
        public async Task AtualizarEmail(AtualizarEmailDTO dto, int usuarioId)
        {
            // 1. Busca: Localiza o usuário no banco de dados através do ID autenticado.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // 2. Validação de Segurança: Exige a senha atual para garantir que o dono da conta autorizou a troca.
            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.Password))
                throw new InvalidOperationException("Senha atual incorreta.");

            // 3. Verificação de Duplicidade: Garante que o novo e-mail já não pertença a outro usuário.
            var emailExistente = await _financasDbContext.Usuarios
                .AnyAsync(u => u.Email == dto.NovoEmail);

            if (emailExistente)
                throw new InvalidOperationException("Este E-mail já está em uso.");

            // 4. Buffer de Segurança: Salva o novo e-mail como pendente para não perder o acesso ao e-mail antigo antes da confirmação.
            usuario.EmailPendente = dto.NovoEmail;

            // 5. Geração de Token: Cria um identificador único e seguro (GUID) para o link de validação.
            var token = Guid.NewGuid().ToString("N");
            usuario.TokenConfirmacao = token;
            usuario.TokenExpiracao = DateTime.UtcNow.AddHours(24);

            // 6. Construção do Link: Gera a URL que o usuário clicará, baseada no ambiente (Desenvolvimento/Produção).
            var baseUrl = _configuration["App:BaseUrl"];
            var linkConfirmacao = $"{baseUrl}/api/usuarios/confirmar-email?token={token}";

            // 7. Template de Comunicação: Estrutura o corpo do e-mail em formato HTML.
            var corpo = $@"
                        <h2>Confirmação de Alteração de E-mail</h2>
                        <p>Você solicitou a alteração do seu e-mail para {dto.NovoEmail}.</p>
                        <p>Clique no link abaixo para confirmar e ativar o novo endereço:</p>
                        <a href='{linkConfirmacao}'>Confirmar Novo E-mail</a>
                        <p>Este link expira em 24 horas.</p>
                        ";

            // 8. Persistência e Notificação: Salva as alterações no MySQL e dispara o e-mail de forma assíncrona.
            await _financasDbContext.SaveChangesAsync();
            await _emailService.EnviarEmailAsync(dto.NovoEmail, "Confirmação de E-mail - Finanças", corpo);
        }

        /// <summary>
        /// Altera o nome de usuário (username) do cliente, validando a duplicidade no banco de dados.
        /// </summary>
        public async Task AlterarUsername(AtualizarUsernameDTO dto, int usuarioId)
        {
            // 1. Busca: Localiza o usuário no banco de dados através do ID autenticado.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // 2. Otimização: Se o nome digitado for exatamente igual ao atual, não há necessidade de processar.
            if (usuario.Username == dto.Username)
                return;

            // 3. Verificação de Duplicidade: Garante que o novo username já não pertença a outro usuário.
            var usernameExistente = await _financasDbContext.Usuarios
                .AnyAsync(u => u.Username == dto.Username);

            if (usernameExistente)
                throw new InvalidOperationException("Este nome de usuário já está sendo utilizado por outra conta.");

            // 4. Aplica a alteração: Atualiza o campo com o valor validado do DTO.
            usuario.Username = dto.Username;

            // 5. Persistência: Salva a alteração de forma assíncrona no MySQL.
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Inicia o fluxo de recuperação de senha, gerando um token temporário e enviando o link de redefinição por e-mail.
        /// </summary>
        public async Task SolicitarRedefinicaoSenha(EsqueciSenhaDTO dto)
        {
            // 1. Localização: Verifica se o e-mail informado pertence a um usuário cadastrado.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // 2. Geração de Identificador: Cria um token único (GUID) para validar a solicitação de troca.
            var token = Guid.NewGuid().ToString("N");

            // 3. Segurança Temporal: Define o token no banco com um prazo curto de validade (1 hora).
            usuario.TokenConfirmacao = token;
            usuario.TokenExpiracao = DateTime.UtcNow.AddHours(1);

            // 4. Persistência: Grava o token e a expiração no banco antes de proceder com o envio do e-mail.
            await _financasDbContext.SaveChangesAsync();

            // 5. Construção da URL: Monta o link que o usuário utilizará no frontend para redefinir a senha.
            var baseUrl = _configuration["App:BaseUrl"];
            var linkConfirmacao = $"{baseUrl}/api/usuarios/redefinir-senha?token={token}";

            // 6. Template de Notificação: Define o conteúdo visual e informativo do e-mail de recuperação.
            var corpo = $@"
                <h2>Recuperação de Senha</h2>
                <p>Você solicitou a alteração da sua senha no sistema de Finanças.</p>
                <p>Clique no link abaixo para prosseguir com a nova senha:</p>
                <a href='{linkConfirmacao}'>Redefinir Senha</a>
                <p>Este link é válido por apenas 1 hora.</p>
                ";

            // 7. Comunicação: Dispara o e-mail para o usuário de forma assíncrona.
            await _emailService.EnviarEmailAsync(dto.Email, "Alterar Senha - Finanças API", corpo);
        }

        /// <summary>
        /// Finaliza o processo de recuperação de conta, validando o token e definindo uma nova senha criptografada.
        /// </summary>
        public async Task RedefinirSenha(RedefinirSenhaDTO dto)
        {
            // 1. Localização: Busca o usuário que possui o token único gerado para a recuperação.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.TokenConfirmacao == dto.Token);

            // 2. Validação de Segurança: Verifica se o token existe e se ainda é válido temporalmente.
            if (usuario == null || usuario.TokenExpiracao < DateTime.UtcNow)
                throw new InvalidOperationException("Token de confirmação inválido ou expirado.");

            // 3. Criptografia: Gera um novo Hash seguro para a nova senha escolhida pelo usuário.
            var novaSenha = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // 4. Atualização: Substitui a senha antiga pelo novo hash criptografado.
            usuario.Password = novaSenha;

            // 5. Invalidação: Limpa o token e a expiração para garantir que o link não seja usado novamente.
            usuario.TokenConfirmacao = null;
            usuario.TokenExpiracao = null;

            // 6. Persistência: Grava as alterações permanentemente no banco de dados MySQL.
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Método para visualizar o perfil do usuário, retornando as informações básicas como ID, username e e-mail.
        /// <param name="id">ID do usuário a ser visualizado.</param>
        /// </summary>
        public async Task<UsuarioResponseDTO> VisualizarPerfil(int id)
        {
            // 1. Localização: Busca o usuário no banco de dados através do ID fornecido.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            // 2. Validação de Existência: Garante que o usuário solicitado realmente exista antes de retornar os dados.
            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // 3. Resposta: Mapeia os dados do usuário para um DTO de resposta, expondo apenas as informações essenciais.
            return new UsuarioResponseDTO
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Email = usuario.Email,
                EmailConfirmado = usuario.EmailConfirmado,
                DataCadastro = usuario.DataCadastro
            };
        }

        // Método responsável por definir a senha após o login com o Google.
        public async Task DefinirSenha(DefinirSenhaDTO dto, int usuarioId)
        {
            // Filtra no banco de dados o ID do usuário logado.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            // Se a nova senha digitada for diferente da confirmação da senha digitada.
            // Retorna a mensagem de operação inválida.
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new InvalidOperationException("A confirmação da senha não confere.");

            // Se o retorno do usuário for Nulo, retorna uma operação inválida de usuário não encontrado.
            if (usuario == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // Gera um novo HASH para a senha digitada.
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // Salva as alterações no Banco de Dados.
            await _financasDbContext.SaveChangesAsync();
        }
    }
}
