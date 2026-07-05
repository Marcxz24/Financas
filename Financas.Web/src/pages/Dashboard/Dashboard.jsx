/**
 * Dashboard.jsx
 *
 * Componente layout principal das rotas autenticadas.
 * Responsável por estruturar navegação global do sistema (menu superior),
 * controle de sessão (logout) e renderização das páginas internas via Outlet.
 */

import "./Dashboard.css"; // Estilos da estrutura principal do painel
import { Link, useNavigate, Outlet } from "react-router-dom"; // Navegação e renderização de rotas aninhadas
import Footer from "../../components/Footer/Footer"; // Rodapé global do sistema

function Dashboard() {
  // Hook responsável por navegação programática entre rotas
  const navigate = useNavigate();

  // Responsável por encerrar a sessão do usuário
  const handleLogout = () => {
    // Remove token de autenticação armazenado localmente
    localStorage.removeItem("token");

    // Redireciona para tela pública de login
    navigate("/");
  };

  return (
    <div className="dashboard-page">

      {/* Cabeçalho fixo com identidade do sistema e menu principal */}
      <header className="dashboard-header">
        <div className="logo-area">
          <h1>Finanças</h1>
          <span>Painel Financeiro</span>
        </div>

        {/* Navegação principal entre módulos do sistema */}
        <nav className="menu-top">
          <Link to="/dashboard">Dashboard</Link>

          <Link to="/dashboard/lancamento">Transações</Link>

          <Link to="/dashboard/categoria">Categorias</Link>

          <Link to="/dashboard/contas-bancarias">Contas Bancárias</Link>

          <Link to="/dashboard/transferencia">Transferências</Link>

          <Link to="/dashboard/cartao-credito">Cartões de Crédito</Link>

          <Link to="/dashboard/fatura">Faturas</Link>

          <Link to="/dashboard/metas">Metas</Link>

          <Link to="/dashboard/chatIA">Chat IA</Link>

          <Link to="/dashboard/relatorios">Relatórios</Link>

          <Link to="/dashboard/perfil">Perfil</Link>

          <Link to="/dashboard/ajuda">Ajuda</Link>

          {/* Ação de encerramento de sessão */}
          <button className="btn-logout" onClick={handleLogout}>
            Sair
          </button>
        </nav>
      </header>

      {/* Container responsável por scroll e layout do conteúdo interno */}
      <div className="dashboard-scroll-container">
        
        <main className="dashboard-content">
          {/* Renderização dinâmica das páginas internas do dashboard */}
          <Outlet />
        </main>

        {/* Rodapé global do sistema */}
        <Footer />
      </div>
    </div>
  );
}

export default Dashboard;