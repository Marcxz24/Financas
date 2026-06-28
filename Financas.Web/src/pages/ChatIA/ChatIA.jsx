import { useState } from "react";
import api from "../../services/api";
import "./ChatIA.css";

const ChatIA = () => {
  // Estados para gerenciar o texto da pergunta, a resposta da IA e o estado de carregamento
  const [pergunta, setPergunta] = useState("");
  const [resposta, setResposta] = useState("");
  const [loading, setLoading] = useState(false);

  // Função para converter marcações de Markdown (**) em elementos negrito (<strong>) do React
  const formatarTexto = (texto) => {
    return texto.split(/(\*\*.*?\*\*)/g).map((parte, index) => {
      // Verifica se o trecho do texto está entre asteriscos duplos
      if (parte.startsWith("**") && parte.endsWith("**")) {
        return <strong key={index}>{parte.slice(2, -2)}</strong>;
      }
      // Retorna o texto normal caso não seja negrito
      return parte;
    });
  };

  // Função disparada ao clicar no botão para enviar a pergunta ao back-end
  const enviarPergunta = async () => {
    // Impede envio de perguntas vazias
    if (!pergunta.trim()) return;

    setLoading(true); // Ativa o estado de carregamento para desabilitar o botão
    try {
      // Realiza a requisição POST para a API com o objeto esperado pelo back-end
      const response = await api.post("/ia/perguntar", { Pergunta: pergunta });
      setResposta(response.data.resposta); // Atualiza a resposta com o retorno da API
    } catch (error) {
      // --- Tratamento de erros para depuração no console do navegador ---
      if (error.response) {
        console.error(
          "Dados do erro (o que a API disse):",
          error.response.data,
        );
        console.error("Status do erro:", error.response.status);
      } else {
        console.error("Erro na requisição:", error.message);
      }
      // ------------------------------------------
      setResposta("Erro na requisição. Verifique o console.");
    } finally {
      setLoading(false); // Desativa o estado de carregamento após a tentativa
    }
  };

  return (
    // Container principal do componente
    <div className="chat-ia-container">
      <h3>🤖 Assistente Financeiro IA</h3>
      {/* Campo de entrada para o usuário digitar a pergunta */}
      <textarea
        value={pergunta}
        onChange={(e) => setPergunta(e.target.value)}
        placeholder="Ex: Analise minhas despesas e me dê uma opinião."
      />
      {/* Botão de envio, desabilitado durante o carregamento */}
      <button onClick={enviarPergunta} disabled={loading}>
        {loading ? "Pensando..." : "Enviar Pergunta"}
      </button>

      {/* Exibe a caixa de resposta apenas se houver uma resposta disponível */}
      {resposta && (
        <div className="resposta-box">
          {/* Renderiza o texto formatado com suporte a negrito */}
          <p>{formatarTexto(resposta)}</p>
        </div>
      )}
    </div>
  );
};

export default ChatIA;
