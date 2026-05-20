// Importação dos hooks do React para controle de estado e efeitos colaterais
import { useState, useEffect } from "react";
// Importação da instância configurada do Axios para requisições HTTP à API
import api from "../../services/api";
// Importação do arquivo de estilização específico deste componente
import "./Perfil.css";

function Perfil() {
  // Estado estruturado para gerenciar os dados cadastrais do usuário com base no UsuarioResponseDTO
  const [usuario, setUsuario] = useState({
    username: "",
    email: "",
    dataCadastro: "",
    emailConfirmado: false,
  });

  // Estados de controle de fluxo: gerenciamento do feedback visual de carregamento e mensagens de erro
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  // Hook useEffect utilizado para buscar os dados de forma assíncrona assim que o componente é montado
  useEffect(() => {
    const buscarPerfil = async () => {
      try {
        // Inicializa o estado resetando erros e ativando o indicador visual de carregamento
        setCarregando(true);
        setErro("");

        // Chamada limpa: a API descobre o usuário olhando direto o Token enviado no Header HTTP
        const response = await api.get("/usuarios/visualizar-perfil");

        // Atualiza o estado da aplicação injetando os dados estritamente tipados mapeados do DTO C#
        setUsuario({
          username: response.data.username,
          email: response.data.email,
          dataCadastro: response.data.dataCadastro,
          emailConfirmado: response.data.emailConfirmado,
        });
      } catch (err) {
        // Tratamento de exceções: exibe o erro no console e alimenta o estado visual com o retorno da API ou fallback
        console.error("Erro ao carregar perfil:", err);
        setErro(
          err.response?.data?.mensagem ||
            "Não foi possível carregar os dados do perfil.",
        );
      } finally {
        // Garante a desativação do spinner tanto em caso de sucesso quanto em caso de erro na requisição
        setCarregando(false);
      }
    };

    // Dispara a execução da função assíncrona de busca
    buscarPerfil();
  }, []); // Array de dependências vazio garante que o efeito rode apenas uma vez na inicialização da página

  // Renderização condicional de barreira: bloqueia o layout do card exibindo a tela de carregamento caso a API não tenha respondido
  if (carregando) {
    return (
      <div className="perfil-loading-container">
        <div className="perfil-spinner"></div>
        <p>Carregando informações do perfil...</p>
      </div>
    );
  }

  // Renderização principal do componente após a conclusão da busca dos dados
  return (
    <div className="perfil-page-container">
      <div className="perfil-main-card">
        {/* Seção de Cabeçalho: Exibe o avatar padrão, o nome do usuário e o badge dinâmico de status */}
        <header className="perfil-card-header">
          <div className="perfil-avatar-placeholder">
            <i className="bi bi-person-circle"></i>
          </div>
          <h2>{usuario.username}</h2>
          {/* Força a comparação estrita aceitando tanto o booleano true quanto a string "True" ou o número 1 para controle das classes CSS */}
          <span
            className={`status-badge ${usuario.emailConfirmado === true || usuario.emailConfirmado === "True" || usuario.emailConfirmado === 1 ? "ativo" : "pendente"}`}
          >
            {usuario.emailConfirmado === true ||
            usuario.emailConfirmado === "True" ||
            usuario.emailConfirmado === 1
              ? "Conta Verificada"
              : "Conta Pendente"}
          </span>
        </header>

        {/* Renderização condicional do alerta: renderiza na tela apenas se houver alguma mensagem de erro capturada no estado */}
        {erro && <p className="perfil-erro-alerta">{erro}</p>}

        {/* Grade de exibição de dados: renderização estática e segura das propriedades do usuário */}
        <div className="perfil-details-grid">
          {/* Campo de Exibição: Username */}
          <div className="perfil-info-group">
            <label>
              <i className="bi bi-person"></i> Nome de Usuário
            </label>
            <div className="perfil-data-field">{usuario.username}</div>
          </div>

          {/* Campo de Exibição: Email */}
          <div className="perfil-info-group">
            <label>
              <i className="bi bi-envelope"></i> E-mail Cadastrado
            </label>
            <div className="perfil-data-field">{usuario.email}</div>
          </div>

          {/* Renderização condicional da data: só exibe o campo se dataCadastro for válida, aplicando a formatação local brasileira */}
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

        {/* Rodapé do card: abriga o botão de ação para abertura posterior do fluxo de edição cadastral */}
        <footer className="perfil-card-footer">
          <button type="button" className="btn-solicitar-edicao">
            <i className="bi bi-pencil-square"></i> Editar Informações
          </button>
        </footer>
      </div>
    </div>
  );
}

export default Perfil;
