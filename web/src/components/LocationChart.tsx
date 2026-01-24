import { Pie } from 'react-chartjs-2';
import { Chart as ChartJS, ArcElement, Title, Tooltip, Legend } from 'chart.js';

ChartJS.register(ArcElement, Title, Tooltip, Legend);

interface LocationChartProps {
  data: Record<string, number>;
}

const COLORS = [
  '#7b9fc4',
  '#c49b7b',
  '#8faa8f',
  '#b8a0c4',
  '#c4a07b',
  '#7bc4b8',
  '#c47b8f',
  '#a0a0a0',
  '#7b8fc4',
  '#c4c47b',
  '#8fc47b',
  '#c47bb8',
];

export function LocationChart({ data }: LocationChartProps) {
  const labels = Object.keys(data);
  const values = Object.values(data);

  const chartData = {
    labels,
    datasets: [
      {
        data: values,
        backgroundColor: labels.map((_, i) => COLORS[i % COLORS.length]),
        borderColor: '#222',
        borderWidth: 1,
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      legend: {
        position: 'right' as const,
        labels: {
          color: '#888',
          font: {
            family: 'Georgia, serif',
            size: 11,
          },
          boxWidth: 12,
          padding: 8,
        },
      },
      title: {
        display: true,
        text: 'by location',
        color: '#888',
        font: {
          family: 'Georgia, serif',
          size: 12,
          weight: 'normal' as const,
        },
      },
    },
  };

  return <Pie data={chartData} options={options} />;
}
