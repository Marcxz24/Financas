import { useRef } from "react";

/**
 * Usa a impressão nativa do navegador.
 * O usuário clica em "Baixar PDF" → abre o diálogo do navegador
 * → escolhe "Salvar como PDF".
 * Resultado idêntico em desktop e mobile, sem dependência externa.
 */
export function useExportarPDF() {
  const folhaRef = useRef(null);

  const exportar = () => window.print();

  return { folhaRef, exportar };
}