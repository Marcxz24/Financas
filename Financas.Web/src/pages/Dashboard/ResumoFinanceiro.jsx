import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import "./Dashboard.css";

/*
  Componente ResumoFinanceiro
  - Renderiza os cards de resumo de receita, despesa, saldo mensal e saldo bancário total
  - Carrega o histórico de transações recentes do mês selecionado
  - Exibe filtros de mês/ano/conta para atualizar o dashboard
*/
function ResumoFinanceiro() {
  const navigate = useNavigate();

  // Estado para armazenar a lista de últimas transações
  const [transacoes, setTransacoes] = useState([]);

  // Estado para armazenar os valores totais do dashboard
  const [resumo, setResumo] = useState({
    totalReceitas: 0,
    totalDespesas: 0,
    saldoMensal: 0,
    saldoBancarioTotal: 0,
  });

  // Contas disponíveis no filtro de seleção
  const [contasBancarias, setContasBancarias] = useState([]);
  const [contaFiltroId, setContaFiltroId] = useState(0);

  const dataAtual = new Date();

  const [mesFiltro, setMesFiltro] = useState(dataAtual.getMonth() + 1);
  const [anoFiltro, setAnoFiltro] = useState(dataAtual.getFullYear());

  const [loadingResumo, setLoadingResumo] = useState(false);

  // Uso de efeito para atualizar os dados do resumo quando qualquer filtro muda.
  // Aqui é onde a API é chamada com mês, ano e conta selecionados.
  useEffect(() => {
    const carregar = async () => {
      setLoadingResumo(true);

      try {
        let url = `/dashboard/resumo-mensal?mes=${mesFiltro}&ano=${anoFiltro}`;

        if (contaFiltroId > 0) {
          url += `&contaBancariaId=${contaFiltroId}`;
        }

        const response = await api.get(url);
        const dados = response.data;

        setTransacoes(dados.lancamentosDoMes || []);

        setResumo({
          totalReceitas: dados.totalReceitas || 0,
          totalDespesas: dados.totalDespesas || 0,
          saldoMensal: dados.saldoMensal || 0,
          saldoBancarioTotal: dados.saldoBancarioTotal || 0,
        });
      } catch (error) {
        console.error(error);
      } finally {
        setLoadingResumo(false);
      }
    };

    carregar();
  }, [contaFiltroId, mesFiltro, anoFiltro]);

  useEffect(() => {
    const carregarContasDoUsuario = async () => {
      try {
        const response = await api.get(
          "/contas-bancarias/listar-conta-bancaria",
        );
        setContasBancarias(response.data || []);
      } catch (error) {
        console.error("Erro ao carregar contas para o filtro:", error);
      }
    };

    carregarContasDoUsuario();
  }, []);

  const meses = [
    { id: 1, nome: "Janeiro" },
    { id: 2, nome: "Fevereiro" },
    { id: 3, nome: "Março" },
    { id: 4, nome: "Abril" },
    { id: 5, nome: "Maio" },
    { id: 6, nome: "Junho" },
    { id: 7, nome: "Julho" },
    { id: 8, nome: "Agosto" },
    { id: 9, nome: "Setembro" },
    { id: 10, nome: "Outubro" },
    { id: 11, nome: "Novembro" },
    { id: 12, nome: "Dezembro" },
  ];

  const anos = [];

  for (let ano = dataAtual.getFullYear(); ano >= 2020; ano--) {
    anos.push(ano);
  }

  /*
    Renderização do dashboard
    - Filtros acima
    - Cards de resumo ao centro
    - Lista de últimas transações abaixo
  */
  return (
    <>
      {/* Barra de Filtros */}
      <div className="dashboard-filtros">
        <select
          value={mesFiltro}
          onChange={(e) => setMesFiltro(Number(e.target.value))}
          className="select-filtro"
        >
          {meses.map((mes) => (
            <option key={mes.id} value={mes.id}>
              {mes.nome}
            </option>
          ))}
        </select>

        <select
          value={anoFiltro}
          onChange={(e) => setAnoFiltro(Number(e.target.value))}
          className="select-filtro"
        >
          {anos.map((ano) => (
            <option key={ano} value={ano}>
              {ano}
            </option>
          ))}
        </select>

        <select
          value={contaFiltroId}
          onChange={(e) => setContaFiltroId(Number(e.target.value))}
          className="select-filtro"
        >
          <option value={0}>Todas as Contas</option>

          {contasBancarias.map((conta) => (
            <option key={conta.id} value={conta.id}>
              {conta.nome}
            </option>
          ))}
        </select>
      </div>

      {/* Cards de Resumo */}
      <div className="resumo-cards">
        <div className="card">
          <h3>Saldo Bancário Total</h3>

          <p>
            R${" "}
            {loadingResumo ? (
              <span style={{ opacity: 0.6, fontSize: "0.9rem" }}>
                Atualizando dados...
              </span>
            ) : (
              resumo.saldoBancarioTotal.toLocaleString("pt-BR", {
                minimumFractionDigits: 2,
              })
            )}
          </p>
        </div>

        <div className="card receitas">
          <h3>Receitas (Mês)</h3>

          <p className="positivo">
            + R${" "}
            {resumo.totalReceitas.toLocaleString("pt-BR", {
              minimumFractionDigits: 2,
            })}
          </p>
        </div>

        <div className="card saldo">
          <h3>Saldo Mensal</h3>

          <p className={resumo.saldoMensal >= 0 ? "positivo" : "negativo"}>
            R${" "}
            {resumo.saldoMensal.toLocaleString("pt-BR", {
              minimumFractionDigits: 2,
            })}
          </p>
        </div>

        <div className="card despesas">
          <h3>Despesas (Mês)</h3>

          <p className="negativo">
            - R${" "}
            {resumo.totalDespesas.toLocaleString("pt-BR", {
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
