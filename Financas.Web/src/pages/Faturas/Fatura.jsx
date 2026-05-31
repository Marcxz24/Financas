import { useState, useEffect } from "react";
import api from "../../services/api";
import "./Fatura.css";

function Fatura() {
  const [faturas, setFaturas] = useState([]);
  const [contas, setContas] = useState([]);
  const [faturaSelecionada, setFaturaSelecionada] = useState(null);
  const [erro, setErro] = useState("");

  // =========================
  // RECARREGAR FATURAS
  // =========================
  const carregarFaturas = async () => {
    try {
      const res = await api.get("/fatura/listar");
      setFaturas(res.data);
    } catch (error) {
      console.error(error);
      setErro("Erro ao carregar faturas.");
    }
  };

  // =========================
  // CARREGAMENTO INICIAL
  // =========================
  useEffect(() => {
    const carregarDados = async () => {
      try {
        await carregarFaturas();

        const resContas = await api.get(
          "/contas-bancarias/listar-conta-bancaria",
        );

        setContas(resContas.data);
      } catch (error) {
        console.error(error);
        setErro("Erro ao carregar dados.");
      }
    };

    carregarDados();
  }, []);

  // =========================
  // ABRIR PAGAMENTO
  // =========================
  const abrirPagamento = (fatura) => {
    setFaturaSelecionada(fatura);
  };

  // =========================
  // FECHAR FATURA
  // =========================
  const fecharFatura = async (id) => {
    try {
      await api.post(`/fatura/${id}/fechar`);

      await carregarFaturas();
    } catch (error) {
      console.error("Erro ao fechar fatura:", error);

      setErro(error.response?.data || "Erro ao fechar fatura.");
    }
  };

  // =========================
  // PAGAR FATURA
  // =========================
  const pagarFatura = async (tipoPagamento, contaId = null) => {
    try {
      await api.post("/fatura/pagar", {
        faturaId: faturaSelecionada.id,
        tipoPagamento,
        contaId,
      });

      setFaturaSelecionada(null);

      const res = await api.get("/fatura/listar");
      setFaturas(res.data);
    } catch (error) {
      console.error("Erro completo:", error);

      const data = error.response?.data;

      if (typeof data === "string") {
        setErro(data);
        return;
      }

      if (data?.errors) {
        const primeiraChave = Object.keys(data.errors)[0];

        setErro(data.errors[primeiraChave][0]);
        return;
      }

      setErro("Erro ao pagar fatura.");
    }
  };

  const formatarData = (data) => {
    if (!data) return "-";

    const dataLimpa = String(data).split(".")[0];

    return new Date(dataLimpa).toLocaleDateString("pt-BR");
  };

  return (
    <div className="fatura-page">
      <div className="fatura-card">
        {/* HEADER */}
        <header className="fatura-header">
          <h1>Faturas do Cartão</h1>
          <p className="descricao-header">
            Gerencie e realize o pagamento das suas faturas.
          </p>
        </header>

        {/* LISTA */}
        <section className="fatura-lista">
          {faturas.length === 0 ? (
            <p className="txt-vazio">Nenhuma fatura encontrada.</p>
          ) : (
            faturas.map((f) => (
              <div key={f.id} className="conta-card">
                <div className="conta-info">
                  <h3>{f.cartaoNome}</h3>

                  <span>
                    Período: {formatarData(f.dataInicio)} até{" "}
                    {formatarData(f.dataFechamento)}
                  </span>

                  <span>Vencimento: {formatarData(f.dataVencimento)}</span>

                  <div className="conta-saldo">
                    {Number(f.valorTotal).toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                  </div>

                  <div className="conta-saldo">
                    Pago:{" "}
                    {Number(f.valorPago).toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                  </div>

                  <span>Status: {f.status}</span>
                </div>

                <div className="conta-actions">
                  {f.status === "Aberta" && (
                    <button
                      className="btn-fechar"
                      onClick={() => fecharFatura(f.id)}
                    >
                      Fechar Fatura
                    </button>
                  )}

                  {f.status === "Fechada" && (
                    <button
                      className="btn-pagar"
                      onClick={() => abrirPagamento(f)}
                    >
                      Pagar
                    </button>
                  )}

                  {f.status === "Paga" && (
                    <span className="status-paga">Fatura Quitada</span>
                  )}
                </div>
              </div>
            ))
          )}
        </section>

        {/* MODAL PAGAMENTO */}
        {faturaSelecionada && (
          <div className="modal-overlay">
            <div className="modal-box">
              <h3>Pagar Fatura</h3>

              <div className="valor-fatura-modal">
                {Number(faturaSelecionada.valorTotal).toLocaleString("pt-BR", {
                  style: "currency",
                  currency: "BRL",
                })}
              </div>

              <button
                className="btn-pagamento-dinheiro"
                onClick={() => pagarFatura("dinheiro")}
              >
                💵 Pagar com Dinheiro
              </button>

              <h4>Selecionar Conta Bancária</h4>

              {contas.length === 0 ? (
                <p className="txt-vazio">Nenhuma conta disponível.</p>
              ) : (
                contas.map((c) => (
                  <button
                    key={c.id}
                    className="btn-pagamento-conta"
                    onClick={() => pagarFatura("conta", c.id)}
                  >
                    🏦 {c.nome ?? c.Nome}
                  </button>
                ))
              )}

              <button
                className="btn-cancelar-modal"
                onClick={() => setFaturaSelecionada(null)}
              >
                Cancelar
              </button>
            </div>
          </div>
        )}

        {/* ERRO */}
        {erro && <p className="erro">{erro}</p>}
      </div>
    </div>
  );
}

export default Fatura;
