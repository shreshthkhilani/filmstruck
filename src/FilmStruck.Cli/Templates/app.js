// Parse date in M/D/YYYY format
function parseDate(str) {
  const [m, d, y] = str.split('/').map(Number);
  return new Date(y, m - 1, d);
}

// Get companion filter from URL
function getCompanionFilter() {
  const params = new URLSearchParams(window.location.search);
  const withParam = params.get('with');
  if (!withParam) return [];
  return withParam.split(',').map(c => decodeURIComponent(c.trim())).filter(c => c).slice(0, 2);
}

// Set companion filter in URL
function setCompanionFilter(companions) {
  const url = new URL(window.location);
  if (companions.length === 0) {
    url.searchParams.delete('with');
  } else {
    url.searchParams.set('with', companions.map(c => encodeURIComponent(c)).join(','));
  }
  window.history.pushState({}, '', url);
  render();
}

// Filter films by companions (case-insensitive)
function filterFilms(films, companions) {
  if (companions.length === 0) return films;
  const lowerCompanions = companions.map(c => c.toLowerCase());
  return films.filter(f => {
    const filmCompanions = f.companions
      .split(',')
      .map(c => c.trim().toLowerCase())
      .filter(c => c);
    return lowerCompanions.every(c => filmCompanions.includes(c));
  });
}

// Sort films by date (reverse chronological), preserving log order for same day
function sortFilms(films) {
  return films
    .map((f, i) => ({ film: f, index: i }))
    .sort((a, b) => {
      const dateDiff = parseDate(b.film.date) - parseDate(a.film.date);
      if (dateDiff !== 0) return dateDiff;
      // Same date: higher index (added later) comes first
      return b.index - a.index;
    })
    .map(item => item.film);
}

// Calculate stats for films
function calculateStats(films) {
  const stats = {
    total: films.length,
    thisYear: 0,
    lastWatched: films.length > 0 ? sortFilms(films)[0].date : '',
    directors: {},
    languages: {},
    locations: {},
    decades: {},
    companions: {}
  };

  const currentYear = new Date().getFullYear().toString();

  for (const f of films) {
    // Watch year
    const watchYear = parseDate(f.date).getFullYear().toString();
    if (watchYear === currentYear) stats.thisYear++;

    // Directors
    if (f.director) {
      const directors = f.director.split(',').map(d => d.trim()).filter(d => d);
      for (const d of directors) {
        stats.directors[d] = (stats.directors[d] || 0) + 1;
      }
    }

    // Languages
    if (f.language) {
      stats.languages[f.language] = (stats.languages[f.language] || 0) + 1;
    }

    // Locations
    if (f.location) {
      stats.locations[f.location] = (stats.locations[f.location] || 0) + 1;
    }

    // Release decades
    if (f.releaseYear) {
      const decade = Math.floor(parseInt(f.releaseYear) / 10) * 10 + 's';
      stats.decades[decade] = (stats.decades[decade] || 0) + 1;
    }

    // Companions
    if (f.companions) {
      const companions = f.companions.split(',').map(c => c.trim()).filter(c => c);
      for (const c of companions) {
        stats.companions[c] = (stats.companions[c] || 0) + 1;
      }
    }
  }

  return stats;
}

// Get top N from object
function getTopN(obj, n) {
  return Object.entries(obj)
    .sort((a, b) => b[1] - a[1])
    .slice(0, n);
}

// Render header
function renderHeader(filter) {
  const header = document.getElementById('user-header');
  if (filter.length === 0) {
    header.innerHTML = '<span class="user-name">' + escapeHtml(USERNAME) + '</span>';
  } else if (filter.length === 1) {
    header.innerHTML = '<span class="user-name">' + escapeHtml(USERNAME) + '</span> <span class="heart">♥</span> <span class="user-name">' + escapeHtml(filter[0].toLowerCase()) + '</span>';
  } else {
    header.innerHTML = '<span class="user-name">' + escapeHtml(USERNAME) + '</span> <span class="heart">♥</span> <span class="user-name">' + escapeHtml(filter[0].toLowerCase()) + '</span> <span class="heart">♥</span> <span class="user-name">' + escapeHtml(filter[1].toLowerCase()) + '</span>';
  }
}

// Escape HTML
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Generate placeholder SVG for films without posters
function generatePlaceholder(title) {
  // Truncate long titles and escape for SVG/XML
  const displayTitle = title.length > 50 ? title.substring(0, 47) + '...' : title;
  const escaped = displayTitle.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  const svg = '<svg xmlns="http://www.w3.org/2000/svg" width="154" height="231" viewBox="0 0 154 231">' +
    '<rect fill="#2a2a2a" width="154" height="231"/>' +
    '<text x="77" y="115" text-anchor="middle" fill="#888" font-family="system-ui,sans-serif" font-size="12">' + escaped + '</text>' +
    '</svg>';
  return 'data:image/svg+xml,' + encodeURIComponent(svg);
}

// Render poster grid
function renderPosters(films) {
  const grid = document.getElementById('poster-grid');
  const sorted = sortFilms(films);

  if (sorted.length === 0) {
    grid.innerHTML = '<div class="empty-state">no films found</div>';
    return;
  }

  grid.innerHTML = sorted.map(f => {
    const lines = [
      f.title + ' (' + f.releaseYear + ')',
      'dir. ' + (f.director || '').replace(/,/g, ', '),
      '',
      'Watched ' + f.date + ' · ' + f.location + (f.companions ? ' · ' + f.companions.replace(/,/g, ', ') : '')
    ];
    const tooltip = lines.join('\n');
    const posterUrl = f.posterPath ? 'https://image.tmdb.org/t/p/w154' + f.posterPath : generatePlaceholder(f.title);
    return '<img src="' + posterUrl + '" alt="' + escapeHtml(f.title) + '" title="' + escapeHtml(tooltip) + '">';
  }).join('\n');
}

// Render stats
function renderStats(stats, filter) {
  const statsEl = document.getElementById('stats');
  const topDirectors = getTopN(stats.directors, 3);
  const topLanguages = getTopN(stats.languages, 3);
  const topDecades = getTopN(stats.decades, 3);
  const topCompanions = getTopN(stats.companions, 3);

  // Filter out the selected companions from "top companions"
  const filterLower = filter.map(c => c.toLowerCase());
  const otherCompanions = getTopN(
    Object.fromEntries(
      Object.entries(stats.companions).filter(([name]) => !filterLower.includes(name.toLowerCase()))
    ),
    3
  );

  let html = '';

  // Total films
  html += '<div class="stat-col">';
  html += '<div class="stat-header total-stat-header">' + stats.total + ' films</div>';
  html += '<div>' + stats.thisYear + ' in ' + new Date().getFullYear() + '</div>';
  if (stats.lastWatched) html += '<div>last watched ' + stats.lastWatched + '</div>';
  html += '</div>';

  // Top directors
  if (topDirectors.length > 0) {
    html += '<div class="stat-col">';
    html += '<div class="stat-header">top directors</div>';
    html += topDirectors.map(([name, count]) => '<div>' + escapeHtml(name) + ': ' + count + '</div>').join('');
    html += '</div>';
  }

  // Top languages
  if (topLanguages.length > 0) {
    html += '<div class="stat-col">';
    html += '<div class="stat-header">top languages</div>';
    html += topLanguages.map(([code, count]) => '<div>' + code + ': ' + count + '</div>').join('');
    html += '</div>';
  }

  // Top decades
  if (topDecades.length > 0) {
    html += '<div class="stat-col">';
    html += '<div class="stat-header">top decades</div>';
    html += topDecades.map(([name, count]) => '<div>' + name + ': ' + count + '</div>').join('');
    html += '</div>';
  }

  // Top locations
  const topLocations = getTopN(stats.locations, 3);
  if (topLocations.length > 0) {
    html += '<div class="stat-col">';
    html += '<div class="stat-header">top locations</div>';
    html += topLocations.map(([name, count]) => '<div>' + escapeHtml(name) + ': ' + count + '</div>').join('');
    html += '</div>';
  }

  // Top companions (or "also with" when filtering)
  const companionLabel = filter.length > 0 ? 'also with' : 'top companions';
  const companionList = filter.length > 0 ? otherCompanions : topCompanions;
  if (companionList.length > 0) {
    html += '<div class="stat-col">';
    html += '<div class="stat-header">' + companionLabel + '</div>';
    html += companionList.map(([name, count]) => '<div>' + escapeHtml(name) + ': ' + count + '</div>').join('');
    html += '</div>';
  }

  statsEl.innerHTML = html;
}

// Render dropdowns
function renderDropdowns(filter) {
  const select1 = document.getElementById('companion1');
  const select2 = document.getElementById('companion2');
  const clearBtn = document.getElementById('clear-filter');

  // Populate companion1
  select1.innerHTML = '<option value="">everyone</option>';
  for (const c of COMPANIONS) {
    const selected = filter[0] && filter[0].toLowerCase() === c.name.toLowerCase() ? ' selected' : '';
    select1.innerHTML += '<option value="' + escapeHtml(c.name) + '"' + selected + '>' + escapeHtml(c.name.toLowerCase()) + ' (' + c.count + ')</option>';
  }

  // Show/hide companion2 and clear button
  if (filter.length >= 1) {
    select2.style.display = 'inline';
    clearBtn.style.display = 'inline';

    // Populate companion2 (exclude first selection)
    select2.innerHTML = '<option value="">+ add companion</option>';
    for (const c of COMPANIONS) {
      if (filter[0] && filter[0].toLowerCase() === c.name.toLowerCase()) continue;
      const selected = filter[1] && filter[1].toLowerCase() === c.name.toLowerCase() ? ' selected' : '';
      select2.innerHTML += '<option value="' + escapeHtml(c.name) + '"' + selected + '>' + escapeHtml(c.name.toLowerCase()) + '</option>';
    }
  } else {
    select2.style.display = 'none';
    clearBtn.style.display = 'none';
  }
}

// Main render function
function render() {
  const filter = getCompanionFilter();
  const filtered = filterFilms(FILMS, filter);
  const stats = calculateStats(filtered);

  renderHeader(filter);
  renderDropdowns(filter);
  renderPosters(filtered);
  renderStats(stats, filter);

  // Update page title
  if (filter.length > 0) {
    document.title = 'filmstruck - ' + USERNAME + ' ♥ ' + filter.map(c => c.toLowerCase()).join(' ♥ ');
  } else {
    document.title = 'filmstruck';
  }
}

// Event listeners
document.getElementById('companion1').addEventListener('change', function() {
  const val = this.value;
  if (val) {
    setCompanionFilter([val]);
  } else {
    setCompanionFilter([]);
  }
});

document.getElementById('companion2').addEventListener('change', function() {
  const filter = getCompanionFilter();
  const val = this.value;
  if (val && filter.length >= 1) {
    setCompanionFilter([filter[0], val]);
  } else if (filter.length >= 1) {
    setCompanionFilter([filter[0]]);
  }
});

document.getElementById('clear-filter').addEventListener('click', function() {
  setCompanionFilter([]);
});

// Handle browser back/forward
window.addEventListener('popstate', render);

// Initial render
render();
