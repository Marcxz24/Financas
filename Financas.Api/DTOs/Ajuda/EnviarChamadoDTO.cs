using Microsoft.OpenApi.MicrosoftExtensions;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Ajuda
{
    // A classe "EnviarChamadoDTO" é um Data Transfer Object (DTO) que representa os dados necessários para enviar um chamado de ajuda.
    public class EnviarChamadoDTO
    {
        // O campo "Assunto" é obrigatório e deve conter um valor não vazio.
        [Required(ErrorMessage = "O Assunto é Obrigatório.")]
        // O Campo "Assunto" deve conter no mínimo 5 caracteres para garantir que seja suficientemente descritivo.
        [MinLength(5, ErrorMessage = "O Assunto deve conter no mínimo 5 caracteres.")]
        public string Assunto { get; set; } = string.Empty;

        // O campo "Descrição" é obrigatório e deve conter um valor não vazio.
        [Required(ErrorMessage = "A Descrição é obrigatória.")]
        // O Campo "Descrição" deve conter no mínimo 10 caracteres para garantir que seja suficientemente detalhado.
        [MinLength(10, ErrorMessage = "A Descrição deve conter no mínimo 10 caracteres.")]
        public string Descricao { get; set; } = string.Empty;
    }
}
