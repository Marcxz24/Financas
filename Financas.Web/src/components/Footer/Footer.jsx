// Importa o arquivo de estilos CSS específico para o componente Footer
import "./Footer.css";

// Define o componente funcional Footer que será renderizado na interface
function Footer() {
  // Obtém o ano corrente do sistema para exibição dinâmica no copyright
  const anoAtual = new Date().getFullYear();

  return (
    // Elemento principal de rodapé com a classe de estilo 'footer-sistema'
    <footer className="footer-sistema">
      {/* Container interno para centralização e organização do conteúdo */}
      <div className="footer-conteudo">

        {/* Bloco de informações do rodapé (Copyright e Desenvolvedor) */}
        <div className="footer-info">
          {/* Exibe o ano atual dinâmico e o nome do sistema */}
          <span>
            © {anoAtual} Finanças
          </span>

          {/* Elemento visual para separação entre informações */}
          <span className="footer-separador">
            •
          </span>

          {/* Exibe a autoria do projeto com destaque no nome */}
          <span>
            Desenvolvido por <strong>Marco Antônio Q Ribeiro</strong>
          </span>
        </div>

        {/* Bloco que exibe a versão atual do sistema */}
        <div className="footer-versao">
          v1.5.4
        </div>

      </div>
    </footer>
  );
}

// Exporta o componente para que possa ser utilizado em outros arquivos (como no Dashboard)
export default Footer;