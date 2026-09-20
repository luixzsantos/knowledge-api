// ============================================================
// SecondBrain — front-end (sem build, sem framework: html puro
// gerado por string + fetch). Organizado em seções, de cima pra
// baixo: ícones -> estado -> cliente da API -> tema -> abas
// laterais -> lista/busca -> modal -> página do verbete -> boot.
//
// Cada função tem um nome que diz exatamente o que ela monta ou
// faz — se for mexer numa seção da página do verbete, procure a
// função "build...Section" correspondente, não um bloco gigante.
// ============================================================

// ---------- ícones (SVG inline, sem lib de ícones) ----------
const ICONS = {
  moon: '<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>',
  sun: '<circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>',
  back: '<line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline>',
  close: '<line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line>',
  folder: '<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>',
  book: '<path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path>',
  bulb: '<path d="M9 18h6"></path><path d="M10 22h4"></path><path d="M12 2a7 7 0 0 0-4 12.7c.6.5 1 1.3 1 2.3h6c0-1 .4-1.8 1-2.3A7 7 0 0 0 12 2z"></path>',
  code: '<polyline points="16 18 22 12 16 6"></polyline><polyline points="8 6 2 12 8 18"></polyline>',
  target: '<circle cx="12" cy="12" r="10"></circle><circle cx="12" cy="12" r="6"></circle><circle cx="12" cy="12" r="2"></circle>',
  link: '<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"></path><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"></path>',
};
function svg(name, size) {
  return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="width:${size || 18}px;height:${size || 18}px">${ICONS[name]}</svg>`;
}

// ---------- estado da tela (tudo que a UI precisa lembrar) ----------
const state = { concepts: [], tags: [], activeTagId: null, view: { type: 'feed' }, allConceptsCache: null };

const RELATION_LABELS = { RelatedTo: 'Relacionado a', AlternativeTo: 'Alternativa a' };

// Ordem fixa de aprendizado — nunca mistura básico com avançado numa lista.
// "Sem nível" (verbetes criados a mão) fica no final, fora da progressão.
const LEVEL_ORDER = ['Basico', 'Intermediario', 'Avancado', null];
const LEVEL_LABEL = { Basico: 'Básico', Intermediario: 'Intermediário', Avancado: 'Avançado' };
function levelLabel(level) { return LEVEL_LABEL[level] || 'Sem nível'; }
function levelClass(level) { return (level || 'sem-nivel').toLowerCase(); }
function groupByLevel(concepts) {
  return LEVEL_ORDER
    .map(level => ({ level, items: concepts.filter(c => (c.level || null) === level) }))
    .filter(group => group.items.length);
}

// Logo oficial (colorido) de cada linguagem, num círculo neutro — nada de
// bandeira colorida no fundo, só o logo real. Servido localmente
// (wwwroot/icons/) em vez de CDN externa (ver decisão registrada no vault:
// dois CDNs diferentes falharam pra ícones diferentes, de forma inconsistente
// entre testes — vendorizar elimina essa dependência de rede de vez).
const TAG_LOGOS = {
  go: 'go.svg', csharp: 'csharp.svg', cpp: 'cplusplus.svg', python: 'python.svg',
  java: 'java.svg', javascript: 'javascript.svg', redis: 'redis.svg',
  postgresql: 'postgresql.svg', html: 'html5.svg',
};
const DEVICON_BASE = 'icons/';
function tagLabel(name) { return name.slice(0, 2).toUpperCase(); }

// ---------- cliente da API ----------
async function api(path, options) {
  const res = await fetch(path, Object.assign({ headers: { 'Content-Type': 'application/json' } }, options || {}));
  if (!res.ok) {
    let title = 'Algo deu errado.';
    try { const body = await res.json(); title = body.title || title; } catch (e) { /* corpo sem JSON, mantém a mensagem padrão */ }
    const err = new Error(title);
    err.status = res.status;
    throw err;
  }
  if (res.status === 204) return null;
  return res.json();
}

function escapeHtml(s) {
  return (s || '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// Render bem simples de Markdown (sem lib), usado só nas Anotações (Notes)
// livres — o conteúdo estruturado do verbete (analogia/explicação/exemplo)
// já vem em campos separados e não passa por aqui.
function renderNoteContent(content) {
  const segments = (content || '').split(/```[a-z]*\n([\s\S]*?)```/g);
  return segments.map((seg, i) => {
    if (i % 2 === 1) return `<pre class="code-block">${escapeHtml(seg.trim())}</pre>`;
    return escapeHtml(seg)
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/(https?:\/\/[^\s<]+)/g, '<a href="$1" target="_blank" rel="noopener">$1</a>');
  }).join('');
}

// Troca o conteúdo principal e sempre volta o scroll pro topo — sem isso, ao
// sair de uma lista longa (scroll bem descido) pra um verbete curto, a página
// ficava "em branco" porque continuava rolada além do conteúdo novo.
function setMainContent(html) {
  const main = document.getElementById('main');
  main.innerHTML = html;
  window.scrollTo(0, 0);
  main.scrollTop = 0;
}

// ---------- tema (claro/escuro) ----------
function applyThemeIcon() {
  const isDark = document.documentElement.getAttribute('data-theme') === 'dark'
    || (!document.documentElement.hasAttribute('data-theme') && window.matchMedia('(prefers-color-scheme: dark)').matches);
  document.getElementById('themeToggle').innerHTML = svg(isDark ? 'sun' : 'moon');
}
function toggleTheme() {
  const root = document.documentElement;
  const current = root.getAttribute('data-theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  const next = current === 'dark' ? 'light' : 'dark';
  root.setAttribute('data-theme', next);
  try { localStorage.setItem('secondbrain-theme', next); } catch (e) { /* modo privado ou storage bloqueado: tema simplesmente não persiste */ }
  applyThemeIcon();
}
(function initTheme() {
  let saved = null;
  try { saved = localStorage.getItem('secondbrain-theme'); } catch (e) { /* idem acima */ }
  if (saved) document.documentElement.setAttribute('data-theme', saved);
  document.getElementById('themeToggle').addEventListener('click', toggleTheme);
  applyThemeIcon();
})();

// ---------- rail lateral (uma aba por linguagem/tag) ----------
async function loadTags() {
  state.tags = await api('/api/tags');
  renderRail();
}

function railItemContent(name) {
  const logoPath = TAG_LOGOS[name.toLowerCase()];
  if (!logoPath) return escapeHtml(tagLabel(name));
  const fallback = escapeHtml(tagLabel(name));
  return `<img src="${DEVICON_BASE}${logoPath}" alt="${escapeHtml(name)}" loading="lazy"
    onerror="this.replaceWith(Object.assign(document.createElement('span'),{textContent:'${fallback}'}))" />`;
}

function renderRail() {
  const rail = document.getElementById('rail');
  const allActive = state.activeTagId === null;
  const items = [`<div class="rail-item${allActive ? ' active' : ''}" title="Todos" onclick="selectTag(null)">Tudo</div>`];
  items.push(...state.tags.map(t => `<div class="rail-item${state.activeTagId === t.id ? ' active' : ''}" title="${escapeHtml(t.name)}" onclick="selectTag('${t.id}')">${railItemContent(t.name)}</div>`));
  rail.innerHTML = items.join('');
}

async function selectTag(tagId) {
  state.activeTagId = tagId;
  renderRail();
  await loadAllConcepts();
  renderFeed();
}

// ---------- lista de verbetes (sidebar + feed) ----------
async function ensureAllConceptsCache() {
  if (!state.allConceptsCache) state.allConceptsCache = await api('/api/concepts');
  return state.allConceptsCache;
}

async function loadAllConcepts() {
  const url = state.activeTagId ? `/api/concepts?tagId=${state.activeTagId}` : '/api/concepts';
  state.concepts = await api(url);
  renderNav(state.concepts);
  if (state.view.type === 'feed' || state.view.type === 'search') renderFeed();
}

function activeTagName() {
  if (!state.activeTagId) return null;
  const tag = state.tags.find(t => t.id === state.activeTagId);
  return tag ? tag.name : null;
}

function renderNav(concepts) {
  const nav = document.getElementById('navList');
  if (!concepts.length) {
    nav.innerHTML = '<div class="nav-empty">Nada guardado ainda.</div>';
    return;
  }
  nav.innerHTML = groupByLevel(concepts).map(group => `
    <div class="nav-section-title">${escapeHtml(levelLabel(group.level))}</div>
    ${group.items.map(c => `
      <div class="nav-item${state.view.type === 'detail' && state.view.id === c.id ? ' active' : ''}" onclick="openConceptDetail('${c.id}')">${escapeHtml(c.name)}</div>
    `).join('')}
  `).join('');
}

function renderFeed() {
  state.view = { type: 'feed' };
  document.getElementById('searchInput').value = '';
  renderNav(state.concepts);
  const tagName = activeTagName();
  const title = tagName ? `Verbetes · ${tagName}` : 'Seus verbetes';
  if (!state.concepts.length) {
    setMainContent(`
      <div class="main-header"><h1>${escapeHtml(title)}</h1></div>
      <div class="empty-state">
        <h2>Nada por aqui ainda</h2>
        <p>${tagName ? 'Nenhum verbete guardado com essa tag ainda.' : 'Clique em "Novo verbete" pra guardar a primeira coisa que você aprendeu.'}</p>
      </div>
    `);
    return;
  }
  setMainContent(`
    <div class="main-header"><h1>${escapeHtml(title)}</h1></div>
    <div class="main-body">
      ${groupByLevel(state.concepts).map(group => `
        <div class="section-title" style="padding: 18px 20px 4px;">${escapeHtml(levelLabel(group.level))}</div>
        ${group.items.map(c => `
          <div class="row" onclick="openConceptDetail('${c.id}')">
            <div class="row-title">${escapeHtml(c.name)}</div>
            <p class="row-desc">${escapeHtml(c.description || 'Sem descrição ainda.')}</p>
          </div>
        `).join('')}
      `).join('')}
    </div>
  `);
}

// ---------- busca ----------
let searchTimer = null;
function onSearchInput() {
  clearTimeout(searchTimer);
  const q = document.getElementById('searchInput').value.trim();
  if (!q) { renderFeed(); return; }
  searchTimer = setTimeout(() => runSearch(q), 250);
}

async function runSearch(q) {
  state.view = { type: 'search', q };
  const data = await api('/api/search?q=' + encodeURIComponent(q));
  const typeLabel = { concept: 'Verbete', note: 'Anotação', project: 'Projeto' };
  if (!data.results.length) {
    setMainContent(`
      <div class="main-header"><h1>Busca</h1></div>
      <div class="empty-state">
        <h2>Nada encontrado para "${escapeHtml(q)}"</h2>
        <p>Talvez você ainda não tenha guardado isso — <a href="#" onclick="openNewConceptModal('${escapeHtml(q)}');return false;">criar um verbete</a> com esse nome?</p>
      </div>
    `);
    return;
  }
  setMainContent(`
    <div class="main-header"><h1>Busca: "${escapeHtml(q)}"</h1></div>
    <div class="main-body">
      ${data.results.map(r => `
        <div class="row" onclick="${r.type === 'concept' ? `openConceptDetail('${r.id}')` : ''}" style="${r.type !== 'concept' ? 'cursor:default' : ''}">
          <span class="row-type">${typeLabel[r.type] || r.type}</span>
          <div class="row-title">${escapeHtml(r.title)}</div>
        </div>
      `).join('')}
    </div>
  `);
}

// ---------- modal: novo verbete ----------
function modalShell(innerHtml) {
  document.getElementById('modalRoot').innerHTML = `
    <div class="overlay" onclick="if(event.target===this) closeModal()">
      <div class="modal">
        <div class="modal-head">
          <div></div>
          <button class="icon-btn" onclick="closeModal()">${svg('close', 16)}</button>
        </div>
        ${innerHtml}
      </div>
    </div>
  `;
}
function closeModal() { document.getElementById('modalRoot').innerHTML = ''; }

function openNewConceptModal(prefillName) {
  modalShell(`
    <h2>Novo verbete</h2>
    <label>Nome (o termo, a palavra, a classe...)</label>
    <input type="text" id="newName" placeholder="Ex: Redis" value="${escapeHtml(prefillName || '')}" />
    <label>O que é isso? (em suas próprias palavras)</label>
    <textarea id="newDesc" placeholder="Ex: um jeito de guardar dados na memória pra acessar rapidinho depois."></textarea>
    <label>Nível (opcional)</label>
    <select class="field" id="newLevel">
      <option value="">Sem nível</option>
      <option value="Basico">Básico</option>
      <option value="Intermediario">Intermediário</option>
      <option value="Avancado">Avançado</option>
    </select>
    <div id="newMsg" class="msg"></div>
    <div class="modal-actions">
      <button class="btn" onclick="closeModal()">Cancelar</button>
      <button class="btn-pill btn-primary" onclick="submitNewConcept()">Guardar</button>
    </div>
  `);
  document.getElementById('newName').focus();
}

async function submitNewConcept() {
  const name = document.getElementById('newName').value.trim();
  const description = document.getElementById('newDesc').value.trim();
  const level = document.getElementById('newLevel').value || null;
  const msg = document.getElementById('newMsg');
  if (!name) { msg.textContent = 'Dá um nome pra essa ideia primeiro.'; msg.className = 'msg error'; return; }
  try {
    const created = await api('/api/concepts', { method: 'POST', body: JSON.stringify({ name, description, level }) });
    closeModal();
    state.allConceptsCache = null;
    await loadAllConcepts();
    openConceptDetail(created.id);
  } catch (e) {
    msg.textContent = e.status === 409 ? 'Você já guardou algo com esse nome.' : e.message;
    msg.className = 'msg error';
  }
}

// ============================================================
// Página do verbete ("tudo centralizado"): uma leitura só, do mais
// simples ao mais técnico — analogia -> como funciona -> exemplo
// -> resultado -> onde usar -> projetos -> anotações -> relacionados.
// Cada seção só aparece se tiver conteúdo (nada de caixa vazia).
// ============================================================

async function openConceptDetail(id) {
  const c = await api('/api/concepts/' + id);
  state.view = { type: 'detail', id };
  renderNav(state.concepts);
  await renderConceptDetail(c);
}

function buildHeaderHtml(c) {
  return `
    <div class="main-header">
      <button class="icon-btn" onclick="renderFeed()">${svg('back')}</button>
      <h1>${escapeHtml(c.name)}</h1>
      ${c.level ? `<span class="level-badge ${levelClass(c.level)}">${escapeHtml(levelLabel(c.level))}</span>` : ''}
    </div>
  `;
}

// "Em termos simples" — a analogia sem jargão, primeira coisa que se lê.
function buildAnalogySection(c) {
  if (!c.simpleAnalogy) return '';
  return `
    <div class="section">
      <div class="section-title">${svg('bulb', 14)} Em termos simples</div>
      <div class="callout callout-analogy"><div class="section-body">${escapeHtml(c.simpleAnalogy)}</div></div>
    </div>
  `;
}

// "Como funciona" — a explicação técnica de verdade.
function buildExplanationSection(c) {
  if (!c.explanation) return '';
  return `
    <div class="section">
      <div class="section-title">Como funciona</div>
      <div class="section-body">${escapeHtml(c.explanation)}</div>
    </div>
  `;
}

// "Exemplo" — o código real, e logo abaixo o que ele produz (ou, se não
// produz nada visível, a explicação do que está acontecendo por baixo).
function buildCodeSection(c) {
  if (!c.codeExample && !c.expectedOutput) return '';
  return `
    <div class="section">
      <div class="section-title">${svg('code', 14)} Exemplo</div>
      ${c.codeExample ? `<pre class="code-block">${escapeHtml(c.codeExample)}</pre>` : ''}
      ${c.expectedOutput ? `<div class="console-block" style="margin-top:8px">${escapeHtml(c.expectedOutput)}</div>` : ''}
    </div>
  `;
}

// "Onde usar" — em que situação real esse código se aplica, com link de
// documentação oficial quando existir.
function buildWhereUsedSection(c) {
  if (!c.whereUsed && !c.documentationUrl) return '';
  return `
    <div class="section">
      <div class="section-title">${svg('target', 14)} Onde usar</div>
      ${c.whereUsed ? `<div class="section-body">${escapeHtml(c.whereUsed)}</div>` : ''}
      ${c.documentationUrl ? `<a class="doc-link" href="${escapeHtml(c.documentationUrl)}" target="_blank" rel="noopener">${svg('link', 13)} Documentação oficial</a>` : ''}
    </div>
  `;
}

function buildProjectsSection(c) {
  return `
    <div class="section">
      <div class="section-title">Usado nos projetos · ${c.projects.length}</div>
      ${c.projects.map(p => `
        <div class="item project-item">
          <div class="item-icon">${projectIcon(p.name)}</div>
          <div><div class="t">${escapeHtml(p.name)}</div><div class="d">${escapeHtml(p.description || '')}</div></div>
        </div>
      `).join('') || '<div class="item-empty">Nenhum ainda.</div>'}
    </div>
  `;
}

// "Referência Geral (...)" é conteúdo genérico, não um repositório de
// verdade — ganha o ícone de livro. O resto é projeto/repositório real.
function projectIcon(name) {
  return name.startsWith('Referência Geral') ? svg('book', 16) : svg('folder', 16);
}

// Anotações: notas livres que você mesmo adiciona sobre este verbete —
// diferente do conteúdo estruturado acima, que vem pronto quando importado.
function buildNotesSection(c) {
  return `
    <div class="section">
      <div class="section-title">Anotações · ${c.notes.length}</div>
      ${c.notes.map(n => `<div class="item"><div class="t">${escapeHtml(n.title)}</div><div class="d">${renderNoteContent(n.content)}</div></div>`).join('') || '<div class="item-empty">Nenhuma ainda.</div>'}
      <div class="quick-add">
        <input class="field" type="text" id="quickNoteTitle" placeholder="Título" />
        <input class="field" type="text" id="quickNoteContent" placeholder="O que você quer lembrar?" />
        <button class="btn" onclick="quickAddNote('${c.id}')">Anotar</button>
      </div>
    </div>
  `;
}

function buildRelationsSection(c, pickableConcepts) {
  return `
    <div class="section">
      <div class="section-title">Relacionados · ${c.relations.length}</div>
      ${c.relations.map(r => `
        <div class="item relation-item" onclick="openConceptDetail('${r.conceptId}')">
          <div><div class="t">${escapeHtml(r.conceptName)}</div><div class="d">${escapeHtml(RELATION_LABELS[r.relationType] || r.relationType)}</div></div>
          <button class="item-remove" onclick="event.stopPropagation(); unlinkRelation('${c.id}','${r.conceptId}','${r.relationType}')">remover</button>
        </div>
      `).join('') || '<div class="item-empty">Nenhum ainda.</div>'}
      <div class="quick-add">
        <input class="field" list="allConceptsList" type="text" id="relationConceptName" placeholder="Nome de outro verbete" />
        <datalist id="allConceptsList">${pickableConcepts.map(x => `<option value="${escapeHtml(x.name)}">`).join('')}</datalist>
        <select class="field" id="relationType">
          <option value="RelatedTo">Relacionado a</option>
          <option value="AlternativeTo">Alternativa a</option>
        </select>
        <button class="btn" onclick="addRelation('${c.id}')">Relacionar</button>
      </div>
    </div>
  `;
}

async function renderConceptDetail(c) {
  const allConcepts = await ensureAllConceptsCache();
  const relatedIds = new Set(c.relations.map(r => r.conceptId));
  const pickableConcepts = allConcepts.filter(x => x.id !== c.id && !relatedIds.has(x.id));

  setMainContent(`
    ${buildHeaderHtml(c)}
    <div class="detail">
      <p class="desc">${escapeHtml(c.description || 'Sem descrição ainda.')}</p>
      ${c.tags.length ? `<div class="tag-row">${c.tags.map(t => `<span class="tag-pill">${escapeHtml(t.name)}</span>`).join('')}</div>` : ''}

      ${buildAnalogySection(c)}
      ${buildExplanationSection(c)}
      ${buildCodeSection(c)}
      ${buildWhereUsedSection(c)}
      ${buildProjectsSection(c)}
      ${buildNotesSection(c)}
      ${buildRelationsSection(c, pickableConcepts)}

      <div id="detailMsg" class="msg"></div>
      <div class="detail-footer">
        <button class="btn btn-danger" onclick="deleteConcept('${c.id}')">Apagar este verbete</button>
      </div>
    </div>
  `);
}

// ---------- ações da página do verbete ----------
async function addRelation(conceptId) {
  const nameInput = document.getElementById('relationConceptName');
  const name = nameInput.value.trim();
  const type = document.getElementById('relationType').value;
  const msg = document.getElementById('detailMsg');
  const allConcepts = await ensureAllConceptsCache();
  const target = allConcepts.find(x => x.name.toLowerCase() === name.toLowerCase());
  if (!target) { msg.textContent = 'Digite o nome de um verbete que já existe (aparece sugestão ao digitar).'; msg.className = 'msg error'; return; }
  try {
    await api(`/api/concepts/${conceptId}/relations/${target.id}`, { method: 'POST', body: JSON.stringify({ type }) });
    const c = await api('/api/concepts/' + conceptId);
    await renderConceptDetail(c);
  } catch (e) {
    msg.textContent = e.status === 409 ? 'Esses dois verbetes já estão relacionados assim.' : e.message;
    msg.className = 'msg error';
  }
}

async function unlinkRelation(conceptId, relatedConceptId, type) {
  await api(`/api/concepts/${conceptId}/relations/${relatedConceptId}?type=${type}`, { method: 'DELETE' });
  const c = await api('/api/concepts/' + conceptId);
  await renderConceptDetail(c);
}

async function quickAddNote(conceptId) {
  const title = document.getElementById('quickNoteTitle').value.trim();
  const content = document.getElementById('quickNoteContent').value.trim();
  const msg = document.getElementById('detailMsg');
  if (!title || !content) { msg.textContent = 'Preenche o título e o que você quer lembrar.'; msg.className = 'msg error'; return; }
  try {
    await api('/api/notes', { method: 'POST', body: JSON.stringify({ title, content, conceptIds: [conceptId] }) });
    const c = await api('/api/concepts/' + conceptId);
    await renderConceptDetail(c);
  } catch (e) {
    msg.textContent = e.message;
    msg.className = 'msg error';
  }
}

async function deleteConcept(id) {
  if (!confirm('Tem certeza que quer apagar esse verbete? Isso não pode ser desfeito.')) return;
  await api('/api/concepts/' + id, { method: 'DELETE' });
  await loadAllConcepts();
  renderFeed();
}

// ---------- rodapé de estatísticas ----------
async function loadStats() {
  const [notes, projects, tags] = await Promise.all([api('/api/notes'), api('/api/projects'), api('/api/tags')]);
  document.getElementById('statsLine').textContent =
    `${state.concepts.length} verbetes · ${notes.length} anotações · ${projects.length} projetos · ${tags.length} tags`;
}

// ---------- boot ----------
loadTags();
loadAllConcepts().then(() => { renderFeed(); loadStats(); });
