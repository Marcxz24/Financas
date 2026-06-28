using Financas.Api.Data;
using Financas.Api.DTOs.IA;
using Financas.Api.DTOs.IA.ContextoFinanceiro;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço orquestrador do módulo de Inteligência Artificial.
    /// Responsabilidades exclusivas deste serviço:
    ///   1. Validar o usuário autenticado.
    ///   2. Carregar o contexto financeiro completo do banco de dados.
    ///   3. Montar o prompt estruturado para o LLM via PromptBuilder interno.
    ///   4. Delegar a comunicação HTTP ao OpenRouterService (baixo acoplamento).
    ///   5. Retornar o resultado encapsulado no RespostaIADTO.
    /// Nenhuma chamada HTTP à OpenRouter ocorre aqui — apenas orquestração.
    /// </summary>
    public class IAService
    {
        private readonly FinancasDbContext _context;
        private readonly OpenRouterService _openRouterService;
        private readonly ILogger<IAService> _logger;

        // ── Constante de prompt de sistema ────────────────────────────────────

        /// <summary>
        /// System Prompt separado do prompt do usuário, seguindo boas práticas de
        /// engenharia de prompt para LLMs. Define o papel, as regras de comportamento
        /// e as restrições do assistente financeiro.
        /// </summary>
        private const string SystemPrompt =
            "Você é um assistente especializado em educação financeira, organização financeira, " +
            "planejamento financeiro, orçamento pessoal, investimentos, controle de gastos, " +
            "cartões de crédito, patrimônio e metas financeiras. " +
            "Responda sempre em português brasileiro, de forma clara, objetiva e acessível. " +
            "Utilize EXCLUSIVAMENTE as informações financeiras fornecidas no contexto abaixo — " +
            "jamais invente dados, valores ou suposições que não estejam presentes. " +
            "Quando os dados forem insuficientes para responder com precisão, oriente o usuário " +
            "sobre quais informações adicionais seriam necessárias. " +
            "Seja empático e motivador, sempre buscando orientar o usuário a melhorar " +
            "sua saúde financeira de forma prática e sustentável.";

        /// <summary>
        /// Construtor com injeção de dependência do DbContext, OpenRouterService e Logger.
        /// </summary>
        /// <param name="context">Contexto do EF Core para acesso ao banco de dados.</param>
        /// <param name="openRouterService">Serviço de comunicação HTTP com a OpenRouter.</param>
        /// <param name="logger">Logger estruturado para rastreabilidade das operações.</param>
        public IAService(
            FinancasDbContext context,
            OpenRouterService openRouterService,
            ILogger<IAService> logger)
        {
            _context = context;
            _openRouterService = openRouterService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        // MÉTODO PÚBLICO PRINCIPAL
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Método principal do módulo de IA. Orquestra todo o fluxo:
        /// validação → carregamento de contexto → montagem de prompt → chamada à IA → resposta.
        /// </summary>
        /// <param name="dto">DTO contendo a pergunta do usuário.</param>
        /// <param name="usuarioId">ID do usuário autenticado extraído do token JWT.</param>
        /// <returns>DTO de resposta com o texto gerado pela IA e metadados.</returns>
        /// <exception cref="KeyNotFoundException">Usuário não encontrado no banco de dados.</exception>
        /// <exception cref="ArgumentException">Pergunta vazia ou inválida.</exception>
        public async Task<RespostaIADTO> ProcessarPerguntaAsync(PerguntaIADTO dto, int usuarioId)
        {
            _logger.LogInformation(
                "[IAService] Iniciando processamento de pergunta para usuário {UsuarioId}.",
                usuarioId);

            // 1. Validação do usuário autenticado
            var usuario = await ValidarUsuarioAsync(usuarioId);

            // 2. Carregamento do contexto financeiro completo via EF Core
            var contexto = await CarregarContextoFinanceiroAsync(usuarioId, usuario.Username);

            _logger.LogInformation(
                "[IAService] Contexto financeiro carregado. Contas: {Contas}, Cartões: {Cartoes}, " +
                "Metas: {Metas}, Lançamentos recentes: {Lancamentos}.",
                contexto.ContasBancarias.Count,
                contexto.CartaoCreditos.Count,
                contexto.Metas.Count,
                contexto.UltimosLancamentos.Count);

            // 3. Montagem do prompt estruturado para o LLM
            var userPrompt = MontarPromptCompleto(contexto, dto.Pergunta);

            // 4. Delegação da chamada HTTP ao OpenRouterService
            var respostaTexto = await _openRouterService.EnviarMensagemAsync(SystemPrompt, userPrompt);

            _logger.LogInformation(
                "[IAService] Resposta da IA recebida com sucesso para usuário {UsuarioId}.",
                usuarioId);

            // 5. Retorno encapsulado no DTO de resposta
            return new RespostaIADTO
            {
                Resposta = respostaTexto,
                PerguntaOriginal = dto.Pergunta,
                GeradoEm = DateTime.UtcNow,
                ModeloUtilizado = _openRouterService.ObterNomeModelo(),
                Sucesso = true
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // CARREGAMENTO DO CONTEXTO FINANCEIRO
        // Dividido em métodos privados menores para facilitar manutenção
        // e futuras expansões do contexto enviado à IA
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Monta o contexto financeiro completo do usuário consultando o banco de dados.
        /// Delega para métodos privados especializados por domínio para manter coesão.
        /// </summary>
        private async Task<ContextoFinanceiroDTO> CarregarContextoFinanceiroAsync(int usuarioId, string nomeUsuario)
        {
            var mesAtual = DateTime.UtcNow.Month;
            var anoAtual = DateTime.UtcNow.Year;

            var contexto = new ContextoFinanceiroDTO
            {
                NomeUsuario = nomeUsuario,
                MesReferencia = mesAtual,
                AnoReferencia = anoAtual
            };

            // Carregamento sequencial dos dados para evitar erro de concorrência no DbContext
            contexto.TotalReceitasMes = await CarregarReceitasMesAsync(usuarioId, mesAtual, anoAtual);
            contexto.TotalDespesasMes = await CarregarDespesasMesAsync(usuarioId, mesAtual, anoAtual);
            contexto.SaldoMensal = contexto.TotalReceitasMes - contexto.TotalDespesasMes;

            var contas = await CarregarContasAsync(usuarioId);
            contexto.ContasBancarias = contas;
            contexto.PatrimonioTotal = contas.Sum(c => c.Saldo);
            contexto.QuantidadeContas = contas.Count;

            var cartoes = await CarregarCartoesAsync(usuarioId);
            contexto.CartaoCreditos = cartoes;
            contexto.TotalFaturasAbertas = cartoes.Sum(c => c.TotalEmAberto);
            contexto.LimiteTotalDisponivel = cartoes.Sum(c => c.LimiteDisponivel);

            var metas = await CarregarMetasAsync(usuarioId);
            contexto.Metas = metas;
            contexto.QuantidadeMetas = metas.Count;
            contexto.MetasEstouradas = metas.Count(m => m.Status == "Estourado");
            contexto.MetasEmAtencao = metas.Count(m => m.Status == "Atenção");

            contexto.UltimosLancamentos = await CarregarUltimosLancamentosAsync(usuarioId);
            contexto.Indicadores = await CarregarIndicadoresAsync(usuarioId, mesAtual, anoAtual);

            return contexto;
        }

        /// <summary>
        /// Carrega o total de receitas do usuário no mês/ano de referência.
        /// Utiliza AsNoTracking para consultas somente leitura (melhor performance).
        /// </summary>
        private async Task<decimal> CarregarReceitasMesAsync(int usuarioId, int mes, int ano)
        {
            return await _context.Lancamentos
                .AsNoTracking()
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Receita &&
                    l.Data.Month == mes &&
                    l.Data.Year == ano)
                .SumAsync(l => (decimal?)l.Valor) ?? 0m;
        }

        /// <summary>
        /// Carrega o total de despesas do usuário no mês/ano de referência.
        /// Utiliza AsNoTracking para consultas somente leitura (melhor performance).
        /// </summary>
        private async Task<decimal> CarregarDespesasMesAsync(int usuarioId, int mes, int ano)
        {
            return await _context.Lancamentos
                .AsNoTracking()
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Despesa &&
                    l.Data.Month == mes &&
                    l.Data.Year == ano)
                .SumAsync(l => (decimal?)l.Valor) ?? 0m;
        }

        /// <summary>
        /// Carrega a lista de contas bancárias do usuário com saldo atual.
        /// </summary>
        private async Task<List<ContaIADTO>> CarregarContasAsync(int usuarioId)
        {
            return await _context.ContasBancarias
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .Select(c => new ContaIADTO
                {
                    Nome = c.Nome,
                    Tipo = c.Tipo.ToString(),
                    Saldo = c.Saldo
                })
                .ToListAsync();
        }

        /// <summary>
        /// Carrega os cartões de crédito do usuário com uso atual e limite disponível.
        /// Calcula o total em aberto somando as faturas com status Aberta ou Fechada
        /// (não pagas) de cada cartão.
        /// </summary>
        private async Task<List<CartaoIADTO>> CarregarCartoesAsync(int usuarioId)
        {
            var cartoes = await _context.CartaoCredito
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();

            var resultado = new List<CartaoIADTO>();

            foreach (var cartao in cartoes)
            {
                // Soma das faturas abertas e fechadas (não pagas) para calcular uso real do crédito
                var totalEmAberto = await _context.Fatura
                    .AsNoTracking()
                    .Where(f =>
                        f.CartaoCreditoId == cartao.Id &&
                        (f.Status == FaturaStatus.Aberta || f.Status == FaturaStatus.Fechada))
                    .SumAsync(f => (decimal?)f.ValorTotal) ?? 0m;

                var limiteDisponivel = cartao.Limite - totalEmAberto;

                resultado.Add(new CartaoIADTO
                {
                    Nome = cartao.Nome,
                    Limite = cartao.Limite,
                    TotalEmAberto = totalEmAberto,
                    LimiteDisponivel = limiteDisponivel < 0 ? 0 : limiteDisponivel,
                    Status = cartao.Status.ToString()
                });
            }

            return resultado;
        }

        /// <summary>
        /// Carrega as metas financeiras ativas do usuário com cálculo de progresso atual.
        /// </summary>
        private async Task<List<MetaIADTO>> CarregarMetasAsync(int usuarioId)
        {
            var hoje = DateTime.UtcNow.Date;

            var metas = await _context.MetasGasto
                .AsNoTracking()
                .Include(m => m.Categoria)
                .Where(m => m.UsuarioId == usuarioId)
                .ToListAsync();

            var lancamentos = await _context.Lancamentos
                .AsNoTracking()
                .Where(l => l.UsuarioId == usuarioId)
                .ToListAsync();

            var resultado = new List<MetaIADTO>();

            foreach (var meta in metas)
            {
                decimal valorAtual;

                if (meta.TipoMeta == TipoMeta.Despesa)
                {
                    // Calcula gastos no período da meta com filtros opcionais
                    var query = lancamentos.Where(l =>
                        l.Tipo == TipoLancamento.Despesa &&
                        l.Data.Date >= meta.DataInicio.Date &&
                        l.Data.Date <= meta.DataFinal.Date);

                    if (meta.CategoriaId.HasValue)
                        query = query.Where(l => l.CategoriaId == meta.CategoriaId.Value);

                    if (meta.CartaoCreditoId.HasValue)
                        query = query.Where(l => l.CartaoCreditoId == meta.CartaoCreditoId.Value);

                    valorAtual = query.Sum(l => l.Valor);
                }
                else
                {
                    // Patrimônio: usa saldo da conta específica ou total do usuário
                    valorAtual = meta.ContaBancariaId.HasValue
                        ? await _context.ContasBancarias
                            .AsNoTracking()
                            .Where(c => c.Id == meta.ContaBancariaId.Value && c.UsuarioId == usuarioId)
                            .Select(c => (decimal?)c.Saldo)
                            .FirstOrDefaultAsync() ?? 0m
                        : await _context.ContasBancarias
                            .AsNoTracking()
                            .Where(c => c.UsuarioId == usuarioId)
                            .SumAsync(c => (decimal?)c.Saldo) ?? 0m;
                }

                var percentual = meta.ValorMeta > 0
                    ? Math.Round((valorAtual / meta.ValorMeta) * 100, 2)
                    : 0m;

                var status = ObterStatusMeta(meta.TipoMeta, percentual);

                resultado.Add(new MetaIADTO
                {
                    Nome = meta.Nome,
                    Tipo = meta.TipoMeta.ToString(),
                    ValorMeta = meta.ValorMeta,
                    ValorAtual = valorAtual,
                    PercentualUtilizado = percentual,
                    Status = status,
                    DataInicio = meta.DataInicio,
                    DataFinal = meta.DataFinal,
                    CategoriaNome = meta.Categoria?.Nome
                });
            }

            return resultado;
        }

        /// <summary>
        /// Carrega os 15 lançamentos mais recentes do usuário para análise de padrões.
        /// Limitado a 15 registros para controlar o consumo de tokens no prompt da IA.
        /// </summary>
        private async Task<List<LancamentoIADTO>> CarregarUltimosLancamentosAsync(int usuarioId)
        {
            return await _context.Lancamentos
                .AsNoTracking()
                .Include(l => l.Categoria)
                .Include(l => l.ContaBancaria)
                .Include(l => l.CartaoCredito)
                .Where(l => l.UsuarioId == usuarioId)
                .OrderByDescending(l => l.Data)
                .Take(15)
                .Select(l => new LancamentoIADTO
                {
                    Descricao = l.Descricao,
                    Valor = l.Valor,
                    Data = l.Data,
                    Tipo = l.Tipo.ToString(),
                    CategoriaNome = l.Categoria != null ? l.Categoria.Nome : null,
                    ContaBancariaNome = l.ContaBancaria != null ? l.ContaBancaria.Nome : null,
                    CartaoCreditoNome = l.CartaoCredito != null ? l.CartaoCredito.Nome : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Carrega indicadores financeiros calculados para enriquecer o contexto da IA.
        /// Inclui médias dos últimos 3 meses, maior categoria de gasto e status de faturas.
        /// </summary>
        private async Task<IndicadoresFinanceirosIADTO> CarregarIndicadoresAsync(
            int usuarioId,
            int mesAtual,
            int anoAtual)
        {
            // Define o período dos últimos 3 meses para cálculo de médias
            var tresAtras = DateTime.UtcNow.AddMonths(-3);

            var mediaReceitas = await _context.Lancamentos
                .AsNoTracking()
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Receita &&
                    l.Data >= tresAtras)
                .GroupBy(l => new { l.Data.Month, l.Data.Year })
                .Select(g => g.Sum(l => l.Valor))
                .AverageAsync(v => (decimal?)v) ?? 0m;

            var mediaDespesas = await _context.Lancamentos
                .AsNoTracking()
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Despesa &&
                    l.Data >= tresAtras)
                .GroupBy(g => new { g.Data.Month, g.Data.Year })
                .Select(g => g.Sum(l => l.Valor))
                .AverageAsync(v => (decimal?)v) ?? 0m;

            // Maior categoria de gasto no mês atual
            var maiorCategoria = await _context.Lancamentos
                .AsNoTracking()
                .Include(l => l.Categoria)
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Despesa &&
                    l.CategoriaId != null &&
                    l.Data.Month == mesAtual &&
                    l.Data.Year == anoAtual)
                .GroupBy(l => l.Categoria!.Nome)
                .Select(g => new { Nome = g.Key, Total = g.Sum(l => l.Valor) })
                .OrderByDescending(g => g.Total)
                .FirstOrDefaultAsync();

            // Faturas atrasadas
            var faturasAtrasadas = await _context.Fatura
                .AsNoTracking()
                .Include(f => f.CartaoCredito)
                .Where(f =>
                    f.CartaoCredito.UsuarioId == usuarioId &&
                    f.Status == FaturaStatus.Atrasada)
                .ToListAsync();

            var percentualGasto = mediaReceitas > 0
                ? Math.Round((mediaDespesas / mediaReceitas) * 100, 2)
                : 0m;

            return new IndicadoresFinanceirosIADTO
            {
                MediaReceitasMensal = Math.Round(mediaReceitas, 2),
                MediaDespesasMensal = Math.Round(mediaDespesas, 2),
                PercentualGastoSobreReceita = percentualGasto,
                MaiorCategoriaGasto = maiorCategoria?.Nome,
                ValorMaiorCategoriaGasto = maiorCategoria?.Total ?? 0m,
                FaturasAtrasadas = faturasAtrasadas.Count,
                ValorTotalFaturasAtrasadas = faturasAtrasadas.Sum(f => f.ValorTotal - f.ValorPago)
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // PROMPT BUILDER
        // Transforma o ContextoFinanceiroDTO em texto estruturado para o LLM
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Monta o prompt completo enviado ao LLM combinando o contexto financeiro
        /// estruturado com a pergunta original do usuário. O texto é otimizado para
        /// consumir o mínimo de tokens possível mantendo toda a informação relevante.
        /// </summary>
        /// <param name="contexto">Contexto financeiro carregado do banco de dados.</param>
        /// <param name="perguntaUsuario">Pergunta original digitada pelo usuário.</param>
        /// <returns>String do prompt completo pronto para envio ao LLM.</returns>
        private string MontarPromptCompleto(ContextoFinanceiroDTO contexto, string perguntaUsuario)
        {
            var sb = new StringBuilder();

            // Cabeçalho de contexto
            sb.AppendLine("=== CONTEXTO FINANCEIRO DO USUÁRIO ===");
            sb.AppendLine($"Usuário: {contexto.NomeUsuario}");
            sb.AppendLine($"Período de referência: {contexto.MesReferencia:D2}/{contexto.AnoReferencia}");
            sb.AppendLine();

            // Resumo mensal
            sb.AppendLine("--- RESUMO DO MÊS ATUAL ---");
            sb.AppendLine($"Receitas: R$ {contexto.TotalReceitasMes:F2}");
            sb.AppendLine($"Despesas: R$ {contexto.TotalDespesasMes:F2}");
            sb.AppendLine($"Saldo mensal: R$ {contexto.SaldoMensal:F2} ({(contexto.SaldoMensal >= 0 ? "positivo" : "negativo")})");
            sb.AppendLine();

            // Patrimônio total
            sb.AppendLine("--- PATRIMÔNIO E CONTAS BANCÁRIAS ---");
            sb.AppendLine($"Patrimônio total (soma de saldos): R$ {contexto.PatrimonioTotal:F2}");
            sb.AppendLine($"Quantidade de contas: {contexto.QuantidadeContas}");

            foreach (var conta in contexto.ContasBancarias)
                sb.AppendLine($"  - {conta.Nome} ({conta.Tipo}): R$ {conta.Saldo:F2}");

            sb.AppendLine();

            // Cartões de crédito
            if (contexto.CartaoCreditos.Any())
            {
                sb.AppendLine("--- CARTÕES DE CRÉDITO ---");
                sb.AppendLine($"Total em faturas abertas/fechadas: R$ {contexto.TotalFaturasAbertas:F2}");
                sb.AppendLine($"Limite total disponível: R$ {contexto.LimiteTotalDisponivel:F2}");

                foreach (var cartao in contexto.CartaoCreditos)
                {
                    sb.AppendLine($"  - {cartao.Nome} ({cartao.Status}): " +
                        $"Limite R$ {cartao.Limite:F2} | " +
                        $"Em aberto R$ {cartao.TotalEmAberto:F2} | " +
                        $"Disponível R$ {cartao.LimiteDisponivel:F2}");
                }

                sb.AppendLine();
            }

            // Metas financeiras
            if (contexto.Metas.Any())
            {
                sb.AppendLine("--- METAS FINANCEIRAS ---");
                sb.AppendLine($"Total de metas: {contexto.QuantidadeMetas} | " +
                    $"Estouradas: {contexto.MetasEstouradas} | " +
                    $"Em atenção: {contexto.MetasEmAtencao}");

                foreach (var meta in contexto.Metas)
                {
                    var categoriaInfo = meta.CategoriaNome != null ? $" | Categoria: {meta.CategoriaNome}" : "";
                    sb.AppendLine($"  - {meta.Nome} ({meta.Tipo}{categoriaInfo}): " +
                        $"Meta R$ {meta.ValorMeta:F2} | " +
                        $"Atual R$ {meta.ValorAtual:F2} | " +
                        $"{meta.PercentualUtilizado:F1}% | {meta.Status} | " +
                        $"Período: {meta.DataInicio:dd/MM/yyyy} a {meta.DataFinal:dd/MM/yyyy}");
                }

                sb.AppendLine();
            }

            // Indicadores financeiros
            sb.AppendLine("--- INDICADORES FINANCEIROS ---");
            sb.AppendLine($"Média de receitas (últimos 3 meses): R$ {contexto.Indicadores.MediaReceitasMensal:F2}");
            sb.AppendLine($"Média de despesas (últimos 3 meses): R$ {contexto.Indicadores.MediaDespesasMensal:F2}");
            sb.AppendLine($"Percentual de gasto sobre receita: {contexto.Indicadores.PercentualGastoSobreReceita:F1}%");

            if (!string.IsNullOrEmpty(contexto.Indicadores.MaiorCategoriaGasto))
                sb.AppendLine($"Maior categoria de gasto no mês: {contexto.Indicadores.MaiorCategoriaGasto} " +
                    $"(R$ {contexto.Indicadores.ValorMaiorCategoriaGasto:F2})");

            if (contexto.Indicadores.FaturasAtrasadas > 0)
                sb.AppendLine($"⚠️  Faturas atrasadas: {contexto.Indicadores.FaturasAtrasadas} " +
                    $"(Total: R$ {contexto.Indicadores.ValorTotalFaturasAtrasadas:F2})");

            sb.AppendLine();

            // Lançamentos recentes
            if (contexto.UltimosLancamentos.Any())
            {
                sb.AppendLine("--- ÚLTIMOS LANÇAMENTOS ---");

                foreach (var lanc in contexto.UltimosLancamentos)
                {
                    var origem = lanc.CartaoCreditoNome != null
                        ? $"Cartão: {lanc.CartaoCreditoNome}"
                        : lanc.ContaBancariaNome != null
                            ? $"Conta: {lanc.ContaBancariaNome}"
                            : "Sem vínculo";

                    var categoriaInfo = lanc.CategoriaNome != null
                        ? $" | {lanc.CategoriaNome}"
                        : "";

                    sb.AppendLine($"  [{lanc.Data:dd/MM}] {lanc.Tipo.ToUpper()} R$ {lanc.Valor:F2} — {lanc.Descricao}{categoriaInfo} ({origem})");
                }

                sb.AppendLine();
            }

            // Pergunta do usuário
            sb.AppendLine("=== PERGUNTA DO USUÁRIO ===");
            sb.AppendLine(perguntaUsuario);

            return sb.ToString();
        }

        // ════════════════════════════════════════════════════════════════════
        // HELPERS PRIVADOS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se o usuário existe no banco de dados.
        /// </summary>
        /// <param name="usuarioId">ID do usuário autenticado.</param>
        /// <returns>Entidade do usuário encontrada.</returns>
        /// <exception cref="KeyNotFoundException">Usuário não encontrado.</exception>
        private async Task<Entities.Usuario> ValidarUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                _logger.LogWarning(
                    "[IAService] Tentativa de acesso com usuário inexistente. UsuarioId: {UsuarioId}",
                    usuarioId);

                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            return usuario;
        }

        /// <summary>
        /// Determina o status textual de uma meta com base no tipo e percentual atingido.
        /// Regras idênticas às do MetaGastoService para garantir consistência.
        /// </summary>
        private static string ObterStatusMeta(TipoMeta tipoMeta, decimal percentual)
        {
            return tipoMeta switch
            {
                TipoMeta.Despesa when percentual >= 100m => "Estourado",
                TipoMeta.Despesa when percentual >= 80m => "Atenção",
                TipoMeta.Despesa => "Dentro do limite",
                TipoMeta.Receita when percentual >= 100m => "Meta atingida",
                TipoMeta.Receita when percentual >= 80m => "Próximo da meta",
                TipoMeta.Receita => "Em andamento",
                _ => "Indefinido"
            };
        }
    }
}
