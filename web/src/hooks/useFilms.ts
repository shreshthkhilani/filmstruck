import { useState, useEffect } from 'react';
import type { Film, FilmStats } from '../types/film';
import { fetchFilms } from '../utils/csv';
import { computeStats } from '../utils/stats';

interface UseFilmsResult {
  films: Film[];
  stats: FilmStats | null;
  loading: boolean;
  error: string | null;
}

export function useFilms(csvUrl: string): UseFilmsResult {
  const [films, setFilms] = useState<Film[]>([]);
  const [stats, setStats] = useState<FilmStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        setLoading(true);
        setError(null);
        const data = await fetchFilms(csvUrl);
        setFilms(data);
        setStats(computeStats(data));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load films');
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [csvUrl]);

  return { films, stats, loading, error };
}
