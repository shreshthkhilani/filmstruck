import './App.css';
import { useFilms } from './hooks/useFilms';
import { YearChart } from './components/YearChart';
import { LocationChart } from './components/LocationChart';
import { CompanionChart } from './components/CompanionChart';
import { StatCard } from './components/StatCard';

const CSV_URL = import.meta.env.BASE_URL + 'films.csv';

function App() {
  const { stats, loading, error } = useFilms(CSV_URL);

  if (loading) {
    return (
      <div className="container">
        <div className="loading">Loading films...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container">
        <div className="error">Error: {error}</div>
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="container">
        <div className="error">No data available</div>
      </div>
    );
  }

  return (
    <div className="container">
      <header>
        <h1>FilmStruck</h1>
        <p className="subtitle">Sophie and Shreshth's film stats</p>
      </header>

      <div className="stats-summary">
        <StatCard title="Total Films" value={stats.totalCount} />
      </div>

      <div className="charts-grid">
        <div className="chart-container">
          <YearChart data={stats.byYear} />
        </div>
        <div className="chart-container">
          <LocationChart data={stats.byLocation} />
        </div>
        <div className="chart-container full-width">
          <CompanionChart data={stats.byCompanion} />
        </div>
      </div>
    </div>
  );
}

export default App;
