using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Cofrinho
{
    /// <summary>
    /// DTO utilizado para o cadastro de um novo cofrinho.
    /// Contém as informações mínimas necessárias para sua criação.
    /// </summary>
    public class CriarCofrinhoDTO
    {
        /// <summary>
        /// Nome que será atribuído ao cofrinho.
        /// Deve ser informado obrigatoriamente e possuir no máximo 100 caracteres.
        /// </summary>
        [Required(ErrorMessage = "O nome do cofrinho é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome do cofrinho não pode ultrapassar 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Saldo inicial do cofrinho.
        /// Permite apenas valores iguais ou superiores a zero.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "O saldo inicial deve ser zero ou positivo.")]
        public decimal Saldo { get; set; }
    }
}
