import {
  BrowserRouter,
  Routes,
  Route,
  Navigate
} from "react-router-dom";

import Login from "./pages/Login/Login";
import Register from "./pages/Register/Register";

import Dashboard from "./pages/Dashboard/Dashboard";
import ResumoFinanceiro from "./pages/Dashboard/ResumoFinanceiro";

import Lancamento from "./pages/Lancamentos/Lancamento";

import Categoria from "./pages/Categoria/Categoria";

import Perfil from "./pages/Perfil/Perfil";

import PrivateRoute from "./routes/PrivateRoute";

function App() {
  return (
    <BrowserRouter>

      <Routes>

        {/* Públicas */}
        <Route path="/" element={<Login />} />
        <Route path="/login" element={<Login />} />
        <Route path="/criar-conta" element={<Register />} />

        {/* Privadas */}
        <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Dashboard />
            </PrivateRoute>
          }
        >

          {/* Dashboard inicial */}
          <Route
            index
            element={<ResumoFinanceiro />}
          />

          {/* Criar */}
          <Route
            path="lancamento"
            element={<Lancamento />}
          />

          {/* Editar */}
          <Route
            path="lancamento/:id"
            element={<Lancamento />}
          />

          {/* Criar Categoria */}
          <Route
            path="categoria"
            element={<Categoria />}
          />

          {/* Editar Categoria */}
          <Route
            path="categoria/:id"
            element={<Categoria />}
            />

          {/* Perfil */}
          <Route
            path="perfil"
            element={<Perfil />}
          />

        </Route>

        {/* Fallback */}
        <Route
          path="*"
          element={<Navigate to="/" />}
        />

      </Routes>

    </BrowserRouter>
  );
}

export default App;