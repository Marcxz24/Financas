import axios from "axios";

// Criação da instância personalizada do Axios
const api = axios.create({
    // Define a URL base da API já contendo o prefixo padrão "/api"
    baseURL: `${import.meta.env.VITE_API_URL}/api`,

    headers: {
        "Content-Type": "application/json"
    },
});


// INTERCEPTOR DE REQUISIÇÃO:
// Este bloco age como um "pedágio". Antes de qualquer requisição sair para a API,
// ele verifica se existe um token no navegador e o injeta no cabeçalho.
api.interceptors.request.use(
    (config) => {
        // Recupera o token JWT armazenado no login (Corrigido para a chave real do sistema)
        const token = localStorage.getItem("token");
        
        if (token) {
            // Adiciona o cabeçalho de autorização padrão para APIs REST
            config.headers.Authorization = `Bearer ${token}`;
        }
        
        return config;
    },
    (error) => {
        // Trata falhas que ocorrem antes mesmo da requisição ser enviada
        return Promise.reject(error);
    }
);

api.interceptors.response.use(
    (response) => response,
    (error) => {
        // Verifica se a resposta de erro é um 401 (Unauthorized)
        if (error.response?.status === 401) {
            // Limpa o token do armazenamento local, efetivamente "deslogando" o usuário
            localStorage.removeItem("token");

            // Redireciona para a página de sessão expirada
            window.location.href = "sessao-expirada";
        }
        return Promise.reject(error);
    }
);

export default api;