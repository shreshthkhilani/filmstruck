const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const script = readFileSync(join(__dirname, '../src/FilmStruck.Cli/Templates/app.js'), 'utf8');

const films = ['F', 'B', 'E', 'D', 'C', 'A'].map((suffix, index) => ({
  date: '1/1/2024',
  title: 'Film ' + suffix,
  director: 'Director ' + suffix,
  language: 'lang-' + suffix,
  releaseYear: String(1960 + 10 * index),
  location: 'Location ' + suffix,
  companions: 'Selected,Companion ' + suffix
}));
films.push(films[2]); // E ranks first. The remaining entries tie in log order.

function renderFooter(films, search = '') {
  const stats = {};
  const element = { style: {}, addEventListener() {} };
  const document = {
    getElementById(id) { return id === 'stats' ? stats : element; },
    createElement() {
      return { get innerHTML() { return this.textContent; } };
    }
  };

  vm.runInNewContext(script, {
    document,
    window: { location: { search }, addEventListener() {} },
    URLSearchParams,
    FILMS: films,
    HEARTS: [],
    COMPANIONS: [],
    USERNAME: 'test'
  });

  return stats.innerHTML;
}

function entries(html, heading) {
  const column = html.split('<div class="stat-header">' + heading + '</div>')[1];
  assert.ok(column, 'expected footer heading ' + heading);
  return [...column.split('</div></div>')[0].matchAll(/<div>([^<]+)/g)].map(match => match[1]);
}

test('footer summary ends with hours watched when some runtimes are missing', () => {
  const year = new Date().getFullYear();
  const date = '1/1/' + year;
  const html = renderFooter([{ ...films[0], date, runtimeMinutes: 120 }, films[1]]);
  const summary = html.slice(0, html.indexOf('</div></div>') + '</div></div>'.length);
  assert.deepEqual([...summary.matchAll(/<div[^>]*>([^<]+)/g)].map(match => match[1]), [
    '2 films', '1 in ' + year, 'last watched ' + date, '2 hours watched'
  ]);
});

test('footer shows five entries per category, with ties in log order', () => {
  const html = renderFooter(films);
  const suffixes = ['E: 2', 'F: 1', 'B: 1', 'D: 1', 'C: 1'];
  assert.deepEqual(entries(html, 'top directors'), suffixes.map(s => 'Director ' + s));
  assert.deepEqual(entries(html, 'top languages'), suffixes.map(s => 'lang-' + s));
  assert.deepEqual(entries(html, 'top locations'), suffixes.map(s => 'Location ' + s));
  assert.deepEqual(entries(html, 'top decades'), ['1980s: 2', '1960s: 1', '1970s: 1', '1990s: 1', '2000s: 1']);
  assert.deepEqual(entries(html, 'top companions'), [
    'Selected: 7', 'Companion E: 2', 'Companion F: 1', 'Companion B: 1', 'Companion D: 1'
  ]);
});

test('also with excludes the selected companion before taking five', () => {
  const html = renderFooter(films, '?with=selected');
  assert.deepEqual(entries(html, 'also with'), [
    'Companion E: 2', 'Companion F: 1', 'Companion B: 1', 'Companion D: 1', 'Companion C: 1'
  ]);
});

test('footer shows every available entry when there are fewer than five', () => {
  assert.deepEqual(entries(renderFooter(films.slice(0, 1)), 'top directors'), ['Director F: 1']);
});

test('numeric and reserved labels rank by count and first appearance', () => {
  const labeledFilms = ['20', '3', 'constructor', '__proto__', '10', '1', '__proto__']
    .map(label => ({ ...films[0], location: label, companions: 'Selected,' + label }));
  const expected = ['__proto__: 2', '20: 1', '3: 1', 'constructor: 1', '10: 1'];
  assert.deepEqual(entries(renderFooter(labeledFilms), 'top locations'), expected);
  assert.deepEqual(entries(renderFooter(labeledFilms, '?with=selected'), 'also with'), expected);
});
