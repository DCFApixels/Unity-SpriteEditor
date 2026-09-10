// Documentation-only checks. Never invokes Unity or modifies its assets.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

const source = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repository = path.dirname(source);
const mode = process.argv[2];
const errors = [];
const fail = message => errors.push(message);
const text = file => fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n');
const decode = value => value.replace(/&amp;/g, '&').replace(/&quot;/g, '"').replace(/&#39;/g, "'");
const ignored = new Set(['.git', '.bundle', '.jekyll-cache', '.sass-cache', 'vendor', '_site']);

function filesUnder(directory, exclusions = ignored) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    if (exclusions.has(entry.name)) return [];
    const file = path.join(directory, entry.name);
    return entry.isDirectory() ? filesUnder(file, exclusions) : [file];
  });
}

function frontMatter(content) {
  const match = content.match(/^---\n([\s\S]*?)\n---\n/);
  const values = {};
  if (!match) return values;
  for (const line of match[1].split('\n')) {
    const field = line.match(/^([\w_]+):\s*(.*?)\s*$/);
    if (!field) { fail(`Unsupported front matter line: ${line}`); continue; }
    let value = field[2];
    if (value.startsWith('"')) value = JSON.parse(value);
    values[field[1]] = value;
  }
  return values;
}

function checkSource() {
  const markdown = filesUnder(source).filter(file => file.endsWith('.md'));
  const pages = new Map();
  const permalinks = new Set();
  for (const file of markdown) {
    const fm = frontMatter(text(file));
    if (!fm.title) continue; // License texts are copied without a layout.
    pages.set(path.relative(source, file).replaceAll('\\', '/'), fm);
    if (!fm.permalink || permalinks.has(fm.permalink)) fail(`${file}: missing or duplicate permalink`);
    permalinks.add(fm.permalink);
  }
  for (const [name, fm] of pages) {
    if (fm.parent && ![...pages.values()].some(p => p.title === fm.parent && (!fm.grand_parent || p.parent === fm.grand_parent)))
      fail(`${name}: unresolved navigation parent ${fm.parent}`);
    for (const key of ['alternate', 'previous_page', 'next_page']) {
      if (fm[key] && !pages.has(fm[key])) fail(`${name}: missing ${key} ${fm[key]}`);
    }
    if (/^(en|ru)\//.test(name)) {
      if (!fm.alternate || pages.get(fm.alternate)?.alternate !== name) fail(`${name}: missing reciprocal translation`);
      if (fm.lang !== name.slice(0, 2)) fail(`${name}: wrong language`);
    }
  }
  for (const file of [...markdown, path.join(repository, 'README.md'), path.join(repository, 'README-RU.md')]) {
    const content = text(file).replace(/^```[^\n]*\n[\s\S]*?^```\s*$/gm, '');
    if (/^```/m.test(content)) fail(`${file}: unclosed code fence`);
    const links = [...content.matchAll(/\]\(([^\s)]+)(?:\s+"[^"]*")?\)/g)].map(m => m[1]);
    for (const link of links) {
      if (/^(?:[a-z]+:|#|\/\/)/i.test(link)) continue;
      const pathname = decodeURIComponent(link.split('#')[0]);
      if (!fs.existsSync(path.resolve(path.dirname(file), pathname))) fail(`${file}: missing source link ${link}`);
    }
    if (/\b(?:PLACEHOLDER|TODO_TRANSLATE)\b/.test(content)) fail(`${file}: unfinished content`);
  }
  console.log(`Source: ${pages.size} pages, reciprocal EN/RU navigation and local Markdown links checked.`);
  const listeners = [];
  let closed = false;
  let stopped = false;
  vm.runInNewContext(text(path.join(source, '_includes/js/custom.js')), {
    jtd: { onReady: callback => callback() },
    document: {
      documentElement: { classList: { remove: () => { closed = true; } } },
      getElementById: () => ({ addEventListener: (name, handler, capture) => { if (name === 'focusout' && capture) listeners.push(handler); } })
    }
  });
  if (listeners.length !== 2) fail('Focus guard was not attached to input and results');
  for (const listener of listeners) {
    closed = stopped = false;
    listener({ relatedTarget: null, stopImmediatePropagation: () => { stopped = true; } });
    if (!closed || !stopped) fail('Search does not close safely without a focus target');
    closed = stopped = false;
    listener({ relatedTarget: {}, stopImmediatePropagation: () => { stopped = true; } });
    if (closed || stopped) fail('Search guard intercepts ordinary focus transitions');
  }
}

function checkSite() {
  const output = path.join(source, '_site');
  if (!fs.existsSync(output)) throw new Error('Build the Jekyll site first.');
  const htmlFiles = filesUnder(output, new Set()).filter(file => file.endsWith('.html'));
  const baseurl = text(path.join(source, '_config.yml')).match(/^baseurl:\s*(.*)$/m)[1].trim();
  const origin = 'https://docs.invalid';
  const anchorCache = new Map();
  for (const file of htmlFiles) {
    const html = text(file);
    if (process.env.GITHUB_SHA && !html.includes(`/assets/js/just-the-docs.js?v=${process.env.GITHUB_SHA}`))
      fail(`${file}: main script is not revisioned`);
    const relative = path.relative(output, file).replaceAll('\\', '/').replace(/index\.html$/, '');
    const current = new URL(`${baseurl}/${relative}`, origin);
    for (const match of html.matchAll(/\b(?:href|src)="([^"]+)"/g)) {
      const href = decode(match[1]);
      if (/^(?:mailto:|tel:|data:|javascript:)/i.test(href)) continue;
      const url = new URL(href, current);
      if (url.origin !== origin) continue;
      if (!url.pathname.startsWith(baseurl + '/') && url.pathname !== baseurl)
        { fail(`${relative}: link escapes baseurl: ${href}`); continue; }
      let target = path.join(output, decodeURIComponent(url.pathname.slice(baseurl.length)));
      if (fs.existsSync(target) && fs.statSync(target).isDirectory()) target = path.join(target, 'index.html');
      if (!fs.existsSync(target)) { fail(`${relative}: missing generated link ${href}`); continue; }
      if (url.hash && target.endsWith('.html')) {
        if (!anchorCache.has(target)) anchorCache.set(target, new Set([...text(target).matchAll(/\bid="([^"]+)"/g)].map(m => decode(m[1]))));
        if (!anchorCache.get(target).has(decodeURIComponent(url.hash.slice(1)))) fail(`${relative}: missing fragment ${href}`);
      }
    }
    if (/\{%|\{\{/.test(html.replace(/<pre[\s\S]*?<\/pre>/g, ''))) fail(`${relative}: unrendered Liquid`);
  }
  const search = JSON.parse(text(path.join(output, 'assets/js/search-data.json')));
  const entries = Object.values(search);
  const lunrFile = filesUnder(path.join(output, 'assets/js'), new Set()).find(file => /lunr(?:\.min)?\.js$/.test(file));
  if (!lunrFile) throw new Error('Generated Lunr library is missing.');
  const context = vm.createContext({ console });
  vm.runInContext(text(lunrFile), context);
  const customize = vm.runInContext('(function () {\n' + text(path.join(source, '_includes/lunr/custom-index.js')) + '\n})', context);
  const index = context.lunr(function () {
    this.ref('id');
    this.field('title');
    this.field('content');
    customize.call(this);
    for (const [id, entry] of Object.entries(search)) this.add({ id, title: entry.title, content: entry.content });
  });
  for (const [query, expected] of [['симметрия', '/ru/symmetry/'], ['brush', '/en/painting/']]) {
    if (!index.search(query).some(result => search[result.ref].url.includes(expected))) fail(`Search query '${query}' did not find ${expected}`);
  }
  for (const lang of ['en', 'ru']) {
    if (!entries.some(entry => entry.url?.includes(`/${lang}/painting/`) && entry.content?.length > 100)) fail(`${lang}: painting page absent from search index`);
    const start = text(path.join(output, lang, 'getting-started/index.html'));
    if (!start.includes(`/${lang === 'en' ? 'ru' : 'en'}/getting-started/`)) fail(`${lang}: translation link missing from rendered page`);
  }
  if (fs.existsSync(path.join(output, 'Gemfile')) || fs.existsSync(path.join(output, 'scripts'))) fail('Build tooling leaked into published site');
  console.log(`Site: ${htmlFiles.length} HTML pages, local assets/fragments and ${entries.length} search entries checked.`);
}

if (mode === 'source') checkSource();
else if (mode === 'site') checkSite();
else throw new Error('Usage: node scripts/check-docs.mjs source|site');
if (errors.length) {
  console.error(errors.join('\n'));
  process.exitCode = 1;
} else console.log('Documentation checks passed.');
