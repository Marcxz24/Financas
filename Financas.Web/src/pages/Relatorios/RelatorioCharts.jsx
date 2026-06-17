/*
 * Componente de Gráfico de Linha (LineChart)
 * Renderiza uma linha conectando pontos de dados baseados em coordenadas calculadas.
 */
export function LineChart({ labels = [], values = [], color = "#3b82f6" }) {
  // Configurações dimensionais do gráfico
  const width = 760;
  const height = 220;
  const padding = 34;
  const step = 60;
  const chartWidth = Math.max(width, labels.length * step);

  // Normalização e cálculo da escala de valores
  const validValues = values.length ? values : [0];
  const minValue = Math.min(...validValues, 0);
  const maxValue = Math.max(...validValues, 1);
  const range = maxValue - minValue || 1;

  // Mapeia valores para coordenadas X e Y dentro do SVG
  const points = validValues.map((value, index) => {
    const x = padding + (index * (chartWidth - padding * 2)) / Math.max(validValues.length - 1, 1);
    const y = height - padding - ((value - minValue) / range) * (height - padding * 2);
    return { x, y };
  });

  // Constrói o caminho (d) do elemento SVG path
  const pathD = points
    .map((point, index) => `${index === 0 ? "M" : "L"}${point.x} ${point.y}`)
    .join(" ");

  // Lógica para espaçamento dinâmico dos rótulos (labels)
  const maxLabels = 8;
  const labelSpacing = labels.length > maxLabels ? Math.ceil(labels.length / maxLabels) : 1;
  const showLabel = (index) => index % labelSpacing === 0 || index === labels.length - 1;
  const labelFontSize = labels.length > maxLabels ? 10 : 12;

  return (
    <div className="grafico-linha-container">
      <svg viewBox={`0 0 ${chartWidth} ${height}`} className="grafico-linha-svg" style={{ minWidth: `${chartWidth}px` }}>
        {/* Linha do gráfico */}
        <path
          d={pathD}
          fill="none"
          stroke={color}
          strokeWidth="3"
          strokeLinecap="round"
        />
        {/* Pontos de destaque (círculos) nos dados */}
        {points.map((point, index) => (
          <circle
            key={index}
            cx={point.x}
            cy={point.y}
            r="4"
            fill="white"
            stroke={color}
            strokeWidth="2"
          />
        ))}
        {/* Eixos do gráfico */}
        <line
          x1={padding}
          y1={height - padding}
          x2={chartWidth - padding}
          y2={height - padding}
          stroke="#334155"
          strokeWidth="1"
        />
        <line
          x1={padding}
          y1={padding}
          x2={padding}
          y2={height - padding}
          stroke="#334155"
          strokeWidth="1"
        />
        {/* Rótulos do eixo X */}
        {labels.map((label, index) => {
          if (!showLabel(index)) return null;
          const x = padding + (index * (chartWidth - padding * 2)) / Math.max(labels.length - 1, 1);
          return (
            <text
              key={index}
              x={x}
              y={height - 12}
              textAnchor="middle"
              fill="#94a3b8"
              fontSize={labelFontSize}
            >
              {label}
            </text>
          );
        })}
      </svg>
    </div>
  );
}

/*
 * Componente de Gráfico de Barras (BarChart)
 * Renderiza barras verticais proporcionais aos valores fornecidos.
 */
export function BarChart({ labels = [], values = [], color = "#3b82f6" }) {
  // Configurações dimensionais
  const width = 760;
  const height = 260;
  const padding = 34;
  const step = 60;
  const chartWidth = Math.max(width, labels.length * step);

  // Normalização e escala das barras
  const validValues = values.length ? values : [0];
  const maxValue = Math.max(...validValues, 1);
  const barCount = Math.max(validValues.length, 1);
  const barWidth = (chartWidth - padding * 2) / barCount * 0.75;

  return (
    <div className="grafico-bar-container">
      <svg viewBox={`0 0 ${chartWidth} ${height}`} className="grafico-bar-svg" style={{ minWidth: `${chartWidth}px` }}>
        {/* Eixos do gráfico */}
        <line
          x1={padding}
          y1={padding}
          x2={padding}
          y2={height - padding}
          stroke="#334155"
          strokeWidth="1"
        />
        <line
          x1={padding}
          y1={height - padding}
          x2={chartWidth - padding}
          y2={height - padding}
          stroke="#334155"
          strokeWidth="1"
        />
        {/* Mapeamento e renderização das barras */}
        {validValues.map((value, index) => {
          const x = padding + index * ((chartWidth - padding * 2) / barCount) + ((chartWidth - padding * 2) / barCount - barWidth) / 2;
          const barHeight = ((value / maxValue) * (height - padding * 2)) || 0;
          return (
            <g key={index}>
              {/* Retângulo da barra */}
              <rect
                x={x}
                y={height - padding - barHeight}
                width={barWidth}
                height={barHeight}
                fill={color}
                rx="4"
              />
              {/* Rótulo da barra */}
              <text
                x={x + barWidth / 2}
                y={height - padding + 18}
                textAnchor="middle"
                fill="#94a3b8"
                fontSize="10"
              >
                {labels[index]}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

/*
 * Componente de Gráfico de Pizza (PieChart)
 * Renderiza setores circulares proporcionais ao total da soma dos valores.
 */
export function PieChart({ slices = [], size = 260, innerRadius = 0 }) {
  // Configurações do raio e centro do círculo
  const radius = size / 2 - 10;
  const center = size / 2;
  const total = slices.reduce((sum, slice) => sum + Math.max(slice.value, 0), 0);
  const startAngle = -Math.PI / 2; // Inicia no topo

  // Verifica se há dados para renderizar
  if (!total) {
    return (
      <div className="grafico-pizza-vazio">
        <span>Sem dados suficientes</span>
      </div>
    );
  }

  // Gera os caminhos SVG para cada fatia (setor)
  const sectors = [];
  let currentAngle = startAngle;

  for (let index = 0; index < slices.length; index += 1) {
    const slice = slices[index];
    const value = Math.max(slice.value, 0);
    const angle = (value / total) * 2 * Math.PI;
    const endAngle = currentAngle + angle;
    
    // Coordenadas trigonométricas para desenhar o arco
    const x1 = center + radius * Math.cos(currentAngle);
    const y1 = center + radius * Math.sin(currentAngle);
    const x2 = center + radius * Math.cos(endAngle);
    const y2 = center + radius * Math.sin(endAngle);
    
    // Define se o arco é maior ou menor que 180 graus
    const largeArc = angle > Math.PI ? 1 : 0;
    const path = `M ${center} ${center} L ${x1} ${y1} A ${radius} ${radius} 0 ${largeArc} 1 ${x2} ${y2} Z`;
    
    sectors.push(
      <path key={index} d={path} fill={slice.color} stroke="#111827" strokeWidth="1" />
    );
    currentAngle = endAngle;
  }

  return (
    <div className="grafico-pizza-wrapper">
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="grafico-pizza-svg">
        {sectors}
        {/* Renderiza um círculo interno se for um Donut Chart */}
        {innerRadius > 0 && (
          <circle cx={center} cy={center} r={innerRadius} fill="#0f172a" />
        )}
      </svg>
    </div>
  );
}