import { useEffect, useState } from "react";
import api from "../../../services/api";
import { BarChart } from "../RelatorioCharts";

function Receitas() {
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
  const [receitas, setReceitas] = useState([]);

  useEffect(() => {
    // 1. Defina as funções auxiliares DENTRO do useEffect
    // Assim, elas ficam disponíveis antes de serem chamadas e não causam erros de escopo.
    const isReceita = (item) => {
      const tipo = item.tipo;
      if (Number(tipo) === 1) return true;
      if (typeof tipo === "string")
        return tipo.toLowerCase().includes("receita");
      return false;
    };

    const getDataKey = (data) => {
      if (!data) return null;
      const value = String(data);
      const isoMatch = value.match(/^(\d{4}-\d{2}-\d{2})/);
      if (isoMatch) return isoMatch[1];
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return null;
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
    };

    const carregarDados = async () => {
      setLoading(true);
      setErro("");

      try {
        const [resContas, resCategorias] = await Promise.all([
          api.get("/contas-bancarias/listar-conta-bancaria"),
          api.get("/categorias/listar-categorias"),
        ]);

        setContas(resContas.data || []);
        setCategorias(resCategorias.data || []);

        const response = await api.get("/lancamentos/visualizar-lancamentos");
        const dados = response.data || [];

        const filtrado = dados.filter((item) => {
          if (!isReceita(item)) return false;
          if (
            filtros.contaBancariaId &&
            Number(item.contaBancariaId) !== Number(filtros.contaBancariaId)
          )
            return false;
          if (
            filtros.categoriaId &&
            Number(item.categoriaId) !== Number(filtros.categoriaId)
          )
            return false;

          const dataKey = getDataKey(
            item.data || item.dataLancamento || item.dataMovimento,
          );
          if (!dataKey) return false;
          if (filtros.dataInicio && dataKey < filtros.dataInicio) return false;
          if (filtros.dataFim && dataKey > filtros.dataFim) return false;

          return true;
        });

        setReceitas(filtrado);
      } catch (error) {
        console.error(error);
        setErro("Não foi possível carregar o relatório.");
      } finally {
        setLoading(false);
      }
    };

    carregarDados();

    // 2. Adicione os filtros como dependências aqui.
    // Agora o React sabe que, se o usuário alterar qualquer filtro, o efeito deve rodar novamente.
  }, [
    filtros.categoriaId,
    filtros.contaBancariaId,
    filtros.dataFim,
    filtros.dataInicio,
  ]);

  const isReceita = (item) => {
    const tipo = item.tipo;
    if (Number(tipo) === 1) return true;
    if (typeof tipo === "string") {
      return tipo.toLowerCase().includes("receita");
    }
    return false;
  };

  async function carregarReceitas() {
    setLoading(true);
    setErro("");

    try {
      const response = await api.get("/lancamentos/visualizar-lancamentos");
      const dados = response.data || [];
      const filtrado = dados.filter((item) => {
        if (!isReceita(item)) return false;
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
      setReceitas(filtrado);
    } catch (error) {
      console.error(error);
      setErro(
        "Não foi possível buscar as receitas. O relatório está vazio ou o serviço de API não está disponível.",
      );
      setReceitas([]);
    } finally {
      setLoading(false);
    }
  }

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
    const dados = receitas.reduce((acc, item) => {
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
          <i className="bi bi-graph-up-arrow"></i>
          Relatório de Receitas
        </h2>
        <p>
          Consulte receitas cadastradas por período, conta bancária e categoria.
        </p>
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
                  categoria.tipo === 1 ||
                  categoria.tipo === "receita" ||
                  categoria.tipo === "Receita",
              )
              .map((categoria) => (
                <option key={categoria.id} value={categoria.id}>
                  {categoria.nome}
                </option>
              ))}
          </select>
        </div>
        <div className="filtro-card" style={{ alignSelf: "end" }}>
          <button className="botao-filtro" onClick={() => carregarReceitas()}>
            Aplicar filtros
          </button>
        </div>
      </div>

      <div className="relatorio-resumo">
        <div className="resumo-card">
          <span>Total de receitas</span>
          <strong>
            {formatarMoeda(
              receitas.reduce(
                (total, item) => total + Number(item.valor || 0),
                0,
              ),
            )}
          </strong>
        </div>
        <div className="resumo-card">
          <span>Registros</span>
          <strong>{receitas.length}</strong>
        </div>
      </div>

      <div className="grafico-card">
        <h3>Receitas por dia</h3>

        {receitas.length > 0 ? (
          <>
            <BarChart labels={labels} values={values} color="#22c55e" />

            <div className="relatorio-legenda">
              <div className="legenda-item">
                <span
                  className="legenda-cor"
                  style={{ background: "#22c55e" }}
                />
                {labels.length} dias com movimento
              </div>
            </div>
          </>
        ) : (
          <div className="sem-resultados">
            Nenhuma receita encontrada com os filtros selecionados.
          </div>
        )}
      </div>

      {erro && <div className="relatorio-erro">{erro}</div>}

      <div className="grafico-card">
        <h3>Detalhes das receitas</h3>
        {loading ? (
          <div className="sem-resultados">Carregando dados...</div>
        ) : receitas.length > 0 ? (
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
                {receitas.map((item) => (
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
            Não existem receitas cadastradas para este período.
          </div>
        )}
      </div>
    </div>
  );
}

export default Receitas;
