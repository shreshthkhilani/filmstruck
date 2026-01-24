import { Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend);

interface CompanionChartProps {
  data: Record<string, number>;
}

export function CompanionChart({ data }: CompanionChartProps) {
  const labels = Object.keys(data);
  const values = Object.values(data);

  const chartData = {
    labels,
    datasets: [
      {
        label: 'Films',
        data: values,
        backgroundColor: '#c49b7b',
        borderColor: '#996b4b',
        borderWidth: 1,
      },
    ],
  };

  const options = {
    indexAxis: 'y' as const,
    responsive: true,
    plugins: {
      legend: {
        display: false,
      },
      title: {
        display: true,
        text: 'by companion',
        color: '#888',
        font: {
          family: 'Georgia, serif',
          size: 12,
          weight: 'normal' as const,
        },
      },
    },
    scales: {
      x: {
        beginAtZero: true,
        ticks: {
          stepSize: 5,
          color: '#666',
        },
        grid: {
          color: '#333',
        },
      },
      y: {
        ticks: {
          color: '#666',
        },
        grid: {
          color: '#333',
        },
      },
    },
  };

  return <Bar data={chartData} options={options} />;
}
