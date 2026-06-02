import { useState } from "react";
import api from "../../services/api";
import "./Ajuda.css";

function Ajuda() {
  const [assunto, setAssunto] = useState("");
  const [descricao, setDescricao] = useState("");

  const [loading, setLoading] = useState(false);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");

  const MAX_CARACTERES = 1000;

  const enviarChamado = async (e) => {
    e.preventDefault();

    setErro("");
    setSucesso("");

    if (!assunto.trim()) {
      setErro("Informe o assunto do chamado.");
      return;
    }

    if (assunto.trim().length < 5) {
      setErro("O assunto deve possuir no mínimo 5 caracteres.");
      return;
    }

    if (!descricao.trim()) {
      setErro("Informe a descrição do chamado.");
      return;
    }

    if (descricao.trim().length < 10) {
      setErro("A descrição deve possuir no mínimo 10 caracteres.");
      return;
    }

    try {
      setLoading(true);

      await api.post("/ajuda/enviar", {
        assunto,
        descricao,
      });

      await new Promise((resolve) => setTimeout(resolve, 1500));

      setSucesso(
        "Chamado enviado com sucesso. Nossa equipe retornará em breve.",
      );

      setAssunto("");
      setDescricao("");
    } catch (error) {
      console.log("Erro completo:", error);
      console.log("Response:", error.response);
      console.log("Data:", error.response?.data);

      setErro(
        JSON.stringify(error.response?.data, null, 2) ||
          "Não foi possível enviar o chamado.",
      );
    } finally {
      setLoading(false);
    }
  };

  const contadorClass =
    descricao.length >= 950
      ? "danger"
      : descricao.length >= 800
        ? "warning"
        : "";

  return (
    <div className="ajuda-page-container">
      <div className="ajuda-main-card">
        <div className="ajuda-header">
          <h2>Central de Ajuda</h2>

          <p>
            Descreva sua dúvida, problema ou sugestão e envie um chamado para
            nossa equipe de suporte.
          </p>
        </div>

        {erro && <div className="ajuda-erro">{erro}</div>}

        {sucesso && <div className="ajuda-sucesso">{sucesso}</div>}

        <form onSubmit={enviarChamado}>
          <div className="ajuda-form-group">
            <label>Assunto</label>

            <input
              type="text"
              value={assunto}
              onChange={(e) => setAssunto(e.target.value)}
              placeholder="Informe o assunto do chamado"
              maxLength={150}
            />
          </div>

          <div className="ajuda-form-group">
            <label>Descrição</label>

            <textarea
              rows="8"
              value={descricao}
              onChange={(e) =>
                setDescricao(e.target.value.slice(0, MAX_CARACTERES))
              }
              placeholder="Descreva detalhadamente sua solicitação..."
            />
          </div>

          <div className={`contador-caracteres ${contadorClass}`}>
            {descricao.length}/{MAX_CARACTERES}
          </div>

          <button
            type="submit"
            className="btn-enviar-chamado"
            disabled={loading}
          >
            {loading ? "Enviando..." : "Enviar Chamado"}
          </button>
        </form>
      </div>
    </div>
  );
}

export default Ajuda;
