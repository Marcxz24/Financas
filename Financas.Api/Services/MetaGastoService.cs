using Financas.Api.Data;
using Financas.Api.DTOs.Metas;
using Financas.Api.DTOs.MetasGasto;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    public class MetaGastoService
    {
        // Contexto do EF Core responsável por todas as operações no banco
        private readonly FinancasDbContext _financasDbcontext;

        public MetaGastoService(FinancasDbContext context)
        {
            _financasDbcontext = context;
        }

        public async Task<MetaGastoResponseDTO> CriarMetaGasto(CriarMetaGastoDTO dto, int usuarioId)
        {
            // Impede criação de meta com intervalo inválido de datas
            ValidarPeriodo(dto.DataInicio, dto.DataFinal);

            // Garante que o usuário existe e que os relacionamentos informados são válidos
            await ValidarUsuario(usuarioId);
            await ValidarRelacionamentos(dto, usuarioId);

            // Mapeamento direto do DTO para entidade de persistência
            var meta = new MetasGasto
            {
                Nome = dto.Nome,
                CategoriaId = dto.CategoriaId,
                CartaoCreditoId = dto.CartaoCreditoId,
                ValorMeta = dto.ValorMeta,
                TipoMeta = dto.TipoMeta,
                DataInicio = dto.DataInicio.Date,
                DataFinal = dto.DataFinal.Date,
                UsuarioId = usuarioId
            };

            _financasDbcontext.MetasGasto.Add(meta);
            await _financasDbcontext.SaveChangesAsync();

            // Retorna resposta já com cálculos de progresso aplicados
            return await MontarResponse(meta, usuarioId);
        }

        public async Task<List<MetaGastoListagemDTO>> GetMetasGasto(int usuarioId)
        {
            // Busca todas as metas do usuário sem aplicar filtros adicionais
            var metas = await _financasDbcontext.MetasGasto
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();

            // Carrega todos os lançamentos de uma vez para evitar N+1 queries
            var lancamentos = await ObterLancamentos(usuarioId);

            // Para cada meta, calcula gasto acumulado e percentual de uso
            return metas.Select(meta =>
            {
                var gasto = CalcularValorAcumulado(meta, lancamentos);
                var percentual = CalcularPercentual(meta.ValorMeta, gasto);

                return new MetaGastoListagemDTO
                {
                    Id = meta.Id,
                    Nome = meta.Nome,
                    ValorMeta = meta.ValorMeta,
                    TipoMeta = meta.TipoMeta,
                    ValorGastoAtual = gasto,
                    PercentualUtilizado = percentual,
                    DataInicio = meta.DataInicio,
                    DataFinal = meta.DataFinal,
                    Status = ObterStatus(meta.TipoMeta, percentual)
                };
            }).ToList();
        }

        public async Task<List<MetaGastoResumoDTO>> GetResumoMetasGasto(int usuarioId)
        {
            // Versão simplificada da listagem, sem informações de período
            var metas = await _financasDbcontext.MetasGasto
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();

            var lancamentos = await ObterLancamentos(usuarioId);

            return metas.Select(meta =>
            {
                var gasto = CalcularValorAcumulado(meta, lancamentos);
                var percentual = CalcularPercentual(meta.ValorMeta, gasto);

                return new MetaGastoResumoDTO
                {
                    Id = meta.Id,
                    Nome = meta.Nome,
                    ValorMeta = meta.ValorMeta,
                    TipoMeta = meta.TipoMeta,
                    ValorGastoAtual = gasto,
                    PercentualUtilizado = percentual,
                    Status = ObterStatus(meta.TipoMeta, percentual)
                };
            }).ToList();
        }

        public async Task<MetaGastoResponseDTO> AtualizarMetaGasto(AtualizarMetaGastoDTO dto, int id, int usuarioId)
        {
            // Valida novamente o intervalo de datas antes de persistir alterações
            ValidarPeriodo(dto.DataInicio, dto.DataFinal);

            // Garante que a meta pertence ao usuário autenticado
            var meta = await _financasDbcontext.MetasGasto
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);

            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            // Revalida os relacionamentos para evitar inconsistência após update
            await ValidarRelacionamentos(dto, usuarioId);

            // Atualização direta da entidade rastreada pelo EF Core
            meta.Nome = dto.Nome;
            meta.CategoriaId = dto.CategoriaId;
            meta.CartaoCreditoId = dto.CartaoCreditoId;
            meta.ValorMeta = dto.ValorMeta;
            meta.TipoMeta = dto.TipoMeta;
            meta.DataInicio = dto.DataInicio.Date;
            meta.DataFinal = dto.DataFinal.Date;

            await _financasDbcontext.SaveChangesAsync();

            return await MontarResponse(meta, usuarioId);
        }

        public async Task DeletarMetaGasto(int id, int usuarioId)
        {
            // Busca garantindo isolamento por usuário
            var meta = await _financasDbcontext.MetasGasto
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);

            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            _financasDbcontext.MetasGasto.Remove(meta);
            await _financasDbcontext.SaveChangesAsync();
        }

        public async Task<MetaGastoResponseDTO> MontarResponse(MetasGasto meta, int usuarioId)
        {
            // Reutiliza lançamentos para calcular progresso da meta
            var lancamentos = await ObterLancamentos(usuarioId);

            var valorAcumulado = CalcularValorAcumulado(meta, lancamentos);
            var percentual = CalcularPercentual(meta.ValorMeta, valorAcumulado);

            // DTO final já pronto para consumo da API/UI
            return new MetaGastoResponseDTO
            {
                Id = meta.Id,
                Nome = meta.Nome,
                ValorMeta = meta.ValorMeta,
                ValorGastoAtual = valorAcumulado,
                DataInicio = meta.DataInicio,
                DataFinal = meta.DataFinal,
                CategoriaId = meta.CategoriaId,
                CartaoCreditoId = meta.CartaoCreditoId,
                TipoMeta = meta.TipoMeta,
                PercentualUtilizado = percentual,
                Status = ObterStatus(meta.TipoMeta, percentual)
            };
        }

        private decimal CalcularValorAcumulado(MetasGasto meta, List<Lancamento> lancamentos)
        {
            // Define qual tipo de lançamento entra no cálculo da meta
            var tipoLancamento = meta.TipoMeta == TipoMeta.Despesa
                ? TipoLancamento.Despesa
                : TipoLancamento.Receita;

            // Filtra apenas lançamentos dentro do período da meta
            var query = lancamentos.Where(x =>
                x.Tipo == tipoLancamento &&
                x.Data >= meta.DataInicio &&
                x.Data <= meta.DataFinal
            );

            // Aplica filtro por categoria caso a meta seja segmentada
            if (meta.CategoriaId.HasValue)
                query = query.Where(x => x.CategoriaId == meta.CategoriaId.Value);

            // Aplica filtro por cartão caso a meta esteja vinculada a um cartão específico
            if (meta.CartaoCreditoId.HasValue)
                query = query.Where(x => x.CartaoCreditoId == meta.CartaoCreditoId.Value);

            return query.Sum(x => x.Valor);
        }

        private decimal CalcularPercentual(decimal valorMeta, decimal valorAtual)
        {
            // Evita divisão por zero ao calcular progresso
            return valorMeta == 0 ? 0 : (valorAtual / valorMeta) * 100;
        }

        private string ObterStatus(TipoMeta tipoMeta, decimal percentual)
        {
            // Regras de status variam conforme o tipo da meta (receita ou despesa)
            if (tipoMeta == TipoMeta.Despesa)
            {
                if (percentual >= 100) return "Estourado";
                if (percentual >= 80) return "Atenção";
                return "Dentro do limite";
            }

            if (tipoMeta == TipoMeta.Receita)
            {
                if (percentual >= 100) return "Meta atingida";
                if (percentual >= 80) return "Próximo da meta";
                return "Em andamento";
            }

            return "Indefinido";
        }

        private async Task<List<Lancamento>> ObterLancamentos(int usuarioId)
        {
            // Centraliza a busca para evitar duplicação de queries em múltiplos métodos
            return await _financasDbcontext.Lancamentos
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();
        }

        private async Task ValidarUsuario(int usuarioId)
        {
            // Garante que o usuário existe antes de permitir qualquer operação
            var existe = await _financasDbcontext.Usuarios.AnyAsync(x => x.Id == usuarioId);

            if (!existe)
                throw new KeyNotFoundException("Usuário não encontrado.");
        }

        private async Task ValidarRelacionamentos(CriarMetaGastoDTO dto, int usuarioId)
        {
            // Validação de categoria vinculada à meta
            if (dto.CategoriaId.HasValue)
            {
                var ok = await _financasDbcontext.Categorias
                    .AnyAsync(x => x.Id == dto.CategoriaId && x.UsuarioId == usuarioId);

                if (!ok)
                    throw new KeyNotFoundException("Categoria não encontrada.");
            }

            // Validação de cartão vinculado à meta
            if (dto.CartaoCreditoId.HasValue)
            {
                var ok = await _financasDbcontext.CartaoCredito
                    .AnyAsync(x => x.Id == dto.CartaoCreditoId && x.UsuarioId == usuarioId);

                if (!ok)
                    throw new KeyNotFoundException("Cartão não encontrado.");
            }
        }

        private async Task ValidarRelacionamentos(AtualizarMetaGastoDTO dto, int usuarioId)
        {
            if (dto.CategoriaId.HasValue)
            {
                var ok = await _financasDbcontext.Categorias
                    .AnyAsync(x => x.Id == dto.CategoriaId && x.UsuarioId == usuarioId);

                if (!ok)
                    throw new KeyNotFoundException("Categoria não encontrada.");
            }

            if (dto.CartaoCreditoId.HasValue)
            {
                var ok = await _financasDbcontext.CartaoCredito
                    .AnyAsync(x => x.Id == dto.CartaoCreditoId && x.UsuarioId == usuarioId);

                if (!ok)
                    throw new KeyNotFoundException("Cartão não encontrado.");
            }
        }

        private void ValidarPeriodo(DateTime inicio, DateTime fim)
        {
            // Evita criação de metas com intervalo inválido
            if (inicio.Date > fim.Date)
                throw new InvalidOperationException("Data de início não pode ser maior que a data final.");
        }
    }
}