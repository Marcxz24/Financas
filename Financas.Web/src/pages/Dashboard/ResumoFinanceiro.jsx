import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import "./Dashboard.css";

function ResumoFinanceiro() {
  const navigate = useNavigate();

  // Estado para armazenar a lista de últimas transações
  const [transacoes, setTransacoes] = useState([]);

  // Estado para armazenar os valores totais do dashboard
  const [resumo, setResumo] = useState({
    totalReceitas: 0,
    totalDespesas: 0,
    saldoBancarioTotal: 0,
  });

  const [contasBancarias, setContasBancarias] = useState([]);
  const [contaFiltroId, setContaFiltroId] = useState(0);

  // 1. Função estabilizada para carregar dados (pode ser chamada pelo useEffect ou pelos botões)
  const carregarDados = useCallback(async () => {
    try {
      const url = contaFiltroId > 0
      ? `/dashboard/resumo-mensal?contaId=${contaFiltroId}`
      : "/dashboard/resumo-mensal";

      const response = await api.get(url);
      const dados = response.data;

      setTransacoes(dados.ultimosLancamentos || []);
      setResumo({
        totalReceitas: dados.totalReceitas || 0,
        totalDespesas: dados.totalDespesas || 0,
        saldoBancarioTotal: dados.saldoBancarioTotal || 0,
      });
    } catch (error) {
      console.error("Erro ao carregar dados:", error);
    }
  }, [contaFiltroId]);

  useEffect(() => {
    const carregarContasDoUsuario = async () => {
      try {
        const response = await api.get("/contas-bancarias/listar-conta-bancaria");
        setContasBancarias(response.data || []);
      } catch (error) {
        console.error("Erro ao carregar contas para o filtro:", error);
      }
    };
    carregarContasDoUsuario();
  }, []);

  // 2. useEffect com Cleanup para evitar erros de renderização em cascata ou vazamento de memória
  useEffect(() => {
    let isMounted = true;

    const fetchData = async () => {
      if (isMounted) {
        await carregarDados();
      }
    };

    fetchData();

    return () => {
      isMounted = false;
    };
  }, [carregarDados]);

  return (
    <>
  {/* Seção de Cards de Resumo */}
  <div className="resumo-cards">
    <div className="card">
      <h3>Saldo Bancário Total</h3>

      <select
      value={contaFiltroId}
      onChange={(e) => setContaFiltroId(Number(e.target.value))}
      className="select-filtro-conta"
    >
      <option value={0} style={{ background: "#1a1f29" }}>
        Conta Principal(Padrão)
      </option>
      {contasBancarias.map((conta) => (
        <option key={conta.id} value={conta.id} style={{ background: "#1a1f29" }}>
          {conta.nome}
        </option>
      ))}
    </select>

      <p>
        R$ {resumo.saldoBancarioTotal.toLocaleString("pt-BR", {
          minimumFractionDigits: 2,
        })}
      </p>
    </div>

    <div className="card receitas">
      <h3>Receitas (Mês)</h3>
      <p className="positivo">
        + R$ {resumo.totalReceitas.toLocaleString("pt-BR", {
          minimumFractionDigits: 2,
        })}
      </p>
    </div>

    <div className="card despesas">
      <h3>Despesas (Mês)</h3>
      <p className="negativo">
        - R$ {resumo.totalDespesas.toLocaleString("pt-BR", {
          minimumFractionDigits: 2,
        })}
      </p>
    </div>
  </div>

  {/* Seção da Listagem de Transações Recentes */}
  <div className="ultimas-transacoes">
    <h2>Últimas Transações</h2>
    <div className="transacoes-box">
      {transacoes.length > 0 ? (
        transacoes.map((t) => (
          <div key={t.id} className="item-transacao">
            <div className="info-esquerda">
              <div className="data-wrapper">
                <span className="data-txt">
                  {new Date(t.dataLancamento).toLocaleDateString("pt-BR")}
                </span>
                <span className="hora-txt">
                  {new Date(t.dataLancamento).toLocaleTimeString("pt-BR", {
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </span>
              </div>

              <div className="detalhes-wrapper">
                <div className="desc-linha">
                  <strong className="desc-txt">{t.descricao}</strong>
                </div>

                <div className="origem-info">
                  <div className="info-item">
                    <small>Conta: </small>
                    <span className="origem-txt">
                      {t.contaBancariaNome || "Nenhuma"}
                    </span>
                  </div>

                  <div className="info-item">
                    <small>Cartão: </small>
                    <span className="origem-txt">
                      {t.cartaoCreditoNome || "Nenhum"}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Seção de Ações (Botões) e Valor */}
            <div className="acoes-valor-wrapper">
              <div className="btn-group-acoes">
                <button
                  className="btn-acao-tabela editar"
                  onClick={() => navigate(`/dashboard/lancamento/${t.id}`)}
                >
                  Editar
                </button>
              </div>

              <span
                className={t.tipo === "Receita" ? "positivo" : "negativo"}
              >
                {t.tipo === "Receita" ? "+" : "-"} R${" "}
                {t.valor.toLocaleString("pt-BR", {
                  minimumFractionDigits: 2,
                })}
              </span>
            </div>
          </div>
        ))
      ) : (
        <p className="empty-msg">
          Nenhuma transação encontrada no momento.
        </p>
      )}
    </div>
  </div>
</>
  );
}

export default ResumoFinanceiro;