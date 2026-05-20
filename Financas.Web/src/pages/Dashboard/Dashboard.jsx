import "./dashboard.css";
import { Link, useNavigate, Outlet } from "react-router-dom";

function Dashboard() {

  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem("token");
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

          <Link to="#">
            Relatórios
          </Link>

          <Link to="/dashboard/perfil">
            Perfil
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