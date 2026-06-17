import "./Relatorios.css";
import { useNavigate } from "react-router-dom";

// Página de seleção de relatórios
// Exibe cards de relatórios disponíveis e navega para cada relatório.
function Relatorios() {
  const navigate = useNavigate();

  // Configuração dos cards de relatório exibidos na página
  const relatorios = [
    {
      titulo: "Receitas",
      descricao: "Consulte receitas recebidas por período e categoria.",
      icone: "bi-graph-up-arrow",
      rota: "/dashboard/relatorios/receitas",
    },
    {
      titulo: "Despesas",
      descricao: "Analise despesas realizadas por período e categoria.",
      icone: "bi-graph-down-arrow",
      rota: "/dashboard/relatorios/despesas",
    },
    {
      titulo: "Fluxo de Caixa",
      descricao: "Acompanhe entradas e saídas financeiras do período.",
      icone: "bi-cash-stack",
      rota: "/dashboard/relatorios/fluxo-caixa",
    },
    // Adicione este objeto no array relatorios:
    {
      titulo: "Fatura",
      descricao: "Visualize e exporte o extrato detalhado de uma fatura.",
      icone: "bi-receipt",
      rota: "/dashboard/relatorios/fatura", // rota de seleção — veja nota abaixo
    },
  ];

  // Navega para o relatório selecionado, quando disponível
  const abrirRelatorio = (rota) => {
    if (rota !== "#") {
      navigate(rota);
    }
  };

  // Renderiza a página de relatório com cards clicáveis
  return (
    <div className="relatorios-container">
      <div className="relatorios-header">
        <h2>
          <i className="bi bi-file-earmark-bar-graph"></i>
          Relatórios
        </h2>

        <p>Central de relatórios financeiros do sistema.</p>
      </div>

      <div className="relatorios-grid">
        {relatorios.map((relatorio, index) => (
          <div
            key={index}
            className="relatorio-card"
            onClick={() => abrirRelatorio(relatorio.rota)}
          >
            <div className="relatorio-icon">
              <i className={`bi ${relatorio.icone}`}></i>
            </div>

            <h3>{relatorio.titulo}</h3>

            <p>{relatorio.descricao}</p>

            <button className="btn-relatorio" disabled={relatorio.rota === "#"}>
              {relatorio.rota === "#" ? "Em Breve" : "Acessar"}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}

export default Relatorios;
