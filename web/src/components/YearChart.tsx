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

interface YearChartProps {
  data: Record<string, number>;
}

export function YearChart({ data }: YearChartProps) {
  const labels = Object.keys(data);
  const values = Object.values(data);

  const chartData = {
    labels,
    datasets: [
      {
        label: 'Films',
        data: values,
        backgroundColor: '#7b9fc4',
        borderColor: '#5a7a99',
        borderWidth: 1,
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      legend: {
        display: false,
      },
      title: {
        display: true,
        text: 'by year',
        color: '#888',
        font: {
          family: 'Georgia, serif',
          size: 12,
          weight: 'normal' as const,
        },
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          stepSize: 10,
          color: '#666',
        },
        grid: {
          color: '#333',
        },
      },
      x: {
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
