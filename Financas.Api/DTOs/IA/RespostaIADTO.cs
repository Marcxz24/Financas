namespace Financas.Api.DTOs.IA
{
    /// <summary>
    /// DTO de saída retornado ao cliente (Front-end) após o processamento
    /// da pergunta pelo módulo de Inteligência Artificial.
    /// Encapsula a resposta da IA junto a metadados de rastreabilidade.
    /// O Front-end jamais acessa a OpenRouter diretamente — toda comunicação
    /// ocorre exclusivamente por este contrato de retorno da API.
    /// </summary>
    public class RespostaIADTO
    {
        /// <summary>
        /// Resposta textual gerada pelo modelo de IA em português brasileiro.
        /// Contém análise financeira personalizada baseada no contexto do usuário.
        /// </summary>
        public string Resposta { get; set; } = string.Empty;

        /// <summary>
        /// Pergunta original enviada pelo usuário, retornada para confirmação
        /// e para facilitar a exibição em histórico de conversas no Front-end.
        /// </summary>
        public string PerguntaOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora (UTC) em que a resposta foi gerada pelo servidor.
        /// Útil para ordenação e exibição temporal no histórico de conversas.
        /// </summary>
        public DateTime GeradoEm { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Identificador do modelo de IA utilizado na geração da resposta
        /// (ex: "google/gemini-flash-1.5"). Útil para rastreabilidade e debug.
        /// </summary>
        public string ModeloUtilizado { get; set; } = string.Empty;

        /// <summary>
        /// Indica se a resposta foi gerada com sucesso ou se ocorreu algum problema
        /// durante a comunicação com a OpenRouter. Permite ao Front-end diferenciar
        /// respostas válidas de mensagens de erro amigáveis.
        /// </summary>
        public bool Sucesso { get; set; } = true;
    }
}
