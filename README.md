# 🧠 knowledge-api

Uma enciclopédia pessoal: guarde o que você aprende (um termo, uma definição, uma tecnologia) e encontre de
novo quando esquecer. Por baixo tem uma API em **C# / ASP.NET Core**, mas o ponto de entrada pra quem só quer
usar é uma página simples — sem jargão, sem Swagger — com busca e um botão "Guardar algo novo". O conhecimento
também pode vir importado direto do [Segundo Cérebro (Obsidian)](#sincronizar-com-o-obsidian), pra não precisar
digitar tudo de novo.

Este README documenta até a **V0.3.3** (Knowledge Graph da V0.3 + nível/analogia/resposta de cada verbete das
V0.3.1/V0.3.2 + conhecimento real de um colaborador do GitHub na V0.3.3 — catálogo de 250 verbetes prontos,
entre 6 linguagens, Redis, PostgreSQL e HTML/CSS).

---

## Índice

- [Visão do produto](#visão-do-produto)
- [A interface (pra qualquer pessoa usar)](#a-interface-pra-qualquer-pessoa-usar)
- [Sincronizar com o Obsidian](#sincronizar-com-o-obsidian)
- [Base de conhecimento de linguagens](#base-de-conhecimento-de-linguagens)
- [Arquitetura](#arquitetura)
- [Modelo de dados](#modelo-de-dados)
- [Stack](#stack)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Pré-requisitos](#pré-requisitos)
- [Como rodar](#como-rodar)
- [Uso da API](#uso-da-api)
- [Busca full-text](#busca-full-text)
- [Testes](#testes)
- [Segurança](#segurança)
- [Decisões desta versão](#decisões-desta-versão)
- [Roadmap](#roadmap)

---

## Visão do produto

Separar **conhecimento** ("o que é Redis?"), **experiência** ("como eu usei Redis?"), **projeto** ("em qual
projeto?") e **decisão** ("por que Redis e não RabbitMQ?") em vez de jogar tudo num bloco de texto genérico
(estilo Notion) — mas sem exigir que quem usa entenda essa separação. Na prática, pra quem abre a página, é só
um verbete de enciclopédia: nome, definição, e o que mais está relacionado (anotações, projetos, tags).

## A interface (pra qualquer pessoa usar)

`http://localhost:5080/` (aberta automaticamente pelo [`start.bat`](start.bat)) é uma página HTML única, sem
build/framework — só abrir e usar:

- **Layout em três colunas** (rail de abas + sidebar com verbetes/busca + conteúdo principal) — inspirado em
  Discord/X: flat, sem gradiente, sem sombra colorida, bordas finas separando os blocos, um único accent
  (azul), zero emoji na interface. Tema claro e escuro, cada um seguindo a paleta desses apps.
- **Abas laterais por linguagem** (a "rail" mais à esquerda, estilo lista de servidores do Discord): um círculo
  por Tag ("Go", "C#", "Py"...) filtra os verbetes só daquela linguagem; "Tudo" volta a mostrar todos.
- **Busca** na sidebar, estilo enciclopédia: digite um termo e aparece na hora (verbetes, anotações, projetos).
- **"+ Novo verbete"**: um formulário de duas perguntas — "qual o nome?" e "o que é isso, nas suas
  palavras?" — sem nenhum campo técnico.
- **Clicar num verbete** troca o conteúdo principal pra definição completa + anotações e projetos
  relacionados (like uma thread do X, com botão de voltar), com um campo rápido pra anotar mais alguma coisa
  ali mesmo.

Pensada pra alguém que não sabe (nem precisa saber) o que é uma API — quem quiser mexer nos detalhes técnicos
ainda tem o Swagger em `/swagger`.

## Sincronizar com o Obsidian

O vault de origem (Segundo Cérebro) é público em
[github.com/luixzsantos/obsidian-vault](https://github.com/luixzsantos/obsidian-vault) — é o próprio vault
Obsidian versionado, não um espelho estático: sempre reflete o conteúdo mais recente, então não faz sentido
duplicar um snapshot dele aqui dentro do knowledge-api.

Se você já anota tecnologias e conceitos no seu vault do Obsidian (pastas `05-STACK TECNOLÓGICA` e
`08-CONCEITOS FUNDAMENTAIS`), não precisa digitar tudo de novo aqui: [`sync-obsidian.bat`](sync-obsidian.bat)
importa cada nota como um verbete (`Concept`) — a definição curta vem da seção "## O que é" da nota (quando
existe), e o conteúdo completo (limpo de sintaxe Markdown/wikilinks) vira uma `Note` relacionada. Rodar de novo
**atualiza** em vez de duplicar (casa por nome). Precisa da API no ar (`start.bat` primeiro).

Script: [`scripts/sync-obsidian.ps1`](scripts/sync-obsidian.ps1). Hoje é sob demanda (você decide quando
rodar); automatizar via tarefa agendada do Windows é uma opção futura, ainda não configurada.

## Base de conhecimento de linguagens e bancos de dados

O knowledge-api vem com **250 verbetes** cobrindo 6 linguagens + Redis + PostgreSQL + HTML/CSS, do "Hello
World" a tópicos avançados (OOP, generics, async, ponteiros inteligentes, consumer groups, índices parciais,
FastAPI, window functions...):

- **108 vêm de código real** de projetos de verdade — não são exemplos inventados: **Go**, **Redis** e
  **PostgreSQL** (Notification Engine — Redis Streams/consumer groups e SQL puro sobre `database/sql`), **C#**
  (este projeto), **C++/Arduino** (PlatformIO-Projects), **Java** e **JavaScript** (all-notes), **Python**
  (Python-notes, pid-system-in-Python, pdf-editor) — do próprio usuário — mais **Python** (FastAPI, Pydantic,
  Strategy pattern), **JavaScript** e **HTML/CSS** e **SQL** de repositórios públicos de um colaborador do
  GitHub (identificados como "colaborador", não "usuário", no Project de origem).
- **142 são currículo de referência geral** — o restante do que uma linguagem/tecnologia tem, do básico ao
  avançado, pensado pra alguém que não sabe nada do assunto: exemplos genéricos corretos (não vêm de um
  repositório específico, por isso ficam num Project à parte, "Referência Geral (Linguagem)"), cada um com um
  link pra documentação oficial (MDN, docs.python.org, Microsoft Learn, Oracle, cppreference, go.dev,
  redis.io, postgresql.org).

Cada verbete tem um **nível** (`Concept.Level`: Básico/Intermediário/Avançado) que ordena toda listagem e
filtro por linguagem — a interface nunca mistura conteúdo básico com avançado na mesma tela, seguindo a
progressão de quem está aprendendo do zero. Verbetes criados manualmente pela interface podem ficar sem
nível (aparecem por último, fora da progressão).

A anotação de cada verbete segue uma ordem fixa de leitura: primeiro uma **analogia em linguagem simples**
(pra alguém que nunca programou), depois o nível e a explicação técnica com o exemplo de código, depois a
**resposta esperada** (o que aparece de fato no console/terminal/retorno ao rodar aquele código — a "resposta"
que faltava só com o exemplo + a explicação), e só no final o **repositório real** onde aquele mesmo código
foi usado — do mais simples ao mais técnico, terminando sempre em "isso é real, não é um exemplo de livro".

Scripts: [`scripts/import-language-knowledge.ps1`](scripts/import-language-knowledge.ps1), lendo os dados de
[`scripts/data/language-knowledge/`](scripts/data/language-knowledge) (um arquivo por linguagem/tecnologia, com
`level`, `simpleAnalogy` e `resposta` por conceito). Idempotente (roda de novo sem duplicar, só atualiza).
Requer a API no ar.

## Arquitetura

Clean Architecture em 4 camadas, dependência sempre de fora pra dentro:

```
SecondBrain.API            → Controllers, Swagger, middleware de erro, DI, appsettings
      ↓ depende de
SecondBrain.Infrastructure  → EF Core + Npgsql, DbContext, repositórios (implementa Application)
      ↓ depende de
SecondBrain.Application     → DTOs, interfaces, services (regra de negócio)
      ↓ depende de
SecondBrain.Domain          → Entidades puras, sem dependência de nada externo
```

**Por quê Clean Architecture pra um CRUD que começou simples:** o projeto existe pra virar um grafo de
conhecimento (relações N:N, depois busca semântica e integração com Obsidian/GitHub/LLMs) e também é usado
como estudo de C#/ASP.NET Core — vale pagar o custo de indireção agora, com poucas camadas, pra ter fronteiras
claras quando a regra de negócio parar de ser trivial.

**Nome único** (`Concept.Name`, `Tag.Name`) é garantido em duas camadas: o service checa duplicidade antes de
gravar (retorna 409 direto) e o banco tem um índice único como garantia final contra condição de corrida.

## Modelo de dados

```
Concept ──┬── ConceptNote ─────── Note       ("explicado em")
          ├── ConceptProject ──── Project    ("usado em")
          ├── ConceptTag ──────── Tag
          └── ConceptRelation ─── Concept    ("relacionado a" / "alternativa a" — grafo de conhecimento)
```

Cada relação é uma **tabela de junção tipada** (`concept_notes`, `concept_projects`, `concept_tags`,
`concept_relations`), não uma tabela `KnowledgeRelation` genérica/polimórfica — ver
[Decisões](#decisões-desta-versão). `ConceptRelation` é um auto-relacionamento (Concept↔Concept): guardado num
sentido só (`SourceConceptId`→`TargetConceptId`), mas mostrado nos dois lados — "Redis relacionado a Redis
Streams" aparece tanto na página de Redis quanto na de Redis Streams. `Project.Status` e
`ConceptRelation.Type` são enums (`Active`/`Paused`/`Completed`/`Archived` e `RelatedTo`/`AlternativeTo`),
serializados como texto no JSON.

## Stack

- **.NET 8** (LTS) / ASP.NET Core Web API
- **Entity Framework Core** + **Npgsql** (PostgreSQL) — inclusive full-text search nativo (`tsvector`/`tsquery`)
- **PostgreSQL 16**
- **Swagger / OpenAPI** (Swashbuckle)
- **xUnit** (testes unitários e de integração via `WebApplicationFactory`)
- **Docker Compose** (Postgres local)

Sem framework de validação externo (FluentValidation) nem mock library (Moq) — Data Annotations e repositórios
fake resolvem com poucas linhas o que o projeto precisa até aqui (ver [Decisões](#decisões-desta-versão)).

## Estrutura de pastas

```
second-brain/
├── src/
│   ├── SecondBrain.API/            # Controllers, Program.cs, middleware, appsettings
│   ├── SecondBrain.Application/    # DTOs, interfaces, services (regra de negócio)
│   ├── SecondBrain.Domain/         # Entidades (Concept, Note, Tag, Project, ConceptNote/Project/Tag)
│   └── SecondBrain.Infrastructure/ # DbContext, migrations, repositórios EF Core
├── tests/
│   ├── SecondBrain.UnitTests/        # Services contra repositórios fake em memória
│   └── SecondBrain.IntegrationTests/ # API real via WebApplicationFactory + EF InMemory
├── scripts/
│   └── sync-obsidian.ps1  # Importa/atualiza verbetes a partir do vault Obsidian
├── compose.yml            # Postgres local
├── .env.example
├── start.bat              # Sobe Postgres (Docker, com fallback nativo) + migrations + API + interface
├── stop.bat               # Encerra a API e derruba o Postgres do Docker
├── sync-obsidian.bat      # Atalho pro script de sincronização
└── SecondBrain.sln
```

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Docker + Docker Compose (ou um PostgreSQL local, se preferir não usar Docker)
- Ferramenta `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Como rodar

**Windows — atalho:** dois cliques em [`start.bat`](start.bat) sobe o Postgres (Docker; se não responder, tenta
o serviço nativo `postgresql-x64-16`), aplica as migrations pendentes e abre a API numa janela nova + a
interface no navegador. [`stop.bat`](stop.bat) encerra tudo. [`sync-obsidian.bat`](sync-obsidian.bat) importa
o vault (ver [seção acima](#sincronizar-com-o-obsidian)).

Ou manualmente:

```bash
# 1. Subir o Postgres
docker compose up -d postgres

# 2. Aplicar as migrations (cria o banco/tabelas)
dotnet ef database update --project src/SecondBrain.Infrastructure --startup-project src/SecondBrain.API

# 3. Rodar a API
dotnet run --project src/SecondBrain.API
```

A API sobe em `http://localhost:5080` (ou na porta do `launchSettings.json`) com Swagger em `/swagger`.

Em desenvolvimento, a connection string já vem configurada em `appsettings.Development.json` apontando pro
Postgres do `compose.yml` (usuário/senha `secondbrain`/`secondbrain_dev` — só vale para ambiente local). Em
outros ambientes, defina a variável `ConnectionStrings__DefaultConnection` (nunca commitar credencial real —
ver `.env.example`).

> **Nota Docker Desktop no Windows:** se `docker compose up` travar com erro de `sailor-ingest.sock`, é um bug
> conhecido do Docker Desktop (socket travado de uma sessão anterior). Reiniciar o Windows resolve; ou, via
> WSL, `wsl -d docker-desktop -- rm -f /mnt/c/Users/<você>/AppData/Local/Docker/run/sailor-ingest.sock*`.

## Uso da API

```bash
# Concepts
curl -X POST http://localhost:5080/api/concepts -H "Content-Type: application/json" \
  -d '{"name":"Redis","description":"Banco de dados em memória, usado para cache, filas e pub/sub."}'
curl http://localhost:5080/api/concepts                # lista
curl http://localhost:5080/api/concepts?tagId={id}     # lista só os relacionados a uma Tag (ex: uma linguagem)
curl http://localhost:5080/api/concepts/{id}           # "página do conceito": + notes/projects/tags relacionados
curl -X PUT http://localhost:5080/api/concepts/{id} -H "Content-Type: application/json" -d '{...}'
curl -X DELETE http://localhost:5080/api/concepts/{id}

# Notes (ConceptIds é opcional — já relaciona na criação)
curl -X POST http://localhost:5080/api/notes -H "Content-Type: application/json" \
  -d '{"title":"Redis Streams na prática","content":"...","conceptIds":["<concept-id>"]}'

# Projects (status: Active|Paused|Completed|Archived)
curl -X POST http://localhost:5080/api/projects -H "Content-Type: application/json" \
  -d '{"name":"Notification Engine","description":"...","status":"Active"}'

# Tags
curl -X POST http://localhost:5080/api/tags -H "Content-Type: application/json" -d '{"name":"database"}'

# Relacionar/desrelacionar depois de já criados
curl -X POST   http://localhost:5080/api/concepts/{conceptId}/notes/{noteId}
curl -X DELETE http://localhost:5080/api/concepts/{conceptId}/notes/{noteId}
curl -X POST   http://localhost:5080/api/concepts/{conceptId}/projects/{projectId}
curl -X POST   http://localhost:5080/api/concepts/{conceptId}/tags/{tagId}

# Grafo de conhecimento: Concept <-> Concept (type: RelatedTo|AlternativeTo)
curl -X POST   http://localhost:5080/api/concepts/{conceptId}/relations/{relatedConceptId} \
  -H "Content-Type: application/json" -d '{"type":"RelatedTo"}'
curl -X DELETE "http://localhost:5080/api/concepts/{conceptId}/relations/{relatedConceptId}?type=RelatedTo"
```

Respostas de erro seguem um formato consistente (`ExceptionHandlingMiddleware`):

```json
{ "status": 404, "title": "Concept '...' não encontrado.", "traceId": "..." }
```

| Situação | Status |
|---|---|
| Sucesso (GET/PUT) | 200 |
| Criado (POST) | 201 |
| Sem corpo (DELETE, link/unlink) | 204 |
| Validação de entrada | 400 |
| Não encontrado (Concept/Note/Project/Tag ou a relação) | 404 |
| Nome duplicado / relação já existe | 409 |

## Busca full-text

```bash
curl "http://localhost:5080/api/search?q=redis"
```

Procura em `Concept` (nome+descrição), `Note` (título+conteúdo) e `Project` (nome+descrição) usando o
full-text search nativo do Postgres (`to_tsvector`/`plainto_tsquery`), não `LIKE`. Config `simple` (sem
stemming) em vez de `portuguese`: o vocabulário é bilíngue (termos técnicos em inglês + texto em português), e
stemming de português aplicado a palavras em inglês dava resultado imprevisível.

```json
{ "query": "redis", "results": [{ "type": "concept", "id": "...", "title": "Redis" }, { "type": "note", "...": "..." }] }
```

## Testes

```bash
dotnet test
```

74 testes (unit + integração). **Unit**: cada service contra repositórios fake em memória (sem mock library,
sem banco). **Integração**: API real (`WebApplicationFactory<Program>`) com EF Core InMemory no lugar do
Postgres — cobre roteamento, serialização, DI e as relações N:N de ponta a ponta.

Limitações conhecidas do InMemory provider (por isso também validado manualmente contra Postgres real a cada
versão): não reproduz o índice único do Postgres (nome duplicado só é coberto no unit test) e não sabe traduzir
`EF.Functions.ToTsVector`/`PlainToTsQuery` — a busca full-text não tem teste de integração automatizado ainda,
só validação manual.

## Segurança

- Sem secrets no código: connection string de dev fica só em `appsettings.Development.json` (credenciais
  locais, sem uso fora da máquina de dev); produção usa variável de ambiente.
- Erros não tratados nunca vazam stack trace pro cliente (middleware global retorna mensagem genérica + loga
  o detalhe no servidor).
- Entrada validada via Data Annotations antes de chegar na regra de negócio.
- Ainda sem autenticação — API de uso pessoal/local por enquanto; entra no roadmap (V0.4).

## Decisões desta versão

- **Sem `KnowledgeRelation` genérico.** Uma tabela `Source/Target` polimórfica (Concept↔Note↔Project…) não tem
  integridade referencial real em EF Core/Postgres. Cada par de tipos tem sua própria tabela de junção
  (`concept_notes`, `concept_projects`, `concept_tags`, `concept_relations`) com FK de verdade.
- **`ConceptRelation` (Concept↔Concept) começa com só 2 tipos** (`RelatedTo`, `AlternativeTo`), não os 7 do
  plano de produto original — cresce o enum quando fizer falta de verdade, sem migrar nada existente. Guardado
  num sentido só, mas exibido nos dois lados (união de `RelationsAsSource`/`RelationsAsTarget` na consulta) —
  pra quem usa, "A relacionado a B" e "B relacionado a A" são a mesma relação. Cascade delete dos dois lados da
  FK — restrição de "múltiplos caminhos de cascade" é do SQL Server, não existe no Postgres/Npgsql.
- **Grafo mostrado como lista, não como visualização interativa.** Um mapa visual (nós/setas, force-directed)
  foi cogitado e descartado por agora: exigiria uma lib de layout de grafo ou D3 na mão, conflita com
  "minimalista, sem framework", e a lista de "Relacionados" na página do conceito já entrega a maior parte do
  valor por uma fração do custo. Revisita se a lista não bastar no uso real.
- **Busca full-text calculada em tempo de consulta, sem coluna gerada + índice GIN ainda.** Funciona correto
  hoje; dataset pessoal de baixo volume não justifica a complexidade extra até isso realmente doer
  (performance medida depois, não otimizada antes de existir problema).
- **Enums como texto no JSON** (`JsonStringEnumConverter`) em vez de número — número puro é opaco pra quem
  consome a API e frágil se a ordem do enum mudar.
- **Sem FluentValidation/Moq.** Data Annotations cobrem a validação e repositórios fake cobrem os testes —
  adicionar uma lib pra isso agora seria peso sem ganho real.
- **.NET 8 (LTS)**, não a versão mais nova instalada na máquina — prioriza maturidade de tooling/documentação
  pra um projeto que também é estudo de C#/ASP.NET Core.
- **Base de conhecimento de linguagens gerada com apoio de agentes em paralelo.** Extrair/escrever ~200
  verbetes de 6 linguagens era grande demais pra fazer em série — um agente por linguagem escreveu
  explicação+exemplo, com instrução explícita de nunca inventar um trecho de código: na rodada de código real
  (nunca fabricar, pular o conceito se não achasse exemplo real no repositório), e na rodada de currículo geral
  (exemplo genérico correto, mas nunca fingir que veio de um projeto do usuário — daí o Project
  "Referência Geral (Linguagem)" separado dos repositórios reais).
- **`setMainContent()` centraliza toda troca de conteúdo principal na UI e sempre reseta o scroll pro topo.**
  Bug real encontrado: sem isso, sair de uma lista rolada bem pra baixo pra um verbete mais curto deixava a
  tela em branco (o scroll antigo ficava além do conteúdo novo, mais curto).
- **`Concept.Level` é opcional e mapeado do português com acento pro nome do enum no script de import.** O
  JSON de conhecimento das linguagens guarda `"básico"/"intermediário"/"avançado"` (legível pra quem edita o
  arquivo); o enum C# usa `Basico/Intermediario/Avancado` (sem acento, serializado como texto). O script
  normaliza antes de mandar pra API — sem isso a deserialização do enum falharia silenciosamente.
- **Redis e PostgreSQL como duas Tags separadas, não uma única "Banco de Dados".** Cada tecnologia tem logo
  próprio (devicon) e uma progressão básico→avançado independente — misturar as duas sob um rótulo genérico
  perderia a possibilidade de filtrar só por uma delas, o mesmo raciocínio já usado para as 6 linguagens.
- **Logos das linguagens servidos localmente (`wwwroot/icons/`), não via CDN.** Os SVGs vinham do devicon pela
  jsdelivr, mas dois formatos de URL diferentes (mirror "gh" sem versão e pacote npm com versão fixa) se
  mostraram inconsistentes — funcionavam para uns ícones e falhavam silenciosamente para outros, de forma
  diferente em cada teste. Como são só 9 arquivos pequenos e estáticos, baixá-los uma vez pro próprio
  `wwwroot` elimina de vez a dependência de rede/cache de terceiros pra algo tão simples.
- **Campo `resposta` é conteúdo, não schema.** Diferente do `Concept.Level` (uma coluna nova, com migration),
  `resposta` vive só dentro do texto da `Note` gerada pelo script de import — não exigiu nenhuma mudança no
  banco, só no template da nota e nos dados de origem.
- **Conhecimento de um colaborador do GitHub, clonado temporariamente e apagado depois.** Os repositórios
  públicos usados como fonte real (personal-finance, daily-routine-app, front-end, financial-analysis) foram
  clonados só-leitura pra extrair o código, e removidos do disco depois de extraído — nada do repositório em
  si fica versionado aqui, só os trechos de código citados dentro dos verbetes. O Project de cada um deixa
  explícito que é código do colaborador, não do usuário.
- **Sync do Obsidian é sob demanda, não automático ainda.** Rodar em background (tarefa agendada do Windows)
  exigiria a API sempre no ar; hoje ela só sobe quando você chama `start.bat`. Preferi entregar o sync
  funcionando primeiro e decidir a automação depois, com o usuário confirmando explicitamente (criar uma
  tarefa agendada é uma mudança persistente no sistema, não algo pra fazer silenciosamente).

## Roadmap

- **V0.3 ✅** — Knowledge Graph: relação Concept↔Concept (`RelatedTo`/`AlternativeTo`), "Relacionados" na
  página do conceito, contadores na sidebar.
- **V0.3.1 ✅** — Nível de dificuldade (`Concept.Level`) ordenando toda listagem/filtro (nunca mistura básico
  com avançado) e reestruturação das notas de linguagem: analogia simples primeiro, depois nível + explicação
  técnica, repositório real só no final.
- **V0.3.2 ✅** — Campo `resposta` (o resultado esperado de rodar o exemplo) retroativo nos 224 verbetes
  existentes, mais duas categorias novas na sidebar — **Redis** e **PostgreSQL** — com 26 verbetes extraídos
  do código real do Notification Engine (Streams, consumer groups, índices, constraints, pool de conexões).
- **V0.3.3 ✅** — Conhecimento real extraído dos repositórios públicos de um colaborador do GitHub
  (GustavoMelo1): 17 verbetes novos (FastAPI, Pydantic, Strategy pattern com ABC, SQLite, validação de
  formulário em JS puro, HAVING/window functions em SQL) e uma categoria nova — **HTML/CSS** — com 7 verbetes.
  Os Projects desses repositórios são marcados explicitamente como "colaborador", não "usuário".
- **V0.4** — Users, login, JWT (adiado da V0.3 original — o grafo tinha prioridade maior: sem ele, o app ainda
  parecia "um Notion simplificado"; com ele, começa a parecer um mapa do que você sabe).
- **V0.5** — Timeline de aprendizado (já dá pra fazer sem schema novo — `CreatedAt` já existe em tudo).
- **Considerando, não decidido**: revisão espaçada (é um produto de estudo à parte, só vale investir se o uso
  real pedir isso); Experience/Decision do plano original; scanner de tecnologia por projeto (custo alto,
  valor incerto — melhor esperar uma necessidade concreta aparecer).
- **V1.0** — Automatizar o sync do Obsidian (tarefa agendada, hoje é sob demanda), sincronizar outras pastas do
  vault, integração com GitHub, embeddings/busca semântica, assistente via LLM ("explique usando o que eu já
  sei" — não um chatbot genérico).
