import { useNavigate } from 'react-router-dom';
import './SessaoExpirada.css';

export default function SessaoExpirada() {
  const navigate = useNavigate();

  const handleRedirect = () => {
    navigate('/login');
  };

  return (
    <div className="sessao-expirada-container">
      <div className="sessao-expirada-content">
        <div className="sessao-expirada-icon">
          <svg width="120" height="120" viewBox="0 0 120 120" fill="none">
            <circle cx="60" cy="60" r="55" stroke="#FF6B6B" strokeWidth="2" />
            <path d="M60 30V60M60 75V75.5" stroke="#FF6B6B" strokeWidth="3" strokeLinecap="round" />
            <circle cx="40" cy="45" r="3" fill="#FF6B6B" />
            <circle cx="80" cy="45" r="3" fill="#FF6B6B" />
          </svg>
        </div>
        
        <h1>Sessão Expirada</h1>
        
        <p className="sessao-expirada-subtitle">
          Sua sessão foi encerrada por inatividade.
        </p>
        
        <p className="sessao-expirada-description">
          Por motivos de segurança, pedimos que você faça login novamente para continuar acessando o sistema.
        </p>
        
        <button 
          className="sessao-expirada-button"
          onClick={handleRedirect}
        >
          Fazer Login Novamente
        </button>
        
        <p className="sessao-expirada-footer">
          Precisa de ajuda? <a href="/suporte">Entre em contato com o suporte</a>
        </p>
      </div>
    </div>
  );
}
