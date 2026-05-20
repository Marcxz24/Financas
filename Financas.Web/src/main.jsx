// Importa o método responsável por inicializar e renderizar a árvore de componentes do React na árvore do DOM (HTML)
import { createRoot } from 'react-dom/client';
// Importa o provedor de contexto global do ecossistema de autenticação oficial do Google para o React
import { GoogleOAuthProvider } from '@react-oauth/google';

// Importação dos estilos globais que afetam toda a aplicação (como resets e variáveis de cores)
import './index.css';
// Importação do componente raiz 'App', que encapsula todas as rotas e páginas do sistema
import App from './App.jsx';

// Recupera a chave Client ID do Google das variáveis de ambiente injetadas pelo Vite (.env)
// O prefixo VITE_ é obrigatório para que a chave seja exposta com segurança no front-end
const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

// Localiza a div com id 'root' no index.html, cria a raiz do React e renderiza a aplicação
createRoot(document.getElementById('root')).render(
    // Encapsula a aplicação com o provedor do Google, disponibilizando o serviço de login para qualquer sub-componente
    <GoogleOAuthProvider clientId={googleClientId}>
      {/* Componente principal contendo a estrutura de layout e navegação do sistema */}
      <App />
    </GoogleOAuthProvider>
);