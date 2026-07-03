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
    // Divide o texto em linhas para preservar a estrutura de tópicos
    return texto.split("\n").map((linha, i) => {
      // 1. Verifica se é um título (começa com ###)
      if (linha.startsWith("###")) {
        return (
          <h4 key={i} style={{ marginTop: "15px", color: "#3b82f6" }}>
            {linha.replace("###", "").trim()}
          </h4>
        );
      }

      // 2. Processa o restante da linha (negrito e texto comum)
      // Usamos split para separar o que está entre **
      const partes = linha.split(/(\*\*.*?\*\*)/g);

      return (
        <p key={i} style={{ marginBottom: "8px" }}>
          {partes.map((parte, j) => {
            if (parte.startsWith("**") && parte.endsWith("**")) {
              return <strong key={j}>{parte.slice(2, -2)}</strong>;
            }
            return parte;
          })}
        </p>
      );
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

      {/* Bloco de ajuda estilizado */}
      <div
        className="ia-info-box"
        style={{
          fontSize: "0.9rem",
          color: "#555",
          marginBottom: "15px",
          padding: "10px",
          background: "#f9f9f9",
          borderRadius: "8px",
        }}
      >
        <h4>
          ❓<strong>Como usar:</strong> Nossa IA analisa seu cenário atual antes
          de responder.
        </h4>
        <br />
        <ul style={{ margin: "5px 0", paddingLeft: "20px" }}>
          <li>
            <strong>Foco total:</strong> Não precisa de perguntas complexas. Uma
            simples saudação (como "Oi" ou "Bom dia") já ativa nossa análise
            completa, comparando suas receitas, gastos e metas do período atual.
          </li>
          <br />
          <li>
            <strong>Seja específico:</strong> Você pode ser direto para obter
            respostas focadas. Pergunte coisas como "Onde posso cortar gastos?",
            "Como estão minhas metas de economia?" ou "Tenho saldo para novas
            compras?".
          </li>
          <br />
          <li>
            <strong>Análise Estruturada:</strong> Para facilitar sua tomada de decisão,
            entregamos as respostas divididas em 3 pilares:{" "}
            <strong>Problemas</strong> (o que exige atenção imediata),{" "}
            <strong>Pontos Positivos</strong> (suas vitórias financeiras) e{" "}
            <strong>Recomendações</strong> (ações práticas para melhorar).
          </li>
        </ul>
      </div>

      {/* Rótulo indicativo para o chat */}
      <p
        style={{
          color: "#d1d5db",
          fontSize: "0.9rem",
          marginBottom: "8px",
          marginTop: "10px",
        }}
      >
        💬 <strong>Chat financeiro:</strong> Digite sua pergunta abaixo
      </p>

      <textarea
        value={pergunta}
        onChange={(e) => setPergunta(e.target.value)}
        placeholder="Ex: Analise minhas despesas e me dê uma opinião."
      />

      <button onClick={enviarPergunta} disabled={loading}>
        {loading ? "Pensando..." : "Enviar Pergunta"}
      </button>

      {resposta && (
        <div className="resposta-box">
          <div>{formatarTexto(resposta)}</div>
        </div>
      )}
    </div>
  );
};

export default ChatIA;
