import type { Film, FilmStats } from '../types/film';

export function computeStats(films: Film[]): FilmStats {
  return {
    totalCount: films.length,
    byYear: aggregateByYear(films),
    byLocation: aggregateByLocation(films),
    byCompanion: aggregateByCompanion(films),
  };
}

function aggregateByYear(films: Film[]): Record<string, number> {
  const counts: Record<string, number> = {};

  for (const film of films) {
    const year = extractYear(film.date);
    counts[year] = (counts[year] || 0) + 1;
  }

  return sortByKey(counts);
}

function extractYear(dateStr: string): string {
  // Handle M/D/YYYY format
  if (dateStr.includes('/')) {
    const parts = dateStr.split('/');
    return parts[parts.length - 1];
  }
  // Handle YYYY-MM-DD format
  return dateStr.substring(0, 4);
}

function aggregateByLocation(films: Film[]): Record<string, number> {
  const counts: Record<string, number> = {};

  for (const film of films) {
    const location = film.location || 'Unknown';
    counts[location] = (counts[location] || 0) + 1;
  }

  return sortByValue(counts);
}

function aggregateByCompanion(films: Film[]): Record<string, number> {
  const counts: Record<string, number> = {};

  for (const film of films) {
    for (const companion of film.companions) {
      counts[companion] = (counts[companion] || 0) + 1;
    }
  }

  return sortByValue(counts);
}

function sortByKey(obj: Record<string, number>): Record<string, number> {
  return Object.fromEntries(
    Object.entries(obj).sort(([a], [b]) => a.localeCompare(b))
  );
}

function sortByValue(obj: Record<string, number>): Record<string, number> {
  return Object.fromEntries(
    Object.entries(obj).sort(([, a], [, b]) => b - a)
  );
}
