/**
 * App.jsx
 *
 * Responsável por centralizar o roteamento principal da aplicação.
 * Define rotas públicas, privadas e estrutura de navegação aninhada (Dashboard como layout base).
 * Também controla acesso autenticado através do PrivateRoute.
 */

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
import Transferencia from "./pages/Transferencia/Transferencia";
import CartaoCredito from "./pages/CartaoCredito/CartaoCredito";
import Fatura from "./pages/Faturas/Fatura";
import Relatorios from "./pages/Relatorios/Relatorios";
import Receitas from "./pages/Relatorios/Receitas/Receitas";
import Despesas from "./pages/Relatorios/Despesas/Despesas";
import FluxoCaixa from "./pages/Relatorios/FluxoCaixa/FluxoCaixa";
import FaturaRelatorio from "./pages/Relatorios/Fatura/FaturaRelatorio";
import Metas from "./pages/Metas/MetasGasto";
import ChatIA from "./pages/ChatIA/ChatIA"
import Perfil from "./pages/Perfil/Perfil";
import Ajuda from "./pages/Ajuda/Ajuda";
import SessaoExpirada from "./pages/SessaoExpirada/SessaoExpirada";

// Middleware de proteção de rotas (validação de autenticação)
import PrivateRoute from "./routes/PrivateRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>

        {/* Rotas públicas não dependem de autenticação */}
        <Route path="/" element={<Login />} />
        <Route path="/login" element={<Login />} />
        <Route path="/criar-conta" element={<Register />} />
        <Route path="/sessao-expirada" element={<SessaoExpirada />} />

        {/* Rotas protegidas por autenticação */}
        <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Dashboard />
            </PrivateRoute>
          }
        >

          {/* Rota padrão do dashboard (home interna) */}
          <Route index element={<ResumoFinanceiro />} />

          {/* Lançamentos financeiros (CRUD com e sem ID) */}
          <Route path="lancamento" element={<Lancamento />} />
          <Route path="lancamento/:id" element={<Lancamento />} />

          {/* Categorias (CRUD) */}
          <Route path="categoria" element={<Categoria />} />
          <Route path="categoria/:id" element={<Categoria />} />

          {/* Contas bancárias */}
          <Route path="contas-bancarias" element={<ContaBancaria />} />

          <Route path="transferencia" element={<Transferencia />} />

          {/* Cartões de crédito */}
          <Route path="cartao-credito" element={<CartaoCredito />} />

          {/* Faturas */}
          <Route path="fatura" element={<Fatura />} />

          {/* Relatórios gerais e segmentados */}
          <Route path="relatorios" element={<Relatorios />} />
          <Route path="relatorios/receitas" element={<Receitas />} />
          <Route path="relatorios/despesas" element={<Despesas />} />
          <Route path="relatorios/fluxo-caixa" element={<FluxoCaixa />} />

          {/* Relatório detalhado de fatura por ID */}
          <Route path="relatorios/fatura/:id" element={<FaturaRelatorio />} />

          {/* Módulo de metas financeiras */}
          <Route path="metas" element={<Metas />} />

          {/* Módulo do Chat com a IA para analises financeiras*/}
          <Route path="chatIA" element={<ChatIA/>} />

          {/* Perfil do usuário */}
          <Route path="perfil" element={<Perfil />} />

          {/* Central de ajuda/suporte */}
          <Route path="ajuda" element={<Ajuda />} />

        </Route>

        {/* Fallback para rotas inexistentes */}
        <Route path="*" element={<Navigate to="/" />} />

      </Routes>
    </BrowserRouter>
  );
}

export default App;