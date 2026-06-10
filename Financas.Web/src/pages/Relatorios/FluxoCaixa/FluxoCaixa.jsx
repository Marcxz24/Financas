import { useEffect, useState } from "react";
import api from "../../../services/api";
import { LineChart, PieChart } from "../RelatorioCharts";
import "../../Relatorios/RelatorioReport.css";

function FluxoCaixa() {
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
  const [fluxo, setFluxo] = useState({ entradas: [], saidas: [], resumo: {} });

  function isReceita(item) {
    const tipo = item.tipo;
    if (Number(tipo) === 1) return true;
    if (typeof tipo === "string") {
      return tipo.toLowerCase().includes("receita");
    }
    return false;
  }

  function isDespesa(item) {
    const tipo = item.tipo;
    if (Number(tipo) === 2) return true;
    if (typeof tipo === "string") {
      return tipo.toLowerCase().includes("despesa");
    }
    return false;
  }

  function getDateKey(value) {
    if (!value) return null;
    const asString = String(value);
    const isoMatch = asString.match(/^(\d{4}-\d{2}-\d{2})/);
    if (isoMatch) return isoMatch[1];
    const date = new Date(asString);
    if (Number.isNaN(date.getTime())) return null;
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  }

  // 2. useEffect unificado
  useEffect(() => {
    const carregarDados = async () => {
      setLoading(true);
      setErro("");

      try {
        // Busca tudo de uma vez para otimizar a performance
        const [resContas, resCategorias, resLancamentos] = await Promise.all([
          api.get("/contas-bancarias/listar-conta-bancaria"),
          api.get("/categorias/listar-categorias"),
          api.get("/lancamentos/visualizar-lancamentos"),
        ]);

        // Atualiza os estados das listas de apoio para os seus selects
        setContas(resContas.data || []);
        setCategorias(resCategorias.data || []);

        const dados = resLancamentos.data || [];
        const entradasFiltradas = [];
        const saidasFiltradas = [];

        dados.forEach((item) => {
          const dataRaw =
            item.data || item.dataLancamento || item.dataMovimento;
          const dataKey = getDateKey(dataRaw); // Função externa (global)

          if (!dataKey) return;
          if (filtros.dataInicio && dataKey < filtros.dataInicio) return;
          if (filtros.dataFim && dataKey > filtros.dataFim) return;

          if (
            filtros.contaBancariaId &&
            Number(item.contaBancariaId) !== Number(filtros.contaBancariaId)
          )
            return;
          if (
            filtros.categoriaId &&
            Number(item.categoriaId) !== Number(filtros.categoriaId)
          )
            return;

          if (isReceita(item)) {
            // Função externa (global)
            entradasFiltradas.push(item);
          } else if (isDespesa(item)) {
            // Função externa (global)
            saidasFiltradas.push(item);
          }
        });

        setFluxo({
          entradas: entradasFiltradas,
          saidas: saidasFiltradas,
          resumo: {},
        });
      } catch (error) {
        console.error(error);
        setErro("Erro ao buscar o fluxo de caixa.");
        setFluxo({ entradas: [], saidas: [], resumo: {} });
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

  const formatarMoeda = (valor) =>
    Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });

  const formatarData = (data) => {
    if (!data) return "-";
    return new Date(data).toLocaleDateString("pt-BR");
  };

  const entradas = fluxo.entradas || [];
  const saidas = fluxo.saidas || [];
  const totalEntradas = entradas.reduce(
    (total, item) => total + Number(item.valor || 0),
    0,
  );
  const totalSaidas = saidas.reduce(
    (total, item) => total + Number(item.valor || 0),
    0,
  );
  const saldo = totalEntradas - totalSaidas;

  const movimentosOrdenados = [...entradas, ...saidas]
    .map((item) => {
      const dataRaw = item.data || item.dataLancamento || item.dataMovimento;
      const dataDate = new Date(dataRaw);
      return {
        ...item,
        dataBase: dataDate,
        dataKey: getDateKey(dataRaw),
        valorNum: Number(item.valor || 0) * (isReceita(item) ? 1 : -1),
        tipoLabel: isReceita(item) ? "Receita" : "Despesa",
      };
    })
    .filter((item) => !Number.isNaN(item.dataBase.getTime()))
    .sort((a, b) => a.dataBase - b.dataBase);

  const fluxoAcumulado = [];
  let acumulado = 0;
  movimentosOrdenados.forEach((item) => {
    acumulado += item.valorNum;
    fluxoAcumulado.push({
      label: item.dataKey
        ? new Date(`${item.dataKey}T00:00:00`).toLocaleDateString("pt-BR")
        : "-",
      value: acumulado,
    });
  });

  const linhaLabels = fluxoAcumulado.map((item) => item.label);
  const linhaValues = fluxoAcumulado.map((item) => item.value);

  const gastoPorCategoria = saidas.reduce((acc, item) => {
    const nomeCategoria =
      item.categoriaNome || item.categoria?.nome || "Sem categoria";
    acc[nomeCategoria] = (acc[nomeCategoria] || 0) + Number(item.valor || 0);
    return acc;
  }, {});

  const palette = [
    "#ef4444",
    "#f97316",
    "#f59e0b",
    "#eab308",
    "#14b8a6",
    "#22c55e",
    "#2563eb",
    "#8b5cf6",
    "#ec4899",
  ];
  const categoriasSlices = Object.entries(gastoPorCategoria).map(
    ([label, value], index) => ({
      label,
      value,
      color: palette[index % palette.length],
    }),
  );

  return (
    <div className="relatorio-page">
      <div className="relatorio-header">
        <h2>
          <i className="bi bi-cash-stack"></i>
          Relatório de Fluxo de Caixa
        </h2>
        <p>
          Visualize a distribuição de entradas e saídas e filtre por conta
          bancária e categoria.
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
            {categorias.map((categoria) => (
              <option key={categoria.id} value={categoria.id}>
                {categoria.nome}
              </option>
            ))}
          </select>
        </div>
        <div className="filtro-card" style={{ alignSelf: "end" }}>
          <button className="botao-filtro">Aplicar filtros</button>
        </div>
      </div>

      <div className="relatorio-resumo">
        <div className="resumo-card">
          <span>Entradas totais</span>
          <strong>{formatarMoeda(totalEntradas)}</strong>
        </div>
        <div className="resumo-card">
          <span>Saídas totais</span>
          <strong>{formatarMoeda(totalSaidas)}</strong>
        </div>
        <div className="resumo-card">
          <span>Saldo do período</span>
          <strong>{formatarMoeda(saldo)}</strong>
        </div>
      </div>

      <div className="grafico-card">
        <h3>Saldo acumulado</h3>
        {linhaLabels.length > 0 ? (
          <>
            <LineChart
              labels={linhaLabels}
              values={linhaValues}
              color="#2563eb"
            />
            <div className="relatorio-legenda">
              <div className="legenda-item">
                <span
                  className="legenda-cor"
                  style={{ background: "#2563eb" }}
                />
                Evolução do saldo no período
              </div>
            </div>
          </>
        ) : (
          <div className="sem-resultados">
            Nenhum movimento para apresentar o fluxo acumulado.
          </div>
        )}
      </div>

      <div className="grafico-card">
        <h3>Gastos por categoria</h3>
        {categoriasSlices.length > 0 ? (
          <>
            <PieChart slices={categoriasSlices} size={280} innerRadius={40} />
            <div className="relatorio-legenda">
              {categoriasSlices.map((item) => (
                <div key={item.label} className="legenda-item">
                  <span
                    className="legenda-cor"
                    style={{ background: item.color }}
                  />
                  {item.label}: {formatarMoeda(item.value)}
                </div>
              ))}
            </div>
          </>
        ) : (
          <div className="sem-resultados">
            Não há gastos categorizados neste período.
          </div>
        )}
      </div>

      {erro && <div className="relatorio-erro">{erro}</div>}

      <div className="grafico-card">
  <h3>Detalhamento por movimento</h3>
  {loading ? (
    <div className="sem-resultados">Carregando dados...</div>
  ) : (fluxo.entradas.length + fluxo.saidas.length) > 0 ? (
    <div className="tabela-container-scroll">
      <table className="tabela-relatorio">
        <thead>
          <tr>
            <th>Data</th>
            <th>Descrição</th>
            <th>Tipo</th>
            <th>Categoria</th>
            <th>Valor</th>
          </tr>
        </thead>
        <tbody>
          {[...fluxo.entradas, ...fluxo.saidas].map((item, index) => (
            <tr key={item.id || index}>
              <td>
                {formatarData(
                  item.data || item.dataLancamento || item.dataMovimento
                )}
              </td>
              <td>{item.descricao || item.historico || "-"}</td>
              <td>
                {isReceita(item) ? "Receita" : "Despesa"}
              </td>
              <td>{item.categoriaNome || item.categoria?.nome || "-"}</td>
              <td>{formatarMoeda(item.valor)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  ) : (
    <div className="sem-resultados">
      Nenhum registro encontrado no período selecionado.
    </div>
  )}
</div>
    </div>
  );
}

export default FluxoCaixa;
