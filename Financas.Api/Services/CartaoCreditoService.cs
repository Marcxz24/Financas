using Financas.Api.Data;
using Financas.Api.DTOs.CartaoCredito;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    // Classe de serviço para o Cartão de Crédito, responsável por realizar as operações de CRUD
    public class CartaoCreditoService
    {
        // Injeção de dependência do contexto do banco de dados para acessar os dados relacionados ao Cartão de Crédito
        private readonly FinancasDbContext _FinancasDbContext;

        // Construtor para injetar o contexto do banco de dados
        public CartaoCreditoService(FinancasDbContext financasDbContext)
        {
            _FinancasDbContext = financasDbContext;
        }

        /// <summary>
        /// Métodos Responsáveis para realizar as operações de CRUD para o Cartão de Crédito
        /// </summary>

        // Método para criar um novo Cartão de Crédito, recebendo os dados necessários através do DTO e o ID do usuário para associar o cartão ao usuário correto
        public async Task<CartaoCreditoResponseDTO> CriarCartaoCredito(CriarCartaoCreditoDTO dto, int usuarioId)
        {
            // 1. Validação de usuário
            var usuario = await _FinancasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            // 2. Regra de negócio
            if (dto.Limite <= 0)
                throw new ArgumentException("O limite do cartão deve ser maior que zero.");

            // 3. Criação da entidade
            var cartao = new CartaoCredito
            {
                UsuarioId = usuarioId,
                Nome = dto.Nome,
                Limite = dto.Limite,
                DiaFechamento = dto.DiaFechamento,
                DiaVencimento = dto.DiaVencimento,
                Status = StatusCartaoCredito.Ativo
            };

            // 4. Persistência
            _FinancasDbContext.CartaoCredito.Add(cartao);
            await _FinancasDbContext.SaveChangesAsync();

            // 5. Response
            return new CartaoCreditoResponseDTO
            {
                Id = cartao.Id,
                UsuarioId = usuarioId,
                Nome = cartao.Nome,
                Limite = cartao.Limite,
                DiaFechamento = cartao.DiaFechamento,
                DiaVencimento = cartao.DiaVencimento,
                Status = cartao.Status.ToString()
            };
        }

        /// <summary>
        /// Listar os cartões de crédito de um usuário específico, filtrando por status (Ativo/Inativo) e ordenando pela data de fechamento mais próxima.
        /// </summary>

        // Método para listar os cartões de crédito de um usuário específico, filtrando por status (Ativo/Inativo) e ordenando pela data de fechamento mais próxima
        public async Task<List<CartaoCreditoResponseDTO>> GetCartaoCredito(int usuarioId)
        {
            // 1. Consulta ao Banco de Dados: O método inicia realizando uma consulta ao banco de dados para recuperar todos os cartões de crédito associados ao ID do usuário fornecido. A consulta é feita usando o Entity Framework Core, filtrando os cartões pelo ID do usuário e convertendo o resultado em uma lista assíncrona
            var cartoes = await _FinancasDbContext.CartaoCredito
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();

            var response = new List<CartaoCreditoResponseDTO>();

            // 2. Processamento dos Dados: Itera sobre a lista de cartões encontrados para calcular métricas financeiras de cada um, como limite utilizado e disponível
            foreach (var cartao in cartoes)
            {
                // 3. Cálculo de Saldo em Aberto: Invoca o método auxiliar ObterTotalEmAberto para somar os valores das faturas correntes vinculadas ao cartão
                var limiteUtilizado = await ObterTotalEmAberto(cartao.Id, usuarioId);

                // 4. Determinação de Limite Disponível: Subtrai o valor já comprometido (faturas em aberto) do limite total definido no cartão
                var limiteDisponivel = cartao.Limite - limiteUtilizado;

                // 5. Cálculo de Percentual de Utilização: Calcula a proporção do limite utilizada, tratando o caso de divisão por zero caso o limite seja nulo
                var percentualUtilizado = cartao.Limite == 0
                    ? 0
                    : Math.Round((limiteUtilizado / cartao.Limite) * 100, 2);

                // 6. Mapeamento para DTO: Constrói o objeto de resposta (Data Transfer Object) com as informações calculadas para envio ao front-end
                response.Add(new CartaoCreditoResponseDTO
                {
                    Id = cartao.Id,
                    UsuarioId = cartao.UsuarioId,
                    Nome = cartao.Nome,
                    Limite = cartao.Limite,
                    LimiteUtilizado = limiteUtilizado,
                    LimiteDisponivel = limiteDisponivel,
                    PercentualUtilizado = percentualUtilizado,
                    DiaFechamento = cartao.DiaFechamento,
                    DiaVencimento = cartao.DiaVencimento,
                    Status = cartao.Status.ToString()
                });
            }

            // 7. Retorno: Entrega a lista completa de DTOs mapeados, contendo o estado atual de todos os cartões do usuário
            return response;
        }

        /// <summary>
        /// Método responsável por atualizar o status de um cartão de crédito específico, permitindo ativar ou desativar o cartão conforme necessário.
        /// </summary>

        // Método para atualizar o status de um cartão de crédito específico, permitindo ativar ou desativar o cartão conforme necessário
        public async Task<CartaoCreditoResponseDTO> AtualizarCartaoCredito(
        AtualizarCartaoCreditoDTO dto,
        int cartaoId,
        int usuarioId)
        {
            // 1. Busca
            var cartao = await _FinancasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId);

            if (cartao == null)
                throw new Exception("Cartão não encontrado");

            // 2. Autorização
            if (cartao.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("Cartão não pertence ao usuário");

            // 3. Regras de atualização
            if (dto.Nome != null)
                cartao.Nome = dto.Nome;

            if (dto.Limite.HasValue)
            {
                if (dto.Limite.Value <= 0)
                    throw new ArgumentException("Limite deve ser maior que zero.");

                cartao.Limite = dto.Limite.Value;
            }

            if (dto.Status.HasValue)
                cartao.Status = dto.Status.Value;

            if (dto.DiaFechamento.HasValue)
                cartao.DiaFechamento = dto.DiaFechamento.Value;

            if (dto.DiaVencimento.HasValue)
                cartao.DiaVencimento = dto.DiaVencimento.Value;

            // 4. Persistência
            await _FinancasDbContext.SaveChangesAsync();

            // 5. Response
            return new CartaoCreditoResponseDTO
            {
                Id = cartao.Id,
                UsuarioId = usuarioId,
                Nome = cartao.Nome,
                Limite = cartao.Limite,
                DiaFechamento = cartao.DiaFechamento,
                DiaVencimento = cartao.DiaVencimento,
                Status = cartao.Status.ToString()
            };
        }

        /// <summary>
        /// Método responsável por excluir um cartão de crédito específico, removendo-o do banco de dados e garantindo que ele não possa mais ser acessado ou utilizado.
        /// </summary>
        /// <param name="cartaoId"></param>
        /// <param name="usuarioId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>

        // Método para excluir um cartão de crédito específico, removendo-o do banco de dados e garantindo que ele não possa mais ser acessado ou utilizado
        public async Task DeletarCartao(int cartaoId, int usuarioId)
        {
            var cartao = await _FinancasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId);

            if (cartao == null)
                throw new Exception("Cartão não encontrado");

            if (cartao.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("Cartão não pertence ao usuário");

            _FinancasDbContext.CartaoCredito.Remove(cartao);
            await _FinancasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Recupera o limite total configurado para um cartão de crédito específico.
        /// </summary>
        /// <param name="cartaoId">Identificador único do cartão.</param>
        /// <param name="usuarioId">Identificador do usuário para garantir a segurança do acesso.</param>
        /// <returns>O valor decimal correspondente ao limite total do cartão.</returns>
        /// <exception cref="KeyNotFoundException">Lançada caso o cartão não exista ou não pertença ao usuário informado.</exception>
        public async Task<decimal> ObterLimiteCartao(int cartaoId, int usuarioId)
        {
            // Realiza a busca no banco de dados aplicando o filtro de segurança por UsuarioId
            var cartao = await _FinancasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.UsuarioId == usuarioId);

            // Validação de existência: essencial para evitar erros de referência nula ao tentar acessar .Limite
            if (cartao == null)
                throw new KeyNotFoundException("Cartão não encontrado");

            return cartao.Limite;
        }

        /// <summary>
        /// Calcula o valor total pendente de pagamento somando todas as faturas que não estão quitadas (Abertas ou Fechadas).
        /// Realiza a subtração entre o valor total da fatura e o que já foi pago.
        /// </summary>
        /// <param name="cartaoId">Identificador do cartão de crédito.</param>
        /// <param name="usuarioId">Identificador do usuário para validação de segurança.</param>
        /// <returns>O saldo devedor total (ValorTotal - ValorPago) de todas as faturas pendentes.</returns>
        public async Task<decimal> ObterTotalEmAberto(int cartaoId, int usuarioId)
        {
            // Realiza a soma de forma otimizada no banco de dados
            var totalEmAberto = await _FinancasDbContext.Fatura
                .AsNoTracking() // Melhora a performance ao não rastrear as entidades para edição
                .Where(f => f.CartaoCreditoId == cartaoId
                    && f.CartaoCredito.UsuarioId == usuarioId // Garante que o cartão pertence ao usuário logado
                    && (f.Status == FaturaStatus.Aberta || f.Status == FaturaStatus.Fechada))
                .Select(f => (decimal?)(f.ValorTotal - f.ValorPago)) // Calcula a diferença individual de cada fatura
                .SumAsync();

            // Se não houver faturas, retorna 0 em vez de nulo
            return totalEmAberto ?? 0;
        }

        /// <summary>
        /// Realiza a validação de crédito para uma nova compra.
        /// Verifica se o valor da transação ultrapassa o limite disponível (Limite Total - Dívida Acumulada).
        /// </summary>
        /// <param name="cartaoId">Identificador do cartão de crédito.</param>
        /// <param name="usuarioId">Identificador do usuário para validação de segurança.</param>
        /// <param name="valorCompra">Valor da nova despesa que se deseja lançar.</param>
        /// <exception cref="ArgumentException">Lançada se o valor da compra for zero ou negativo.</exception>
        /// <exception cref="InvalidOperationException">Lançada se o valor da compra for maior que o limite disponível.</exception>
        public async Task ValidarLimite(int cartaoId, int usuarioId, decimal valorCompra)
        {
            // Validação básica de entrada para evitar processamento desnecessário
            if (valorCompra <= 0)
                throw new ArgumentException("O Valor deve ser maior que zero.");

            // Recupera os valores necessários utilizando os métodos auxiliares já existentes
            var limite = await ObterLimiteCartao(cartaoId, usuarioId);
            var totalEmAberto = await ObterTotalEmAberto(cartaoId, usuarioId);

            // O limite disponível é o que sobra após subtrair todas as faturas não pagas
            var limiteDisponivel = limite - totalEmAberto;

            // Bloqueia a transação caso o saldo do cartão seja insuficiente
            if (valorCompra > limiteDisponivel)
                throw new InvalidOperationException("Limite do cartão insuficiente.");
        }
    }
}
