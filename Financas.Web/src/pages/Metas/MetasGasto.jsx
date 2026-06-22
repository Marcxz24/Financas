/**
 * MetasGasto.jsx
 *
 * Página responsável pelo gerenciamento de metas financeiras do usuário.
 * Realiza CRUD completo (criar, listar, editar e excluir) integrado com a API.
 * Também calcula indicadores de progresso e exibe visualmente o avanço das metas.
 */

import { useState, useEffect } from "react";
import api from "../../services/api";
import "../Metas/MetasGasto.css";

function MetasGasto() {
  // Estado de erro global da página (validação e falhas de API)
  const [erro, setErro] = useState("");

  // Estados do formulário de criação/edição de meta
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [dataInicio, setDataInicio] = useState("");
  const [dataFinal, setDataFinal] = useState("");
  const [tipoMeta, setTipoMeta] = useState(0); // 0: Despesa | 1: Receita

  // Lista de metas carregadas da API
  const [metas, setMetas] = useState([]);

  // Controla se está em modo edição (ID ativo) ou criação (null)
  const [idEdicao, setIdEdicao] = useState(null);

  // Controle de exibição do modal de formulário
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Gatilho simples para forçar recarregamento da lista após alterações
  const [atualizarLista, setAtualizarLista] = useState(0);

  // Define o modo da tela com base na existência de ID de edição
  const modo = idEdicao ? "editar" : "criar";

  // Carrega metas sempre que o gatilho de atualização muda
  useEffect(() => {
    const buscarMetas = async () => {
      try {
        const res = await api.get("/metas-gasto/listar-metas-gasto");
        setMetas(res.data);
      } catch (error) {
        console.error("Erro ao carregar metas:", error);
        setErro("Erro ao sincronizar informações com o servidor.");
      }
    };

    buscarMetas();
  }, [atualizarLista]);

  // Limpa todos os campos do formulário
  const limparFormulario = () => {
    setDescricao("");
    setValor("");
    setDataInicio("");
    setDataFinal("");
    setTipoMeta(0);
  };

  // Abre modal no modo criação
  const handleAbrirModalNovo = () => {
    setIdEdicao(null);
    limparFormulario();
    setErro("");
    setIsModalOpen(true);
  };

  // Fecha modal e reseta estados relacionados
  const handleFecharModal = () => {
    setIsModalOpen(false);
    setIdEdicao(null);
    setErro("");
  };

  // Ativa modo edição carregando dados da meta selecionada
  const handleAtivarEdicao = (id) => {
    setIdEdicao(id);
    setErro("");

    const metaSel = metas.find((m) => m.id === id);

    if (metaSel) {
      setDescricao(metaSel.nome || "");
      setValor(metaSel.valorMeta || "");
      setTipoMeta(metaSel.tipoMeta ?? 0);

      // Ajuste de formato para input date (YYYY-MM-DD)
      setDataInicio(
        metaSel.dataInicio ? String(metaSel.dataInicio).split("T")[0] : ""
      );
      setDataFinal(
        metaSel.dataFinal ? String(metaSel.dataFinal).split("T")[0] : ""
      );

      setIsModalOpen(true);
    }
  };

  // Salva criação ou atualização de meta
  const handleSalvar = async (e) => {
    e.preventDefault();
    setErro("");

    // Payload compatível com a API
    const dados = {
      nome: descricao,
      categoriaId: null,
      cartaoCreditoId: null,
      valorMeta: Number(valor),
      tipoMeta: Number(tipoMeta),
      dataInicio: `${dataInicio}T00:00:00`,
      dataFinal: `${dataFinal}T23:59:59`,
    };

    try {
      if (modo === "criar") {
        await api.post("/metas-gasto/criar-meta-gasto", dados);
      } else {
        await api.patch(`/metas-gasto/atualizar-meta-gasto/${idEdicao}`, dados);
      }

      limparFormulario();
      setIdEdicao(null);
      setIsModalOpen(false);

      // Força atualização da listagem
      setAtualizarLista((prev) => prev + 1);
    } catch (error) {
      let mensagemErro = "Erro ao salvar meta.";

      if (error.response?.data?.errors) {
        mensagemErro = Object.values(error.response.data.errors)
          .flat()
          .join(" | ");
      } else if (error.response?.data?.message) {
        mensagemErro = error.response.data.message;
      } else if (typeof error.response?.data === "string") {
        mensagemErro = error.response.data;
      }

      setErro(mensagemErro);
    }
  };

  // Remove meta após confirmação do usuário
  const handleExcluir = async (id) => {
    if (window.confirm("Deseja realmente excluir esta meta?")) {
      try {
        await api.delete(`/metas-gasto/deletar-meta-gasto/${id}`);
        setAtualizarLista((prev) => prev + 1);
      } catch {
        setErro("Erro ao excluir meta.");
      }
    }
  };

  // Cálculos agregados para cards de resumo
  const metaTotal = metas.reduce((acc, m) => acc + (m.valorMeta || 0), 0);
  const gastoAtualTotal = metas.reduce(
    (acc, m) => acc + (m.valorGastoAtual || 0),
    0
  );
  const saldoRestanteTotal = metaTotal - gastoAtualTotal;

  return (
    <div className="metas-page">

      {/* Resumo geral das metas */}
      <div className="resumo-cards">
        <div className="card-info">
          <span>META TOTAL</span>
          <h2>R$ {metaTotal.toFixed(2).replace(".", ",")}</h2>
        </div>

        <div className="card-info">
          <span>VALOR ATUAL</span>
          <h2 className="text-green">
            R$ {gastoAtualTotal.toFixed(2).replace(".", ",")}
          </h2>
        </div>

        <div className="card-info">
          <span>SALDO RESTANTE</span>
          <h2 className="text-green">
            R$ {saldoRestanteTotal.toFixed(2).replace(".", ",")}
          </h2>
        </div>
      </div>

      {/* Lista principal de metas com progresso visual */}
      <div className="painel-metas">
        <div className="painel-header">
          <h2>Suas Metas</h2>
          <button className="btn-adicionar" onClick={handleAbrirModalNovo}>
            Adicionar Nova Meta
          </button>
        </div>

        <div className="lista-barras">
          {metas.map((m) => {
            const limite = m.valorMeta || 0;
            const gasto = m.valorGastoAtual || 0;
            const faltam = limite - gasto;
            const pct = limite > 0 ? (gasto / limite) * 100 : 0;

            const corBarra =
              m.status === "Estourado" ? "#ff4d4d" : "#4caf50";

            const tipoLabel = m.tipoMeta === 1 ? "Receita" : "Despesa";

            const dataFinalExibicao = m.dataFinal
              ? new Date(m.dataFinal).toLocaleDateString()
              : "N/A";

            return (
              <div key={m.id} className="meta-bar-item">

                {/* Cabeçalho da meta com ações */}
                <div className="meta-bar-header">
                  <div className="meta-nome">
                    <span>
                      {m.nome} ({tipoLabel}) - Até {dataFinalExibicao}
                    </span>
                  </div>

                  <div
                    style={{
                      flex: 1,
                      textAlign: "right",
                      paddingRight: "20px",
                      fontSize: "0.9rem",
                      color: "#ccc",
                    }}
                  >
                    R$ {gasto.toFixed(2).replace(".", ",")} atual
                    (Faltam R$ {faltam.toFixed(2).replace(".", ",")})
                  </div>

                  <div className="meta-dados">
                    <button
                      className="btn-acao"
                      onClick={() => handleAtivarEdicao(m.id)}
                    >
                      Editar
                    </button>

                    <button
                      className="btn-acao"
                      onClick={() => handleExcluir(m.id)}
                    >
                      Remover
                    </button>
                  </div>
                </div>

                {/* Barra de progresso da meta */}
                <div className="progress-bg">
                  <div
                    className="progress-fill"
                    style={{
                      width: `${pct > 100 ? 100 : pct}%`,
                      backgroundColor: corBarra,
                    }}
                  >
                    <span>
                      {pct.toFixed(0)}%
                    </span>
                  </div>
                </div>

              </div>
            );
          })}
        </div>
      </div>

      {/* Modal de criação/edição */}
      {isModalOpen && (
        <div className="modal-overlay">
          <div className="metas-card">

            <h1>{modo === "criar" ? "Nova Meta" : "Editar Meta"}</h1>

            <form onSubmit={handleSalvar} className="metas-form">

              <div className="input-group">
                <label>Descrição</label>
                <input
                  type="text"
                  value={descricao}
                  onChange={(e) => setDescricao(e.target.value)}
                  required
                />
              </div>

              <div className="input-group">
                <label>Tipo de Meta</label>
                <select
                  value={tipoMeta}
                  onChange={(e) => setTipoMeta(Number(e.target.value))}
                >
                  <option value={0}>Despesa</option>
                  <option value={1}>Receita</option>
                </select>
              </div>

              <div className="input-group">
                <label>Valor Limite</label>
                <input
                  type="number"
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                  required
                />
              </div>

              <div className="input-group">
                <label>Data Inicio</label>
                <input
                  type="date"
                  value={dataInicio}
                  onChange={(e) => setDataInicio(e.target.value)}
                  required
                />
              </div>

              <div className="input-group">
                <label>Data Final</label>
                <input
                  type="date"
                  value={dataFinal}
                  onChange={(e) => setDataFinal(e.target.value)}
                  required
                />
              </div>

              {erro && (
                <div className="mensagem-erro">{erro}</div>
              )}

              <button type="submit" className="btn-salvar">
                Salvar
              </button>

              <button
                type="button"
                className="btn-cancelar"
                onClick={handleFecharModal}
              >
                Cancelar
              </button>

            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default MetasGasto;