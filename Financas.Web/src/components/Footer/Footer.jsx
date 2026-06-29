/**
 * Footer.jsx
 *
 * Componente responsável pelo rodapé global da aplicação.
 * Exibe informações institucionais, autoria do sistema e versão atual.
 * Utilizado como elemento fixo de identidade visual em todas as páginas.
 */

// Importa o arquivo de estilos CSS específico para o componente Footer
import "./Footer.css";

// Define o componente funcional Footer que será renderizado na interface
function Footer() {
  // Captura o ano atual dinamicamente para manter o copyright sempre atualizado
  const anoAtual = new Date().getFullYear();

  return (
    // Estrutura principal do rodapé
    <footer className="footer-sistema">
      
      {/* Container responsável por organizar layout interno do footer */}
      <div className="footer-conteudo">

        {/* Área de informações institucionais */}
        <div className="footer-info">

          {/* Exibe copyright com ano dinâmico */}
          <span>
            © {anoAtual} Finanças
          </span>

          {/* Separador visual entre informações */}
          <span className="footer-separador">
            •
          </span>

          {/* Identificação do desenvolvedor responsável pelo sistema */}
          <span>
            Desenvolvido por <strong>Marco Antônio Q Ribeiro</strong>
          </span>

        </div>

        {/* Exibição da versão atual do sistema */}
        <div className="footer-versao">
          v1.8.0
        </div>

      </div>
    </footer>
  );
}

// Exporta o componente para uso global na aplicação
export default Footer;