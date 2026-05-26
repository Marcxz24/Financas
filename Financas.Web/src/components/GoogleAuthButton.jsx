import { memo } from "react";
import { GoogleLogin } from "@react-oauth/google";

/**
 * Componente de Botão de Autenticação do Google.
 * Encapsula a lógica de login social, validação com o backend C# e gerenciamento de sessão.
 * * @param {Function} navigate - Função de navegação do react-router-dom repassada pelo componente pai.
 * @param {Function} setCarregando - Função para controlar o estado de carregamento.
 */
function GoogleAuthButton({ navigate, setCarregando }) {
  return (
    <GoogleLogin
      // Disparado quando o usuário faz login com sucesso na janela popup do Google
      onSuccess={async (credentialResponse) => {

        setCarregando(true);

        try {
          // Extrai o ID Token (JWT) retornado pelos servidores do Google
          const googleToken = credentialResponse.credential;

          // Envia o token do Google para a Web API em C# validar e registrar/logar o usuário
          const response = await fetch(
            `${import.meta.env.VITE_API_URL}/api/usuarios/google`,
            {
              method: "POST",
              headers: {
                "Content-Type": "application/json",
              },
              // O backend espera um objeto JSON contendo a propriedade 'token'
              body: JSON.stringify({
                token: googleToken,
              }),
            },
          );

          // Se o backend C# retornar qualquer status fora da faixa 200-299 (ex: 400, 404, 500)
          if (!response.ok) {
            throw new Error("Falha na autenticação com Google.");
          }
          // Lê o JWT customizado gerado pela nossa API C# (retornado como texto puro)
          const tokenSistema = await response.text();

          // Alterado para salvar com a chave simples "token", igual ao login tradicional
          localStorage.setItem("token", tokenSistema);

          // Redireciona o usuário autenticado para a tela principal do sistema
          navigate("/dashboard");
        } catch (error) {
          // Captura e exibe erros de rede ou falhas na validação do backend C#
          console.error("Erro ao integrar com a API:", error);
        } finally {
          // Garante que o estado de carregamento seja desativado após a tentativa de login
          setCarregando(false);
        }
      }}
      // Disparado quando o fluxo de login falha no lado do próprio Google (ex: popup fechada pelo usuário)
      onError={() => {
        console.log("Falha na autenticação com o Google");
      }}
      // CONFIGURAÇÕES VISUAIS E DE COMPORTAMENTO DO BOTÃO:
      theme="filled_blue" // Define o tema com o fundo azul padrão do Google
      shape="rectangular" // Define as bordas com formato retangular plano
      width={300} // Define a largura fixa em pixels (evita warnings de validação no console)
      text="signin_with" // Exibe o texto oficial "Iniciar sessão com o Google"
      locale="pt_BR" // Força a internacionalização do botão para o português do Brasil
      useOneTap={false} // Desativa o prompt flutuante automático no canto superior da tela (One Tap)
    />
  );
}

// O uso do memo impede que o botão do Google sofra re-renderizações desnecessárias
// sempre que o estado do formulário de login pai (como e-mail e senha) for alterado.
export default memo(GoogleAuthButton);
