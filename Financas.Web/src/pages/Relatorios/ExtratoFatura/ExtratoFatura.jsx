import { useEffect, useState } from "react";
import api from "../../../services/api";
import "../../Relatorios/ExtratoFatura/ExtratoFatura.css";

// Página de extrato de faturas
// Exibe faturas encerradas, detalhes do extrato e prepara impressão em PDF.
function ExtratoFatura() {
  const [faturas, setFaturas] = useState([]);
  const [faturaExpandida, setFaturaExpandida] = useState(null);
  const [loading, setLoading] = useState(true);
  const [extratos, setExtratos] = useState({});

  // Carrega a lista de faturas encerradas ao montar o componente
  useEffect(() => {
    const carregarFaturas = async () => {
      try {
        setLoading(true);
        const response = await api.get("/fatura/listar-encerradas");
        setFaturas(response.data || []);
      } catch (error) {
        console.error("Erro ao carregar extrato:", error);
      } finally {
        setLoading(false);
      }
    };

    carregarFaturas();
  }, []);

  // Busca detalhes do extrato para uma fatura específica
  // Busca detalhes do extrato para uma fatura específica
  const carregarExtrato = async (id) => {
    if (extratos[id]) {
      return extratos[id];
    }

    const response = await api.get(`/fatura/${id}/extrato`);
    setExtratos((anterior) => ({
      ...anterior,
      [id]: response.data,
    }));
    return response.data;
  };

  // Alterna entre exibir e ocultar o extrato da fatura
  const toggleFatura = async (id) => {
    if (faturaExpandida === id) {
      setFaturaExpandida(null);
      return;
    }

    try {
      await carregarExtrato(id);
      setFaturaExpandida(id);
    } catch (error) {
      console.error("Erro ao carregar extrato da fatura:", error);
    }
  };

  const [imprimindoId, setImprimindoId] = useState(null);

  // Prepara a impressão do extrato selecionado
  const imprimirFatura = async (id) => {
    try {
      await carregarExtrato(id);
      setFaturaExpandida(id);
      setImprimindoId(id);
    } catch (error) {
      console.error("Erro ao preparar impressão da fatura:", error);
    }
  };

  useEffect(() => {
    if (!imprimindoId) {
      return;
    }

    const timeout = window.setTimeout(() => {
      window.print();
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [imprimindoId]);

  useEffect(() => {
    const handleAfterPrint = () => {
      setImprimindoId(null);
    };

    window.addEventListener("afterprint", handleAfterPrint);
    return () => {
      window.removeEventListener("afterprint", handleAfterPrint);
    };
  }, []);

  // Funções utilitárias de formatação de valores e datas
  const formatarMoeda = (valor) =>
    Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  const formatarData = (data) => {
    if (!data) return "-";
    return new Date(data).toLocaleDateString("pt-BR");
  };

  // Renderiza a interface do extrato de faturas
  return (
    <div className="extrato-fatura-container">
      <div className="extrato-header">
        <h2>
          <i className="bi bi-receipt"></i>
          Extrato de Faturas
        </h2>
        <p>Histórico completo das faturas fechadas e pagas.</p>
      </div>

      {loading ? (
        <div className="extrato-loading">
          <div className="spinner-border text-primary"></div>
        </div>
      ) : faturas.length === 0 ? (
        <div className="extrato-vazio">
          <i className="bi bi-folder-x"></i>
          <h3>Nenhuma fatura encontrada</h3>
          <p>Ainda não existem faturas fechadas ou pagas.</p>
        </div>
      ) : (
        <div className="extrato-lista">
          {faturas.map((fatura) => {
            const extrato = extratos[fatura.id];

            return (
              <div key={fatura.id} className={`fatura-card ${imprimindoId === fatura.id ? "pdf-imprimir" : ""}`}>
                <div className="fatura-card-header">
                  <div>
                    <h3>
                      <i className="bi bi-credit-card"></i>
                      {fatura.cartaoNome}
                    </h3>
                    <span className="periodo-fatura">
                      {formatarData(fatura.dataInicio)} até{" "}
                      {formatarData(fatura.dataFechamento)}
                    </span>
                  </div>

                  <div className="fatura-status-area">
                    <span
                      className={`status-badge ${
                        fatura.status?.toLowerCase() === "paga"
                          ? "status-paga"
                          : "status-fechada"
                      }`}
                    >
                      {fatura.status}
                    </span>

                    <button
                      className="btn-pdf"
                      onClick={() => imprimirFatura(fatura.id)}
                    >
                      <i className="bi bi-file-earmark-pdf"></i>
                      PDF
                    </button>

                    <button
                      className="btn-visualizar"
                      onClick={() => toggleFatura(fatura.id)}
                    >
                      {faturaExpandida === fatura.id ? (
                        <>
                          <i className="bi bi-chevron-up"></i>
                          Ocultar
                        </>
                      ) : (
                        <>
                          <i className="bi bi-eye"></i>
                          Visualizar Extrato
                        </>
                      )}
                    </button>
                  </div>
                </div>

                {faturaExpandida === fatura.id && (
                  <div className="extrato-detalhes">
                    {extrato ? (
                      <>
                        <div className="resumo-fatura">
                          <div className="resumo-item">
                            <span>Valor Total</span>
                            <strong>{formatarMoeda(extrato.valorTotal)}</strong>
                          </div>
                          <div className="resumo-item">
                            <span>Total Pago</span>
                            <strong>{formatarMoeda(extrato.totalPago)}</strong>
                          </div>
                          <div className="resumo-item">
                            <span>Saldo Restante</span>
                            <strong>
                              {formatarMoeda(extrato.saldoRestante)}
                            </strong>
                          </div>
                        </div>

                        <div className="lancamentos-fatura">
                          <h4>
                            <i className="bi bi-list-ul"></i>
                            Lançamentos da Fatura
                          </h4>

                          {extrato.lancamentos?.length > 0 ? (
                            <table className="tabela-lancamentos">
                              <thead>
                                <tr>
                                  <th>Data</th>
                                  <th>Descrição</th>
                                  <th>Tipo</th>
                                  <th>Valor</th>
                                </tr>
                              </thead>
                              <tbody>
                                {extrato.lancamentos.map((lancamento) => (
                                  <tr key={lancamento.id}>
                                    <td>{formatarData(lancamento.data)}</td>
                                    <td>{lancamento.descricao}</td>
                                    <td>{lancamento.tipo}</td>
                                    <td>{formatarMoeda(lancamento.valor)}</td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          ) : (
                            <div className="sem-lancamentos">
                              Nenhum lançamento encontrado para esta fatura.
                            </div>
                          )}
                        </div>

                        <div className="pagamentos-fatura">
                          <h4>
                            <i className="bi bi-cash-stack"></i> Pagamentos
                            Realizados
                          </h4>

                          {extrato.pagamentos?.length > 0 ? (
                            <table className="tabela-lancamentos">
                              <thead>
                                <tr>
                                  <th>Data</th>
                                  <th>Observação</th>
                                  <th>Valor Pago</th>
                                </tr>
                              </thead>
                              <tbody>
                                {extrato.pagamentos.map((pagamento) => (
                                  <tr key={pagamento.id}>
                                    <td>
                                      {formatarData(pagamento.dataPagamento)}
                                    </td>
                                    <td>{pagamento.observacao}</td>
                                    <td>
                                      {formatarMoeda(pagamento.valorPago)}
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          ) : (
                            <div className="sem-lancamentos">
                              Nenhum pagamento encontrado.
                            </div>
                          )}
                        </div>
                      </>
                    ) : (
                      <div className="extrato-loading">
                        <div className="spinner-border text-primary"></div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default ExtratoFatura;
