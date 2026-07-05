using Financas.Api.Data;
using Financas.Api.DTOs.Transferencia;
using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável pela lógica de negócio das transferências entre contas bancárias.
    /// Faz a ponte entre os Controllers e o Repositório (Base de Dados).
    /// </summary>
    public class TransferenciaService
    {
        private readonly FinancasDbContext _financasDbContext;

        /// <summary>
        /// Injeção de dependência do contexto da base de dados.
        /// </summary>
        public TransferenciaService(FinancasDbContext financasDbContext)
        {
            _financasDbContext = financasDbContext;
        }

        /// <summary>
        /// Cria uma nova transferência entre duas contas bancárias do usuário autenticado.
        /// Debita o valor da conta de origem, credita na conta de destino e registra a transferência,
        /// tudo dentro de uma única transação.
        /// </summary>
        /// <param name="dto">Dados de entrada validados.</param>
        /// <param name="userId">ID do usuário autenticado (obtido via Token/Sessão).</param>
        /// <returns>Dados da transferência criada formatados para resposta.</returns>
        /// <exception cref="Exception">Lançada em caso de violação de alguma regra de negócio.</exception>
        /// <exception cref="KeyNotFoundException">Lançada quando alguma das contas não existe.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada quando alguma conta não pertence ao usuário.</exception>
        public async Task<TransferenciaResponseDTO> CriarTransferencia(TransferenciaRequestDTO dto, int userId)
        {
            // 1. Validação de Existência:
            // Verifica se o usuário realmente existe antes de realizar a transferência.
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            // 2. Validação de Regra de Negócio:
            // Impede que a conta de origem e destino sejam a mesma conta.
            if (dto.ContaOrigemId == dto.ContaDestinoId)
                throw new Exception("A conta de origem e a conta de destino não podem ser a mesma.");

            // 3. Validação de Regra de Negócio:
            // Garante que o valor da transferência seja maior que zero.
            if (dto.Valor <= 0)
                throw new Exception("O valor da transferência deve ser maior que zero.");

            // 4. Localização e Validação da Conta de Origem:
            var contaOrigem = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == dto.ContaOrigemId);

            if (contaOrigem == null)
                throw new KeyNotFoundException("Conta de origem não encontrada");

            if (contaOrigem.UsuarioId != userId)
                throw new UnauthorizedAccessException("A conta de origem não pertence ao usuário");

            // 5. Localização e Validação da Conta de Destino:
            var contaDestino = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == dto.ContaDestinoId);

            if (contaDestino == null)
                throw new KeyNotFoundException("Conta de destino não encontrada");

            if (contaDestino.UsuarioId != userId)
                throw new UnauthorizedAccessException("A conta de destino não pertence ao usuário");

            // 6. Validação de Regra de Negócio:
            // Garante que a conta de origem possua saldo suficiente para a transferência.
            if (contaOrigem.Saldo < dto.Valor)
                throw new Exception("Saldo insuficiente na conta de origem.");

            // 7. Transação:
            // Garante que a atualização dos saldos e a criação do registro ocorram de forma atômica.
            using var transacao = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                // 7.1. Atualização de Saldos:
                DebitarContaOrigem(contaOrigem, dto.Valor);
                CreditarContaDestino(contaDestino, dto.Valor);

                // 7.2. Criação do Registro da Transferência:
                var transferencia = new Transferencia
                {
                    UsuarioId = userId,
                    ContaOrigemId = contaOrigem.Id,
                    ContaDestinoId = contaDestino.Id,
                    Valor = dto.Valor,
                    Observacao = dto.Observacao,
                    Data = DateTime.Now
                };

                _financasDbContext.Transferencias.Add(transferencia);

                // 7.3. Persistência:
                await _financasDbContext.SaveChangesAsync();
                await transacao.CommitAsync();

                // 8. Resposta:
                // Mapeia a entidade persistida para o DTO de resposta.
                return new TransferenciaResponseDTO
                {
                    Id = transferencia.Id,
                    Valor = transferencia.Valor,
                    Data = transferencia.Data,
                    ContaOrigem = contaOrigem.Nome,
                    ContaDestino = contaDestino.Nome,
                    Observacao = transferencia.Observacao
                };
            }
            catch
            {
                // Em caso de falha, desfaz todas as alterações realizadas na transação.
                await transacao.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Edita uma transferência existente, estornando os saldos antigos e aplicando os novos
        /// valores informados. Realiza novamente todas as validações de negócio.
        /// </summary>
        /// <param name="dto">Novos dados da transferência.</param>
        /// <param name="transferenciaId">ID da transferência que será editada.</param>
        /// <param name="userId">ID do usuário autenticado para validação de posse.</param>
        /// <returns>DTO com os dados atualizados da transferência.</returns>
        /// <exception cref="Exception">Lançada em caso de violação de alguma regra de negócio.</exception>
        /// <exception cref="KeyNotFoundException">Lançada quando a transferência ou alguma conta não existe.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada quando a transferência ou alguma conta não pertence ao usuário.</exception>
        public async Task<TransferenciaResponseDTO> EditarTransferencia(TransferenciaRequestDTO dto, int transferenciaId, int userId)
        {
            // 1. Localização da Transferência:
            var transferencia = await _financasDbContext.Transferencias
                .FirstOrDefaultAsync(t => t.Id == transferenciaId);

            if (transferencia == null)
                throw new KeyNotFoundException("Transferência não encontrada");

            // 2. Validação de Segurança (Propriedade):
            if (transferencia.UsuarioId != userId)
                throw new UnauthorizedAccessException("A transferência não pertence ao usuário");

            // 3. Validação de Regra de Negócio:
            if (dto.ContaOrigemId == dto.ContaDestinoId)
                throw new Exception("A conta de origem e a conta de destino não podem ser a mesma.");

            if (dto.Valor <= 0)
                throw new Exception("O valor da transferência deve ser maior que zero.");

            // 4. Localização das Contas Antigas (para estorno):
            var contaOrigemAntiga = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == transferencia.ContaOrigemId);

            if (contaOrigemAntiga == null)
                throw new KeyNotFoundException("Conta de origem original não encontrada");

            var contaDestinoAntiga = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == transferencia.ContaDestinoId);

            if (contaDestinoAntiga == null)
                throw new KeyNotFoundException("Conta de destino original não encontrada");

            // 5. Localização e Validação das Novas Contas:
            var contaOrigemNova = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == dto.ContaOrigemId);

            if (contaOrigemNova == null)
                throw new KeyNotFoundException("Conta de origem não encontrada");

            if (contaOrigemNova.UsuarioId != userId)
                throw new UnauthorizedAccessException("A conta de origem não pertence ao usuário");

            var contaDestinoNova = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == dto.ContaDestinoId);

            if (contaDestinoNova == null)
                throw new KeyNotFoundException("Conta de destino não encontrada");

            if (contaDestinoNova.UsuarioId != userId)
                throw new UnauthorizedAccessException("A conta de destino não pertence ao usuário");

            // 6. Transação:
            // Garante que o estorno, a validação de saldo e a aplicação dos novos valores ocorram de forma atômica.
            using var transacao = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                // 6.1. Estorno Completo da Transferência Antiga:
                EstornarTransferencia(contaOrigemAntiga, contaDestinoAntiga, transferencia.Valor);

                // 6.2. Aplicação dos Novos Valores:
                DebitarContaOrigem(contaOrigemNova, dto.Valor);
                CreditarContaDestino(contaDestinoNova, dto.Valor);

                // 6.3. Atualização do Registro da Transferência:
                transferencia.ContaOrigemId = contaOrigemNova.Id;
                transferencia.ContaDestinoId = contaDestinoNova.Id;
                transferencia.Valor = dto.Valor;
                transferencia.Observacao = dto.Observacao;

                // 6.4. Persistência:
                await _financasDbContext.SaveChangesAsync();
                await transacao.CommitAsync();

                // 7. Resposta:
                return new TransferenciaResponseDTO
                {
                    Id = transferencia.Id,
                    Valor = transferencia.Valor,
                    Data = transferencia.Data,
                    ContaOrigem = contaOrigemNova.Nome,
                    ContaDestino = contaDestinoNova.Nome,
                    Observacao = transferencia.Observacao
                };
            }
            catch
            {
                // Em caso de falha, desfaz todas as alterações realizadas na transação.
                await transacao.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Exclui uma transferência existente, estornando os valores para as contas de origem e destino
        /// antes de remover o registro.
        /// </summary>
        /// <param name="transferenciaId">ID da transferência que será removida.</param>
        /// <param name="userId">ID do usuário autenticado para validação de posse.</param>
        /// <exception cref="KeyNotFoundException">Lançada quando a transferência ou alguma conta não existe.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada quando a transferência não pertence ao usuário.</exception>
        public async Task ExcluirTransferencia(int transferenciaId, int userId)
        {
            // 1. Localização da Transferência:
            var transferencia = await _financasDbContext.Transferencias
                .FirstOrDefaultAsync(t => t.Id == transferenciaId);

            if (transferencia == null)
                throw new KeyNotFoundException("Transferência não encontrada");

            // 2. Validação de Segurança (Propriedade):
            if (transferencia.UsuarioId != userId)
                throw new UnauthorizedAccessException("A transferência não pertence ao usuário");

            // 3. Localização das Contas Envolvidas:
            var contaOrigem = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == transferencia.ContaOrigemId);

            if (contaOrigem == null)
                throw new KeyNotFoundException("Conta de origem não encontrada");

            var contaDestino = await _financasDbContext.ContasBancarias
                .FirstOrDefaultAsync(c => c.Id == transferencia.ContaDestinoId);

            if (contaDestino == null)
                throw new KeyNotFoundException("Conta de destino não encontrada");

            // 4. Transação:
            // Garante que o estorno dos saldos e a exclusão do registro ocorram de forma atômica.
            using var transacao = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                // 4.1. Estorno dos Valores:
                EstornarTransferencia(contaOrigem, contaDestino, transferencia.Valor);

                // 4.2. Remoção do Registro:
                _financasDbContext.Transferencias.Remove(transferencia);

                // 4.3. Persistência:
                await _financasDbContext.SaveChangesAsync();
                await transacao.CommitAsync();
            }
            catch
            {
                // Em caso de falha, desfaz todas as alterações realizadas na transação.
                await transacao.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Debita o valor informado da conta de origem, validando a existência da conta,
        /// se o valor é maior que zero e se há saldo suficiente para a operação.
        /// </summary>
        /// <param name="contaOrigem">Conta bancária de onde o valor será debitado.</param>
        /// <param name="valor">Valor a ser debitado.</param>
        /// <exception cref="Exception">Lançada quando a conta é nula, o valor é inválido ou o saldo é insuficiente.</exception>
        private void DebitarContaOrigem(ContaBancaria contaOrigem, decimal valor)
        {
            if (contaOrigem == null)
                throw new Exception("Conta de origem não informada.");

            if (valor <= 0)
                throw new Exception("O valor da transferência deve ser maior que zero.");

            if (contaOrigem.Saldo < valor)
                throw new Exception("Saldo insuficiente na conta de origem.");

            contaOrigem.Saldo -= valor;
        }

        /// <summary>
        /// Credita o valor informado na conta de destino, validando a existência da conta
        /// e se o valor é maior que zero.
        /// </summary>
        /// <param name="contaDestino">Conta bancária que receberá o valor.</param>
        /// <param name="valor">Valor a ser creditado.</param>
        /// <exception cref="Exception">Lançada quando a conta é nula ou o valor é inválido.</exception>
        private void CreditarContaDestino(ContaBancaria contaDestino, decimal valor)
        {
            if (contaDestino == null)
                throw new Exception("Conta de destino não informada.");

            if (valor <= 0)
                throw new Exception("O valor da transferência deve ser maior que zero.");

            contaDestino.Saldo += valor;
        }

        /// <summary>
        /// Estorna uma transferência já realizada, devolvendo o valor para a conta de origem
        /// e removendo o valor da conta de destino, validando a existência das contas,
        /// se o valor é maior que zero e se há saldo suficiente na conta de destino.
        /// </summary>
        /// <param name="contaOrigem">Conta bancária que receberá o valor de volta.</param>
        /// <param name="contaDestino">Conta bancária de onde o valor será removido.</param>
        /// <param name="valor">Valor a ser estornado.</param>
        /// <exception cref="Exception">Lançada quando alguma conta é nula, o valor é inválido ou o saldo é insuficiente.</exception>
        private void EstornarTransferencia(ContaBancaria contaOrigem, ContaBancaria contaDestino, decimal valor)
        {
            if (contaOrigem == null)
                throw new Exception("Conta de origem não informada.");

            if (contaDestino == null)
                throw new Exception("Conta de destino não informada.");

            if (valor <= 0)
                throw new Exception("O valor da transferência deve ser maior que zero.");

            if (contaDestino.Saldo < valor)
                throw new Exception("Saldo insuficiente na conta de destino para o estorno.");

            contaOrigem.Saldo += valor;
            contaDestino.Saldo -= valor;
        }
    }
}