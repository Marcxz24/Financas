import { useState, useEffect } from "react";
import api from "../../services/api";
import "../CartaoCredito/CartaoCredito.css";

function CartaoCredito() {
  const [erro, setErro] = useState("");

  const [nome, setNome] = useState("");
  const [limite, setLimite] = useState("");
  const [diaFechamento, setDiaFechamento] = useState(1);
  const [diaVencimento, setDiaVencimento] = useState(10);

  const [cartoes, setCartoes] = useState([]);
  const [idEdicao, setIdEdicao] = useState(null);

  const modo = idEdicao ? "editar" : "criar";

  useEffect(() => {
    const carregar = async () => {
      try {
        const res = await api.get("/cartoes-credito/listar-cartoes-credito");
        setCartoes(res.data);
      } catch (error) {
        console.error(error);
        setErro("Erro ao carregar cartões de crédito.");
      }
    };

    carregar();
  }, []);

  const carregarCartoes = async () => {
    const res = await api.get("/cartoes-credito/listar-cartoes-credito");
    setCartoes(res.data);
  };

  const handleAtivarEdicao = (id) => {
    setErro("");
    setIdEdicao(id);

    const cartao = cartoes.find((c) => c.id === id || c.Id === id);

    if (!cartao) {
      setErro("Cartão não encontrado.");
      return;
    }

    setNome(cartao.nome ?? cartao.Nome ?? "");
    setLimite(cartao.limite ?? cartao.Limite ?? "");
    setDiaFechamento(cartao.diaFechamento ?? cartao.DiaFechamento ?? 1);
    setDiaVencimento(cartao.diaVencimento ?? cartao.DiaVencimento ?? 10);
  };

  const handleSalvar = async (e) => {
    e.preventDefault();
    setErro("");

    const payload = {
      nome: nome.trim(),
      limite: Number(limite),
      diaFechamento: Number(diaFechamento),
      diaVencimento: Number(diaVencimento),
    };

    try {
      if (modo === "criar") {
        await api.post("/cartoes-credito/criar-cartao-credito", payload);
      } else {
        await api.patch(
          `/cartoes-credito/atualizar-cartao-credito/${idEdicao}`,
          payload,
        );
      }

      setNome("");
      setLimite("");
      setDiaFechamento(1);
      setDiaVencimento(10);
      setIdEdicao(null);

      carregarCartoes();
    } catch (error) {
      console.error(error);
      setErro("Erro ao salvar cartão de crédito.");
    }
  };

  const handleExcluir = async () => {
    if (!window.confirm("Deseja excluir este cartão?")) return;

    try {
      await api.delete(`/cartoes-credito/deletar-cartao-credito/${idEdicao}`);

      setNome("");
      setLimite("");
      setDiaFechamento(1);
      setDiaVencimento(10);
      setIdEdicao(null);

      carregarCartoes();
    } catch (error) {
      console.error(error);
      setErro("Erro ao excluir cartão.");
    }
  };

  return (
    <div className="cartao-page">
      <div className="cartao-card">
        <header className="cartao-header">
          <h1>
            {modo === "criar" ? "Novo Cartão de Crédito" : "Editar Cartão"}
          </h1>
          <p className="descricao-header">
            Gerencie seus cartões e configure limites e vencimentos.
          </p>
        </header>

        <form onSubmit={handleSalvar} className="cartao-form">
          <div className="campo">
            <label>Nome do Cartão</label>
            <input value={nome} onChange={(e) => setNome(e.target.value)} />
          </div>

          <div className="grid-form">
            <div className="campo">
              <label>Limite</label>
              <input
                type="number"
                value={limite}
                onChange={(e) => setLimite(e.target.value)}
              />
            </div>

            <div className="campo">
              <label>Dia do Fechamento</label>
              <input
                type="number"
                value={diaFechamento}
                onChange={(e) => setDiaFechamento(e.target.value)}
              />
            </div>

            <div className="campo">
              <label>Dia do Vencimento</label>
              <input
                type="number"
                value={diaVencimento}
                onChange={(e) => setDiaVencimento(e.target.value)}
              />
            </div>
          </div>

          <div className="acoes">
            <button className="btn-salvar" type="submit">
              {modo === "criar" ? "Criar Cartão" : "Salvar Alterações"}
            </button>

            {modo === "editar" && (
              <button
                type="button"
                className="btn-deletar"
                onClick={handleExcluir}
              >
                Excluir
              </button>
            )}
          </div>

          {erro && <p className="erro">{erro}</p>}
        </form>

        {modo === "criar" && (
          <section className="contas-listagem-section">
            <h2>
              Cartões de Crédito
              <span className="listagem-contador">
                {cartoes.length} {cartoes.length === 1 ? "cartão" : "cartões"}
              </span>
            </h2>

            <div className="contas-grid">
              {cartoes.length === 0 ? (
                <p className="txt-vazio">Nenhum cartão cadastrado.</p>
              ) : (
                cartoes.map((c) => {
                  const id = c.id ?? c.Id;

                  const limite = c.limite ?? c.Limite;
                  const limiteDisponivel =
                    c.limiteDisponivel ?? c.LimiteDisponivel;

                  const limiteUtilizado =
                    c.limiteUtilizado ?? c.LimiteUtilizado;

                  const percentualUtilizado =
                    c.percentualUtilizado ?? c.PercentualUtilizado;

                  return (
                    <div key={id} className="conta-card">
                      <div className="conta-info">
                        <h3>{c.nome ?? c.Nome}</h3>

                        <span>
                          Fechamento: {c.diaFechamento ?? c.DiaFechamento} •
                          Vencimento: {c.diaVencimento ?? c.DiaVencimento}
                        </span>

                        <div className="cartao-limites">
                          <div className="limite-disponivel">
                            {Number(limiteDisponivel).toLocaleString("pt-BR", {
                              style: "currency",
                              currency: "BRL",
                            })}
                          </div>

                          <div className="texto-disponivel">Disponível</div>

                          <div className="barra-limite">
                            <div
                              className="barra-utilizada"
                              style={{
                                width: `${percentualUtilizado}%`,
                              }}
                            />
                          </div>

                          <div className="percentual-utilizado">
                            {Number(percentualUtilizado).toFixed(2)}% do limite
                            utilizado
                          </div>

                          <div className="resumo-item">
                            <label className="resumo-label">Utilizado</label>

                            <strong className="resumo-valor">
                              {Number(limiteUtilizado).toLocaleString("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              })}
                            </strong>
                          </div>

                          <div className="resumo-item">
                            <label className="resumo-label">Limite</label>

                            <strong className="resumo-valor">
                              {Number(limite).toLocaleString("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              })}
                            </strong>
                          </div>
                        </div>
                      </div>

                      <div className="conta-actions">
                        <button
                          onClick={() => handleAtivarEdicao(id)}
                          title="Editar"
                        >
                          <i className="bi bi-pencil-square"></i>
                        </button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

export default CartaoCredito;
