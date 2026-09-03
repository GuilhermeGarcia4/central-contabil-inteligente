# Arquitetura

## Monólito modular evolutivo

A Central é um deploy de API, um frontend e um PostgreSQL. V2/V3 ampliam o monólito modular sem microsserviços, brokers ou consistência distribuída prematuros.

```text
Angular → REST /api/v1 e /api/v2 → módulos ASP.NET Core → EF Core → PostgreSQL
                                      ↓
                         provedores públicos via HttpClient
```

V1 permanece em `Identity`, `Content`, `Calculators`, `Rules`, `Users`, `Admin` e `Shared`. V2 adiciona `Integrations` e expande `Search`. O módulo de integrações possui limites explícitos:

```text
Domain          CnpjIdentifier e modelos canônicos
Application     interfaces, orquestração, cache e fallback
Infrastructure  DTOs externos, mapeadores e clientes tipados
Endpoints       composição HTTP /api/v2
```

Assim, JSON e peculiaridades de BrasilAPI, CNPJ.ws, Câmara e Senado não vazam para endpoints nem para o Angular.

## Fluxo V2

1. O endpoint recebe DTO/string e propaga `CancellationToken`.
2. O domínio valida e normaliza sem perda; CNPJ nunca é numérico.
3. A aplicação consulta snapshot válido antes da rede.
4. O provider executa GET com timeout, retry transitório e circuito.
5. O mapper converte o payload externo para o contrato interno.
6. O snapshot JSONB é atualizado com origem, horário e expiração.
7. A resposta identifica provider e `fromCache`; falhas usam `ProblemDetails`.

O fallback CNPJ.ws somente participa quando BrasilAPI falha de forma transitória. Ausência, entrada inválida e rejeição permanente não disparam uma segunda fonte.

## Busca universal

`SearchIntentDetector` classifica `Cnpj`, `Ncm`, `LegalReference` ou `General`. A busca sempre consulta o conteúdo interno, mas somente aciona a integração coerente com a intenção. O Angular acrescenta debounce de 350 ms e abas; assim, digitar texto comum não produz chamadas para todos os provedores.

Normas vigentes e proposições são conceitos distintos: `LegalNorm` representa referência normativa; `LegislativeProposal` representa matéria ainda sujeita a tramitação. Câmara e Senado preservam casa, status, fonte e URL oficial.

## Banco

PostgreSQL mantém Identity, conteúdo, fontes, referências legais, regras, cálculos, favoritos, consentimentos, auditoria e cache externo. `ExternalEntitySnapshot` tem índice único `(Provider, EntityType, ExternalKey)`, payload `jsonb`, `RetrievedAt` e `ExpiresAt`. `ExternalProvider` prepara persistência de catálogo e governança futura.

Regras financeiras continuam versionadas por `CalculationRuleSet`; valores monetários usam `decimal`/`numeric(18,2)` e nenhum conteúdo do banco é executado como código.

## Segurança e observabilidade

- Identity, lockout, JWT curto e refresh token opaco com hash;
- policy `AdminOnly`, CORS restritivo, rate limiting e headers defensivos;
- `.env` apenas em desenvolvimento e secrets apenas em ambiente/secret store;
- timeout de 10 s, uma repetição GET transitória e circuito de 30 s após três falhas;
- logs estruturados por provider, caminho, tentativa e latência, sem payloads ou credenciais;
- painel `/admin/integracoes` exibe estado do circuito e cache, nunca secrets.

## Frontend

Angular usa componentes standalone, lazy loading, signals, HttpClient e guards. As rotas V1 continuam válidas; `/pesquisa` redireciona para `/buscar`. As novas telas são responsivas e mantêm origem, vigência e estado de cache visíveis.

SSR, pagamentos, scraping, OCR e automações amplas permanecem fora desta fase. A arquitetura detalhada de Knowledge, pgvector e RAG está em `docs/AI_ARCHITECTURE.md`.

## V4 — Controle Financeiro Pessoal

O módulo `Modules/Finance` segue a divisão `Domain`, `Application` e `Endpoints`. Transações, categorias, recorrências e preferências usam o mesmo `AppDbContext`, sem criar outro deploy ou banco. O usuário é sempre obtido do JWT; DTOs não aceitam `UserId` e queries pessoais incluem esse filtro.

Resumos mensais são agregados no PostgreSQL. A categorização roda localmente através de `ICategorySuggestionService`, sem compartilhar descrições com a infraestrutura de IA da V3. No Angular, as rotas financeiras são lazy-loaded e o gráfico donut fica encapsulado em componente próprio. Detalhes estão em [PERSONAL_FINANCE.md](PERSONAL_FINANCE.md).
