import "./Dashboard.css"; // Importa o arquivo de estilos CSS para aplicar o layout visual da página
import { Link, useNavigate, Outlet } from "react-router-dom"; // Importa hooks e componentes do React Router para navegação

function Dashboard() {

  // O hook 'useNavigate' é usado para redirecionar o usuário programaticamente (via código)
  const navigate = useNavigate();

  // Função disparada ao clicar no botão de sair
  const handleLogout = () => {
    // Remove o token de autenticação armazenado no navegador (localStorage)
    // Isso encerra a sessão do usuário localmente
    localStorage.removeItem("token");
    
    // Redireciona o usuário de volta para a rota raiz ("/"), geralmente a tela de login
    navigate("/");
  };
  
  return (
    <div className="dashboard-page">

      <header className="dashboard-header">

        <div className="logo-area">
          <h1>Finanças</h1>
          <span>Painel Financeiro</span>
        </div>

        <nav className="menu-top">
          <Link to="/dashboard">
            Dashboard
          </Link>

          <Link to="/dashboard/lancamento">
            Transações
          </Link>

          <Link to="/dashboard/categoria">
            Categorias
          </Link>

          <Link to="/dashboard/contas-bancarias">
            Contas Bancárias
          </Link>

          <Link to="/dashboard/cartao-credito">
            Cartões de Crédito
          </Link>

          <Link to="/dashboard/fatura">
            Faturas
          </Link>

          <Link to="#">
            Relatórios
          </Link>

          <Link to="/dashboard/perfil">
            Perfil
          </Link>

          <Link to="/dashboard/ajuda">
            Ajuda
          </Link>

          <button
            className="btn-logout"
            onClick={handleLogout}
          >
            Sair
          </button>
        </nav>

      </header>

      <main className="dashboard-content">

        {/* Rotas filhas */}
        <Outlet />

      </main>

    </div>
  );
}

export default Dashboard;