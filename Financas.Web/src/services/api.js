import axios from "axios";

// Criação da instância personalizada do Axios
const api = axios.create({
    // Lê a URL do seu back-end C# definida no arquivo .env
    baseURL: import.meta.env.VITE_API_URL,
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

export default api;