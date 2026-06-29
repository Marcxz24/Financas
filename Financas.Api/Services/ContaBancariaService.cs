using Financas.Api.Data;
using Financas.Api.DTOs.ContaBancaria;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável pela lógica de negócio das contas bancárias.
    /// Faz a ponte entre os Controllers e o Repositório (Base de Dados).
    /// </summary>
    public class ContaBancariaService
    {
        private readonly FinancasDbContext _financasDbContext;

        /// <summary>
        /// Injeção de dependência do contexto da base de dados.
        /// </summary>
        public ContaBancariaService(FinancasDbContext financasDbContext)
        {
            _financasDbContext = financasDbContext;
        }

        /// <summary>
        /// Cria uma nova conta bancária vinculada a um utilizador específico.
        /// </summary>
        /// <param name="dto">Dados de entrada validados.</param>
        /// <param name="userId">ID do utilizador autenticado (obtido via Token/Sessão).</param>
        /// <returns>Dados da conta criada formatados para resposta.</returns>
        /// <exception cref="Exception">Lançada caso o utilizador proprietário não exista.</exception>
        public async Task<ContaBancariaResponseDTO> CriarContaBancaria(CriarContaBancariaDTO dto, int userId)
        {
            // 1. Validação de Existência:
            // Verifica se o utilizador realmente existe antes de criar a conta.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            // 2. Validação de Regra de Negócio:
            // Garante que o usuário possua apenas uma Carteira Física.
            await ValidarCarteiraFisica(userId, dto.Tipo);

            // 3. Mapeamento:
            // Transforma o DTO de entrada na entidade de domínio (ContaBancaria).
            //
            // OBS.:
            // A Carteira Física utiliza a mesma entidade de Conta Bancária.
            // O comportamento é diferenciado apenas pelo campo Tipo,
            // permitindo reaproveitar toda a infraestrutura já existente
            // (saldo, lançamentos, dashboard, IA e patrimônio).
            var contaBancaria = new ContaBancaria
            {
                UsuarioId = userId,
                Nome = dto.Nome,
                Tipo = dto.Tipo,
                Saldo = dto.Saldo
            };

            // 4. Persistência:
            // Adiciona ao rastreamento do EF Core e salva no banco.
            _financasDbContext.ContasBancarias.Add(contaBancaria);
            await _financasDbContext.SaveChangesAsync();

            // 5. Resposta:
            // Mapeia a entidade persistida para o DTO de resposta.
            return new ContaBancariaResponseDTO
            {
                Id = contaBancaria.Id,
                UsuarioId = userId,
                Nome = contaBancaria.Nome,
                Tipo = contaBancaria.Tipo.ToString(),
                Saldo = contaBancaria.Saldo
            };
        }

        /// <summary>
        /// Recupera a lista de todas as contas bancárias pertencentes a um utilizador específico.
        /// </summary>
        /// <param name="userId">Identificador do utilizador cujas contas serão listadas.</param>
        /// <returns>Uma lista de DTOs representando as contas encontradas.</returns>
        public async Task<List<ContaBancariaResponseDTO>> GetContaBancaria(int userId)
        {
            // 1. Filtragem e Consulta:
            // Utiliza o .Where para garantir que o utilizador apenas aceda às suas próprias contas (Segurança).
            // O .ToListAsync() executa a consulta no banco de dados de forma assíncrona.
            var contas = await _financasDbContext.ContasBancarias
                .Where(c => c.UsuarioId == userId)
                .ToListAsync();

            // 2. Transformação (Projeção):
            // Converte a coleção de entidades internas 'ContaBancaria' para o formato de saída 'ContaBancariaResponseDTO'.
            return contas.Select(conta => new ContaBancariaResponseDTO
            {
                Id = conta.Id,
                UsuarioId = conta.UsuarioId,
                Nome = conta.Nome,
                // Converte o Enum para String para que o Front-end receba o nome do tipo de conta.
                Tipo = conta.Tipo.ToString(),
                Saldo = conta.Saldo
            }).ToList();
        }

        /// <summary>
        /// Atualiza parcialmente os dados de uma conta bancária existente.
        /// Realiza verificações de segurança para garantir que o usuário é o dono da conta.
        /// </summary>
        /// <param name="dto">Objeto contendo os campos a serem atualizados (campos nulos são ignorados).</param>
        /// <param name="contaBancariaId">ID da conta que será modificada.</param>
        /// <param name="userId">ID do usuário autenticado para validação de posse.</param>
        /// <returns>DTO com os dados atualizados da conta.</returns>
        public async Task<ContaBancariaResponseDTO> AtualizarContaBancaria(AtualizarContaBancariaDTO dto, int contaBancariaId, int userId)
        {
            // 1. Busca a conta no banco de dados pelo ID fornecido.
            var contaBancaria = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == contaBancariaId);

            // 2. Validação de existência: Retorna erro se o ID não corresponder a nenhuma conta.
            if (contaBancaria == null)
                throw new KeyNotFoundException("Conta bancária não encontrada");

            // 3. Validação de Segurança (Propriedade): 
            // Garante que um usuário não consiga editar a conta de outra pessoa, mesmo sabendo o ID.
            if (contaBancaria.UsuarioId != userId)
                throw new UnauthorizedAccessException("A Conta Bancaria não pertence ao usuário");

            // 4. Atualização Seletiva:
            // Só altera os campos que o usuário enviou no JSON (campos não nulos no DTO).
            // A Carteira Física utiliza a mesma entidade ContaBancaria.
            if (dto.Nome != null)
                contaBancaria.Nome = dto.Nome;

            if (dto.Tipo != null)
                contaBancaria.Tipo = dto.Tipo.Value; // .Value é usado pois o tipo no DTO é Nullable

            if (dto.Saldo != null)
                contaBancaria.Saldo = dto.Saldo.Value;

            // 5. Persistência: O EF Core detecta as mudanças nas propriedades e gera o SQL UPDATE.
            await _financasDbContext.SaveChangesAsync();

            // 6. Retorno: Devolve o estado atual da conta formatado para o cliente.
            return new ContaBancariaResponseDTO
            {
                Id = contaBancaria.Id,
                UsuarioId = userId,
                Nome = contaBancaria.Nome,
                Tipo = contaBancaria.Tipo.ToString(),
                Saldo = contaBancaria.Saldo
            };
        }

        /// <summary>
        /// Remove uma conta bancária do sistema de forma permanente.
        /// Valida se a conta existe e se o usuário logado é o real proprietário antes de excluir.
        /// </summary>
        /// <param name="contaBancariaId">O ID da conta que será removida.</param>
        /// <param name="userId">O ID do usuário autenticado para validação de permissão.</param>
        /// <returns>Uma Task representando a operação assíncrona.</returns>
        /// <exception cref="KeyNotFoundException">Lançada quando o ID da conta não existe no banco.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada quando a conta pertence a outro usuário.</exception>
        public async Task DeletarContaBancaria(int contaBancariaId, int userId)
        {
            // 1. Localização da Conta:
            var contaBancaria = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == contaBancariaId);

            if (contaBancaria == null)
                throw new KeyNotFoundException("Conta bancária não encontrada");

            // 2. Validação de Propriedade:
            if (contaBancaria.UsuarioId != userId)
                throw new UnauthorizedAccessException("A Conta Bancária não pertence ao usuário");

            // 3. REMOÇÃO EM CASCATA (Nova lógica):
            // Busca todos os lançamentos vinculados a esta conta
            var lancamentosVinculados = await _financasDbContext.Lancamentos
                .Where(l => l.ContaBancariaId == contaBancariaId)
                .ToListAsync();

            // Remove todos os lançamentos encontrados
            if (lancamentosVinculados.Any())
            {
                _financasDbContext.Lancamentos.RemoveRange(lancamentosVinculados);
            }

            // 4. Remoção da Conta:
            _financasDbContext.ContasBancarias.Remove(contaBancaria);

            // 5. Persistência (Executa o DELETE dos lançamentos e da conta em uma única transação)
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Valida as regras de criação da Carteira Física.
        /// Garante que cada usuário possua apenas uma Carteira Física cadastrada.
        /// </summary>
        private async Task ValidarCarteiraFisica(int userId, TipoContaBancaria tipo)
        {
            if (tipo != TipoContaBancaria.Fisica)
                return;

            var carteiraExiste = await _financasDbContext.ContasBancarias
                .AnyAsync(c =>
                    c.UsuarioId == userId &&
                    c.Tipo == TipoContaBancaria.Fisica);

            if (carteiraExiste)
                throw new Exception("O usuário já possui uma Carteira Física cadastrada.");
        }
    }
}
