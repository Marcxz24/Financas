import { useState, useEffect, useCallback, useRef } from "react";
import api from "../../services/api";
import "./ContaBancaria.css";

function ContaBancaria() {
  // useRef garante leitura imediata do status de processamento, evitando submissões duplicadas
  const operandoRef = useRef(false);

  // Estados para controle de UI, feedback de mensagens e armazenamento das contas
  const [contas, setContas] = useState([]);
  const [carregando, setCarregando] = useState(false);
  const [operando, setOperando] = useState(false);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");

  // Estado do formulário: reflete o DTO que a API espera receber
  const [formulario, setFormulario] = useState({
    id: null,
    nomeContaBancaria: "",
    tipoContaBancaria: 1,
    saldoContaBancaria: 0,
  });

  const [modoEdicao, setModoEdicao] = useState(false);

  // Busca as contas no backend; useCallback otimiza a performance evitando re-criação da função
  const buscarContas = useCallback(async () => {
    try {
      const response = await api.get("/contas-bancarias/listar-conta-bancaria");
      setContas(response.data);
    } catch (err) {
      console.error("Erro ao buscar contas:", err);
      setErro("Não foi possível carregar suas contas bancárias.");
    }
  }, []);

  // Efeito de inicialização: busca as contas ao montar o componente
  useEffect(() => {
    let ignore = false;

    const carregarDadosIniciais = async () => {
      setCarregando(true);
      await buscarContas();
      if (!ignore) setCarregando(false);
    };

    carregarDadosIniciais();
    return () => {
      ignore = true;
    }; // Cleanup para evitar atualizações em componentes desmontados
  }, [buscarContas]);

  // Atualiza o estado do formulário conforme o usuário digita
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    let parsedValue = value;

    // Garante que campos numéricos não sejam enviados como string para a API
    if (name === "tipoContaBancaria" || name === "saldoContaBancaria") {
      parsedValue = value === "" ? "" : Number(value);
    }

    setFormulario((prev) => ({ ...prev, [name]: parsedValue }));
  };

  // Lógica principal de persistência (Criação ou Edição)
  const handleSalvar = async (e) => {
    if (e) e.preventDefault();

    // Trava de segurança contra cliques duplos (debounce)
    if (operandoRef.current) return;

    operandoRef.current = true;
    setOperando(true);

    const payload = {
      nome: formulario.nomeContaBancaria,
      tipo: Number(formulario.tipoContaBancaria),
      saldo: Number(formulario.saldoContaBancaria),
    };

    try {
      if (modoEdicao) {
        await api.patch(
          `/contas-bancarias/Atualizar-conta-bancaria/${formulario.id}`,
          payload,
        );
      } else {
        await api.post("/contas-bancarias/criar-conta-bancaria", payload);
      }

      resetarFormulario();
      await buscarContas(); // Recarrega a listagem após sucesso
    } catch (err) {
      console.error("Erro ao salvar:", err);
      setErro("Não foi possível salvar a conta.");
    } finally {
      setOperando(false);
      operandoRef.current = false;
    }
  };

  // Exclusão com cascata configurada no backend
  const handleExcluir = async (id) => {
    if (
      !window.confirm(
        "Deseja realmente excluir esta conta? Lançamentos vinculados serão removidos.",
      )
    )
      return;

    if (operandoRef.current) return;
    operandoRef.current = true;
    setOperando(true);

    try {
      await api.delete(`/contas-bancarias/Deletar-conta-bancaria/${id}`);
      setSucesso("Conta removida com sucesso!");
      await buscarContas();
    } catch (err) {
      setErro(err.response?.data?.mensagem || "Erro ao excluir conta.");
    } finally {
      setOperando(false);
      operandoRef.current = false;
    }
  };

  // Popula o formulário para edição e mapeia o tipo do enum
  const prepararEdicao = (conta) => {
    const enumMap = {
      Corrente: 1,
      Poupanca: 2,
      Digital: 3,
      Salario: 4,
      Pagamento: 5,
      Investimento: 6,
      Universitaria: 7,
      Internacional: 8,
      Fisica: 9
    };

    setFormulario({
      id: conta.id,
      nomeContaBancaria: conta.nome,
      tipoContaBancaria: enumMap[conta.tipo] || 1,
      saldoContaBancaria: conta.saldo,
    });

    setModoEdicao(true);
    document
      .querySelector(".dashboard-content")
      ?.scrollTo({ top: 0, behavior: "smooth" });
  };

  const resetarFormulario = () => {
    setFormulario({
      id: null,
      nomeContaBancaria: "",
      tipoContaBancaria: 1,
      saldoContaBancaria: 0,
    });
    setModoEdicao(false);
  };

  // Exibe spinner enquanto os dados são carregados pela primeira vez
  if (carregando) {
    return (
      <div className="loading-center">
        <div className="spinner"></div>
      </div>
    );
  }

  return (
    <div className="contas-container">
      {/* Overlay global para bloquear interação durante requisições */}
      {operando && (
        <div className="global-overlay">
          <div className="spinner"></div>
          <p>Processando operação...</p>
        </div>
      )}

      <header className="contas-header">
        <h1>
          <i className="bi bi-bank"></i> Minhas Contas Bancárias
        </h1>
        <p>Gerencie seus bancos, carteiras e saldos iniciais.</p>
      </header>

      {/* Exibição de alertas de sucesso ou erro */}
      {erro && <div className="alerta alerta-erro">{erro}</div>}
      {sucesso && <div className="alerta alerta-sucesso">{sucesso}</div>}

      <section className="form-card">
        <form onSubmit={handleSalvar}>
          <div className="form-grid">
            <div className="input-group">
              <label>Nome da Conta</label>
              <input
                type="text"
                name="nomeContaBancaria"
                value={formulario.nomeContaBancaria || ""}
                onChange={handleInputChange}
                placeholder="Ex: Nubank Principal"
                required
              />
            </div>

            <div className="input-group">
              <label>Tipo de Conta</label>
              <select
                name="tipoContaBancaria"
                value={formulario.tipoContaBancaria || ""}
                onChange={handleInputChange}
                required
              >
                {!modoEdicao && (
                  <option value="" disabled>
                    Selecione um tipo...
                  </option>
                )}
                <option value={1}>Corrente</option>
                <option value={2}>Poupança</option>
                <option value={3}>Digital</option>
                <option value={4}>Salário</option>
                <option value={5}>Pagamento</option>
                <option value={6}>Investimento</option>
                <option value={7}>Universitária</option>
                <option value={8}>Internacional</option>
                <option value={9}>Física</option>
              </select>
            </div>

            <div className="input-group">
              <label>Saldo Inicial</label>
              <input
                type="number"
                name="saldoContaBancaria"
                value={formulario.saldoContaBancaria ?? 0}
                onChange={handleInputChange}
                step="0.01"
                disabled={modoEdicao}
                placeholder="0,00"
                required
              />
              {!modoEdicao && (
                <small>
                  Caso ainda não possua saldo nesta conta, mantenha o valor em
                  R$ 0,00.
                </small>
              )}

              {modoEdicao && (
                <small>
                  O saldo deve ser alterado através dos lançamentos financeiros.
                </small>
              )}
            </div>
          </div>

          <div className="form-actions">
            <button type="submit" className="btn-salvar">
              {modoEdicao ? "Salvar Alterações" : "Cadastrar Conta"}
            </button>
            {modoEdicao && (
              <button
                type="button"
                className="btn-cancelar"
                onClick={resetarFormulario}
              >
                Cancelar
              </button>
            )}
          </div>
        </form>
      </section>

      {/* Listagem das contas cadastradas */}
      <section className="contas-listagem-section">
        <h2>
          Contas Cadastradas
          <span className="listagem-contador">
            {contas.length} {contas.length === 1 ? "conta" : "contas"}
          </span>
        </h2>
        <div className="contas-grid">
          {contas.length === 0 ? (
            <p className="txt-vazio">Nenhuma conta bancária cadastrada.</p>
          ) : (
            contas.map((conta) => (
              <div key={conta.id} className="conta-card">
                <div className="conta-info">
                  <h3>{conta.nome}</h3>
                  <span>{conta.tipo}</span>
                  <div className="conta-saldo">
                    {Number(conta.saldo).toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                  </div>
                </div>
                <div className="conta-actions">
                  <button onClick={() => prepararEdicao(conta)} title="Editar">
                    <i className="bi bi-pencil-square"></i>
                  </button>
                  <button
                    onClick={() => handleExcluir(conta.id)}
                    title="Excluir"
                  >
                    <i className="bi bi-trash"></i>
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

export default ContaBancaria;
