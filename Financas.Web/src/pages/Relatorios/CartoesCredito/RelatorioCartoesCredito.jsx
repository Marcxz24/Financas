import { useEffect, useState } from "react";
import api from "../../../services/api";
import "../../Relatorios/RelatorioReport.css";

function RelatorioCartoesCredito() {
  const [cartoes, setCartoes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState("");
  const [filtroCartao, setFiltroCartao] = useState("");

  const exportarPDF = () => {
    const script = document.createElement("script");
    script.src = "https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js";
    script.onload = () => {
      const element = document.querySelector(".relatorio-page");
      const opt = {
        margin: 10,
        filename: `Relatorio-Cartoes-Credito-${new Date().getTime()}.pdf`,
        image: { type: "jpeg", quality: 0.98 },
        html2canvas: { scale: 2, useCORS: true },
        jsPDF: { orientation: "portrait", unit: "mm", format: "a4" },
      };
      if (window.html2pdf) {
        window.html2pdf().set(opt).from(element).save();
      }
    };
    document.head.appendChild(script);
  };

  useEffect(() => {
    const carregarCartoes = async () => {
      setLoading(true);
      setErro("");
      try {
        const response = await api.get("/cartoes-credito/listar-cartoes-credito");
        setCartoes(response.data || []);
      } catch (error) {
        console.error(error);
        setErro("Não foi possível carregar os cartões de crédito. Verifique a API.");
        setCartoes([]);
      } finally {
        setLoading(false);
      }
    };
    carregarCartoes();
  }, []);

  const formatarMoeda = (valor) =>
    Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  const cartoesFiltrados = cartoes.filter((cartao) => {
    const nome = String(cartao.nome || cartao.nomeCartaoCredito || "").toLowerCase();
    return nome.includes(filtroCartao.toLowerCase());
  });

  return (
    <div className="relatorio-page">
      <div className="relatorio-header">
        <h2>
          <i className="bi bi-credit-card"></i>
          Relatório de Cartões de Crédito
        </h2>
        <p>Consulte cartões cadastrados e acompanhe limites, datas de fechamento e vencimento.</p>
      </div>

      <div className="relatorio-actions">
        <button className="botao-pdf" onClick={exportarPDF}>
          <i className="bi bi-file-earmark-pdf"></i> Exportar PDF
        </button>
      </div>

      <div className="relatorio-filtros">
        <div className="filtro-card">
          <label>Filtrar por cartão</label>
          <input
            type="text"
            value={filtroCartao}
            onChange={(e) => setFiltroCartao(e.target.value)}
            placeholder="Buscar por nome do cartão"
          />
        </div>
      </div>

      <div className="relatorio-resumo">
        <div className="resumo-card">
          <span>Total de cartões</span>
          <strong>{cartoesFiltrados.length}</strong>
        </div>
        <div className="resumo-card">
          <span>Limite total</span>
          <strong>{formatarMoeda(cartoesFiltrados.reduce((total, cartao) => total + Number(cartao.limite || 0), 0))}</strong>
        </div>
      </div>

      {erro && <div className="relatorio-erro">{erro}</div>}

      <div className="grafico-card">
  <h3>Cartões de crédito registrados</h3>
  {loading ? (
    <div className="sem-resultados">Carregando cartões...</div>
  ) : cartoesFiltrados.length > 0 ? (
    <div className="tabela-container-scroll">
      <table className="tabela-relatorio">
        <thead>
          <tr>
            <th>Nome</th>
            <th>Limite</th>
            <th>Fechamento</th>
            <th>Vencimento</th>
          </tr>
        </thead>
        <tbody>
          {cartoesFiltrados.map((cartao) => (
            <tr key={cartao.id || cartao.nome}>
              <td>{cartao.nome || cartao.nomeCartaoCredito || "-"}</td>
              <td>{formatarMoeda(cartao.limite || cartao.limiteCredito || 0)}</td>
              <td>{cartao.diaFechamento || cartao.dataFechamento || "-"}</td>
              <td>{cartao.diaVencimento || cartao.dataVencimento || "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  ) : (
    <div className="sem-resultados">Nenhum cartão de crédito encontrado.</div>
  )}
</div>
    </div>
  );
}

export default RelatorioCartoesCredito;
