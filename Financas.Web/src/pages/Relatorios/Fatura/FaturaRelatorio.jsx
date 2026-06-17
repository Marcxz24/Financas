import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import api from "../../../services/api";
import { useExportarPDF } from "../base/useExportarPDF";
import "../base/DocumentoA4.css";
import "./FaturaRelatorio.css";

function FaturaRelatorio() {
  const { id } = useParams();
  const navigate = useNavigate();

  // Estados para controle da requisição
  const [loading, setLoading] = useState(true);
  const [extrato, setExtrato] = useState(null);
  const [erro, setErro] = useState(false);

  // Hook personalizado para gerar o PDF da folha referenciada
  const { folhaRef, exportar } = useExportarPDF(`fatura-${id}`);

  // Busca os dados do extrato da fatura ao carregar o componente
  useEffect(() => {
    api
      .get(`/fatura/${id}/extrato`)
      .then((r) => setExtrato(r.data))
      .catch(() => setErro(true))
      .finally(() => setLoading(false));
  }, [id]);

  // Formata números para o padrão de moeda brasileira (BRL)
  const moeda = (v) =>
    Number(v ?? 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  // Formata datas para o formato local (pt-BR)
  const data = (iso) => {
    if (!iso) return "—";
    return new Date(iso).toLocaleDateString("pt-BR");
  };

  // Extrai apenas a hora e minuto de um objeto de data/string
  const hora = (iso) => {
    if (!iso) return "";
    return new Date(iso).toLocaleTimeString("pt-BR", {
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  // Define o rótulo para o tipo de lançamento
  const tipoLabel = (tipo) =>
    tipo === "Despesa" ? "Despesa" : tipo === "Receita" ? "Receita" : tipo;

  // Exibe tela de carregamento enquanto aguarda a API
  if (loading) {
    return (
      <div className="fat-estado">
        <span>Carregando relatório...</span>
      </div>
    );
  }

  // Exibe tela de erro caso a requisição falhe
  if (erro || !extrato) {
    return (
      <div className="fat-estado fat-estado--erro">
        <span>Não foi possível carregar o relatório.</span>
        <button onClick={() => navigate(-1)}>Voltar</button>
      </div>
    );
  }

  // Verifica se o saldo restante é zero para status da fatura
  const quitada = extrato.saldoRestante === 0;

  return (
    <div className="a4-viewport">
      {/* Barra de navegação e exportação */}
      <div className="a4-barra-acoes">
        <button className="fat-btn fat-btn--ghost" onClick={() => navigate(-1)}>
          ← Voltar
        </button>
        <div className="fat-btn-grupo">
          <button className="fat-btn fat-btn--primary" onClick={exportar}>
            ⬇ Baixar PDF
          </button>
        </div>
      </div>

      {/* Container principal da folha para impressão */}
      <div ref={folhaRef} className="a4-folha">
        <div className="a4-pagina">
          {/* Cabeçalho com identificação da fatura e data de emissão */}
          <div className="a4-cabecalho a4-unido">
            <div>
              <h1 className="a4-cab-titulo">Relatório de Fatura</h1>
              <p className="fat-cab-sub">Extrato financeiro detalhado</p>
            </div>
            <div className="a4-cab-meta">
              <strong>Fatura #{extrato.faturaId}</strong>
              <br />
              <span>Gerado em {data(new Date().toISOString())}</span>
              <br />
              <span
                className={`a4-badge ${quitada ? "a4-badge--pago" : "a4-badge--aberto"}`}
              >
                {quitada ? "Quitada" : "Em aberto"}
              </span>
            </div>
          </div>

          <div className="a4-conteudo">
            {/* Resumo financeiro (KPIs) */}
            <div className="a4-secao-titulo">Resumo financeiro</div>
            <div className="a4-kpi-grid a4-unido">
              <div className="a4-kpi">
                <span className="a4-kpi-label">Valor total</span>
                <span className="a4-kpi-valor a4-kpi-valor--azul">
                  {moeda(extrato.valorTotal)}
                </span>
              </div>
              <div className="a4-kpi">
                <span className="a4-kpi-label">Total pago</span>
                <span className="a4-kpi-valor a4-kpi-valor--verde">
                  {moeda(extrato.totalPago)}
                </span>
              </div>
              <div className="a4-kpi">
                <span className="a4-kpi-label">Saldo restante</span>
                <span
                  className={`a4-kpi-valor ${extrato.saldoRestante > 0 ? "" : "a4-kpi-valor--cinza"}`}
                >
                  {moeda(extrato.saldoRestante)}
                </span>
              </div>
            </div>

            {/* Lista detalhada de pagamentos efetuados */}
            <div className="a4-secao-titulo">Pagamentos</div>
            <table className="a4-tabela a4-unido">
              <thead>
                <tr>
                  <th className="col-data">Data</th>
                  <th className="col-data">Hora</th>
                  <th>Observação</th>
                  <th className="col-num">Valor pago</th>
                </tr>
              </thead>
              <tbody>
                {extrato.pagamentos?.length ? (
                  extrato.pagamentos.map((pg) => (
                    <tr key={pg.id}>
                      <td className="col-data">{data(pg.dataPagamento)}</td>
                      <td className="col-data">{hora(pg.dataPagamento)}</td>
                      <td>{pg.observacao || "—"}</td>
                      <td className="col-num">{moeda(pg.valorPago)}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="fat-vazio">
                      Nenhum pagamento registrado
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {/* Lista detalhada dos lançamentos da fatura */}
            <div className="a4-secao-titulo">Lançamentos da fatura</div>
            <table className="a4-tabela a4-unido">
              <thead>
                <tr>
                  <th className="col-data">Data</th>
                  <th>Descrição</th>
                  <th>Tipo</th>
                  <th className="col-num">Valor</th>
                </tr>
              </thead>
              <tbody>
                {extrato.lancamentos?.length ? (
                  extrato.lancamentos.map((lc) => (
                    <tr key={lc.id}>
                      <td className="col-data">{data(lc.data)}</td>
                      <td>{lc.descricao}</td>
                      <td>
                        <span
                          className={`fat-tipo fat-tipo--${lc.tipo?.toLowerCase()}`}
                        >
                          {tipoLabel(lc.tipo)}
                        </span>
                      </td>
                      <td className="col-num">{moeda(lc.valor)}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="fat-vazio">
                      Nenhum lançamento registrado
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {/* Bloco de totais finais */}
            <div className="a4-total-bloco a4-unido">
              <div className="a4-total-linha">
                <span>Total dos lançamentos</span>
                <span className="a4-total-valor">
                  {moeda(extrato.valorTotal)}
                </span>
              </div>
              <div className="a4-total-linha">
                <span>Total pago</span>
                <span className="a4-total-valor">
                  {moeda(extrato.totalPago)}
                </span>
              </div>
              <div className="a4-total-linha a4-total-linha--destaque">
                <span>Saldo restante</span>
                <span className="a4-total-valor">
                  {moeda(extrato.saldoRestante)}
                </span>
              </div>
            </div>
          </div>

          {/* Rodapé do documento */}
          <div className="a4-rodape">
            <span>
              Fatura #{extrato.faturaId} — Sistema de Gestão Financeira
            </span>
            <span>
              Documento gerado em {new Date().toLocaleString("pt-BR")}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default FaturaRelatorio;