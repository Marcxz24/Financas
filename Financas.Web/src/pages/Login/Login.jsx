import { useState } from "react";
// Importa a instância configurada do cliente HTTP (Axios)
import api from "../../services/api";
// Importa o arquivo de estilos CSS específico deste componente
import "./Login.css";
// Importa o componente Link para navegação entre rotas
import { Link, useNavigate } from "react-router-dom";
// Importa o componente de botão de autenticação do Google
import GoogleAuthButton from "../../components/GoogleAuthButton";

import logoArt from "../../assets/financas-api-art.jpeg";

function Login() {
  // Define o estado para armazenar mensagens de erro relacionadas ao login
  const [erro, setErro] = useState("");

  // Define o estado para armazenar o valor do campo de e-mail
  const [email, setEmail] = useState("");

  // Define o estado para controlar a visibilidade da senha
  const [mostrarSenha, setMostrarSenha] = useState(false);

  // Define o estado para armazenar o valor do campo de senha
  const [password, setPassword] = useState("");

  // Estado booleano para controle de display do Overlay de carregamento (Loading State)
  const [carregando, setCarregando] = useState(false);

  const navigate = useNavigate();

  // Define a função assíncrona que processa a submissão do formulário
  const handleLogin = async (e) => {
    e.preventDefault();

    setErro("");
    // Ativa o estado de carregamento para bloquear a interface via Overlay
    setCarregando(true);

    try {
      const response = await api.post("/usuarios/login", {
        email,
        password,
      });

      localStorage.setItem("token", response.data.token || response.data);

      navigate("/dashboard");
    } catch (error) {
      console.error(error);

      setErro(error.response?.data || "Erro ao realizar login.");
    } finally {
      // O bloco finally garante o reset do estado de carregamento independentemente do resultado da Promise
      setCarregando(false);
    }
  };

  return (
    <>
      {/* Overlay de Carregamento: Renderização condicional de bloqueio de UI para feedback de latência de rede */}
      {carregando && (
        <div className="login-overlay-carregamento">
          <div className="login-spinner"></div>
          <p>Autenticando, por favor aguarde...</p>
        </div>
      )}

      {/* Container principal da página de login */}
      <div className="login-page">
        {/* Container do cartão central de login */}
        <div className="login-card">
          {/* Cabeçalho contendo o título e a descrição */}
          <header className="login-header">
            <img src={logoArt} alt="Finanças API Logo" className="brand-logo" />
            <h1>Finanças</h1>

            <p className="descricao-login">
              Faça login para acessar sua conta e gerenciar suas finanças de
              forma fácil e segura.
            </p>
          </header>

          {/* Formulário que dispara a função handleLogin ao ser submetido */}
          <form onSubmit={handleLogin}>
            {/* Campo de entrada para o e-mail */}
            <input
              type="email"
              placeholder="Email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              required
            />

            {/* Wrapper relativo para segurar o botão dentro do input */}
            <div className="input-icone-wrapper">
              <input
                type={mostrarSenha ? "text" : "password"}
                className="input-senha-custom"
                placeholder="Senha"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
              />

              {/* Botão posicionado de forma absoluta para flutuar dentro do campo */}
              <button
                type="button"
                className="btn-mostrar-senha"
                onClick={() => setMostrarSenha(!mostrarSenha)}
                title={mostrarSenha ? "Esconder senha" : "Mostrar senha"}
              >
                <i
                  className={mostrarSenha ? "bi bi-eye-slash" : "bi bi-eye"}
                ></i>
              </button>
            </div>

            {/* O botão volta ao normal, sem mudar de texto */}
            <button type="submit">Entrar</button>

            <div className="google-login-container">
              <GoogleAuthButton
                navigate={navigate}
                setCarregando={setCarregando}
              />
            </div>

            {/* Mensagem de erro */}
            {erro && <p className="mensagem-erro">{erro}</p>}
          </form>

          {/* Seção inferior para redirecionamento à página de cadastro */}
          <header className="criar-conta">
            <p>
              Não tem uma conta? <Link to="/criar-conta">Crie uma aqui</Link>
            </p>
          </header>
        </div>
      </div>
    </>
  );
}

export default Login;
