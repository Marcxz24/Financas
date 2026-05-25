// Importa o Hook useState para gerenciar o estado local do componente
import { useState } from 'react';
// Importa o componente Link para navegação interna sem recarregar a página
import { Link } from 'react-router-dom'; 
// Importa o arquivo de estilos CSS específico para a página de registro
import './Register.css';

const Register = () => {
  // Inicializa um objeto de estado para armazenar todos os campos do formulário
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: ''
  });

  // Função para manipular as mudanças nos inputs de forma dinâmica
  const handleChange = (e) => {
    // Extrai as propriedades name e value do elemento que disparou o evento
    const { name, value } = e.target;
    // Atualiza o estado mantendo os valores anteriores e alterando apenas o campo atual
    setFormData({
      ...formData,
      [name]: value
    });
  };

  // Função executada ao submeter o formulário
  const handleRegister = (e) => {
    // Evita o comportamento padrão do navegador de recarregar a página
    e.preventDefault();
    // Exibe os dados atuais do estado no console para conferência
    console.log('Dados prontos para o .NET 8:', formData);
  };

  return (
    // Container principal que define o contexto da página de registro
    <div className="register-page">
      {/* Container visual do cartão de cadastro */}
      <div className="register-card">
        
        {/* Cabeçalho da seção de registro */}
        <header className="register-header">
          <h1>Criar Conta</h1>
          <p className="register-subtitle">
            Preencha os dados para começar
          </p>
        </header>

        {/* Formulário vinculado à função de manipulação de submissão */}
        <form onSubmit={handleRegister} className="register-form">
          {/* Campo de texto para o nome de usuário vinculado ao estado */}
          <input
            type="text"
            name="username"
            placeholder="Username"
            value={formData.username}
            onChange={handleChange}
            required
          />

          {/* Campo de e-mail com validação nativa e vínculo ao estado */}
          <input
            type="email"
            name="email"
            placeholder="Email"
            value={formData.email}
            onChange={handleChange}
            required
          />

          {/* Campo de senha vinculado ao estado */}
          <input
            type="password"
            name="password"
            placeholder="Password"
            value={formData.password}
            onChange={handleChange}
            required
          />

          {/* Botão para disparar o evento de submissão do formulário */}
          <button type="submit" className="btn-register">
            Cadastrar
          </button>
        </form>

        {/* Seção de rodapé do cartão para alternância entre telas */}
        <footer className="register-footer">
          <span>Já possui conta?</span>
          {/* Direciona o usuário para a rota de login */}
          <Link to="/login" className="link-login">Entrar</Link>
        </footer>
      </div>
    </div>
  );
};

// Exporta o componente para ser renderizado pelo sistema de rotas
export default Register;