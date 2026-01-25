const fs = require('fs');
const path = require('path');

// Parse CSV
function parseCsv(text) {
  const lines = text.trim().split('\n');
  const headers = parseLine(lines[0]);
  const rows = [];
  for (let i = 1; i < lines.length; i++) {
    const values = parseLine(lines[i]);
    const row = {};
    headers.forEach((h, j) => row[h] = values[j] || '');
    rows.push(row);
  }
  return rows;
}

function parseLine(line) {
  const result = [];
  let current = '';
  let inQuotes = false;
  for (const c of line) {
    if (c === '"') inQuotes = !inQuotes;
    else if (c === ',' && !inQuotes) {
      result.push(current.trim());
      current = '';
    } else {
      current += c;
    }
  }
  result.push(current.trim());
  return result;
}

// Parse date in M/D/YYYY format
function parseDate(str) {
  const [m, d, y] = str.split('/').map(Number);
  return new Date(y, m - 1, d);
}

// Load data
const logCsv = fs.readFileSync(path.join(__dirname, 'data/log.csv'), 'utf-8');
const filmsCsv = fs.readFileSync(path.join(__dirname, 'data/films.csv'), 'utf-8');

const log = parseCsv(logCsv);
const films = parseCsv(filmsCsv);

// Create lookup by tmdbId
const filmLookup = {};
for (const f of films) {
  filmLookup[f.tmdbId] = f;
}

// Join and sort by date (reverse chronological)
const watchedFilms = log
  .filter(entry => entry.tmdbId && filmLookup[entry.tmdbId])
  .map(entry => ({
    ...entry,
    film: filmLookup[entry.tmdbId],
    dateObj: parseDate(entry.date)
  }))
  .sort((a, b) => b.dateObj - a.dateObj);

// Generate HTML
const html = `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>filmstruck</title>
</head>
<body>
<center>
<h1>filmstruck</h1>
${watchedFilms.map(w => `<img src="https://image.tmdb.org/t/p/w154${w.film.posterPath}" alt="${w.film.title} (${w.film.releaseYear})" title="${w.film.title} (${w.film.releaseYear}) dir. ${w.film.director} - watched ${w.date}">
`).join('')}
</center>
<p align="right"><small>last updated ${new Date().toLocaleDateString()}</small></p>
</body>
</html>`;

// Write output
fs.writeFileSync(path.join(__dirname, 'index.html'), html);
console.log(`Generated index.html with ${watchedFilms.length} films`);
