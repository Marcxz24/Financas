using Financas.Api.Data;
using Financas.Api.DTOs.Fatura;
using Financas.Api.DTOs.Lancamento;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável pelo gerenciamento de faturas de cartões de crédito.
    /// Contém a lógica de cálculo de ciclos (competências), datas de fechamento e vencimento,
    /// refletindo o funcionamento real utilizado pelos bancos: cada fatura representa uma
    /// competência (ano/mês) específica, e as datas de início, fechamento e vencimento são
    /// calculadas com base na configuração vigente do cartão enquanto o ciclo estiver aberto,
    /// tornando-se imutáveis assim que a fatura é encerrada.
    /// </summary>
    public class FaturaService
    {
        private readonly FinancasDbContext _financasDbContext;

        /// <summary>
        /// Construtor do serviço de faturas, injetando o contexto do banco de dados.
        /// </summary>
        public FaturaService(FinancasDbContext financasDbContext)
        {
            _financasDbContext = financasDbContext;
        }

        /// <summary>
        /// Incrementa o valor total da fatura após um novo lançamento de gasto.
        /// </summary>
        /// <param name="fatura">A entidade da fatura que será atualizada.</param>
        /// <param name="valor">O valor a ser somado ao total.</param>
        /// <exception cref="ArgumentNullException">Lançada se a entidade fatura for nula.</exception>
        /// <exception cref="ArgumentException">Lançada se o valor for negativo ou zero.</exception>
        public void AplicarValorFatura(Fatura fatura, decimal valor)
        {
            if (fatura == null)
                throw new ArgumentNullException(nameof(fatura), "A fatura não pode ser nula.");

            if (valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            fatura.ValorTotal += valor;
        }

        /// <summary>
        /// Subtrai um valor do total da fatura (útil para exclusão de lançamentos ou estornos).
        /// </summary>
        /// <param name="fatura">A entidade da fatura que será atualizada.</param>
        /// <param name="valor">O valor a ser subtraído do total.</param>
        /// <exception cref="ArgumentNullException">Lançada se a entidade fatura for nula.</exception>
        /// <exception cref="ArgumentException">Lançada se o valor for negativo ou zero.</exception>
        public void EstornarValorFatura(Fatura fatura, decimal valor)
        {
            if (fatura == null)
                throw new ArgumentNullException(nameof(fatura), "A fatura não pode ser nula.");

            if (valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            fatura.ValorTotal -= valor;
        }

        /// <summary>
        /// Recupera o histórico completo de faturas de todos os cartões vinculados ao usuário.
        /// As faturas são retornadas ordenadas da mais recente para a mais antiga (por competência).
        /// </summary>
        /// <param name="usuarioId">Identificador do usuário para filtragem dos dados.</param>
        /// <returns>Uma lista de DTOs contendo o resumo de cada fatura encontrada.</returns>
        public async Task<IEnumerable<FaturaResponseDTO>> ListarFaturas(int usuarioId)
        {
            // Sincroniza as faturas abertas do usuário antes de listar, garantindo que qualquer
            // alteração recente no dia de fechamento/vencimento do cartão (ou dados legados com
            // datas desatualizadas) já apareça corrigida na resposta, mesmo sem a criação de um
            // lançamento novo.
            await SincronizarFaturasAbertasDoUsuario(usuarioId);

            // Realiza a consulta ao banco de dados aplicando filtros de segurança e ordenação
            var faturas = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito) // Carrega os dados do cartão para validar o UsuarioId
                .Where(f => f.CartaoCredito.UsuarioId == usuarioId) // Garante que o usuário veja apenas seus próprios dados
                .OrderByDescending(f => f.Competencia) // Organiza para que a fatura atual/recente apareça primeiro
                .Select(f => new FaturaResponseDTO // Projeção direta para o DTO, otimizando a query SQL
                {
                    Id = f.Id,
                    CartaoCreditoId = f.CartaoCreditoId,
                    DataInicio = f.DataInicio,
                    DataFechamento = f.DataFechamento,
                    DataVencimento = f.DataVencimento,
                    ValorTotal = f.ValorTotal,
                    ValorPago = f.ValorPago,
                    Status = f.Status.ToString() // Converte o Enum para string facilitando o consumo no Front-end
                })
                .ToListAsync();

            return faturas;
        }

        /// <summary>
        /// Consulta o extrato detalhado de uma fatura, incluindo o histórico de pagamentos e o cálculo do saldo restante.
        /// </summary>
        /// <param name="faturaId">ID da fatura a ser consultada.</param>
        /// <param name="usuarioId">ID do usuário proprietário, utilizado para validação de segurança.</param>
        /// <returns>Retorna um objeto com os totais da fatura e a lista de pagamentos efetuados.</returns>
        /// <exception cref="KeyNotFoundException">Lançada caso a fatura não exista ou não pertença ao usuário logado.</exception>
        public async Task<ExtratoFaturaResponseDTO> ObterExtratoFatura(int faturaId, int usuarioId)
        {
            // Busca a fatura no banco, incluindo o cartão para garantir a verificação de posse do usuário (Multitenancy)
            var fatura = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .FirstOrDefaultAsync(f => f.Id == faturaId && f.CartaoCredito.UsuarioId == usuarioId);

            // Valida se a fatura existe
            if (fatura == null)
                throw new KeyNotFoundException("Fatura não encontrada.");

            // Busca todos os pagamentos vinculados a esta fatura, ordenando pelos mais recentes
            var pagamentos = await _financasDbContext.PagamentoFatura
                .Where(p => p.FaturaId == faturaId)
                .OrderByDescending(p => p.DataPagamento)
                .Select(p => new PagamentoResponseDTO
                {
                    Id = p.Id,
                    ValorPago = p.ValorPago,
                    DataPagamento = p.DataPagamento,
                    ContaBancariaId = p.ContaBancariaId,
                    Observacao = p.Observacao
                })
                .ToListAsync();

            var lancamentos = await _financasDbContext.Lancamentos
                .Where(l => l.FaturaId == faturaId)
                .OrderByDescending(l => l.Data)
                .Select(l => new LancamentoExtratoDTO
                {
                    Id = l.Id,
                    Descricao = l.Descricao,
                    Valor = l.Valor,
                    Data = l.Data,
                    Tipo = l.Tipo.ToString()
                })
                .ToListAsync();

            // Calcula o somatório dos pagamentos realizados
            decimal totalPago = 0;
            if (pagamentos.Any())
                totalPago = pagamentos.Sum(p => p.ValorPago);

            // Define quanto ainda resta para quitar a fatura
            var saldoRestante = fatura.ValorTotal - totalPago;

            // Monta o objeto de resposta para o DTO
            return new ExtratoFaturaResponseDTO
            {
                FaturaId = fatura.Id,
                ValorTotal = fatura.ValorTotal,
                TotalPago = totalPago,
                SaldoRestante = saldoRestante,
                Pagamentos = pagamentos,
                Lancamentos = lancamentos
            };
        }

        /// <summary>
        /// Obtém os dados de uma fatura para retorno à interface de usuário (DTO).
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário proprietário.</param>
        /// <param name="dataCompra">Data e hora completas da transação, usadas para determinar a competência correta.</param>
        /// <returns>Objeto de resposta formatado com os dados da fatura.</returns>
        public async Task<FaturaResponseDTO> ObterOuCriarFaturaAtual(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            var fatura = await ObterOuCriarFaturaAtualEntidade(cartaoId, usuarioId, dataCompra);

            return new FaturaResponseDTO
            {
                Id = fatura.Id,
                CartaoCreditoId = fatura.CartaoCreditoId,
                DataInicio = fatura.DataInicio,
                DataFechamento = fatura.DataFechamento,
                DataVencimento = fatura.DataVencimento,
                ValorTotal = fatura.ValorTotal,
                ValorPago = fatura.ValorPago,
                Status = fatura.Status.ToString()
            };
        }

        /// <summary>
        /// Determina a competência (ano/mês) à qual uma compra pertence, com base na configuração
        /// atual do cartão. O fechamento efetivo de um ciclo ocorre às 23:59:59.999 do dia de
        /// fechamento configurado; compras realizadas até esse instante pertencem à competência
        /// corrente, enquanto compras a partir de 00:00:00.000 do dia seguinte já pertencem à
        /// competência seguinte. A comparação é sempre feita com o valor completo de
        /// <see cref="DateTime"/> (data e hora), nunca apenas pelo número do dia.
        /// </summary>
        /// <param name="cartao">Cartão de crédito, já validado como pertencente ao usuário.</param>
        /// <param name="dataCompra">Data e hora da compra.</param>
        /// <returns>Uma tupla com o ano e o mês da competência correspondente.</returns>
        private (int ano, int mes) DeterminarCompetencia(CartaoCredito cartao, DateTime dataCompra)
        {
            // Normaliza como horário local para garantir comparações consistentes com as datas
            // armazenadas/calculadas para as faturas (também em horário local, sem uso de UTC
            // em nenhum ponto do projeto).
            var dataCompraLocal = dataCompra.Kind == DateTimeKind.Local
                ? dataCompra
                : DateTime.SpecifyKind(dataCompra, DateTimeKind.Local);

            var ano = dataCompraLocal.Year;
            var mes = dataCompraLocal.Month;

            // Respeita meses com 28, 29, 30 ou 31 dias.
            var ultimoDiaMes = DateTime.DaysInMonth(ano, mes);
            var diaFechamentoAjustado = Math.Min(cartao.DiaFechamento, ultimoDiaMes);

            // O fechamento efetivo do ciclo ocorre no último instante do dia de fechamento.
            var fechamentoCandidato = new DateTime(ano, mes, diaFechamentoAjustado, 23, 59, 59, 999, DateTimeKind.Local);

            // Compra realizada até o instante de fechamento pertence à competência do próprio mês.
            if (dataCompraLocal <= fechamentoCandidato)
                return (ano, mes);

            // Compra realizada a partir de 00:00:00.000 do dia seguinte pertence à próxima competência.
            var proximaCompetencia = new DateTime(ano, mes, 1).AddMonths(1);
            return (proximaCompetencia.Year, proximaCompetencia.Month);
        }

        /// <summary>
        /// Calcula as datas de início, fechamento e vencimento de uma competência específica,
        /// utilizando sempre a configuração atual do cartão (<see cref="CartaoCredito.DiaFechamento"/>
        /// e <see cref="CartaoCredito.DiaVencimento"/>). Respeita meses com diferentes quantidades
        /// de dias através de <see cref="DateTime.DaysInMonth(int, int)"/>.
        /// </summary>
        /// <param name="cartao">Cartão de crédito com a configuração vigente de ciclo.</param>
        /// <param name="ano">Ano da competência.</param>
        /// <param name="mes">Mês da competência.</param>
        /// <returns>As três datas que compõem o ciclo da fatura.</returns>
        private (DateTime dataInicio, DateTime dataFechamento, DateTime dataVencimento) CalcularCicloFatura(
            CartaoCredito cartao, int ano, int mes)
        {
            var ultimoDiaMes = DateTime.DaysInMonth(ano, mes);
            var diaFechamentoAjustado = Math.Min(cartao.DiaFechamento, ultimoDiaMes);

            // Fechamento: último instante (23:59:59.999) do dia de fechamento configurado.
            var dataFechamento = new DateTime(ano, mes, diaFechamentoAjustado, 23, 59, 59, 999, DateTimeKind.Local);

            // Início: dia seguinte ao fechamento do ciclo anterior, também calculado com a
            // configuração ATUAL do cartão (garante consistência caso o dia de fechamento
            // tenha sido alterado recentemente).
            var referenciaMesAnterior = new DateTime(ano, mes, 1).AddMonths(-1);
            var ultimoDiaMesAnterior = DateTime.DaysInMonth(referenciaMesAnterior.Year, referenciaMesAnterior.Month);
            var diaFechamentoAnteriorAjustado = Math.Min(cartao.DiaFechamento, ultimoDiaMesAnterior);

            var dataInicio = new DateTime(
                referenciaMesAnterior.Year,
                referenciaMesAnterior.Month,
                diaFechamentoAnteriorAjustado,
                0, 0, 0, DateTimeKind.Local).AddDays(1);

            // Vencimento: na prática dos bancos, quando o dia de vencimento é numericamente menor
            // ou igual ao dia de fechamento, o vencimento cai no mês SEGUINTE ao fechamento
            // (ex: fecha dia 28, vence dia 05 do mês seguinte). Caso contrário, vence no mesmo mês.
            var referenciaVencimento = new DateTime(ano, mes, 1);
            if (cartao.DiaVencimento <= diaFechamentoAjustado)
                referenciaVencimento = referenciaVencimento.AddMonths(1);

            var ultimoDiaMesVencimento = DateTime.DaysInMonth(referenciaVencimento.Year, referenciaVencimento.Month);
            var diaVencimentoAjustado = Math.Min(cartao.DiaVencimento, ultimoDiaMesVencimento);

            var dataVencimento = new DateTime(
                referenciaVencimento.Year,
                referenciaVencimento.Month,
                diaVencimentoAjustado,
                23, 59, 59, 999, DateTimeKind.Local);

            return (dataInicio, dataFechamento, dataVencimento);
        }

        /// <summary>
        /// Aplica em uma entidade <see cref="Fatura"/> já carregada as datas de ciclo
        /// recalculadas a partir da configuração ATUAL do cartão informado, usando a
        /// <see cref="Fatura.Competencia"/> da própria fatura como referência de ano/mês.
        /// Não persiste a alteração (quem chama decide quando salvar) e não faz nenhuma
        /// validação de status — a decisão de "só recalcular se Aberta" é responsabilidade
        /// de quem invoca este método.
        /// </summary>
        /// <param name="fatura">Fatura a ter as datas recalculadas.</param>
        /// <param name="cartao">Cartão com a configuração vigente de fechamento/vencimento.</param>
        private void RecalcularDatasFatura(Fatura fatura, CartaoCredito cartao)
        {
            var (dataInicio, dataFechamento, dataVencimento) =
                CalcularCicloFatura(cartao, fatura.Competencia.Year, fatura.Competencia.Month);

            fatura.DataInicio = dataInicio;
            fatura.DataFechamento = dataFechamento;
            fatura.DataVencimento = dataVencimento;
        }

        /// <summary>
        /// Lógica interna que localiza a fatura correspondente à competência de uma compra,
        /// ou cria uma nova caso o ciclo ainda não possua registro no banco. Para faturas ainda
        /// <see cref="FaturaStatus.Aberta"/>, as datas de início, fechamento e vencimento são
        /// recalculadas a partir da configuração ATUAL do cartão a cada chamada — permitindo que
        /// alterações no dia de fechamento/vencimento reflitam corretamente no ciclo vigente sem
        /// comprometer faturas já encerradas, cujas datas permanecem congeladas como histórico.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário para validação de segurança.</param>
        /// <param name="dataCompra">Data e hora completas da compra, usadas para determinar a competência.</param>
        /// <returns>A entidade de Fatura (existente, recalculada ou recém-criada).</returns>
        /// <exception cref="KeyNotFoundException">Lançada se o cartão não for localizado.</exception>
        public async Task<Fatura> ObterOuCriarFaturaAtualEntidade(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            // Validação de segurança: o cartão deve pertencer ao usuário.
            var cartao = await _financasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.UsuarioId == usuarioId);

            if (cartao == null)
                throw new KeyNotFoundException("Cartão não encontrado.");

            // 1. Determina a competência (ano/mês) com base em comparação de DateTime completo,
            // e não mais apenas pelo número do dia da compra.
            var (ano, mes) = DeterminarCompetencia(cartao, dataCompra);
            var competencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Local);

            // 2. Busca a fatura pela chave estável (Cartão + Competência).
            var fatura = await _financasDbContext.Fatura
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartaoId && f.Competencia == competencia);

            if (fatura != null)
            {
                // Fatura ainda aberta: recalcula as datas do ciclo com a configuração vigente do
                // cartão, garantindo que mudanças recentes no dia de fechamento/vencimento sejam
                // corretamente refletidas enquanto o ciclo não for encerrado.
                if (fatura.Status == FaturaStatus.Aberta)
                {
                    RecalcularDatasFatura(fatura, cartao);
                    await _financasDbContext.SaveChangesAsync();
                }

                // Faturas Fechadas, Pagas ou Atrasadas mantêm suas datas históricas imutáveis:
                // nenhum recálculo é realizado, preservando a integridade do histórico financeiro.
                return fatura;
            }

            // 3. Não existe fatura para esta competência: abre um novo ciclo automaticamente,
            // já calculado com a configuração atual do cartão.
            var (dataInicio, dataFechamento, dataVencimento) = CalcularCicloFatura(cartao, ano, mes);

            fatura = new Fatura
            {
                CartaoCreditoId = cartaoId,
                Competencia = competencia,
                DataInicio = dataInicio,
                DataFechamento = dataFechamento,
                DataVencimento = dataVencimento,
                ValorTotal = 0,
                ValorPago = 0,
                Status = FaturaStatus.Aberta
            };

            _financasDbContext.Fatura.Add(fatura);
            await _financasDbContext.SaveChangesAsync();

            return fatura;
        }

        /// <summary>
        /// Recalcula as datas (<see cref="Fatura.DataInicio"/>, <see cref="Fatura.DataFechamento"/>,
        /// <see cref="Fatura.DataVencimento"/>) de todas as faturas com status
        /// <see cref="FaturaStatus.Aberta"/> de um cartão específico, com base na configuração
        /// ATUAL do cartão (<see cref="CartaoCredito.DiaFechamento"/> e
        /// <see cref="CartaoCredito.DiaVencimento"/>).
        ///
        /// Cobre dois cenários:
        /// 1. O usuário alterou o dia de fechamento/vencimento do cartão e quer que o ciclo
        ///    vigente reflita a mudança imediatamente, sem esperar um novo lançamento.
        /// 2. Existem faturas abertas com datas desatualizadas (por exemplo, gravadas antes da
        ///    configuração atual do cartão existir, ou por dados legados/de teste).
        ///
        /// Faturas <see cref="FaturaStatus.Fechada"/>, <see cref="FaturaStatus.Paga"/> ou
        /// <see cref="FaturaStatus.Atrasada"/> nunca são tocadas por este método — o histórico
        /// financeiro encerrado permanece imutável, preservando a integridade dos dados já
        /// consolidados.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito a ser sincronizado.</param>
        /// <param name="usuarioId">ID do usuário, para validação de segurança (o cartão precisa pertencer a ele).</param>
        /// <returns>A lista das faturas abertas do cartão, já com as datas recalculadas.</returns>
        /// <exception cref="KeyNotFoundException">Lançada se o cartão não for localizado ou não pertencer ao usuário.</exception>
        public async Task<List<Fatura>> SincronizarCicloAberto(int cartaoId, int usuarioId)
        {
            // Validação de segurança: o cartão deve pertencer ao usuário.
            var cartao = await _financasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.UsuarioId == usuarioId);

            if (cartao == null)
                throw new KeyNotFoundException("Cartão não encontrado.");

            // Busca TODAS as faturas abertas do cartão — não apenas a competência "corrente" —
            // pois faturas futuras já criadas por parcelamentos também precisam refletir a nova
            // configuração do ciclo.
            var faturasAbertas = await _financasDbContext.Fatura
                .Where(f => f.CartaoCreditoId == cartaoId && f.Status == FaturaStatus.Aberta)
                .ToListAsync();

            foreach (var fatura in faturasAbertas)
                RecalcularDatasFatura(fatura, cartao);

            if (faturasAbertas.Count > 0)
                await _financasDbContext.SaveChangesAsync();

            return faturasAbertas;
        }

        /// <summary>
        /// Sincroniza (recalcula) todas as faturas abertas de todos os cartões de um usuário de
        /// uma só vez, evitando o problema N+1 de chamar <see cref="SincronizarCicloAberto"/>
        /// individualmente por cartão. Utilizado internamente antes de listagens (ex.:
        /// <see cref="ListarFaturas"/>) para garantir que qualquer leitura já reflita a
        /// configuração vigente dos cartões, mesmo sem lançamentos novos.
        /// </summary>
        /// <param name="usuarioId">ID do usuário cujas faturas abertas serão sincronizadas.</param>
        private async Task SincronizarFaturasAbertasDoUsuario(int usuarioId)
        {
            var faturasAbertas = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .Where(f => f.CartaoCredito.UsuarioId == usuarioId && f.Status == FaturaStatus.Aberta)
                .ToListAsync();

            if (faturasAbertas.Count == 0)
                return;

            foreach (var fatura in faturasAbertas)
                RecalcularDatasFatura(fatura, fatura.CartaoCredito);

            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Realiza o pagamento de uma fatura, atualizando o saldo da conta bancária de origem 
        /// e o status de quitação da fatura. Todo o processo é protegido por uma transação de banco de dados.
        /// </summary>
        /// <param name="dto">Dados do pagamento (ID da fatura, valor e conta de origem).</param>
        /// <param name="usuarioId">ID do usuário que está realizando a operação.</param>
        /// <exception cref="KeyNotFoundException">Lançada se a fatura ou conta bancária não existirem.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada se o recurso não pertencer ao usuário logado.</exception>
        /// <exception cref="InvalidOperationException">Lançada em caso de saldo insuficiente ou fatura já paga.</exception>
        public async Task PagarFatura(PagarFaturaDTO dto, int usuarioId)
        {
            // Inicia uma transação para garantir que o dinheiro não "suma" se houver erro no meio do processo
            using var transaction = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                // Busca a fatura incluindo os dados do cartão para validar a posse do usuário
                var fatura = await _financasDbContext.Fatura
                    .Include(f => f.CartaoCredito)
                    .FirstOrDefaultAsync(f => f.Id == dto.FaturaId);

                if (fatura == null)
                    throw new KeyNotFoundException("Fatura não encontrada.");

                // Validação de Segurança (Multitenancy)
                if (fatura.CartaoCredito.UsuarioId != usuarioId)
                    throw new UnauthorizedAccessException("Fatura não pertence ao usuário.");

                if (fatura.Status == FaturaStatus.Paga)
                    throw new InvalidOperationException("Fatura já está totalmente paga.");

                if (fatura.Status != FaturaStatus.Fechada && fatura.Status != FaturaStatus.Atrasada)
                    throw new InvalidOperationException("Somente faturas fechadas ou atrasadas podem ser pagas.");

                if (dto.ValorPago <= 0)
                    throw new ArgumentException("Valor pago deve ser maior que zero.");

                var totalPagoAtual = await _financasDbContext.PagamentoFatura
                    .Where(p => p.FaturaId == fatura.Id)
                    .SumAsync(p => p.ValorPago);

                var valorRestante = Math.Round(fatura.ValorTotal - totalPagoAtual, 2);
                var valorPagamentoArredondado = Math.Round(dto.ValorPago, 2);

                // Impede pagamentos maiores que a dívida atual da fatura
                if (valorPagamentoArredondado > valorRestante)
                    throw new ArgumentException("Valor pago excede o valor restante da fatura.");

                // Se houver uma conta bancária vinculada, realiza a baixa do saldo
                if (dto.ContaBancariaId.HasValue)
                {
                    var conta = await _financasDbContext.ContasBancarias
                        .FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId.Value && c.UsuarioId == usuarioId);

                    if (conta == null)
                        throw new KeyNotFoundException("Conta bancária não encontrada.");

                    if (conta.Saldo < dto.ValorPago)
                        throw new InvalidOperationException("Saldo insuficiente.");

                    // Subtrai o valor do saldo disponível na conta selecionada
                    conta.Saldo -= dto.ValorPago;
                }

                var pagamento = new PagamentoFatura
                {
                    FaturaId = fatura.Id,
                    ValorPago = dto.ValorPago,
                    DataPagamento = dto.DataPagamento == default
                            ? DateTime.Now
                            : dto.DataPagamento,
                    ContaBancariaId = dto.ContaBancariaId,
                    Observacao = dto.Observacao
                };

                _financasDbContext.PagamentoFatura.Add(pagamento);

                fatura.ValorPago += dto.ValorPago;

                fatura.Status = fatura.ValorPago >= fatura.ValorTotal
                    ? FaturaStatus.Paga
                    : FaturaStatus.Fechada;

                // Persiste as alterações e confirma a transação
                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Em caso de qualquer erro, reverte as alterações (Saldo da conta e ValorPago da fatura)
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Encerra o ciclo atual de uma fatura e, caso necessário, abre automaticamente a fatura
        /// da próxima competência. A fatura encerrada tem suas datas congeladas definitivamente
        /// (deixam de ser recalculadas), preservando o histórico financeiro imutável. A nova
        /// fatura é criada com base na configuração ATUAL do cartão, permitindo que alterações
        /// de dia de fechamento/vencimento feitas pelo usuário valham a partir do próximo ciclo.
        /// Utiliza transação para garantir que o fechamento da antiga e a criação da nova ocorram simultaneamente.
        /// </summary>
        /// <param name="faturaId">Identificador da fatura a ser encerrada.</param>
        /// <param name="usuarioId">Identificador do usuário para validação de segurança.</param>
        /// <exception cref="KeyNotFoundException">Lançada se a fatura não existir.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada se a fatura pertencer a outro usuário.</exception>
        /// <exception cref="InvalidOperationException">Lançada se a fatura não puder ser fechada por regras de status ou data.</exception>
        public async Task FecharFatura(int faturaId, int usuarioId)
        {
            // Inicia transação para garantir a integridade ao criar a nova fatura
            await using var transaction = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                var fatura = await _financasDbContext.Fatura
                    .Include(f => f.CartaoCredito)
                    .FirstOrDefaultAsync(f => f.Id == faturaId);

                if (fatura == null)
                    throw new KeyNotFoundException("Fatura não encontrada.");

                if (fatura.CartaoCredito.UsuarioId != usuarioId)
                    throw new UnauthorizedAccessException("Fatura não pertence ao usuário.");

                // Validação: Garante que apenas faturas em uso (Aberta/Atrasada) sejam processadas
                if (fatura.Status != FaturaStatus.Aberta && fatura.Status != FaturaStatus.Atrasada)
                    throw new InvalidOperationException("Somente faturas em abertas ou atrasadas podem ser fechadas.");

                // Regra de Negócio: Impede o fechamento antes do instante de fechamento do ciclo
                // (23:59:59.999 do dia de fechamento configurado no cartão).
                if (DateTime.Now < fatura.DataFechamento)
                    throw new InvalidOperationException("Não é possível fechar a fatura antes da data de fechamento.");

                // Calcula saldo pendente
                var saldoPendente = fatura.ValorTotal - fatura.ValorPago;

                // Define o status correto
                fatura.Status = saldoPendente <= 0
                    ? FaturaStatus.Paga
                    : FaturaStatus.Fechada;

                // A partir deste ponto, DataInicio/DataFechamento/DataVencimento desta fatura
                // não serão mais recalculadas (ObterOuCriarFaturaAtualEntidade só recalcula
                // faturas com status Aberta), preservando o histórico congelado do ciclo.

                // Determina a próxima competência (mês seguinte à fatura encerrada).
                var proximaCompetencia = fatura.Competencia.AddMonths(1);

                // Verifica se já existe uma fatura para a próxima competência, evitando duplicidade.
                var existeFaturaProximoCiclo = await _financasDbContext.Fatura
                    .AnyAsync(f => f.CartaoCreditoId == fatura.CartaoCreditoId &&
                                   f.Competencia == proximaCompetencia);

                if (!existeFaturaProximoCiclo)
                {
                    // Usa a configuração ATUAL do cartão para calcular o próximo ciclo,
                    // respeitando eventuais alterações de dia de fechamento/vencimento.
                    var (dataInicio, dataFechamento, dataVencimento) = CalcularCicloFatura(
                        fatura.CartaoCredito, proximaCompetencia.Year, proximaCompetencia.Month);

                    var novaFatura = new Fatura
                    {
                        CartaoCreditoId = fatura.CartaoCreditoId,
                        Competencia = proximaCompetencia,
                        DataInicio = dataInicio,
                        DataFechamento = dataFechamento,
                        DataVencimento = dataVencimento,
                        ValorTotal = 0,
                        ValorPago = 0,
                        Status = FaturaStatus.Aberta
                    };

                    _financasDbContext.Fatura.Add(novaFatura);
                }

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Caso ocorra erro ao criar a nova fatura, o fechamento da anterior é cancelado
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Retorna todas as faturas encerradas do usuário.
        /// Considera como encerradas as faturas Pagas e Fechadas.
        /// </summary>
        /// <param name="usuarioId">Identificador do usuário autenticado.</param>
        /// <returns>Lista de faturas encerradas.</returns>
        public async Task<List<FaturaEncerradaDTO>> ListarFaturasEncerradas(int usuarioId)
        {
            return await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .Where(f =>
                    f.CartaoCredito.UsuarioId == usuarioId &&
                    (f.Status == FaturaStatus.Paga ||
                     f.Status == FaturaStatus.Fechada))
                .OrderByDescending(f => f.Competencia)
                .Select(f => new FaturaEncerradaDTO
                {
                    Id = f.Id,
                    CartaoNome = f.CartaoCredito.Nome,
                    DataInicio = f.DataInicio,
                    DataFechamento = f.DataFechamento,
                    DataVencimento = f.DataVencimento,
                    ValorTotal = f.ValorTotal,
                    ValorPago = f.ValorPago,
                    SaldoPendente = f.ValorTotal - f.ValorPago,
                    Status = f.Status.ToString()
                })
                .ToListAsync();
        }
    }
}