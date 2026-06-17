import { useEffect, useState } from "react";
import api from "../../../services/api";
import { BarChart } from "../RelatorioCharts";

function Despesas() {
  const [contas, setContas] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [filtros, setFiltros] = useState({
    dataInicio: "",
    dataFim: "",
    contaBancariaId: 0,
    categoriaId: 0,
  });
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState("");
  const [despesas, setDespesas] = useState([]);

  useEffect(() => {
    // 1. Funções auxiliares mantidas dentro do useEffect
    const isDespesa = (item) => {
      const tipo = item.tipo;
      // Ajuste aqui conforme o valor do seu sistema para despesas (ex: tipo 2)
      if (Number(tipo) === 2) return true;
      if (typeof tipo === "string") {
        return (
          tipo.toLowerCase().includes("despesa") ||
          tipo.toLowerCase().includes("gasto")
        );
      }
      return false;
    };

    const getDataKey = (data) => {
      if (!data) return null;
      const value = String(data);
      const isoMatch = value.match(/^(\d{4}-\d{2}-\d{2})/);
      if (isoMatch) return isoMatch[1];
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return null;
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const day = String(date.getDate()).padStart(2, "0");
      return `${year}-${month}-${day}`;
    };

    const carregarDados = async () => {
      setLoading(true);
      setErro("");

      try {
        // 2. Busca dados de apoio
        const [resContas, resCategorias] = await Promise.all([
          api.get("/contas-bancarias/listar-conta-bancaria"),
          api.get("/categorias/listar-categorias"),
        ]);

        setContas(resContas.data || []);
        setCategorias(resCategorias.data || []);

        // 3. Busca e filtra as despesas
        const response = await api.get("/lancamentos/visualizar-lancamentos");
        const dados = response.data || [];

        const filtrado = dados.filter((item) => {
          if (!isDespesa(item)) return false;

          if (
            filtros.contaBancariaId &&
            Number(item.contaBancariaId) !== Number(filtros.contaBancariaId)
          ) {
            return false;
          }
          if (
            filtros.categoriaId &&
            Number(item.categoriaId) !== Number(filtros.categoriaId)
          ) {
            return false;
          }

          const dataKey = getDataKey(
            item.data || item.dataLancamento || item.dataMovimento,
          );
          if (!dataKey) return false;
          if (filtros.dataInicio && dataKey < filtros.dataInicio) return false;
          if (filtros.dataFim && dataKey > filtros.dataFim) return false;

          return true;
        });

        setDespesas(filtrado); // Assumindo que você usa setDespesas
      } catch (error) {
        console.error(error);
        setErro("Não foi possível carregar o relatório de despesas.");
        setDespesas([]);
      } finally {
        setLoading(false);
      }
    };

    carregarDados();
  }, [
    filtros.categoriaId,
    filtros.contaBancariaId,
    filtros.dataFim,
    filtros.dataInicio,
  ]);

  const isDespesa = (item) => {
    const tipo = item.tipo;
    if (Number(tipo) === 2) return true;
    if (typeof tipo === "string") {
      return tipo.toLowerCase().includes("despesa");
    }
    return false;
  };

  const carregarDespesas = async () => {
    setLoading(true);
    setErro("");
    try {
      const response = await api.get("/lancamentos/visualizar-lancamentos");
      const dados = response.data || [];
      const filtrado = dados.filter((item) => {
        if (!isDespesa(item)) return false;
        if (
          filtros.contaBancariaId &&
          Number(item.contaBancariaId) !== Number(filtros.contaBancariaId)
        ) {
          return false;
        }
        if (
          filtros.categoriaId &&
          Number(item.categoriaId) !== Number(filtros.categoriaId)
        ) {
          return false;
        }
        const dataKey = getDataKey(
          item.data || item.dataLancamento || item.dataMovimento,
        );
        if (!dataKey) return false;
        if (filtros.dataInicio && dataKey < filtros.dataInicio) {
          return false;
        }
        if (filtros.dataFim && dataKey > filtros.dataFim) {
          return false;
        }
        return true;
      });
      setDespesas(filtrado);
    } catch (error) {
      console.error(error);
      setErro(
        "Erro ao buscar despesas. O serviço de relatórios não respondeu corretamente.",
      );
      setDespesas([]);
    } finally {
      setLoading(false);
    }
  };

  const formatarMoeda = (valor) =>
    Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  const formatarData = (data) => {
    if (!data) return "-";
    return new Date(data).toLocaleDateString("pt-BR");
  };

  const getDataKey = (data) => {
    if (!data) return null;
    const value = String(data);
    const isoMatch = value.match(/^(\d{4}-\d{2}-\d{2})/);
    if (isoMatch) return isoMatch[1];
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return null;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  };

  const agruparPorData = () => {
    const dados = despesas.reduce((acc, item) => {
      const chave = getDataKey(
        item.data || item.dataLancamento || item.dataMovimento,
      );
      if (!chave) return acc;
      acc[chave] = (acc[chave] || 0) + Number(item.valor || 0);
      return acc;
    }, {});

    const datasOrdenadas = Object.keys(dados).sort(
      (a, b) => new Date(a) - new Date(b),
    );
    const labels = datasOrdenadas.map((date) =>
      new Date(`${date}T00:00:00`).toLocaleDateString("pt-BR"),
    );
    const values = datasOrdenadas.map((date) => dados[date]);
    return { labels, values };
  };

  const { labels, values } = agruparPorData();

  return (
    <div className="relatorio-page">
      <div className="relatorio-header">
        <h2>
          <i className="bi bi-graph-down-arrow"></i>
          Relatório de Despesas
        </h2>
        <p>Analise despesas por período, conta bancária e categoria.</p>
      </div>

      <div className="relatorio-filtros">
        <div className="filtro-card">
          <label>Data Início</label>
          <input
            type="date"
            value={filtros.dataInicio}
            onChange={(e) =>
              setFiltros({ ...filtros, dataInicio: e.target.value })
            }
          />
        </div>
        <div className="filtro-card">
          <label>Data Fim</label>
          <input
            type="date"
            value={filtros.dataFim}
            onChange={(e) =>
              setFiltros({ ...filtros, dataFim: e.target.value })
            }
          />
        </div>
        <div className="filtro-card">
          <label>Conta Bancária</label>
          <select
            value={filtros.contaBancariaId}
            onChange={(e) =>
              setFiltros({
                ...filtros,
                contaBancariaId: Number(e.target.value),
              })
            }
          >
            <option value={0}>Todas as contas</option>
            {contas.map((conta) => (
              <option key={conta.id} value={conta.id}>
                {conta.nomeContaBancaria || conta.nome}
              </option>
            ))}
          </select>
        </div>
        <div className="filtro-card">
          <label>Categoria</label>
          <select
            value={filtros.categoriaId}
            onChange={(e) =>
              setFiltros({ ...filtros, categoriaId: Number(e.target.value) })
            }
          >
            <option value={0}>Todas as categorias</option>
            {categorias
              .filter(
                (categoria) =>
                  categoria.tipo === 2 ||
                  categoria.tipo === "despesa" ||
                  categoria.tipo === "Despesa",
              )
              .map((categoria) => (
                <option key={categoria.id} value={categoria.id}>
                  {categoria.nome}
                </option>
              ))}
          </select>
        </div>
        <div className="filtro-card" style={{ alignSelf: "end" }}>
          <button className="botao-filtro" onClick={() => carregarDespesas()}>
            Aplicar filtros
          </button>
        </div>
      </div>

      <div className="relatorio-resumo">
        <div className="resumo-card">
          <span>Total de despesas</span>
          <strong>
            {formatarMoeda(
              despesas.reduce(
                (total, item) => total + Number(item.valor || 0),
                0,
              ),
            )}
          </strong>
        </div>
        <div className="resumo-card">
          <span>Registros</span>
          <strong>{despesas.length}</strong>
        </div>
      </div>

      <div className="grafico-card">
        <h3>Despesas por dia</h3>

        {despesas.length > 0 ? (
          <>
            <BarChart labels={labels} values={values} color="#ef4444" />

            <div className="relatorio-legenda">
              <div className="legenda-item">
                <span
                  className="legenda-cor"
                  style={{ background: "#ef4444" }}
                />
                {labels.length} dias com movimento
              </div>
            </div>
          </>
        ) : (
          <div className="sem-resultados">
            Nenhuma despesa encontrada com os filtros selecionados.
          </div>
        )}
      </div>

      {erro && <div className="relatorio-erro">{erro}</div>}

      <div className="grafico-card">
        <h3>Detalhes das despesas</h3>
        {loading ? (
          <div className="sem-resultados">Carregando dados...</div>
        ) : despesas.length > 0 ? (
          /* Adicionamos a div abaixo para habilitar o scroll horizontal no mobile */
          <div className="tabela-container-scroll">
            <table className="tabela-relatorio">
              <thead>
                <tr>
                  <th>Data</th>
                  <th>Descrição</th>
                  <th>Categoria</th>
                  <th>Conta</th>
                  <th>Valor</th>
                </tr>
              </thead>
              <tbody>
                {despesas.map((item) => (
                  <tr key={item.id || `${item.data}-${item.valor}`}>
                    <td>
                      {formatarData(
                        item.data || item.dataLancamento || item.dataMovimento,
                      )}
                    </td>
                    <td>{item.descricao || item.historico || "-"}</td>
                    <td>{item.categoriaNome || item.categoria?.nome || "-"}</td>
                    <td>{item.contaBancariaNome || item.conta?.nome || "-"}</td>
                    <td>{formatarMoeda(item.valor)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="sem-resultados">
            Não existem despesas cadastradas para este período.
          </div>
        )}
      </div>
    </div>
  );
}

export default Despesas;
