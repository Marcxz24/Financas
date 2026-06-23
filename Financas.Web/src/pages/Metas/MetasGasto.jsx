/**
 * MetasGasto.jsx
 *
 * Página responsável pelo gerenciamento de metas financeiras do usuário.
 * Realiza CRUD completo (criar, listar, editar e excluir) integrado com a API.
 *
 * Suporta dois tipos de meta com regras de campos exclusivas:
 * - Despesa (tipoMeta === 0): sem ContaBancariaId
 * - Patrimônio (tipoMeta === 1): ContaBancariaId obrigatório; sem CategoriaId/CartaoCreditoId
 */

// Componente principal para gerenciamento completo de metas financeiras realizando operações de criação, leitura, atualização e exclusão na API.

import { useState, useEffect } from "react";
import api from "../../services/api";
import "../Metas/MetasGasto.css";

function MetasGasto() {
  // Armazena mensagens de erro globais para validações de formulário e falhas de requisição na API.
  const [erro, setErro] = useState("");

  // Estados responsáveis por armazenar os dados preenchidos no formulário de criação e edição das metas.
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [dataInicio, setDataInicio] = useState("");
  const [dataFinal, setDataFinal] = useState("");
  const [tipoMeta, setTipoMeta] = useState(0);
  // Identificador da conta bancária vinculado à meta, sendo obrigatório apenas quando o tipo de meta for classificado como Patrimônio.
  const [contaBancariaId, setContaBancariaId] = useState("");

  // Armazena a lista de metas e contas bancárias recuperadas diretamente da API para exibição na interface.
  const [metas, setMetas] = useState([]);
  const [contasBancarias, setContasBancarias] = useState([]);

  // Variáveis de controle para gerenciar a abertura do modal, identificar a meta em edição e forçar atualizações na listagem da tela.
  const [idEdicao, setIdEdicao] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [atualizarLista, setAtualizarLista] = useState(0);

  // Gerencia os filtros aplicados aos cards de resumo e armazena os detalhes temporários carregados em tempo real na seleção da meta.
  const [idFiltroMeta, setIdFiltroMeta] = useState("");
  const [metaDetalhe, setMetaDetalhe] = useState(null);
  const [carregandoDetalhe, setCarregandoDetalhe] = useState(false);

  // Define dinamicamente se o formulário está operando em modo de criação de uma nova meta ou na edição de um registro existente.
  const modo = idEdicao ? "editar" : "criar";

  // Busca simultaneamente as metas cadastradas e as contas bancárias no servidor assim que o componente é renderizado ou a lista é atualizada.
  useEffect(() => {
    const carregarDados = async () => {
      try {
        const [resMetas, resContas] = await Promise.all([
          api.get("/metas-gasto/listar-metas-gasto"),
          api.get("/contas-bancarias/listar-conta-bancaria"),
        ]);

        setMetas(resMetas.data);
        setContasBancarias(resContas.data);
      } catch (error) {
        console.error("Erro ao carregar dados:", error);
        setErro("Erro ao sincronizar informações com o servidor.");
      }
    };

    carregarDados();
  }, [atualizarLista]);

  // Limpa rigorosamente todos os campos do formulário para garantir que os dados de uma operação anterior não interfiram na próxima interação do usuário.
  const limparFormulario = () => {
    setDescricao("");
    setValor("");
    setDataInicio("");
    setDataFinal("");
    setTipoMeta(0);
    setContaBancariaId("");
  };

  const handleAbrirModalNovo = () => {
    setIdEdicao(null);
    limparFormulario();
    setErro("");
    setIsModalOpen(true);
  };

  const handleFecharModal = () => {
    setIsModalOpen(false);
    setIdEdicao(null);
    setErro("");
  };

  // Preenche de forma automatizada todos os inputs do formulário no modal utilizando os dados da meta selecionada para agilizar o processo de edição.
  const handleAtivarEdicao = (id) => {
    setIdEdicao(id);
    setErro("");

    const metaSel = metas.find((m) => m.id === id);


    if (metaSel) {
      setDescricao(metaSel.nome || "");
      setValor(metaSel.valorMeta || "");
      setTipoMeta(metaSel.tipoMeta ?? 0);
      setDataInicio(
        metaSel.dataInicio ? String(metaSel.dataInicio).split("T")[0] : "",
      );
      setDataFinal(
        metaSel.dataFinal ? String(metaSel.dataFinal).split("T")[0] : "",
      );

      // Restaura corretamente o vínculo visual da conta bancária caso a meta carregada pelo usuário seja do tipo Patrimônio.
      setContaBancariaId(
        metaSel.contaBancariaId != null ? String(metaSel.contaBancariaId) : "",
      );

      setIsModalOpen(true);
    }
  };

  // Realiza a validação lógica e comercial dos dados antes de submetê-los, bloqueando o envio de metas de Patrimônio sem uma conta selecionada.
  const validarFormulario = () => {
    if (tipoMeta === 1 && !contaBancariaId) {
      setErro("Selecione uma conta bancária para a meta de Patrimônio.");
      return false;
    }

    return true;
  };

  // Estrutura dinamicamente o objeto de dados que será enviado ao servidor, formatando os horários limites das datas informadas.
  const montarPayload = () => {
    const base = {
      nome: descricao,
      tipoMeta: Number(tipoMeta),
      valorMeta: Number(valor),
      dataInicio: `${dataInicio}T00:00:00`,
      dataFinal: `${dataFinal}T23:59:59`,
    };

    if (tipoMeta === 0) {
      // Para metas de Despesa, os identificadores de categoria e cartão de crédito são rigorosamente anulados no payload de requisição.
      return {
        ...base,
        categoriaId: null,
        cartaoCreditoId: null,
      };
    }

      // Para metas de Patrimônio, a requisição anexa exclusivamente o ID da conta bancária e descarta automaticamente configurações desnecessárias de despesas.
    return {
      ...base,
      contaBancariaId: Number(contaBancariaId),
    };
  };

  // Gerencia o ciclo completo de submissão do formulário identificando se a operação acionada pelo usuário representa a criação ou a edição da meta.
  const handleSalvar = async (e) => {
    e.preventDefault();
    setErro("");

    if (!validarFormulario()) return;

    const dados = montarPayload();

    try {
      if (modo === "criar") {
        await api.post("/metas-gasto/criar-meta-gasto", dados);
      } else {
        await api.patch(`/metas-gasto/atualizar-meta-gasto/${idEdicao}`, dados);
      }

      limparFormulario();
      setIdEdicao(null);
      setIsModalOpen(false);
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

  // Aciona um modal nativo do navegador para solicitar confirmação de segurança do usuário antes de solicitar a remoção definitiva da meta na API.
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

  // Controla a interação do usuário com o filtro principal, manipulando a exibição individual ou o agrupamento completo das informações nos cards.
  const handleFiltroMeta = async (e) => {
    const idSelecionado = e.target.value;
    setIdFiltroMeta(idSelecionado);

    if (!idSelecionado) {
      setMetaDetalhe(null);
      return;
    }

    setCarregandoDetalhe(true);
    setMetaDetalhe(null);

    try {
      const res = await api.get(`/metas-gasto/${idSelecionado}`);
      setMetaDetalhe(res.data);
    } catch (error) {
      console.error("Erro ao carregar detalhe da meta:", error);
      setErro("Erro ao carregar detalhe da meta selecionada.");
    } finally {
      setCarregandoDetalhe(false);
    }
  };

  // Processa os valores financeiros exibidos nos painéis superiores executando reduções em tempo real quando o filtro global está em modo geral.
  const metaTotal = metaDetalhe
    ? metaDetalhe.valorMeta
    : metas.reduce((acc, m) => acc + (m.valorMeta || 0), 0);

  const valorAtualTotal = metaDetalhe
    ? metaDetalhe.valorAtual
    : metas.reduce((acc, m) => acc + (m.valorAtual || 0), 0);

  const saldoRestanteTotal = metaTotal - valorAtualTotal;

  // Retorna dinamicamente a coloração hexadecimal apropriada da barra de progresso analisando diretamente o status devolvido pela lógica do backend.
  const obterCorBarra = (status) => {
    if (status === "Estourado") return "#ff4d4d";
    if (status === "Atenção") return "#f0ad4e";
    if (status === "Meta atingida") return "#1e70c1";
    return "#4caf50";
  };

  const formatarMoeda = (valor) => (valor || 0).toFixed(2).replace(".", ",");

  // Monta e renderiza a estrutura completa da interface contendo o sistema de filtros, os totalizadores dinâmicos, a listagem interativa e os modais ocultos.
  return (
    <div className="metas-page">
      {/* Renderiza o filtro principal na parte superior para isolar e analisar o progresso financeiro de uma meta específica. */}
      <div className="filtro-meta-wrapper">
        <select
          className="filtro-meta-select"
          value={idFiltroMeta}
          onChange={handleFiltroMeta}
        >
          <option value="">Todas as metas</option>
          {metas.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
            </option>
          ))}
        </select>
      </div>

      {/* Painel informativo superior que unifica e recalcula o total estipulado, o valor atingido e o saldo restante baseado no contexto do filtro. */}
      <div className="resumo-cards">
        <div className="card-info">
          <span>META TOTAL</span>
          <h2>
            {carregandoDetalhe ? "..." : `R$ ${formatarMoeda(metaTotal)}`}
          </h2>
        </div>

        <div className="card-info">
          <span>VALOR ATUAL</span>
          <h2 className="text-green">
            {carregandoDetalhe ? "..." : `R$ ${formatarMoeda(valorAtualTotal)}`}
          </h2>
        </div>

        <div className="card-info">
          <span>SALDO RESTANTE</span>
          <h2 className="text-green">
            {carregandoDetalhe
              ? "..."
              : `R$ ${formatarMoeda(saldoRestanteTotal)}`}
          </h2>
        </div>

        {/* O card complementar de status detalhado é inserido condicionalmente no layout exclusivamente quando o usuário isola uma meta no filtro. */}
        {metaDetalhe && !carregandoDetalhe && (
          <div className="card-info">
            <span>STATUS</span>
            <h2 style={{ fontSize: "1.2rem" }}>
              {metaDetalhe.status}
              <span
                style={{
                  fontSize: "0.95rem",
                  color: "#aaa",
                  marginLeft: "0.5rem",
                }}
              >
                ({metaDetalhe.percentualUtilizado.toFixed(1)}%)
              </span>
            </h2>
          </div>
        )}
      </div>

      {/* Estrutura visual que lista sequencialmente todas as metas cadastradas apresentando seus limites, indicadores visuais e botões de gerenciamento rápido. */}
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
            // Normaliza e calcula matematicamente os atributos monetários da listagem para construir as proporções de preenchimento da barra de progresso da meta.
            const atual = m.valorAtual || 0;
            const faltam = limite - atual;
            const pct = limite > 0 ? (atual / limite) * 100 : 0;

            const corBarra = obterCorBarra(m.status);
            const tipoLabel = m.tipoMeta === 1 ? "Patrimônio" : "Despesa";
            const dataFinalExib = m.dataFinal
              ? new Date(m.dataFinal).toLocaleDateString("pt-BR")
              : "N/A";

            return (
              <div key={m.id} className="meta-bar-item">
                {/* Exibe detalhadamente as configurações principais do registro como nomenclatura, formato e limites e abriga os gatilhos diretos de edição e remoção. */}
                <div className="meta-bar-header">
                  <div className="meta-nome">
                    <span>
                      {m.nome} ({tipoLabel}) — Até {dataFinalExib}
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
                    R$ {formatarMoeda(atual)} atual (Faltam R${" "}
                    {formatarMoeda(faltam)})
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

                {/* Projeta uma régua gráfica no front-end para fornecer uma perspectiva analítica e intuitiva da aderência do usuário em relação à meta financeira. */}
                <div className="progress-bg">
                  <div
                    className="progress-fill"
                    style={{
                      width: `${Math.min(pct, 100)}%`,
                      backgroundColor: corBarra,
                    }}
                  >
                    <span>{pct.toFixed(0)}%</span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Conjunto flutuante sobreposto que consolida todo o formulário de captação de dados essenciais para o fluxo de registro ou atualização de metas do sistema. */}
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
                  onChange={(e) => {
                    setTipoMeta(Number(e.target.value));
                    // Remove por precaução qualquer vínculo anterior de conta bancária no estado local caso o usuário alterne para uma categoria de despesa convencional.
                    setContaBancariaId("");
                    setErro("");
                  }}
                >
                  <option value={0}>Despesa</option>
                  <option value={1}>Patrimônio</option>
                </select>
              </div>

              {/* Renderiza condicionalmente e obriga o preenchimento de uma conta bancária de referência garantindo que o sistema obedeça as regras exclusivas do tipo Patrimônio. */}
              {tipoMeta === 1 && (
                <div className="input-group">
                  <label>Conta Bancária *</label>
                  <select
                    value={contaBancariaId}
                    onChange={(e) => setContaBancariaId(e.target.value)}
                    required
                  >
                    <option value="">Selecione uma conta</option>
                    {contasBancarias.map((c) => (
                      <option key={c.id} value={String(c.id)}>
                        {c.nome}
                      </option>
                    ))}
                  </select>
                </div>
              )}

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
                <label>Data Início</label>
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

              {erro && <div className="mensagem-erro">{erro}</div>}

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