# Central Contábil Inteligente

Plataforma brasileira de conteúdo, consultas públicas, calculadoras auditáveis e assistência fundamentada. A V3 preserva V1/V2 e adiciona pgvector, base de conhecimento, busca híbrida e RAG com tools determinísticas.

## Arquitetura

- `frontend/`: Angular 22 standalone, SCSS, Router, HttpClient e busca com debounce.
- `backend/`: ASP.NET Core 10, API REST `/api/v1` + `/api/v2`, Identity, JWT curto e refresh token `HttpOnly`.
- PostgreSQL 17 + pgvector + EF Core para domínio, conhecimento, auditoria e snapshots de cache em JSONB.
- monólito modular; integrações externas divididas em `Domain`, `Application` e `Infrastructure`.

Veja [arquitetura](docs/ARCHITECTURE.md), [integrações V2](docs/V2-INTEGRATIONS.md) e [roadmap](docs/ROADMAP.md).
Veja também [arquitetura de IA e Knowledge](docs/AI_ARCHITECTURE.md).
Veja o [módulo de Controle Financeiro Pessoal](docs/PERSONAL_FINANCE.md) da V4.

## Pré-requisitos e configuração

- .NET SDK 10
- Node.js 22.22.3+ e npm
- Docker Desktop com Compose

Copie `.env.example` para `.env`, substitua todas as credenciais e use uma `JWT_KEY` aleatória com ao menos 32 caracteres. O `.env` é ignorado pelo Git e carregado somente em `Development`; variáveis do processo têm precedência. Nunca coloque senhas ou tokens em `appsettings.json`.

As integrações públicas usam as variáveis `Integrations__...`. BrasilAPI, Câmara e Senado vêm habilitados; CNPJ.ws é fallback opcional e vem desabilitado. Nenhuma chave externa é obrigatória nesta versão.

## Executar

```powershell
docker compose up -d --wait postgres
dotnet restore CentralContabil.slnx
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/src/CentralContabil.Api --startup-project backend/src/CentralContabil.Api
dotnet run --project backend/src/CentralContabil.Api
```

Em outro terminal:

```powershell
cd frontend
npm install
npm start
```

O PostgreSQL usa volume persistente, healthcheck e `restart: unless-stopped`. A API espera o banco ficar disponivel durante a inicializacao e repete operacoes transitorias, portanto se recupera de reinicios breves do container. Use `GET /health` para validar banco + API e `GET /health/live` para validar apenas o processo da API. O Docker Desktop ainda precisa estar em execucao; habilite a inicializacao automatica dele com o Windows para o banco subir apos reiniciar a maquina.

O acesso de desenvolvimento fica unificado em `http://localhost:4200`. O Angular encaminha `/api` e `/health` internamente para `http://localhost:5042`. O callback OAuth pertence à API e usa `http://localhost:5042/signin-google`. Swagger técnico permanece local em `http://localhost:5042/swagger`.

## Login com Google

Crie um cliente OAuth do tipo **Aplicativo da Web** no Google Cloud e registre exatamente:

- URI de redirecionamento autorizada: `http://localhost:5042/signin-google`

Não é necessário cadastrar uma origem JavaScript autorizada: o frontend não usa o SDK Google Identity Services. Para produção no Render e Netlify, consulte [DEPLOYMENT.md](docs/DEPLOYMENT.md).

Guarde as credenciais fora do repositório:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "SEU_CLIENT_ID" --project backend/src/CentralContabil.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU_CLIENT_SECRET" --project backend/src/CentralContabil.Api
```

Reinicie a API depois de configurar. O frontend habilita o botão automaticamente por meio de `GET /api/v1/auth/google/status`. O retorno OAuth cria ou associa a conta pelo e-mail verificado, grava o refresh token em cookie `HttpOnly` e nunca coloca o JWT na URL.

## Rotas da interface V2

- `/buscar`: busca universal com intenção e abas;
- `/empresas` e `/empresas/:cnpj`: CNPJ numérico ou alfanumérico;
- `/ncm`: busca por código ou descrição;
- `/novidades`: conteúdo e atividade recente das integrações;
- `/admin/integracoes`: circuitos, falhas, latência e cache (somente Admin).

`/pesquisa` continua válido e redireciona para `/buscar`.

## Endpoints principais

V1 preservada:

- `POST /api/v1/auth/register|login|refresh|logout`
- `GET /api/v1/auth/google/status|start|complete`
- `GET /api/v1/articles|categories|search|calculators`
- `POST /api/v1/calculations/compound-interest|vacation|thirteenth-salary`
- `GET /api/v1/account/*` e `/api/v1/admin/*`

V2:

- `GET /api/v2/companies/{cnpj}`
- `GET /api/v2/ncm/{code}` e `/api/v2/ncm?query=...`
- `GET /api/v2/search?q=...`
- `GET /api/v2/legislation/search?q=...`
- `GET /api/v2/updates`
- `GET /api/v2/admin/integrations`

V3:

- `GET /api/v1/search/intelligent?q=...`
- `GET /api/v1/search/suggestions?q=...`
- `POST /api/v1/assistant/ask`
- `GET|POST|PATCH /api/v1/admin/knowledge/*` (Admin)
- `GET /api/v1/admin/ai` e `/api/v1/admin/ai/unanswered` (Admin)

V4 — Controle Financeiro Pessoal:

- interface em `/minha-conta/financas` e histórico em `/minha-conta/financas/lancamentos`;
- `GET /api/v1/finance/summary|dashboard|transactions|categories|recurrences`;
- CRUD autenticado de lançamentos, categorias pessoais e recorrências mensais;
- `POST /api/v1/finance/category-suggestion` com regras locais e preferências privadas do usuário.

Configuração opcional de provider externo: `AI_PROVIDER=gemini` usa `GEMINI_API_KEY`, `GEMINI_MODEL` e `GEMINI_EMBEDDING_MODEL`; `AI_PROVIDER=openai` usa `OPENAI_API_KEY`, `OPENAI_MODEL` e `OPENAI_EMBEDDING_MODEL`. As configurações legadas `AI__*` da OpenAI continuam aceitas. Sem chave, com provider inválido, timeout ou erro remoto, o fallback local fundamentado mantém busca e assistente operacionais. As chaves existem somente no backend e nunca devem ser colocadas no Angular.

Em desenvolvimento, o Gemini é o provider temporário sugerido. Use dados fictícios no free tier. Copie `.env.example` para `.env`, preencha apenas a chave local e reinicie o backend. O modelo de chat padrão é `gemini-3.7-flash`, o embedding é `gemini-embedding-2` e a saída é reduzida para 384 dimensões para permanecer compatível com o `vector(384)` existente.

## Testes e build

```powershell
dotnet restore CentralContabil.slnx
dotnet build CentralContabil.slnx
dotnet test backend/tests/CentralContabil.UnitTests
$env:TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=central_contabil;Username=central_contabil;Password=SUA_SENHA'
dotnet test backend/tests/CentralContabil.IntegrationTests
cd frontend
npm test -- --watch=false
npm run build
```

Os testes de integração são ignorados sem `TEST_CONNECTION_STRING`. Os testes externos usam provedores mockados e não dependem da disponibilidade da internet.

## Decisões e segurança

- CNPJ é `string` em todo o fluxo, com zeros e letras preservados e DV oficial validado.
- Chamadas externas têm timeout, retry restrito a GET transitório, circuito, cache e cancelamento.
- A busca usa detector de intenção para não chamar todos os provedores a cada tecla.
- Erros V2 usam `ProblemDetails`; CORS, rate limiting, policies e headers de segurança da V1 permanecem ativos.
- Logs registram provedor, tentativa e latência sem secrets ou payloads sensíveis.
- O administrador inicial só é criado quando `ADMIN_EMAIL` e `ADMIN_INITIAL_PASSWORD` estão no ambiente; remova a senha inicial após o primeiro acesso.
- Não há IA, scraping, pagamentos, OCR ou automações de grande porte nesta fase.

## Limitações conhecidas

- O OpenAPI publicado da BrasilAPI ainda descreve somente CNPJ numérico, mas a API em produção já respondeu corretamente ao primeiro CNPJ alfanumérico oficial. O adapter preserva letras e isola essa divergência de contrato.
- A busca textual oficial do Senado usada pelo adapter está depreciada e isolada para substituição futura.
- Férias e 13º continuam estimativas brutas com validação jurídica pendente, como na V1.
- SSR continua fora desta fase.
