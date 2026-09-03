# Integrações externas da V2

## Contrato interno

O módulo `Modules/Integrations` separa `Domain`, `Application` e `Infrastructure`. Controllers/endpoints não conhecem o JSON de nenhum provedor. Os contratos internos são `ICompanyDataProvider`, `INcmDataProvider`, `ILegislationProvider` e `IExternalSnapshotCache`.

O CNPJ é sempre `string`. `CnpjIdentifier` preserva zeros à esquerda, aceita máscara, normaliza letras para maiúsculas e valida os dois dígitos pelo módulo 11 oficial (valor ASCII menos 48). Os doze primeiros caracteres podem ser alfanuméricos; os dois últimos são numéricos.

## Fontes e limitações

- Receita Federal — CNPJ alfanumérico e cálculo do DV: <https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/cnpj-alfanumerico>
- BrasilAPI — CNPJ e NCM: <https://github.com/BrasilAPI/BrasilAPI/tree/main/pages/docs/doc>
- Câmara dos Deputados — proposições: <https://dadosabertos.camara.leg.br/swagger/api.html>
- Senado Federal — matérias/processos legislativos: <https://legis.senado.leg.br/dadosabertos/api-docs/swagger-ui/index.html>

Em agosto de 2026, o contrato OpenAPI publicado do endpoint CNPJ da BrasilAPI ainda declara somente 14 dígitos. Porém, a validação real de `00.000.000/E08G-12` retornou Banco do Brasil com sucesso. A Central aceita o formato alfanumérico, envia o identificador sem perda e mantém tolerância explícita caso contrato e produção voltem a divergir.

O endpoint textual do Senado (`/materia/pesquisa/lista`) está marcado como depreciado pela própria API, mas ainda é o contrato oficial que oferece `palavraChave`. O adapter tolerante o mantém isolado e pode ser trocado sem alterar a busca interna. Resultados são classificados como `LegislativeProposal`; normas internas permanecem `LegalNorm`.

`CNPJ.ws` é fallback opcional e vem desabilitado. Ele só é chamado quando a fonte primária falha de modo transitório (timeout, conexão, `408`, `429` ou `5xx`), nunca para `404`, CNPJ inválido ou rejeição permanente.

## Resiliência e cache

- timeout: 10 segundos por `HttpClient` tipado;
- retry: uma repetição, exclusivamente em `GET` idempotente e falha transitória;
- circuit breaker: abre por 30 segundos após três chamadas consecutivas com falha;
- cache PostgreSQL: `ExternalEntitySnapshot`, JSONB, chave única por provedor/tipo/chave externa e expiração;
- empresas: TTL padrão de 24 horas; NCM: 7 dias; pesquisas NCM: 12 horas;
- cancelamento: o `CancellationToken` HTTP percorre endpoint, EF Core, retry e provedor;
- observabilidade: logs estruturados com provedor, caminho, tentativa e latência, sem credenciais nem payload pessoal.

## Variáveis

Use as chaves `Integrations__...` documentadas no `.env.example`. Nenhum token é obrigatório para os provedores públicos habilitados. Chaves futuras devem existir somente em secrets/variáveis de ambiente.

## Endpoints V2

- `GET /api/v2/companies/{cnpj}`
- `GET /api/v2/ncm/{code}` e `GET /api/v2/ncm?query=...`
- `GET /api/v2/search?q=...`
- `GET /api/v2/legislation/search?q=...`
- `GET /api/v2/updates?days=30`
- `GET /api/v2/admin/integrations` (policy `AdminOnly`)

Falhas usam `application/problem+json`: entrada `400`, ausente `404`, rejeição externa `502` e indisponibilidade transitória `503` com `Retry-After`.
