namespace Financas.Api.DTOs.Cofrinho
{
    /// <summary>
    /// DTO utilizado para retornar o saldo atual de um cofrinho.
    /// Contém apenas as informações necessárias para consultas rápidas
    /// de saldo, evitando o envio de todos os dados da entidade.
    /// </summary>
    public class SaldoCofrinhoDTO
    {
        /// <summary>
        /// Identificador único do cofrinho consultado.
        /// Utilizado para relacionar o saldo retornado ao respectivo registro.
        /// </summary>
        public int CofrinhoId { get; set; }

        /// <summary>
        /// Saldo financeiro atualmente disponível no cofrinho.
        /// O valor é atualizado conforme operações de depósito e resgate.
        /// </summary>
        public decimal Saldo { get; set; }
    }
}
