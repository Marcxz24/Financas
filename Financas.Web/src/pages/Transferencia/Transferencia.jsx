import { useState, useEffect } from "react";
import api from "../../services/api";
import { Link } from "react-router-dom";
import "../Transferencia/Transferencia.css";

function Transferencia() {
  // Estados para controle de erros e campos do formulário
  const [erro, setErro] = useState("");
  const [contaOrigemId, setContaOrigemId] = useState("");
  const [contaDestinoId, setContaDestinoId] = useState("");
  const [valor, setValor] = useState("");
  const [observacao, setObservacao] = useState("");
  const [contas, setContas] = useState([]); // Contas bancárias disponíveis para os selects
  const [transferencias, setTransferencias] = useState([]); // Lista de transferências vinda do banco
  const [idEdicao, setIdEdicao] = useState(null); // Controla qual ID está sendo editado (null = modo criação)

  const hoje = new Date().toISOString().split("T")[0];
  const [data, setData] = useState(hoje);

  // Variável computada para alternar dinamicamente o comportamento visual e lógico do formulário
  const modo = idEdicao ? "editar" : "criar";

  // Hook executado uma única vez no ciclo de vida para popular contas e transferências
  useEffect(() => {
    const carregarDadosIniciais = async () => {
      try {
        // Carrega as contas bancárias exatamente como o restante do projeto já faz
        const resContas = await api.get(
          "/contas-bancarias/listar-conta-bancaria",
        );
        setContas(resContas.data);

        const resTransferencias = await api.get(
          "/transferencias/listar-transferencias",
        );
        setTransferencias(resTransferencias.data);
      } catch (error) {
        console.error("Erro ao carregar transferências:", error);
        setErro(
          "Erro ao sincronizar informações de transferências com o servidor.",
        );
      }
    };

    carregarDadosIniciais();
  }, []);

  // Preenche o formulário com os dados da transferência selecionada para alteração
  const handleAtivarEdicao = (id) => {
    setIdEdicao(id);
    setErro("");

    // Busca o objeto na memória tratando divergências de PascalCase (C#) e camelCase (JS)
    const transferenciaSelecionada = transferencias.find(
      (t) => t.id === id || t.Id === id,
    );

    if (transferenciaSelecionada) {
      const valorBanco =
        transferenciaSelecionada.valor ?? transferenciaSelecionada.Valor ?? "";
      const observacaoBanco =
        transferenciaSelecionada.observacao ??
        transferenciaSelecionada.Observacao ??
        "";
      const nomeOrigemBanco =
        transferenciaSelecionada.contaOrigem ??
        transferenciaSelecionada.ContaOrigem ??
        "";
      const nomeDestinoBanco =
        transferenciaSelecionada.contaDestino ??
        transferenciaSelecionada.ContaDestino ??
        "";
      const dataBanco =
        transferenciaSelecionada.data ?? transferenciaSelecionada.Data ?? "";

      // A listagem retorna apenas os nomes das contas, então localizamos o ID
      // correspondente comparando com a lista de contas bancárias já carregada.
      const contaOrigemEncontrada = contas.find(
        (c) => (c.nome ?? c.Nome) === nomeOrigemBanco,
      );
      const contaDestinoEncontrada = contas.find(
        (c) => (c.nome ?? c.Nome) === nomeDestinoBanco,
      );

      setContaOrigemId(
        contaOrigemEncontrada
          ? String(contaOrigemEncontrada.id ?? contaOrigemEncontrada.Id)
          : "",
      );
      setContaDestinoId(
        contaDestinoEncontrada
          ? String(contaDestinoEncontrada.id ?? contaDestinoEncontrada.Id)
          : "",
      );
      setValor(String(valorBanco));
      setObservacao(observacaoBanco);
      // Extrai apenas a parte "AAAA-MM-DD" para preencher o input type="date"
      setData(dataBanco ? String(dataBanco).split("T")[0] : hoje);
    } else {
      setErro("Transferência não encontrada localmente.");
    }
  };

  // Centraliza as operações de persistência (POST para criação e PATCH para atualização)
  const handleSalvar = async (e) => {
    e.preventDefault(); // Retém o comportamento nativo de recarga do formulário HTML
    setErro("");

    // Validações de negócio replicadas no front-end para feedback imediato ao usuário
    if (!contaOrigemId || !contaDestinoId) {
      setErro("Selecione a conta de origem e a conta de destino.");
      return;
    }

    if (contaOrigemId === contaDestinoId) {
      setErro("A conta de origem e a conta de destino não podem ser a mesma.");
      return;
    }

    const valorNumerico = Number(valor);
    if (!valorNumerico || valorNumerico <= 0) {
      setErro("O valor da transferência deve ser maior que zero.");
      return;
    }

    // Estrutura o DTO exatamente como esperado pela API do .NET
    const dadosParaEnviar = {
      contaOrigemId: Number(contaOrigemId),
      contaDestinoId: Number(contaDestinoId),
      valor: valorNumerico,
      observacao: observacao || null,
      data: data || null,
    };

    try {
      if (modo === "criar") {
        await api.post("/transferencias/criar-transferencia", dadosParaEnviar);
      } else {
        await api.put(
          `/transferencias/editar-transferencia/${idEdicao}`,
          dadosParaEnviar,
        );
      }

      // Reset completo do formulário para o estado inicial pós-sucesso
      setContaOrigemId("");
      setContaDestinoId("");
      setValor("");
      setObservacao("");
      setData(hoje);
      setIdEdicao(null);

      // Revalida os dados da tela disparando um GET para obter a lista atualizada do banco
      const resTransferencias = await api.get(
        "/transferencias/listar-transferencias",
      );
      setTransferencias(resTransferencias.data);
    } catch (error) {
      console.error("Erro detectado:", error.response);
      // Fallback de erro tratando tanto strings diretas quanto objetos de exceção estruturados
      const mensagem =
        typeof error.response?.data === "string"
          ? error.response.data
          : error.response?.data?.message ||
            "Erro ao processar a transferência.";
      setErro(mensagem);
    }
  };

  // Remove o registro do banco de dados através do verbo DELETE
  const handleExcluir = async () => {
    if (window.confirm("Deseja realmente excluir esta transferência?")) {
      try {
        await api.delete(`/transferencias/excluir-transferencia/${idEdicao}`);
        alert("Transferência removida com sucesso!");

        // Reseta o estado do formulário para evitar que dados excluídos fiquem expostos
        setContaOrigemId("");
        setContaDestinoId("");
        setValor("");
        setObservacao("");
        setData(hoje);
        setIdEdicao(null);

        // Atualiza a listagem local para expurgar o item deletado do grid
        const resTransferencias = await api.get(
          "/transferencias/listar-transferencias",
        );
        setTransferencias(resTransferencias.data);
      } catch (error) {
        console.error("Erro ao excluir:", error);
        const mensagemErro =
          error.response?.data?.message || "Erro ao excluir a transferência.";
        setErro(mensagemErro);
      }
    }
  };

  // Formata o valor monetário para exibição na listagem
  const formatarValor = (valorBruto) => {
    const numero = Number(valorBruto ?? 0);
    return numero.toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });
  };

  // Formata a data/hora para exibição na listagem
  const formatarData = (dataBruta) => {
    if (!dataBruta) return "";
    const data = new Date(dataBruta);
    return data.toLocaleString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div className="transferencia-page">
      <div className="transferencia-card">
        <header className="transferencia-header">
          {/* Alternância de títulos baseada no estado de edição */}
          <h1>
            {modo === "criar" ? "Nova Transferência" : "Editar Transferência"}
          </h1>
          <p className="descricao-header">
            {modo === "criar"
              ? "Movimente valores entre suas contas bancárias cadastradas."
              : "Altere as informações da transferência selecionada."}
          </p>
        </header>

        <form onSubmit={handleSalvar} className="transferencias-box">
          <div className="grid-form">
            <div className="detalhes-wrapper">
              <label>Conta de Origem</label>
              <select
                className="item-transferencia"
                value={contaOrigemId}
                onChange={(e) => setContaOrigemId(e.target.value)}
                required
              >
                <option value="">Selecione a conta de origem</option>
                {contas.map((conta) => {
                  const contaId = conta.id ?? conta.Id;
                  const contaNome = conta.nome ?? conta.Nome;
                  return (
                    <option key={contaId} value={contaId}>
                      {contaNome}
                    </option>
                  );
                })}
              </select>
            </div>

            <div className="detalhes-wrapper">
              <label>Conta de Destino</label>
              <select
                className="item-transferencia"
                value={contaDestinoId}
                onChange={(e) => setContaDestinoId(e.target.value)}
                required
              >
                <option value="">Selecione a conta de destino</option>
                {contas
                  // Impede que a conta de origem seja selecionável também como destino
                  .filter(
                    (conta) =>
                      String(conta.id ?? conta.Id) !== String(contaOrigemId),
                  )
                  .map((conta) => {
                    const contaId = conta.id ?? conta.Id;
                    const contaNome = conta.nome ?? conta.Nome;
                    return (
                      <option key={contaId} value={contaId}>
                        {contaNome}
                      </option>
                    );
                  })}
              </select>
            </div>
          </div>

          <div className="grid-form">
            <div className="detalhes-wrapper">
              <label>Valor</label>
              <input
                type="number"
                className="item-transferencia"
                placeholder="0,00"
                value={valor}
                onChange={(e) => setValor(e.target.value)}
                step="0.01"
                min="0.01"
                required
              />
            </div>

            <div className="detalhes-wrapper">
              <label>Data da Transferência</label>
              <input
                type="date"
                className="item-transferencia"
                value={data}
                onChange={(e) => setData(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="detalhes-wrapper">
            <label>Observação (opcional)</label>
            <textarea
              className="item-transferencia campo-textarea"
              placeholder="Ex: Reserva de emergência, transferência para carteira..."
              value={observacao}
              onChange={(e) => setObservacao(e.target.value)}
              maxLength={300}
            />
          </div>

          <div className="acoes-form-container">
            <button type="submit" className="btn-salvar">
              {modo === "criar" ? "Criar Transferência" : "Salvar Alterações"}
            </button>

            {/* Renderização condicional do botão de exclusão: visível apenas em modo de edição */}
            {modo === "editar" && (
              <button
                type="button"
                onClick={handleExcluir}
                className="btn-deletar"
              >
                Excluir Transferência
              </button>
            )}
          </div>

          {erro && <p className="mensagem-erro">{erro}</p>}
        </form>
      </div>

      <div className="transferencia-card listagem-transferencias-card">
        <div className="listagem-transferencias-section">
          <h2>
            Transferências Realizadas
            <span className="listagem-contador">
              {transferencias.length}{" "}
              {transferencias.length === 1 ? "transferência" : "transferências"}
            </span>
          </h2>
          <div className="transferencias-grid-lista">
            {transferencias.length === 0 ? (
              <p className="txt-vazio">
                Nenhuma transferência encontrada no banco de dados.
              </p>
            ) : (
              transferencias.map((transf) => {
                // Mapeamento local blindado contra diferenças de grafia de JSON vindo da API
                const idValido = transf.id ?? transf.Id;
                const valorValido = transf.valor ?? transf.Valor;
                const dataValida = transf.data ?? transf.Data;
                const origemValida = transf.contaOrigem ?? transf.ContaOrigem;
                const destinoValida =
                  transf.contaDestino ?? transf.ContaDestino;
                const observacaoValida = transf.observacao ?? transf.Observacao;

                return (
                  <div key={idValido} className="transferencia-item-card">
                    <div className="transferencia-item-info">
                      <div className="contas-transferencia">
                        <span className="conta-nome">{origemValida}</span>
                        <i className="bi bi-arrow-right seta-transferencia"></i>
                        <span className="conta-nome">{destinoValida}</span>
                      </div>

                      <div className="detalhes-transferencia">
                        <span className="valor-transferencia">
                          {formatarValor(valorValido)}
                        </span>
                        <span className="data-transferencia">
                          {formatarData(dataValida)}
                        </span>
                      </div>

                      {observacaoValida && (
                        <p className="observacao-transferencia">
                          {observacaoValida}
                        </p>
                      )}
                    </div>

                    <div className="acoes-lista-wrapper">
                      <button
                        type="button"
                        onClick={() => handleAtivarEdicao(idValido)}
                        className="btn-editar-lista"
                        title="Editar Transferência"
                      >
                        <i className="bi bi-pencil-square"></i>
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      </div>

      <Link to="/dashboard" className="link-voltar">
        Voltar para o Dashboard
      </Link>
    </div>
  );
}

export default Transferencia;