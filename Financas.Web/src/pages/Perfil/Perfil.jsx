// Importação dos hooks do React para controle de estado e efeitos colaterais
import { useState, useEffect } from "react";
// Importação da instância configurada do Axios para requisições HTTP à API
import api from "../../services/api";
// Importação do arquivo de estilização específico deste componente
import "./Perfil.css";

/**
 * Componente Perfil
 * Responsável por exibir e atualizar os dados cadastrais do usuário autenticado.
 * Implementa validação de formulário com suporte ao ModelState do backend (.NET) e feedback visual dinâmico.
 */
function Perfil() {
  // Estado principal (Fonte da Verdade): armazena os dados consolidados e persistidos no banco
  const [usuario, setUsuario] = useState({
    username: "",
    email: "",
    dataCadastro: "",
    emailConfirmado: false,
  });

  // Estados de controle de UI (Interface de Usuário)
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [editando, setEditando] = useState(false);

  // Estado de rascunho: gerencia o two-way data binding dos inputs durante o modo de edição
  const [formulario, setFormulario] = useState({
    username: "",
    email: "",
  });

  /**
   * Efeito colateral executado na fase de montagem (Mount) do componente.
   * Realiza a requisição HTTP GET para buscar os dados primários do usuário.
   */
  useEffect(() => {
    const buscarPerfil = async () => {
      try {
        setCarregando(true);
        setErro("");

        // A autenticação da requisição ocorre de forma implícita via token JWT injetado no interceptor do Axios
        const response = await api.get("/usuarios/visualizar-perfil");

        // Sincroniza o estado principal com o DTO retornado pela API
        setUsuario({
          username: response.data.username,
          email: response.data.email,
          dataCadastro: response.data.dataCadastro,
          emailConfirmado: response.data.emailConfirmado,
        });

        // Preenche o estado de rascunho para que o formulário de edição inicialize com os dados corretos
        setFormulario({
          username: response.data.username,
          email: response.data.email,
        });
      } catch (err) {
        console.error("Erro na busca de dados do perfil:", err);
        setErro(
          err.response?.data?.mensagem ||
            "Não foi possível carregar os dados do perfil."
        );
      } finally {
        // Assegura a liberação da thread de carregamento da UI independentemente do sucesso ou falha na rede
        setCarregando(false);
      }
    };

    buscarPerfil();
  }, []);

  // Early Return: Exibe o fallback de carregamento e impede a renderização da estrutura do card
  // caso a Promise de busca dos dados iniciais ainda esteja pendente
  if (carregando) {
    return (
      <div className="perfil-loading-container">
        <div className="perfil-spinner"></div>
        <p>Carregando informações do perfil...</p>
      </div>
    );
  }

  /**
   * Habilita o modo de edição do card.
   * Sobrescreve o rascunho com os dados oficiais mais recentes para prevenir inconsistências de estado.
   */
  const handleEntrarModoEdicao = () => {
    setFormulario({
      username: usuario.username,
      email: usuario.email,
    });
    setEditando(true);
  };

  /**
   * Manipulador genérico para atualização de inputs controlados no React.
   * Utiliza desestruturação para inferir a chave do estado dinamicamente através do atributo 'name'.
   */
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormulario((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  /**
   * Processa a submissão das alterações do perfil.
   * Realiza validação otimista e gerencia o parser de erros HTTP mapeando os DataAnnotations do C#.
   */
  const handleSalvarInformacoes = async () => {
    try {
      setErro("");

      // Validação otimista: previne o disparo de chamadas HTTP desnecessárias caso o payload não tenha sofrido mutação
      if (formulario.username === usuario.username) {
        setEditando(false);
        return;
      }

      setCarregando(true);

      // Disparo da mutação de dados via método PATCH (atualização parcial)
      await api.patch("/usuarios/alterar-username", {
        username: formulario.username,
      });

      // Atualiza a fonte da verdade localmente para refletir o sucesso da operação na base de dados
      setUsuario((prev) => ({
        ...prev,
        username: formulario.username,
      }));

      setEditando(false);
    } catch (err) {
      console.error("Falha na requisição PATCH (alterar-username):", err);

      // 1. Interceptação de validações de pipeline (.NET ModelState / DataAnnotations)
      const errosValidacao = err.response?.data?.errors;

      if (errosValidacao) {
        // Extrai a primeira mensagem de erro associada ao campo Username
        const mensagensDeErro =
          errosValidacao.Username || Object.values(errosValidacao)[0];

        if (mensagensDeErro && mensagensDeErro.length > 0) {
          setErro(mensagensDeErro[0]);
          return;
        }
      }

      // 2. Interceptação de exceções mapeadas de regra de negócio (ex: InvalidOperationException)
      if (err.response?.data?.mensagem) {
        setErro(err.response.data.mensagem);
        return;
      }

      // 3. Fallback genérico para instabilidade de conexão ou HTTP 500 (Internal Server Error)
      setErro(
        "Não foi possível atualizar o nome de usuário. Tente novamente mais tarde."
      );
    } finally {
      setCarregando(false);
    }
  };

  return (
    <div className="perfil-page-container">
      <div className="perfil-main-card">
        {/* Cabeçalho: Apresenta a identidade básica e indicador visual (badge) referente à integridade da conta */}
        <header className="perfil-card-header">
          <div className="perfil-avatar-placeholder">
            <i className="bi bi-person-circle"></i>
          </div>
          <h2>{usuario.username}</h2>
          <span
            className={`status-badge ${
              usuario.emailConfirmado === true ||
              usuario.emailConfirmado === "True" ||
              usuario.emailConfirmado === 1
                ? "ativo"
                : "pendente"
            }`}
          >
            {usuario.emailConfirmado === true ||
            usuario.emailConfirmado === "True" ||
            usuario.emailConfirmado === 1
              ? "Conta Verificada"
              : "Conta Pendente"}
          </span>
        </header>

        {/* Bloco de Notificação: Renderização condicional para exibição de validações ou falhas processadas no state */}
        {erro && <p className="perfil-erro-alerta">{erro}</p>}

        <div className="perfil-details-grid">
          {/* Sessão Username: Implementa renderização condicional alternando entre visualização nativa e input editável */}
          <div className="perfil-info-group">
            <label>
              <i className="bi bi-person"></i> Nome de Usuário
            </label>
            {editando ? (
              <input
                type="text"
                name="username"
                className="perfil-data-field perfil-input-editando"
                value={formulario.username}
                onChange={handleInputChange}
              />
            ) : (
              <div className="perfil-data-field">{usuario.username}</div>
            )}
          </div>

          {/* Sessão E-mail: Renderizado estritamente como texto fluido (Read-only) por conformidade com regras de segurança */}
          <div className="perfil-info-group">
            <label>
              <i className="bi bi-envelope"></i> E-mail Cadastrado
            </label>
            <div className="perfil-data-field">{usuario.email}</div>
          </div>

          {/* Sessão Data de Cadastro: Protegida por avaliação de curto-circuito (Short-circuit evaluation) para evitar parse de strings vazias */}
          {usuario.dataCadastro && (
            <div className="perfil-info-group">
              <label>
                <i className="bi bi-calendar-check"></i> Membro Desde
              </label>
              <div className="perfil-data-field">
                {new Date(usuario.dataCadastro).toLocaleDateString("pt-BR")}
              </div>
            </div>
          )}
        </div>

        {/* Rodapé de Ações: Transição CSS e alternância de controles dependentes do estado booleano 'editando' */}
        <footer
          className={`perfil-card-footer ${editando ? "modo-edicao" : ""}`}
        >
          {!editando && (
            <button
              type="button"
              className="btn-excluir-perfil"
              onClick={() => alert("Fluxo de exclusão de conta disparado.")}
            >
              <i className="bi bi-trash3"></i> Excluir Perfil
            </button>
          )}

          {editando ? (
            <button
              type="button"
              className="btn-solicitar-edicao btn-salvar"
              onClick={handleSalvarInformacoes}
            >
              <i className="bi bi-check-circle"></i> Salvar Informações
            </button>
          ) : (
            <button
              type="button"
              className="btn-solicitar-edicao"
              onClick={handleEntrarModoEdicao}
            >
              <i className="bi bi-pencil-square"></i> Editar Informações
            </button>
          )}
        </footer>
      </div>
    </div>
  );
}

export default Perfil;