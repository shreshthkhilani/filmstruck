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
const statsCsv = fs.readFileSync(path.join(__dirname, 'data/stats.csv'), 'utf-8');

const log = parseCsv(logCsv);
const films = parseCsv(filmsCsv);
const statsRows = parseCsv(statsCsv);

// Build stats lookup: { stat_type: { key: value } }
const stats = {};
for (const row of statsRows) {
  if (!stats[row.stat]) stats[row.stat] = {};
  stats[row.stat][row.key] = parseInt(row.value, 10);
}

// Helper to get top N from a stat category
function getTopN(statType, n) {
  const entries = Object.entries(stats[statType] || {});
  return entries.sort((a, b) => b[1] - a[1]).slice(0, n);
}

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

// Get stats for footer
const totalFilms = stats.watch_year?.ALL_TIME || 0;
const filmsThisYear = stats.watch_year?.['2026'] || 0;
const lastWatchedDate = watchedFilms.length > 0 ? watchedFilms[0].date : '';
const topCompanions = getTopN('companion', 3);
const topDirectors = getTopN('director', 3);
const topLanguages = getTopN('language', 3);
const topReleaseDecades = getTopN('release_decade', 3);

// Generate HTML
const html = `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>filmstruck</title>
<style>
/* Poster hover effect */
img {
  transition: transform 0.2s ease;
}
img:hover {
  transform: scale(1.5);
  z-index: 10;
  position: relative;
}
.filmstruck-header {
  font-family: monospace;
  font-size: 24px;
}

/* Footer stats layout */
footer {
  margin-top: 40px;
  padding: 20px;
}
.stats {
  display: flex;
  justify-content: center;
  gap: 60px;
  font-family: monospace;
  font-size: 14px;
}
.stat-col {
  text-align: left;
}
.stat-header {
  font-weight: bold;
  margin-bottom: 8px;
}
</style>
</head>
<body>
<center>
<h1 class="filmstruck-header">filmstruck</h1>
${watchedFilms.map(w => `<img src="https://image.tmdb.org/t/p/w154${w.film.posterPath}" alt="${w.film.title} (${w.film.releaseYear})" title="${w.film.title} (${w.film.releaseYear}) dir. ${w.film.director} - watched ${w.date}">
`).join('')}
</center>
<footer>
  <div class="stats">
    <div class="stat-col">
      <div class="stat-header">${totalFilms} films</div>
      <div>${filmsThisYear} in 2026</div>
      <div>last watched ${lastWatchedDate}</div>
    </div>
    <div class="stat-col">
      <div class="stat-header">top directors</div>
${topDirectors.map(([name, count]) => `      <div>${name}: ${count}</div>`).join('\n')}
    </div>
    <div class="stat-col">
      <div class="stat-header">top languages</div>
${topLanguages.map(([code, count]) => `      <div>${code}: ${count}</div>`).join('\n')}
    </div>
    <div class="stat-col">
      <div class="stat-header">top decades</div>
${topReleaseDecades.map(([name, count]) => `      <div>${name}: ${count}</div>`).join('\n')}
    </div>
    <div class="stat-col">
      <div class="stat-header">top companions</div>
${topCompanions.map(([name, count]) => `      <div>${name}: ${count}</div>`).join('\n')}
    </div>
  </div>
</footer>
</body>
</html>`;

// Write output
fs.writeFileSync(path.join(__dirname, 'index.html'), html);
console.log(`Generated index.html with ${watchedFilms.length} films`);
