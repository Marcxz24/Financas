import { useEffect, useState } from "react";
import api from "../../../services/api";
import "../../Relatorios/RelatorioReport.css";

function RelatorioContasBancarias() {
  const [contas, setContas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState("");
  const [filtroConta, setFiltroConta] = useState("");

  const exportarPDF = () => {
    const script = document.createElement("script");
    script.src = "https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js";
    script.onload = () => {
      const element = document.querySelector(".relatorio-page");
      const opt = {
        margin: 10,
        filename: `Relatorio-Contas-Bancarias-${new Date().getTime()}.pdf`,
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
    const carregarContas = async () => {
      setLoading(true);
      setErro("");
      try {
        const response = await api.get("/contas-bancarias/listar-conta-bancaria");
        setContas(response.data || []);
      } catch (error) {
        console.error(error);
        setErro("Não foi possível carregar as contas bancárias. Verifique a API.");
        setContas([]);
      } finally {
        setLoading(false);
      }
    };
    carregarContas();
  }, []);

  const contasFiltradas = contas.filter((conta) => {
    return (
      String(conta.nomeContaBancaria || conta.nome || "")
        .toLowerCase()
        .includes(filtroConta.toLowerCase())
    );
  });

  const formatarMoeda = (valor) =>
    Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  const tipoTexto = (tipo) => {
    if (tipo === 1 || String(tipo).toLowerCase() === "corrente") return "Conta Corrente";
    if (tipo === 2 || String(tipo).toLowerCase() === "poupanca") return "Poupança";
    return "Outro";
  };

  return (
    <div className="relatorio-page">
      <div className="relatorio-header">
        <h2>
          <i className="bi bi-bank"></i>
          Relatório de Contas Bancárias
        </h2>
        <p>Resumo de contas bancárias cadastradas e seus saldos atuais.</p>
      </div>

      <div className="relatorio-actions">
        <button className="botao-pdf" onClick={exportarPDF}>
          <i className="bi bi-file-earmark-pdf"></i> Exportar PDF
        </button>
      </div>

      <div className="relatorio-filtros">
        <div className="filtro-card">
          <label>Filtrar conta bancária</label>
          <input
            type="text"
            value={filtroConta}
            onChange={(e) => setFiltroConta(e.target.value)}
            placeholder="Buscar por nome da conta"
          />
        </div>
      </div>

      <div className="relatorio-resumo">
        <div className="resumo-card">
          <span>Total de contas</span>
          <strong>{contasFiltradas.length}</strong>
        </div>
        <div className="resumo-card">
          <span>Saldo consolidado</span>
          <strong>{formatarMoeda(contasFiltradas.reduce((total, conta) => total + Number(conta.saldoContaBancaria || conta.saldo || 0), 0))}</strong>
        </div>
      </div>

      {erro && <div className="relatorio-erro">{erro}</div>}

      <div className="grafico-card">
        <h3>Contas bancárias cadastradas</h3>
        {loading ? (
          <div className="sem-resultados">Carregando contas...</div>
        ) : contasFiltradas.length > 0 ? (
          <table className="tabela-relatorio">
            <thead>
              <tr>
                <th>Conta</th>
                <th>Tipo</th>
                <th>Saldo</th>
              </tr>
            </thead>
            <tbody>
              {contasFiltradas.map((conta) => (
                <tr key={conta.id || conta.nomeContaBancaria || conta.nome}>
                  <td>{conta.nomeContaBancaria || conta.nome}</td>
                  <td>{tipoTexto(conta.tipo)}</td>
                  <td>{formatarMoeda(conta.saldoContaBancaria || conta.saldo || 0)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="sem-resultados">Nenhuma conta bancária encontrada.</div>
        )}
      </div>
    </div>
  );
}

export default RelatorioContasBancarias;
