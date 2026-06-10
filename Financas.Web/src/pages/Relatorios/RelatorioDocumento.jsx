import { useRef, useEffect } from "react";
import "./RelatorioDocumento.css";

export function RelatorioDocumento({ 
  titulo, 
  descricao, 
  resumo = [], 
  secoes = [],
  dataGerada = new Date().toLocaleDateString("pt-BR"),
  onExportarPDF
}) {
  const docRef = useRef(null);

  useEffect(() => {
    const exportarPDF = async () => {
      if (!docRef.current) return;
      
      const script = document.createElement("script");
      script.src = "https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js";
      
      script.onload = () => {
        const element = docRef.current;
        const opt = {
          margin: 10,
          filename: `${titulo.replace(/\s+/g, "-")}-${new Date().getTime()}.pdf`,
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

    if (onExportarPDF) {
      onExportarPDF(exportarPDF);
    }
  }, [titulo, onExportarPDF]);

  return (
    <div ref={docRef} className="relatorio-documento">
      {/* Capa */}
      <div className="documento-capa">
        <div className="capa-header">
          <h1>{titulo}</h1>
          <p>{descricao}</p>
        </div>
        <div className="capa-info">
          <div className="info-item">
            <span>Data de Geração</span>
            <strong>{dataGerada}</strong>
          </div>
        </div>
      </div>

      {/* Resumo Executivo */}
      {resumo.length > 0 && (
        <div className="documento-pagina">
          <h2>Resumo Executivo</h2>
          <div className="resumo-grid">
            {resumo.map((item, idx) => (
              <div key={idx} className="resumo-item">
                <span className="resumo-label">{item.label}</span>
                <strong className="resumo-valor">{item.valor}</strong>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Seções de Conteúdo */}
      {secoes.map((secao, idx) => (
        <div key={idx} className="documento-pagina">
          <h2>{secao.titulo}</h2>
          
          {secao.conteudo && (
            <div className="secao-conteudo">
              {secao.conteudo}
            </div>
          )}

          {secao.tabela && (
            <table className="tabela-documento">
              <thead>
                <tr>
                  {secao.tabela.colunas.map((col, cidx) => (
                    <th key={cidx}>{col}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {secao.tabela.dados.map((linha, lidx) => (
                  <tr key={lidx}>
                    {linha.map((celula, cidx) => (
                      <td key={cidx}>{celula}</td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {secao.grafico && (
            <div className="secao-grafico">
              {secao.grafico}
            </div>
          )}
        </div>
      ))}

      {/* Rodapé */}
      <div className="documento-rodape">
        <p>Relatório gerado automaticamente pelo sistema de gestão financeira</p>
        <p>© 2026 - Todos os direitos reservados</p>
      </div>
    </div>
  );
}
