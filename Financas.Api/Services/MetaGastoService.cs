using Financas.Api.Data;
using Financas.Api.DTOs.Metas;
using Financas.Api.DTOs.MetasGasto;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável por todo o ciclo de vida das metas financeiras do usuário.
    /// Centraliza regras de negócio, validações e cálculo de progresso tanto para metas de despesa quanto patrimônio.
    /// </summary>
    public class MetaGastoService
    {
        private readonly FinancasDbContext _financasDbcontext;

        public MetaGastoService(FinancasDbContext context)
        {
            _financasDbcontext = context;
        }

        /// <summary>
        /// Cria uma nova meta financeira respeitando regras de consistência por tipo de meta.
        /// Realiza validação de período, usuário e vínculos antes de persistir.
        /// </summary>
        public async Task<MetaGastoResponseDTO> CriarMetaGasto(CriarMetaGastoDTO dto, int usuarioId)
        {
            ValidarPeriodo(dto.DataInicio, dto.DataFinal);
            await ValidarUsuario(usuarioId);
            await ValidarRelacionamentos(dto.TipoMeta, dto.CategoriaId, dto.CartaoCreditoId, dto.ContaBancariaId, usuarioId);

            var meta = new MetasGasto
            {
                Nome = dto.Nome,
                TipoMeta = dto.TipoMeta,
                ValorMeta = dto.ValorMeta,
                DataInicio = dto.DataInicio.Date,
                DataFinal = dto.DataFinal.Date,
                UsuarioId = usuarioId,
                CategoriaId = dto.TipoMeta == TipoMeta.Despesa ? dto.CategoriaId : null,
                CartaoCreditoId = dto.TipoMeta == TipoMeta.Despesa ? dto.CartaoCreditoId : null,
                ContaBancariaId = dto.TipoMeta == TipoMeta.Receita ? dto.ContaBancariaId : null,
            };

            _financasDbcontext.MetasGasto.Add(meta);
            await _financasDbcontext.SaveChangesAsync();

            return await MontarResponse(meta, usuarioId);
        }

        /// <summary>
        /// Recupera uma meta específica garantindo isolamento por usuário.
        /// Retorna a meta já enriquecida com valores calculados e nomes dos vínculos.
        /// </summary>
        public async Task<MetaGastoResponseDTO> GetMetaGastoPorId(int id, int usuarioId)
        {
            var meta = await _financasDbcontext.MetasGasto
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId)
                ?? throw new KeyNotFoundException("Meta não encontrada.");

            return await MontarResponse(meta, usuarioId);
        }

        /// <summary>
        /// Lista todas as metas do usuário com cálculo de progresso individual.
        /// Utiliza carregamento único de lançamentos para evitar consultas repetidas durante o cálculo.
        /// </summary>
        public async Task<List<MetaGastoListagemDTO>> GetMetasGasto(int usuarioId)
        {
            var metas = await _financasDbcontext.MetasGasto
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();

            var lancamentos = await ObterLancamentos(usuarioId);

            var tasks = metas.Select(async meta =>
            {
                var valorAtual = await CalcularValorAtualAsync(meta, lancamentos, usuarioId);
                var percentual = CalcularPercentual(meta.ValorMeta, valorAtual);

                return new MetaGastoListagemDTO
                {
                    Id = meta.Id,
                    Nome = meta.Nome,
                    ValorMeta = meta.ValorMeta,
                    TipoMeta = meta.TipoMeta,
                    ValorAtual = valorAtual,
                    PercentualUtilizado = percentual,
                    DataInicio = meta.DataInicio,
                    DataFinal = meta.DataFinal,
                    Status = ObterStatus(meta.TipoMeta, percentual),
                    ContaBancariaId = meta.ContaBancariaId
                };
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// Retorna uma visão agregada das metas sem detalhamento individual.
        /// Usado principalmente para dashboards e cards de resumo.
        /// </summary>
        public async Task<List<MetaGastoResumoDTO>> GetResumoMetasGasto(int usuarioId)
        {
            var metas = await _financasDbcontext.MetasGasto
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();

            var lancamentos = await ObterLancamentos(usuarioId);

            var tasks = metas.Select(async meta =>
            {
                var valorAtual = await CalcularValorAtualAsync(meta, lancamentos, usuarioId);
                var percentual = CalcularPercentual(meta.ValorMeta, valorAtual);

                return new MetaGastoResumoDTO
                {
                    Id = meta.Id,
                    Nome = meta.Nome,
                    ValorMeta = meta.ValorMeta,
                    TipoMeta = meta.TipoMeta,
                    ValorAtual = valorAtual,
                    PercentualUtilizado = percentual,
                    Status = ObterStatus(meta.TipoMeta, percentual)
                };
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// Atualiza uma meta existente mantendo regras de consistência por tipo.
        /// Campos incompatíveis com o tipo atual da meta são automaticamente invalidados.
        /// </summary>
        public async Task<MetaGastoResponseDTO> AtualizarMetaGasto(AtualizarMetaGastoDTO dto, int id, int usuarioId)
        {
            ValidarPeriodo(dto.DataInicio, dto.DataFinal);

            var meta = await _financasDbcontext.MetasGasto
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId)
                ?? throw new KeyNotFoundException("Meta não encontrada.");

            await ValidarRelacionamentos(dto.TipoMeta, dto.CategoriaId, dto.CartaoCreditoId, dto.ContaBancariaId, usuarioId);

            meta.Nome = dto.Nome;
            meta.TipoMeta = dto.TipoMeta;
            meta.ValorMeta = dto.ValorMeta;
            meta.DataInicio = dto.DataInicio.Date;
            meta.DataFinal = dto.DataFinal.Date;
            meta.CategoriaId = dto.TipoMeta == TipoMeta.Despesa ? dto.CategoriaId : null;
            meta.CartaoCreditoId = dto.TipoMeta == TipoMeta.Despesa ? dto.CartaoCreditoId : null;
            meta.ContaBancariaId = dto.TipoMeta == TipoMeta.Receita ? dto.ContaBancariaId : null;

            await _financasDbcontext.SaveChangesAsync();

            return await MontarResponse(meta, usuarioId);
        }

        /// <summary>
        /// Remove uma meta respeitando isolamento por usuário.
        /// A exclusão é física e não mantém histórico.
        /// </summary>
        public async Task DeletarMetaGasto(int id, int usuarioId)
        {
            var meta = await _financasDbcontext.MetasGasto
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId)
                ?? throw new KeyNotFoundException("Meta não encontrada.");

            _financasDbcontext.MetasGasto.Remove(meta);
            await _financasDbcontext.SaveChangesAsync();
        }

        /// <summary>
        /// Monta o DTO completo de resposta da meta incluindo cálculo de progresso e enriquecimento de nomes.
        /// Centraliza toda lógica de apresentação evitando duplicação entre endpoints.
        /// </summary>
        public async Task<MetaGastoResponseDTO> MontarResponse(MetasGasto meta, int usuarioId)
        {
            var lancamentos = await ObterLancamentos(usuarioId);
            var valorAtual = await CalcularValorAtualAsync(meta, lancamentos, usuarioId);
            var percentual = CalcularPercentual(meta.ValorMeta, valorAtual);

            var response = new MetaGastoResponseDTO
            {
                Id = meta.Id,
                Nome = meta.Nome,
                ValorMeta = meta.ValorMeta,
                TipoMeta = meta.TipoMeta,
                ValorAtual = valorAtual,
                PercentualUtilizado = percentual,
                Status = ObterStatus(meta.TipoMeta, percentual),
                DataInicio = meta.DataInicio,
                DataFinal = meta.DataFinal,
                CategoriaId = meta.CategoriaId,
                CartaoCreditoId = meta.CartaoCreditoId,
                ContaBancariaId = meta.ContaBancariaId,
            };

            if (meta.CategoriaId.HasValue)
                response.CategoriaNome = await ObterNomeCategoria(meta.CategoriaId.Value);

            if (meta.CartaoCreditoId.HasValue)
                response.CartaoNome = await ObterNomeCartao(meta.CartaoCreditoId.Value);

            if (meta.ContaBancariaId.HasValue)
                response.ContaNome = await ObterNomeConta(meta.ContaBancariaId.Value);

            return response;
        }

        /// <summary>
        /// Calcula o progresso da meta direcionando para a regra correta conforme o tipo.
        /// Despesa utiliza lançamentos e Patrimônio utiliza saldo de contas bancárias.
        /// </summary>
        private async Task<decimal> CalcularValorAtualAsync(
            MetasGasto meta,
            List<Lancamento> lancamentos,
            int usuarioId)
        {
            return meta.TipoMeta switch
            {
                TipoMeta.Despesa => CalcularGastoPorPeriodo(meta, lancamentos),
                TipoMeta.Receita => await CalcularPatrimonioAsync(meta, usuarioId),
                _ => 0m
            };
        }

        /// <summary>
        /// Soma os gastos dentro do período da meta aplicando filtros opcionais de categoria e cartão.
        /// Opera exclusivamente sobre dados já carregados em memória.
        /// </summary>
        private decimal CalcularGastoPorPeriodo(MetasGasto meta, List<Lancamento> lancamentos)
        {
            var query = lancamentos.Where(x =>
                x.Tipo == TipoLancamento.Despesa &&
                x.Data >= meta.DataInicio &&
                x.Data <= meta.DataFinal);

            if (meta.CategoriaId.HasValue)
                query = query.Where(x => x.CategoriaId == meta.CategoriaId.Value);

            if (meta.CartaoCreditoId.HasValue)
                query = query.Where(x => x.CartaoCreditoId == meta.CartaoCreditoId.Value);

            return query.Sum(x => x.Valor);
        }

        /// <summary>
        /// Calcula patrimônio com base em contas bancárias.
        /// Pode ser uma conta específica ou soma total do usuário.
        /// </summary>
        private async Task<decimal> CalcularPatrimonioAsync(MetasGasto meta, int usuarioId)
        {
            if (meta.ContaBancariaId.HasValue)
            {
                var saldo = await _financasDbcontext.ContasBancarias
                    .Where(c => c.Id == meta.ContaBancariaId.Value && c.UsuarioId == usuarioId)
                    .Select(c => (decimal?)c.Saldo)
                    .FirstOrDefaultAsync();

                return saldo ?? 0m;
            }

            var totalContas = await _financasDbcontext.ContasBancarias
                .Where(c => c.UsuarioId == usuarioId)
                .SumAsync(c => (decimal?)c.Saldo);

            return totalContas ?? 0m;
        }

        /// <summary>
        /// Calcula percentual de progresso da meta com proteção contra divisão por zero.
        /// </summary>
        private decimal CalcularPercentual(decimal valorMeta, decimal valorAtual)
        {
            if (valorMeta == 0m) return 0m;
            return Math.Round((valorAtual / valorMeta) * 100, 2);
        }

        /// <summary>
        /// Define o status da meta com base no tipo e no percentual atingido.
        /// Regras diferentes são aplicadas para despesa e patrimônio.
        /// </summary>
        private string ObterStatus(TipoMeta tipoMeta, decimal percentual)
        {
            if (tipoMeta == TipoMeta.Despesa)
            {
                if (percentual >= 100m) return "Estourado";
                if (percentual >= 80m) return "Atenção";
                return "Dentro do limite";
            }

            if (tipoMeta == TipoMeta.Receita)
            {
                if (percentual >= 100m) return "Meta atingida";
                if (percentual >= 80m) return "Próximo da meta";
                return "Em andamento";
            }

            return "Indefinido";
        }

        /// <summary>
        /// Carrega todos os lançamentos do usuário para cálculo em memória.
        /// Estratégia usada para evitar N+1 queries durante processamento das metas.
        /// </summary>
        private async Task<List<Lancamento>> ObterLancamentos(int usuarioId)
        {
            return await _financasDbcontext.Lancamentos
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();
        }

        /// <summary>
        /// Busca o nome de uma categoria pelo identificador.
        /// </summary>
        private async Task<string?> ObterNomeCategoria(int categoriaId)
        {
            return await _financasDbcontext.Categorias
                .Where(c => c.Id == categoriaId)
                .Select(c => c.Nome)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Busca o nome de um cartão de crédito pelo identificador.
        /// </summary>
        private async Task<string?> ObterNomeCartao(int cartaoId)
        {
            return await _financasDbcontext.CartaoCredito
                .Where(c => c.Id == cartaoId)
                .Select(c => c.Nome)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Busca o nome de uma conta bancária pelo identificador.
        /// </summary>
        private async Task<string?> ObterNomeConta(int contaId)
        {
            return await _financasDbcontext.ContasBancarias
                .Where(c => c.Id == contaId)
                .Select(c => c.Nome)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Valida existência do usuário antes de permitir operações de escrita.
        /// </summary>
        private async Task ValidarUsuario(int usuarioId)
        {
            var existe = await _financasDbcontext.Usuarios.AnyAsync(x => x.Id == usuarioId);
            if (!existe)
                throw new KeyNotFoundException("Usuário não encontrado.");
        }

        /// <summary>
        /// Valida se os relacionamentos informados pertencem ao usuário e são compatíveis com o tipo de meta.
        /// </summary>
        private async Task ValidarRelacionamentos(
            TipoMeta tipoMeta,
            int? categoriaId,
            int? cartaoCreditoId,
            int? contaBancariaId,
            int usuarioId)
        {
            if (tipoMeta == TipoMeta.Despesa)
            {
                if (categoriaId.HasValue)
                {
                    var ok = await _financasDbcontext.Categorias
                        .AnyAsync(x => x.Id == categoriaId && x.UsuarioId == usuarioId);
                    if (!ok) throw new KeyNotFoundException("Categoria não encontrada.");
                }

                if (cartaoCreditoId.HasValue)
                {
                    var ok = await _financasDbcontext.CartaoCredito
                        .AnyAsync(x => x.Id == cartaoCreditoId && x.UsuarioId == usuarioId);
                    if (!ok) throw new KeyNotFoundException("Cartão não encontrado.");
                }
            }

            if (tipoMeta == TipoMeta.Receita && contaBancariaId.HasValue)
            {
                var ok = await _financasDbcontext.ContasBancarias
                    .AnyAsync(x => x.Id == contaBancariaId && x.UsuarioId == usuarioId);
                if (!ok) throw new KeyNotFoundException("Conta bancária não encontrada.");
            }
        }

        /// <summary>
        /// Valida consistência do período da meta.
        /// Garante que o início não seja maior que o fim antes de persistir dados.
        /// </summary>
        private void ValidarPeriodo(DateTime inicio, DateTime fim)
        {
            if (inicio.Date > fim.Date)
                throw new InvalidOperationException("A data de início não pode ser maior que a data final.");
        }
    }
}