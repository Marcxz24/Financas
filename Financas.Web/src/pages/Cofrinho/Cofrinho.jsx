import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import api from "../../services/api";
import "./Cofrinho.css";

// Componente responsável por centralizar o ciclo de vida do módulo de cofrinhos:
// carregamento inicial, persistência, transferências e renderização da listagem.
function Cofrinho() {
  // Guarda de proteção para evitar submissões concorrentes em operações sensíveis.
  const operandoRef = useRef(false);

  // Estado principal com a coleção retornada pela API e os dados do formulário local.
  const [cofrinhos, setCofrinhos] = useState([]);
  const [contas, setContas] = useState([]);
  const [carregando, setCarregando] = useState(false);
  const [operando, setOperando] = useState(false);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");
  const [formulario, setFormulario] = useState({ id: null, nome: "" });
  const [modoEdicao, setModoEdicao] = useState(false);
  const [transferencia, setTransferencia] = useState({ contaBancariaId: "", cofrinhoId: "", valor: "" });
  const [tipoTransferencia, setTipoTransferencia] = useState("para-cofrinho");
  // Armazena o resultado da consulta detalhada por ID para exibir saldo e metadados.
  const [cofrinhoDetalhado, setCofrinhoDetalhado] = useState(null);
  // Controla o estado visual de uma consulta assíncrona específica para o detalhe.
  const [consultandoDetalhe, setConsultandoDetalhe] = useState(false);

  // Centraliza a limpeza dos estados de resposta para manter a UI consistente.
  const limparFeedback = () => {
    setErro("");
    setSucesso("");
  };

  // Busca os dados de leitura em paralelo para reduzir o tempo de carregamento inicial.
  const buscarDados = useCallback(async () => {
    try {
      const [cofrinhosResponse, contasResponse] = await Promise.all([
        api.get("/cofrinhos"),
        api.get("/contas-bancarias/listar-conta-bancaria"),
      ]);

      setCofrinhos(cofrinhosResponse.data || []);
      setContas(contasResponse.data || []);
    } catch (err) {
      if (import.meta.env.DEV) {
        console.error("Erro ao buscar dados do módulo de cofrinhos:", err);
      }
      setErro("Não foi possível carregar seus cofrinhos neste momento.");
    }
  }, []);

  const carregarDados = useCallback(async () => {
    setCarregando(true);
    try {
      await buscarDados();
    } finally {
      setCarregando(false);
    }
  }, [buscarDados]);

  useEffect(() => {
    let ignore = false;

    const carregar = async () => {
      setCarregando(true);
      await carregarDados();
      if (!ignore) {
        setCarregando(false);
      }
    };

    carregar();
    return () => {
      ignore = true;
    };
  }, [carregarDados]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormulario((prev) => ({ ...prev, [name]: value }));
  };

  const resetarFormulario = () => {
    setFormulario({ id: null, nome: "" });
    setModoEdicao(false);
  };

  // Preenche o formulário com os dados do item selecionado para edição inline.
  const prepararEdicao = (cofrinho) => {
    setFormulario({ id: cofrinho.id, nome: cofrinho.nome });
    setModoEdicao(true);
    setCofrinhoDetalhado(null);
    limparFeedback();
    document.querySelector(".dashboard-content")?.scrollTo({ top: 0, behavior: "smooth" });
  };

  // Consulta detalhada por ID para recuperar saldo atualizado sem depender apenas da listagem.
  const consultarCofrinho = async (id) => {
    if (operandoRef.current) return;

    operandoRef.current = true;
    setConsultandoDetalhe(true);
    limparFeedback();

    try {
      const [detalheResponse, saldoResponse] = await Promise.all([
        api.get(`/cofrinhos/${id}`),
        api.get(`/cofrinhos/saldo/${id}`),
      ]);

      const detalhe = detalheResponse.data || {};
      const saldo = saldoResponse.data;
      const saldoValor = saldo?.saldo ?? saldo?.valor ?? saldo ?? detalhe.saldo ?? 0;

      setCofrinhoDetalhado({ ...detalhe, saldo: saldoValor });
    } catch (err) {
      if (import.meta.env.DEV) {
        console.error("Erro ao consultar cofrinho:", err);
      }
      setErro(err.response?.data?.mensagem || "Não foi possível consultar o cofrinho.");
    } finally {
      setConsultandoDetalhe(false);
      operandoRef.current = false;
    }
  };

  // Persiste o recurso no backend: cria quando não há ID, atualiza quando existe.
  const handleSalvar = async (e) => {
    if (e) e.preventDefault();

    if (!formulario.nome?.trim()) {
      setErro("Informe o nome do cofrinho.");
      return;
    }

    if (operandoRef.current) return;
    operandoRef.current = true;
    setOperando(true);
    limparFeedback();

    try {
      if (modoEdicao) {
        await api.put(`/cofrinhos/${formulario.id}`, { nome: formulario.nome.trim() });
      } else {
        await api.post("/cofrinhos/criar", { nome: formulario.nome.trim(), saldo: 0 });
      }

      setSucesso(modoEdicao ? "Cofrinho atualizado com sucesso." : "Cofrinho criado com sucesso.");
      setCofrinhoDetalhado(null);
      resetarFormulario();
      await carregarDados();
    } catch (err) {
      if (import.meta.env.DEV) {
        console.error("Erro ao salvar cofrinho:", err);
      }
      setErro(err.response?.data?.mensagem || "Não foi possível salvar o cofrinho.");
      setSucesso("");
    } finally {
      setOperando(false);
      operandoRef.current = false;
    }
  };

  // Exclui o registro após confirmação explícita do usuário.
  const handleExcluir = async (id) => {
    if (!window.confirm("Deseja realmente excluir este cofrinho?")) return;
    if (operandoRef.current) return;

    operandoRef.current = true;
    setOperando(true);
    limparFeedback();

    try {
      await api.delete(`/cofrinhos/${id}`);
      setSucesso("Cofrinho removido com sucesso.");
      setCofrinhoDetalhado(null);
      await carregarDados();
    } catch (err) {
      if (import.meta.env.DEV) {
        console.error("Erro ao excluir cofrinho:", err);
      }
      setErro(err.response?.data?.mensagem || "Não foi possível excluir o cofrinho.");
    } finally {
      setOperando(false);
      operandoRef.current = false;
    }
  };

  // Encapsula o fluxo de transferência entre conta bancária e cofrinho.
  const handleTransferencia = async (e) => {
    e.preventDefault();

    if (!transferencia.contaBancariaId || !transferencia.cofrinhoId || !transferencia.valor) {
      setErro("Selecione a conta, o cofrinho e informe um valor válido.");
      return;
    }

    const valor = Number(transferencia.valor);
    if (!Number.isFinite(valor) || valor <= 0) {
      setErro("O valor deve ser maior que zero.");
      return;
    }

    if (operandoRef.current) return;
    operandoRef.current = true;
    setOperando(true);
    limparFeedback();

    try {
      const payload = {
        contaBancariaId: Number(transferencia.contaBancariaId),
        cofrinhoId: Number(transferencia.cofrinhoId),
        valor,
      };

      if (tipoTransferencia === "para-cofrinho") {
        await api.post("/cofrinhos/transferir-para-cofrinho", payload);
        setSucesso("Transferência para o cofrinho realizada com sucesso.");
      } else {
        await api.post("/cofrinhos/resgatar", payload);
        setSucesso("Resgate realizado com sucesso.");
      }

      setTransferencia({ contaBancariaId: "", cofrinhoId: "", valor: "" });
      setCofrinhoDetalhado(null);
      await carregarDados();
    } catch (err) {
      if (import.meta.env.DEV) {
        console.error("Erro na transferência:", err);
      }
      setErro(err.response?.data?.mensagem || "Não foi possível concluir a operação.");
      setSucesso("");
    } finally {
      setOperando(false);
      operandoRef.current = false;
    }
  };

  // Reduz a coleção para o saldo consolidado exibido no cabeçalho da tela.
  const totalGeral = useMemo(() => cofrinhos.reduce((acc, item) => acc + Number(item.saldo || 0), 0), [cofrinhos]);

  if (carregando) {
    return (
      <div className="loading-center">
        <div className="spinner" />
      </div>
    );
  }

  return (
    <div className="cofrinho-page">
      {operando && (
        <div className="global-overlay">
          <div className="spinner" />
          <p>Processando operação...</p>
        </div>
      )}

      <header className="cofrinho-header">
        <div>
          <h1><i className="bi bi-piggy-bank"></i> Cofrinhos</h1>
          <p>Organize reservas financeiras separadas e acompanhe transferências entre contas e cofrinhos.</p>
        </div>
        <div className="cofrinho-badge">
          <i className="bi bi-wallet2"></i>
          Total: R$ {Number(totalGeral).toFixed(2)}
        </div>
      </header>

      {erro && <div className="cofrinho-alerta erro">{erro}</div>}
      {sucesso && <div className="cofrinho-alerta sucesso">{sucesso}</div>}

      <div className="cofrinho-grid">
        <section className="cofrinho-card">
          <h3>{modoEdicao ? "Editar cofrinho" : "Novo cofrinho"}</h3>
          <form className="cofrinho-form" onSubmit={handleSalvar}>
            <div className="cofrinho-form-grid">
              <div className="cofrinho-input-group">
                <label htmlFor="nome">Nome do cofrinho</label>
                <input
                  id="nome"
                  name="nome"
                  type="text"
                  value={formulario.nome}
                  onChange={handleInputChange}
                  placeholder="Ex: Reserva de viagem"
                  required
                />
              </div>
            </div>
            <div className="cofrinho-actions">
              <button className="cofrinho-btn cofrinho-btn-primary" type="submit" disabled={operando}>
                {modoEdicao ? "Salvar alterações" : "Criar cofrinho"}
              </button>
              {modoEdicao && (
                <button className="cofrinho-btn cofrinho-btn-secondary" type="button" onClick={resetarFormulario} disabled={operando}>
                  Cancelar
                </button>
              )}
            </div>
          </form>
        </section>

        <section className="cofrinho-card">
          <h3>Transferência</h3>
          <form className="cofrinho-form" onSubmit={handleTransferencia}>
            <div className="cofrinho-form-grid">
              <div className="cofrinho-input-group">
                <label htmlFor="tipoTransferencia">Tipo</label>
                <select id="tipoTransferencia" value={tipoTransferencia} onChange={(e) => setTipoTransferencia(e.target.value)}>
                  <option value="para-cofrinho">Conta → Cofrinho</option>
                  <option value="para-conta">Cofrinho → Conta</option>
                </select>
              </div>
              <div className="cofrinho-input-group">
                <label htmlFor="contaBancariaId">Conta bancária</label>
                <select
                  id="contaBancariaId"
                  value={transferencia.contaBancariaId}
                  onChange={(e) => setTransferencia((prev) => ({ ...prev, contaBancariaId: e.target.value }))}
                  required
                >
                  <option value="">Selecione...</option>
                  {contas.map((conta) => (
                    <option key={conta.id} value={conta.id}>
                      {conta.nome}
                    </option>
                  ))}
                </select>
              </div>
              <div className="cofrinho-input-group">
                <label htmlFor="cofrinhoId">Cofrinho</label>
                <select
                  id="cofrinhoId"
                  value={transferencia.cofrinhoId}
                  onChange={(e) => setTransferencia((prev) => ({ ...prev, cofrinhoId: e.target.value }))}
                  required
                >
                  <option value="">Selecione...</option>
                  {cofrinhos.map((cofrinho) => (
                    <option key={cofrinho.id} value={cofrinho.id}>
                      {cofrinho.nome}
                    </option>
                  ))}
                </select>
              </div>
              <div className="cofrinho-input-group">
                <label htmlFor="valor">Valor</label>
                <input
                  id="valor"
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={transferencia.valor}
                  onChange={(e) => setTransferencia((prev) => ({ ...prev, valor: e.target.value }))}
                  placeholder="0,00"
                  required
                />
              </div>
            </div>
            <p className="cofrinho-help">
              {tipoTransferencia === "para-cofrinho"
                ? "Use esta opção para transferir saldo da conta bancária para um cofrinho."
                : "Use esta opção para resgatar saldo de um cofrinho para a conta bancária."}
            </p>
            <div className="cofrinho-actions">
              <button className="cofrinho-btn cofrinho-btn-primary" type="submit" disabled={operando}>
                Confirmar operação
              </button>
            </div>
          </form>
        </section>
      </div>

      <section className="cofrinho-card">
        <div className="cofrinho-summary">
          <h3>Meus cofrinhos</h3>
          <span className="cofrinho-total">Saldo total consolidado: R$ {Number(totalGeral).toFixed(2)}</span>
        </div>

        {cofrinhoDetalhado && (
          <div className="cofrinho-detail">
            <h4>Detalhes do cofrinho</h4>
            <p><strong>Nome:</strong> {cofrinhoDetalhado.nome || "-"}</p>
            <p><strong>Saldo atual:</strong> R$ {Number(cofrinhoDetalhado.saldo || 0).toFixed(2)}</p>
            <p><strong>ID:</strong> {cofrinhoDetalhado.id || "-"}</p>
            <button className="cofrinho-btn cofrinho-btn-secondary" type="button" onClick={() => setCofrinhoDetalhado(null)}>
              Fechar
            </button>
          </div>
        )}

        {cofrinhos.length === 0 ? (
          <div className="cofrinho-empty">Nenhum cofrinho cadastrado ainda.</div>
        ) : (
          <div className="cofrinho-list">
            {cofrinhos.map((cofrinho) => (
              <article key={cofrinho.id} className="cofrinho-item">
                <div className="cofrinho-item-main">
                  <h4>{cofrinho.nome}</h4>
                  <p className="cofrinho-item-saldo">Saldo: R$ {Number(cofrinho.saldo || 0).toFixed(2)}</p>
                  <p className="cofrinho-item-meta">Status: {cofrinho.status || "Ativo"}</p>
                </div>
                <div className="cofrinho-actions">
                  <button className="cofrinho-btn cofrinho-btn-secondary" type="button" onClick={() => consultarCofrinho(cofrinho.id)} disabled={operando || consultandoDetalhe}>
                    {consultandoDetalhe ? "Consultando..." : "Consultar"}
                  </button>
                  <button className="cofrinho-btn cofrinho-btn-secondary" type="button" onClick={() => prepararEdicao(cofrinho)} disabled={operando}>
                    Editar
                  </button>
                  <button className="cofrinho-btn cofrinho-btn-danger" type="button" onClick={() => handleExcluir(cofrinho.id)} disabled={operando}>
                    Excluir
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

export default Cofrinho;
