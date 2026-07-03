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
    ///   2. Detectar o escopo temporal da pergunta (mensal ou comparativo) e
    ///      aplicar um filtro rígido de período em todo o carregamento de dados.
    ///   3. Carregar o contexto financeiro do banco de dados, estritamente
    ///      limitado ao escopo detectado (nunca dados de fora do período).
    ///   4. Montar o prompt estruturado para o LLM via PromptBuilder interno,
    ///      separando claramente "período atual" de "referência histórica".
    ///   5. Delegar a comunicação HTTP ao GoogleGeminiService (baixo acoplamento).
    ///   6. Retornar o resultado encapsulado no RespostaIADTO.
    /// Nenhuma chamada HTTP ao Gemini ocorre aqui — apenas orquestração.
    ///
    /// GARANTIA DE ESCOPO TEMPORAL (contrato deste serviço):
    ///   - O período principal de análise é sempre um único mês/ano.
    ///   - Dados de outros meses só entram no prompt como um resumo agregado
    ///     e compacto (no máximo 3 meses), e apenas quando a pergunta indicar
    ///     explicitamente uma análise comparativa/trimestral.
    ///   - "Últimos lançamentos" NUNCA extrapola o mês/ano de referência —
    ///     não existe mais busca "global" por lançamentos recentes.
    /// </summary>
    public class IAService
    {
        private readonly FinancasDbContext _context;
        private readonly GoogleGeminiService _googleGeminiService;
        private readonly ILogger<IAService> _logger;

        // ── Constante de prompt de sistema ────────────────────────────────────

        /// <summary>
        /// System Prompt separado do prompt do usuário, seguindo boas práticas de
        /// engenharia de prompt para LLMs. Define o papel, as regras de comportamento
        /// e as restrições do assistente financeiro — com ênfase reforçada em nunca
        /// misturar períodos, mesmo quando dados históricos estiverem presentes no
        /// contexto para fins de comparação.
        /// </summary>
        private const string SystemPrompt =
            "Você é um assistente especialista em educação financeira e organização financeira. " +
            "Responda sempre em português brasileiro. " +
            "Baseie-se EXCLUSIVAMENTE nos dados fornecidos na requisição atual, sem usar contexto " +
            "externo, memória de conversas anteriores ou qualquer conhecimento não presente no texto enviado. " +
            "O bloco 'DADOS DO PERÍODO ATUAL' é o escopo OBRIGATÓRIO e principal da análise. " +
            "Se, e somente se, a pergunta do usuário pedir explicitamente uma comparação entre meses, " +
            "trimestre ou evolução ao longo do tempo, você pode usar também o bloco 'REFERÊNCIA HISTÓRICA' " +
            "— mas apenas os valores agregados exatamente como fornecidos, nunca invente lançamentos, " +
            "categorias, meses ou valores que não estejam explicitamente listados. " +
            "Nunca trate dados de um período como se pertencessem a outro. " +
            "Não infira ou assuma dados de períodos não presentes no contexto. Não assuma valores ausentes. " +
            "Se o escopo da pergunta não estiver claro ou os dados forem insuficientes, informe isso claramente. " +
            "Responda apenas o que foi solicitado, sem expandir a análise para outros períodos. " +
            "Sempre apresente a resposta em três tópicos: Problemas, Pontos positivos e Recomendações práticas.";

        /// <summary>
        /// Limite máximo de meses considerados em qualquer análise comparativa/
        /// histórica. Este é o teto rígido citado nos requisitos de escopo
        /// temporal — nenhum método de carregamento pode ultrapassá-lo.
        /// </summary>
        private const int MaxMesesComparativo = 3;

        /// <summary>
        /// Palavras-chave (em minúsculas, sem acentuação especial tratada à parte)
        /// que indicam que o usuário pediu uma análise comparativa/trimestral em
        /// vez de uma análise de um único mês. Mantida como lista simples e
        /// auditável — qualquer novo termo deve ser adicionado aqui.
        /// </summary>
        private static readonly string[] PalavrasChaveComparativo =
        {
            "trimestre", "trimestral",
            "últimos 3 meses", "ultimos 3 meses",
            "3 últimos meses", "3 ultimos meses",
            "últimos meses", "ultimos meses",
            "vários meses", "varios meses",
            "comparar", "comparativo", "comparação", "comparacao",
            "evolução", "evolucao", "evoluiu", "evoluir",
            "ao longo do tempo", "tendência", "tendencia"
        };

        /// <summary>
        /// Tipo de escopo temporal identificado a partir da pergunta do usuário.
        /// </summary>
        private enum TipoEscopoTemporal
        {
            /// <summary>Análise restrita a um único mês/ano (padrão).</summary>
            Mensal,

            /// <summary>Análise comparativa entre até <see cref="MaxMesesComparativo"/> meses.</summary>
            Comparativo
        }

        /// <summary>
        /// Representa o escopo temporal já resolvido para a requisição atual:
        /// o tipo de análise, o mês/ano principal de referência, e quantos meses
        /// de referência histórica (0 a <see cref="MaxMesesComparativo"/>) devem
        /// ser carregados como contexto adicional.
        /// </summary>
        private sealed record EscopoTemporalIA(
            TipoEscopoTemporal Tipo,
            int MesReferencia,
            int AnoReferencia,
            int QuantidadeMesesHistorico);

        /// <summary>
        /// Construtor com injeção de dependência do DbContext, GoogleGeminiService e Logger.
        /// </summary>
        /// <param name="context">Contexto do EF Core para acesso ao banco de dados.</param>
        /// <param name="googleGeminiService">Serviço de comunicação HTTP com o Gemini.</param>
        /// <param name="logger">Logger estruturado para rastreabilidade das operações.</param>
        public IAService(
            FinancasDbContext context,
            GoogleGeminiService googleGeminiService,
            ILogger<IAService> logger)
        {
            _context = context;
            _googleGeminiService = googleGeminiService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        // MÉTODO PÚBLICO PRINCIPAL
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Método principal do módulo de IA. Orquestra todo o fluxo:
        /// validação → detecção de escopo temporal → carregamento de contexto
        /// (limitado ao escopo) → montagem de prompt → chamada à IA → resposta.
        /// Assinatura pública inalterada.
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

            // 2. Detecção do escopo temporal a partir da pergunta (mensal x comparativo)
            var escopo = DetectarEscopoTemporal(dto.Pergunta);

            _logger.LogInformation(
                "[IAService] Escopo temporal detectado: {Tipo} | Período de referência: {Mes:D2}/{Ano} | " +
                "Meses de referência histórica: {QuantidadeMeses}.",
                escopo.Tipo, escopo.MesReferencia, escopo.AnoReferencia, escopo.QuantidadeMesesHistorico);

            // 3. Carregamento do contexto financeiro, estritamente limitado ao escopo
            var contexto = await CarregarContextoFinanceiroAsync(usuarioId, usuario.Username, escopo);

            _logger.LogInformation(
                "[IAService] Contexto financeiro carregado. Contas: {Contas}, Cartões: {Cartoes}, " +
                "Metas: {Metas}, Lançamentos do período: {Lancamentos}.",
                contexto.ContasBancarias.Count,
                contexto.CartaoCreditos.Count,
                contexto.Metas.Count,
                contexto.UltimosLancamentos.Count);

            // 4. Montagem do prompt estruturado para o LLM, com separação clara
            //    entre dados do período atual e referência histórica (se houver)
            var userPrompt = MontarPromptCompleto(contexto, dto.Pergunta, escopo);

            // 5. Delegação da chamada HTTP ao GoogleGeminiService
            var respostaTexto = await _googleGeminiService.EnviarMensagemAsync(SystemPrompt, userPrompt);

            _logger.LogInformation(
                "[IAService] Resposta da IA recebida com sucesso para usuário {UsuarioId}.",
                usuarioId);

            // 6. Retorno encapsulado no DTO de resposta
            return new RespostaIADTO
            {
                Resposta = respostaTexto,
                PerguntaOriginal = dto.Pergunta,
                GeradoEm = DateTime.Now,
                ModeloUtilizado = _googleGeminiService.ObterNomeModelo(),
                Sucesso = true
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // DETECÇÃO DE ESCOPO TEMPORAL
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Analisa a pergunta do usuário para identificar se a análise solicitada
        /// é mensal (padrão, um único mês) ou comparativa (trimestral/múltiplos
        /// meses). O mês/ano de referência principal é sempre o mês/ano corrente
        /// — este serviço não recebe um período explícito no DTO de entrada,
        /// então "o período solicitado" é sempre ancorado no momento da pergunta.
        ///
        /// Quando a análise é comparativa, o número de meses de referência
        /// histórica é sempre limitado a <see cref="MaxMesesComparativo"/>,
        /// independentemente do que a pergunta sugira (ex.: "últimos 6 meses"
        /// ainda assim é limitado a 3, para conter custo e evitar respostas
        /// baseadas em janelas temporais grandes demais).
        /// </summary>
        private static EscopoTemporalIA DetectarEscopoTemporal(string pergunta)
        {
            var perguntaNormalizada = (pergunta ?? string.Empty).Trim().ToLowerInvariant();

            // Extrai mês e ano baseados na string (ex: "junho")
            var (mes, ano) = ExtrairMesAnoDaPergunta(perguntaNormalizada);

            var ehComparativo = PalavrasChaveComparativo
                .Any(palavraChave => perguntaNormalizada.Contains(palavraChave));

            return ehComparativo
                ? new EscopoTemporalIA(TipoEscopoTemporal.Comparativo, mes, ano, MaxMesesComparativo)
                : new EscopoTemporalIA(TipoEscopoTemporal.Mensal, mes, ano, 0);
        }

        // ════════════════════════════════════════════════════════════════════
        // CARREGAMENTO DO CONTEXTO FINANCEIRO
        // Dividido em métodos privados menores para facilitar manutenção
        // e futuras expansões do contexto enviado à IA
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Monta o contexto financeiro do usuário consultando o banco de dados,
        /// estritamente limitado ao escopo temporal já detectado. Delega para
        /// métodos privados especializados por domínio para manter coesão.
        /// </summary>
        private async Task<ContextoFinanceiroDTO> CarregarContextoFinanceiroAsync(
            int usuarioId, string nomeUsuario, EscopoTemporalIA escopo)
        {
            var mesReferencia = escopo.MesReferencia;
            var anoReferencia = escopo.AnoReferencia;

            var contexto = new ContextoFinanceiroDTO
            {
                NomeUsuario = nomeUsuario,
                MesReferencia = mesReferencia,
                AnoReferencia = anoReferencia
            };

            // Carregamento sequencial dos dados para evitar erro de concorrência no DbContext
            contexto.TotalReceitasMes = await CarregarReceitasMesAsync(usuarioId, mesReferencia, anoReferencia);
            contexto.TotalDespesasMes = await CarregarDespesasMesAsync(usuarioId, mesReferencia, anoReferencia);
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

            // CORREÇÃO DE ESCOPO: sempre filtrado por mês/ano do contexto —
            // nunca mais uma busca "global" pelos lançamentos mais recentes.
            contexto.UltimosLancamentos = await CarregarUltimosLancamentosAsync(
                usuarioId, mesReferencia, anoReferencia);

            // Indicadores históricos (médias) só são calculados quando o escopo
            // detectado é comparativo — caso contrário, o custo dessas consultas
            // é evitado e a IA não recebe nenhum dado de outros meses.
            contexto.Indicadores = await CarregarIndicadoresAsync(
                usuarioId,
                mesReferencia,
                anoReferencia,
                incluirReferenciaHistorica: escopo.Tipo == TipoEscopoTemporal.Comparativo);

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
        /// Saldo atual é um dado de estado (não histórico de período), por isso
        /// permanece fora do filtro de mês/ano.
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
        /// (não pagas) de cada cartão. Assim como as contas, é um dado de estado atual.
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
                    // Calcula gastos no período da própria meta (datas da meta, não do escopo da pergunta)
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
        /// Carrega os lançamentos do usuário SEMPRE filtrados pelo mês/ano do
        /// contexto (escopo de referência da pergunta) — nunca uma busca global
        /// pelos "mais recentes". Esta é a correção central de vazamento de
        /// período: antes desta mudança, este método podia retornar lançamentos
        /// de meses diferentes do período que o usuário perguntou, contaminando
        /// a análise da IA. Limitado a 15 registros dentro do próprio mês para
        /// controlar o consumo de tokens.
        /// </summary>
        private async Task<List<LancamentoIADTO>> CarregarUltimosLancamentosAsync(int usuarioId, int mes, int ano)
        {
            return await _context.Lancamentos
                .AsNoTracking()
                .Include(l => l.Categoria)
                .Include(l => l.ContaBancaria)
                .Include(l => l.CartaoCredito)
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Data.Month == mes &&
                    l.Data.Year == ano)
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
        /// Carrega os indicadores financeiros enviados à IA. A parte histórica
        /// (médias de receitas/despesas) só é calculada quando
        /// <paramref name="incluirReferenciaHistorica"/> é verdadeiro — evitando
        /// tanto o custo da consulta quanto o vazamento de dados de outros meses
        /// em perguntas puramente mensais. A janela histórica, quando calculada,
        /// é sempre ancorada no mês/ano de referência (não em DateTime.Now) e
        /// estritamente limitada a <see cref="MaxMesesComparativo"/> meses.
        ///
        /// A maior categoria de gasto e as faturas atrasadas são dados do
        /// estado atual/mês de referência, não histórico — por isso continuam
        /// sendo sempre calculados.
        /// </summary>
        private async Task<IndicadoresFinanceirosIADTO> CarregarIndicadoresAsync(
            int usuarioId,
            int mesReferencia,
            int anoReferencia,
            bool incluirReferenciaHistorica)
        {
            var mediaReceitas = 0m;
            var mediaDespesas = 0m;
            var percentualGasto = 0m;

            if (incluirReferenciaHistorica)
            {
                var inicioMesReferencia = new DateTime(anoReferencia, mesReferencia, 1);
                var inicioJanela = inicioMesReferencia.AddMonths(-(MaxMesesComparativo - 1));
                var fimJanelaExclusivo = inicioMesReferencia.AddMonths(1);

                mediaReceitas = await _context.Lancamentos
                    .AsNoTracking()
                    .Where(l =>
                        l.UsuarioId == usuarioId &&
                        l.Tipo == TipoLancamento.Receita &&
                        l.Data >= inicioJanela &&
                        l.Data < fimJanelaExclusivo)
                    .GroupBy(l => new { l.Data.Month, l.Data.Year })
                    .Select(g => g.Sum(l => l.Valor))
                    .AverageAsync(v => (decimal?)v) ?? 0m;

                mediaDespesas = await _context.Lancamentos
                    .AsNoTracking()
                    .Where(l =>
                        l.UsuarioId == usuarioId &&
                        l.Tipo == TipoLancamento.Despesa &&
                        l.Data >= inicioJanela &&
                        l.Data < fimJanelaExclusivo)
                    .GroupBy(g => new { g.Data.Month, g.Data.Year })
                    .Select(g => g.Sum(l => l.Valor))
                    .AverageAsync(v => (decimal?)v) ?? 0m;

                percentualGasto = mediaReceitas > 0
                    ? Math.Round((mediaDespesas / mediaReceitas) * 100, 2)
                    : 0m;
            }

            // Maior categoria de gasto — sempre restrita ao mês/ano de referência,
            // nunca a outros meses, independentemente do tipo de escopo.
            var maiorCategoria = await _context.Lancamentos
                .AsNoTracking()
                .Include(l => l.Categoria)
                .Where(l =>
                    l.UsuarioId == usuarioId &&
                    l.Tipo == TipoLancamento.Despesa &&
                    l.CategoriaId != null &&
                    l.Data.Month == mesReferencia &&
                    l.Data.Year == anoReferencia)
                .GroupBy(l => l.Categoria!.Nome)
                .Select(g => new { Nome = g.Key, Total = g.Sum(l => l.Valor) })
                .OrderByDescending(g => g.Total)
                .FirstOrDefaultAsync();

            // Faturas atrasadas — reflete o status atual do usuário, não é um
            // recorte temporal de um período específico.
            var faturasAtrasadas = await _context.Fatura
                .AsNoTracking()
                .Include(f => f.CartaoCredito)
                .Where(f =>
                    f.CartaoCredito.UsuarioId == usuarioId &&
                    f.Status == FaturaStatus.Atrasada)
                .ToListAsync();

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
        /// estruturado com a pergunta original do usuário. O texto separa
        /// explicitamente os "DADOS DO PERÍODO ATUAL" (escopo obrigatório) de uma
        /// eventual "REFERÊNCIA HISTÓRICA" (opcional, só presente em análises
        /// comparativas), reduzindo tokens ao omitir por completo o bloco
        /// histórico quando ele não é necessário.
        /// </summary>
        /// <param name="contexto">Contexto financeiro carregado do banco de dados.</param>
        /// <param name="perguntaUsuario">Pergunta original digitada pelo usuário.</param>
        /// <param name="escopo">Escopo temporal já resolvido para esta requisição.</param>
        /// <returns>String do prompt completo pronto para envio ao LLM.</returns>
        private string MontarPromptCompleto(
            ContextoFinanceiroDTO contexto, string perguntaUsuario, EscopoTemporalIA escopo)
        {
            var sb = new StringBuilder();

            var tipoAnaliseDescricao = escopo.Tipo == TipoEscopoTemporal.Comparativo
                ? "Comparativa (referência de até 3 meses)"
                : "Mensal (um único mês)";

            sb.AppendLine("=== CONTEXTO FINANCEIRO ===");
            sb.AppendLine($"Usuário: {contexto.NomeUsuario}");
            sb.AppendLine($"Tipo de análise detectado: {tipoAnaliseDescricao}");
            sb.AppendLine($"Período principal (obrigatório) de análise: {contexto.MesReferencia:D2}/{contexto.AnoReferencia}");
            sb.AppendLine("Tudo em 'DADOS DO PERÍODO ATUAL' pertence exclusivamente a este período.");
            sb.AppendLine();

            // ── DADOS DO PERÍODO ATUAL (escopo principal e obrigatório) ──────
            sb.AppendLine("=== DADOS DO PERÍODO ATUAL ===");

            sb.AppendLine("--- RESUMO DO PERÍODO ---");
            sb.AppendLine($"Receitas: R$ {contexto.TotalReceitasMes:F2}");
            sb.AppendLine($"Despesas: R$ {contexto.TotalDespesasMes:F2}");
            sb.AppendLine($"Saldo: R$ {contexto.SaldoMensal:F2} ({(contexto.SaldoMensal >= 0 ? "positivo" : "negativo")})");
            sb.AppendLine();

            sb.AppendLine("--- PATRIMÔNIO E CONTAS (ESTADO ATUAL) ---");
            sb.AppendLine($"Patrimônio total: R$ {contexto.PatrimonioTotal:F2}");
            sb.AppendLine($"Quantidade de contas: {contexto.QuantidadeContas}");

            foreach (var conta in contexto.ContasBancarias)
                sb.AppendLine($"- {conta.Nome} ({conta.Tipo}): R$ {conta.Saldo:F2}");

            sb.AppendLine();

            if (contexto.CartaoCreditos.Any())
            {
                sb.AppendLine("--- CARTÕES DE CRÉDITO (ESTADO ATUAL) ---");
                sb.AppendLine($"Faturas abertas/fechadas: R$ {contexto.TotalFaturasAbertas:F2}");
                sb.AppendLine($"Limite total disponível: R$ {contexto.LimiteTotalDisponivel:F2}");

                foreach (var cartao in contexto.CartaoCreditos)
                {
                    sb.AppendLine($"- {cartao.Nome} ({cartao.Status}) | Limite: R$ {cartao.Limite:F2} | " +
                        $"Em aberto: R$ {cartao.TotalEmAberto:F2} | Disponível: R$ {cartao.LimiteDisponivel:F2}");
                }

                sb.AppendLine();
            }

            if (contexto.Metas.Any())
            {
                sb.AppendLine("--- METAS FINANCEIRAS ---");
                sb.AppendLine($"Total: {contexto.QuantidadeMetas} | Estouradas: {contexto.MetasEstouradas} | " +
                    $"Em atenção: {contexto.MetasEmAtencao}");

                foreach (var meta in contexto.Metas)
                {
                    var categoriaInfo = meta.CategoriaNome != null ? $" | Categoria: {meta.CategoriaNome}" : "";

                    sb.AppendLine($"- {meta.Nome} ({meta.Tipo}{categoriaInfo}) | Meta: R$ {meta.ValorMeta:F2} | " +
                        $"Atual: R$ {meta.ValorAtual:F2} | {meta.PercentualUtilizado:F1}% | {meta.Status}");
                }

                sb.AppendLine();
            }

            var possuiIndicadorAtual = !string.IsNullOrEmpty(contexto.Indicadores.MaiorCategoriaGasto)
                || contexto.Indicadores.FaturasAtrasadas > 0;

            if (possuiIndicadorAtual)
            {
                sb.AppendLine("--- INDICADORES DO PERÍODO ATUAL ---");

                if (!string.IsNullOrEmpty(contexto.Indicadores.MaiorCategoriaGasto))
                    sb.AppendLine($"Maior gasto do período: {contexto.Indicadores.MaiorCategoriaGasto} " +
                        $"(R$ {contexto.Indicadores.ValorMaiorCategoriaGasto:F2})");

                if (contexto.Indicadores.FaturasAtrasadas > 0)
                    sb.AppendLine($"Faturas atrasadas (status atual): {contexto.Indicadores.FaturasAtrasadas} " +
                        $"(R$ {contexto.Indicadores.ValorTotalFaturasAtrasadas:F2})");

                sb.AppendLine();
            }

            if (contexto.UltimosLancamentos.Any())
            {
                sb.AppendLine("--- LANÇAMENTOS DO PERÍODO ATUAL (mesmo mês/ano acima) ---");

                foreach (var lanc in contexto.UltimosLancamentos)
                {
                    var origem = lanc.CartaoCreditoNome != null
                        ? $"Cartão: {lanc.CartaoCreditoNome}"
                        : lanc.ContaBancariaNome != null
                            ? $"Conta: {lanc.ContaBancariaNome}"
                            : "Sem vínculo";

                    var categoriaInfo = lanc.CategoriaNome != null ? $" | {lanc.CategoriaNome}" : "";

                    sb.AppendLine($"[{lanc.Data:dd/MM}] {lanc.Tipo.ToUpper()} R$ {lanc.Valor:F2} - {lanc.Descricao}{categoriaInfo} ({origem})");
                }

                sb.AppendLine();
            }

            // ── REFERÊNCIA HISTÓRICA (opcional, só em análises comparativas) ─
            // Bloco inteiro omitido do prompt quando o escopo é mensal — isso
            // reduz tokens e elimina qualquer risco de a IA misturar períodos
            // em perguntas que não pediram comparação.
            if (escopo.Tipo == TipoEscopoTemporal.Comparativo)
            {
                sb.AppendLine($"=== REFERÊNCIA HISTÓRICA (até {MaxMesesComparativo} meses, incluindo o período atual) ===");
                sb.AppendLine("Use este bloco SOMENTE porque a pergunta pediu comparação/evolução entre períodos.");
                sb.AppendLine("São valores agregados (médias) — não invente lançamentos ou meses não listados aqui.");
                sb.AppendLine($"Média de receitas: R$ {contexto.Indicadores.MediaReceitasMensal:F2}");
                sb.AppendLine($"Média de despesas: R$ {contexto.Indicadores.MediaDespesasMensal:F2}");
                sb.AppendLine($"Gasto médio sobre receita: {contexto.Indicadores.PercentualGastoSobreReceita:F1}%");
                sb.AppendLine();
            }

            // ── PERGUNTA DO USUÁRIO ───────────────────────────────────────────
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

        private static (int Mes, int Ano) ExtrairMesAnoDaPergunta(string pergunta)
        {
            var agora = DateTime.Now;
            var mes = agora.Month;
            var ano = agora.Year;

            // Dicionário simples para converter texto em mês
            var mesesMap = new Dictionary<string, int>
            {
                {"janeiro", 1}, {"fevereiro", 2}, {"marco", 3}, {"março", 3},
                {"abril", 4}, {"maio", 5}, {"junho", 6}, {"julho", 7},
                {"agosto", 8}, {"setembro", 9}, {"outubro", 10}, {"novembro", 11}, {"dezembro", 12}
            };

            foreach (var item in mesesMap)
            {
                if (pergunta.Contains(item.Key))
                {
                    mes = item.Value;
                    // Se o usuário pedir um mês menor que o atual, assume que é o ano atual.
                    // Em uma implementação robusta, você poderia buscar o ano na string também.
                    break;
                }
            }
            return (mes, ano);
        }
    }
}