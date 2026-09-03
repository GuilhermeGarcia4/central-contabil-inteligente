# V7 — Inteligência Financeira e Assistente

## Objetivo

Dar ao usuário inteligência sobre as próprias finanças e um assistente que responde perguntas financeiras com base nos dados reais da conta, além de importação de dados e alertas.

## Escopo

### V7.1 — Assistente inteligente (Q&A financeiro)

- `FinancialAssistantTools` (backend): ferramenta que responde perguntas financeiras usando os dados reais do usuário (entradas, saídas, saldo, categorias).
- `AssistantService.AskAsync` estendido com `conversationId?`, `FinancialAssistantTools` e `AssistantConversationService`.
- `AssistantIntentDetector.Detect` prioriza termos financeiros antes do cálculo; `FinancialTerms` ampliado (`entrou|entrada|gastei|saida|...`).
- `GroundedTemplateChatCompletionService.CompleteAsync` agora trata `FinancialToolResult` e devolve o `Summary` como resposta.
- Histórico de conversas: `AssistantConversation`/`AssistantMessage` + endpoints CRUD.

### V7.2 — Inteligência financeira

- `FinanceInsightService`: "Entenda meu mês", resumo semanal e mensal, geração de insights (orçamento, parcelas futuras).
- Endpoints: `/understand-month`, `/summary-weekly`, `/summary-monthly`, `/insights`.

### V7.3 — Importação CSV/OFX

- `FinancialImportService`: parse de CSV com detecção automática de delimitador (`;`/`,`), sugestão de categoria, detecção de duplicados.
- Endpoints: `/imports/preview` (multipart) e `/imports/confirm`.
- Frontend: página `/minha-conta/financas/importar` com prévia, seleção de linhas, edição de categoria e confirmação.

### V7.4 — Alertas e resumos

- `FinancialAlertService`: alertas de orçamento, metas, parcelas, contas a pagar e faturas.
- `UserPreferenceService`: perfil de explicação (`Simple`/`Detailed`/`Both`) e toggles de alertas.
- Frontend: Central de alertas no dashboard e página `/minha-conta/financas/preferencias`.

### V7.5 — Documentação

- `docs/ROADMAP.md` atualizado com a seção V7.

## Frontend

- `FinanceService` ampliado com métodos V7 (inteligência, alertas, preferências, importação, conversas).
- Componentes novos: `finance-insights`, `finance-alerts`, `finance-preferences`, `finance-import`.
- `assistant.component` com histórico de conversas (nova conversa, abrir, excluir, limpar).
- Rotas novas: `/minha-conta/financas/importar` e `/minha-conta/financas/preferencias`.
- Links novos na navegação financeira.

## Testes

- Backend: 4 testes de integração V7 em `FinanceV7ApiTests` (todos passando).
- Frontend: testes do `FinanceService` ampliados para os métodos V7.

## Contexto preservado

- Stack: .NET 10, Angular 22, PostgreSQL, pgvector.
- Dados financeiros isolados por `UserId` do JWT (nunca vindos do frontend).
- Sem git; checkpoint em `.checkpoints/`.
- Migração `AddV7Intelligence` aplicada ao banco.
