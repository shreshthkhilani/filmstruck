import type { Film } from '../types/film';

export async function fetchFilms(csvUrl: string): Promise<Film[]> {
  const response = await fetch(csvUrl);
  const text = await response.text();
  return parseCSV(text);
}

export function parseCSV(csvText: string): Film[] {
  const lines = csvText.trim().split('\n');
  if (lines.length < 2) return [];

  const films: Film[] = [];

  for (let i = 1; i < lines.length; i++) {
    const parsed = parseCSVLine(lines[i]);
    if (parsed.length >= 4) {
      films.push({
        date: parsed[0],
        title: parsed[1],
        location: parsed[2],
        companions: parsed[3] ? parsed[3].split(',').map(c => c.trim()).filter(Boolean) : [],
      });
    }
  }

  return films;
}

function parseCSVLine(line: string): string[] {
  const result: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const char = line[i];

    if (char === '"') {
      inQuotes = !inQuotes;
    } else if (char === ',' && !inQuotes) {
      result.push(current.trim());
      current = '';
    } else {
      current += char;
    }
  }

  result.push(current.trim());
  return result;
}
