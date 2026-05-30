import { useState, useEffect } from "react";
import api from "../../services/api";
import { Link } from "react-router-dom";
import "../Categoria/Categoria.css";

function Categoria() {
  // Estados para controle de erros e campos do formulário
  const [erro, setErro] = useState("");
  const [nome, setNome] = useState("");
  const [icone, setIcone] = useState("bi bi-tags"); // Valor padrão inicial do ícone
  const [tipo, setTipo] = useState(1);             // 1 = Receita, 2 = Despesa
  const [categorias, setCategorias] = useState([]); // Armazena a lista de categorias vinda do banco
  const [idEdicao, setIdEdicao] = useState(null);   // Controla qual ID está sendo editado (null = modo criação)

  // Variável computada para alternar dinamicamente o comportamento visual e lógico do formulário
  const modo = idEdicao ? "editar" : "criar";

  // Hook executado uma única vez no ciclo de vida para popular a listagem inicial
  useEffect(() => {
    const carregarCategoriasIniciais = async () => {
      try {
        const resCat = await api.get("/categorias/listar-categorias");
        setCategorias(resCat.data);
      } catch (error) {
        console.error("Erro ao carregar categorias:", error);
        setErro(
          "Erro ao sincronizar informações de categorias com o servidor.",
        );
      }
    };

    carregarCategoriasIniciais();
  }, []);

  // Preenche o formulário com os dados da categoria selecionada para alteração
  const handleAtivarEdicao = (id) => {
    setIdEdicao(id);
    setErro("");

    // Busca o objeto na memória tratando divergências de PascalCase (C#) e camelCase (JS)
    const categoriaSelecionada = categorias.find((cat) => cat.id === id || cat.Id === id);

    if (categoriaSelecionada) {
      // Operador ?? garante fallback caso a propriedade venha em maiúsculo do .NET
      const nomeBanco = categoriaSelecionada.nome ?? categoriaSelecionada.Nome ?? "";
      const iconeBanco = categoriaSelecionada.icone ?? categoriaSelecionada.Icone ?? "bi bi-tags";
      const tipoBruto = categoriaSelecionada.tipo ?? categoriaSelecionada.Tipo;

      // Normaliza o tipo para número inteiro (1 ou 2) prevenindo retorno de Enums textuais
      let tipoTratado = 1; 
      if (tipoBruto !== undefined && tipoBruto !== null) {
        const tipoStr = String(tipoBruto).toLowerCase().trim();
        if (tipoStr === "2" || tipoStr === "despesa") {
          tipoTratado = 2;
        }
      }

      // Sincroniza os estados locais para refletir os valores nos inputs controlados
      setNome(nomeBanco);
      setIcone(iconeBanco);
      setTipo(tipoTratado);
      
    } else {
      setErro("Categoria não encontrada localmente.");
    }
  };

  // Centraliza as operações de persistência (POST para criação e PATCH para atualização)
  const handleSalvar = async (e) => {
    e.preventDefault(); // Retém o comportamento nativo de recarga do formulário HTML
    setErro("");

    // Estrutura o DTO exatamente como esperado pela API do .NET
    const dadosParaEnviar = {
      nome: nome,
      icone: icone,
      tipo: Number(tipo), // Força casting numérico para corresponder à tipagem do back-end
    };

    try {
      if (modo === "criar") {
        await api.post("/categorias/criar-categoria", dadosParaEnviar);
      } else {
        // Envio do ID na rota mapeado no plural conforme endpoint do controlador C#
        await api.patch(
          `/categorias/atualizar-categorias/${idEdicao}`,
          dadosParaEnviar,
        );
      }

      // Reset completo do formulário para o estado inicial pós-sucesso
      setNome("");
      setIcone("bi bi-tags");
      setTipo(1);
      setIdEdicao(null);

      // Revalida os dados da tela disparando um GET para obter a lista atualizada do banco
      const resCat = await api.get("/categorias/listar-categorias");
      setCategorias(resCat.data);
    } catch (error) {
      console.error("Erro detectado:", error.response);
      // Fallback de erro tratando tanto strings diretas quanto objetos de exceção estruturados
      const mensagem =
        typeof error.response?.data === "string"
          ? error.response.data
          : error.response?.data?.message || "Erro ao processar a categoria.";
      setErro(mensagem);
    }
  };

  // Remove o registro do banco de dados através do verbo DELETE
  const handleExcluir = async () => {
    if (window.confirm("Deseja realmente excluir esta categoria?")) {
      try {
        // Rota no plural casando estritamente com o endpoint da API
        await api.delete(`/categorias/deletar-categorias/${idEdicao}`);
        alert("Categoria removida com sucesso!");

        // Reseta o estado do formulário para evitar que dados excluídos fiquem expostos
        setNome("");
        setIcone("bi bi-tags");
        setTipo(1);
        setIdEdicao(null);

        // Atualiza a listagem local para expurgar o item deletado do grid
        const resCat = await api.get("/categorias/listar-categorias");
        setCategorias(resCat.data);
      } catch (error) {
        console.error("Erro ao excluir:", error);
        // Erro genérico preventivo focado em violações de integridade referencial (FK) no banco
        const mensagemErro =
          error.response?.data?.message ||
          "Erro ao excluir a categoria. Verifique vínculos.";
        setErro(mensagemErro);
      }
    }
  };

  return (
    <div className="categoria-page">
        <div className="categoria-card">
          <header className="categoria-header">
            {/* Alternância de títulos baseada no estado de edição */}
            <h1>{modo === "criar" ? "Nova Categoria" : "Editar Categoria"}</h1>
            <p className="descricao-header">
              {modo === "criar"
                ? "Cadastre categorias personalizadas para organize seus fluxos."
                : "Altere as informações da categoria selecionada."}
            </p>
          </header>

          <form onSubmit={handleSalvar} className="categorias-box">
            <div className="detalhes-wrapper">
              <label>Nome da Categoria</label>
              <input
                type="text"
                className="item-categoria"
                placeholder="Ex: Alimentação, Combustível, Lazer..."
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                required
              />
            </div>

            <div className="grid-form">
              <div className="detalhes-wrapper">
                <label>Tipo de Fluxo</label>
                <select
                  className="item-categoria"
                  value={tipo}
                  onChange={(e) => setTipo(Number(e.target.value))}
                >
                  <option value={1}>Receita (+)</option>
                  <option value={2}>Despesa (-)</option>
                </select>
              </div>

              <div className="detalhes-wrapper">
                <label>Ícone da Categoria</label>
                <div className="input-icone-wrapper">
                  {/* Preview dinâmico injetando as classes do Bootstrap Icons vindas do estado */}
                  <i className={`${icone || "bi bi-tags"} icone-preview`}></i>
                  <select
                    className="item-categoria select-com-icone"
                    value={icone}
                    onChange={(e) => setIcone(e.target.value)}
                  >
                    <optgroup label="Geral">
                      <option value="bi bi-tags">Etiqueta Padrão</option>
                      <option value="bi bi-star">Favorito / Outros</option>
                      <option value="bi bi-bookmark">Marcador</option>
                    </optgroup>
                    <optgroup label="Entradas e Rendimentos">
                      <option value="bi bi-cash-stack">Salário / Dinheiro</option>
                      <option value="bi bi-graph-up-arrow">Investimentos / Rendimentos</option>
                      <option value="bi bi-wallet2">Carteira / Extra</option>
                      <option value="bi bi-piggy-bank">Poupança</option>
                    </optgroup>
                    <optgroup label="Despesas Comuns">
                      <option value="bi bi-cart">Supermercado / Compras</option>
                      <option value="bi bi-house">Moradia / Aluguel</option>
                      <option value="bi bi-lightning">Contas (Luz, Água, Internet)</option>
                      <option value="bi bi-car-front">Transporte / Combustível</option>
                      <option value="bi bi-heart-pulse">Saúde / Farmácia</option>
                      <option value="bi bi-mortarboard">Educação</option>
                    </optgroup>
                    <optgroup label="Lazer e Estilo de Vida">
                      <option value="bi bi-controller">Jogos / Entretenimento</option>
                      <option value="bi bi-cup-hot">Alimentação / Restaurantes</option>
                      <option value="bi bi-gift">Presentes / Doações</option>
                      <option value="bi bi-airplane">Viagens</option>
                    </optgroup>
                  </select>
                </div>
              </div>
            </div>

            <div className="acoes-form-container">
              <button type="submit" className="btn-salvar">
                {modo === "criar" ? "Cadastrar Categoria" : "Salvar Alterações"}
              </button>

              {/* Renderização condicional do botão de exclusão: visível apenas em modo de edição */}
              {modo === "editar" && (
                <button
                  type="button"
                  onClick={handleExcluir}
                  className="btn-deletar"
                >
                  Excluir Categoria
                </button>
              )}

              <Link to="/dashboard" className="link-voltar">
                Voltar para o Dashboard
              </Link>
            </div>

            {erro && <p className="mensagem-erro">{erro}</p>}
          </form>

          {/* O bloco de listagem inferior fica oculto durante a edição para focar a atenção do usuário no formulário */}
          {modo === "criar" && (
            <div className="listagem-categorias-section">
              <h2>
                Categorias Ativas
                <span className="listagem-contador">
                  {categorias.length} {categorias.length === 1 ? "categoria" : "categorias"}
                </span>
              </h2>
              <div className="categorias-grid-lista">
                {categorias.length === 0 ? (
                  <p className="txt-vazio">Nenhuma categoria encontrada no banco de dados.</p>
                ) : (
                  categorias.map((cat) => {
                    // Mapeamento local blindado contra diferenças de grafia de JSON vindo da API
                    const tipoValido = cat.tipo ?? cat.Tipo;
                    const nomeValido = cat.nome ?? cat.Nome;
                    const iconeValido = cat.icone ?? cat.Icone;
                    const idValido = cat.id ?? cat.Id;

                    // Avaliação polimórfica aceitando o tipo como número, string numérica ou string de texto plano
                    const ehReceita =
                      tipoValido === 1 ||
                      tipoValido === "1" ||
                      String(tipoValido).toLowerCase() === "receita";

                    return (
                      <div key={idValido} className="categoria-item-card">
                        <div className="categoria-item-info">
                          <i className={`${iconeValido || "bi bi-tags"} icone-cat`}></i>
                          <span>{nomeValido}</span>
                        </div>

                        <div className="acoes-lista-wrapper">
                          <button
                            type="button"
                            onClick={() => handleAtivarEdicao(idValido)}
                            className="btn-editar-lista"
                            title="Editar Categoria"
                          >
                            <i className="bi bi-pencil-square"></i>
                          </button>

                          {/* Classe CSS e Label text injetadas dinamicamente via operador ternário */}
                          <span className={`badge-tipo-cat ${ehReceita ? "cat-receita" : "cat-despesa"}`}>
                            {ehReceita ? "Receita" : "Despesa"}
                          </span>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}
        </div>
    </div>
  );
}

export default Categoria;