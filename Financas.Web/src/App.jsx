import {
  BrowserRouter,
  Routes,
  Route,
  Navigate
} from "react-router-dom";

// Importações dos módulos de página
import Login from "./pages/Login/Login";
import Register from "./pages/Register/Register";
import Dashboard from "./pages/Dashboard/Dashboard";
import ResumoFinanceiro from "./pages/Dashboard/ResumoFinanceiro";
import Lancamento from "./pages/Lancamentos/Lancamento";
import Categoria from "./pages/Categoria/Categoria";
import ContaBancaria from "./pages/ContaBancaria/ContaBancaria";
import Perfil from "./pages/Perfil/Perfil";

// Componente de Middleware para proteção de rotas (Auth check)
import PrivateRoute from "./routes/PrivateRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Rotas Públicas: Acesso livre sem autenticação */}
        <Route path="/" element={<Login />} />
        <Route path="/login" element={<Login />} />
        <Route path="/criar-conta" element={<Register />} />

        {/* Rotas Privadas: Envolvidas por PrivateRoute. 
          O padrão de rota aninhada (Nested Routes) permite que a Dashboard 
          atue como um layout pai para as sub-páginas.
        */}
        <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Dashboard />
            </PrivateRoute>
          }
        >
          {/* Index: Rota padrão ao acessar /dashboard */}
          <Route index element={<ResumoFinanceiro />} />

          {/* Rotas com parâmetros dinâmicos (ID) p/ reutilização de componentes (CRUD) */}
          <Route path="lancamento" element={<Lancamento />} />
          <Route path="lancamento/:id" element={<Lancamento />} />

          <Route path="categoria" element={<Categoria />} />
          <Route path="categoria/:id" element={<Categoria />} />

          <Route path="contas-bancarias" element={<ContaBancaria />} />
          <Route path="perfil" element={<Perfil />} />
        </Route>

        {/* Wildcard (*): Fallback p/ rotas inexistentes, redirecionando p/ login */}
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;