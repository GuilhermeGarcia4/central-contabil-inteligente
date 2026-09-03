# V6 — Finanças mais completas e acompanhamento pessoal

> **Status:** em implementação.
> Fases **V6.1, V6.2 e V6.3 implementadas** (backend + frontend + testes). V6.4 (relatórios anuais, CSV, OFX) parcialmente implementada (relatório anual pronto; importação CSV/OFX pendente).
> Este documento descreve o escopo, as prioridades e a divisão em fases da V6.

## Objetivo

Transformar o Controle Financeiro em uma ferramenta que permita ao usuário acompanhar melhor sua vida financeira **sem deixar o sistema complicado para iniciantes**. A V6 evolui o módulo da V4/V5 mantendo a linguagem simples (Entradas/Saídas), o isolamento de dados por usuário e a identidade visual atual.

## Princípios transversais

- **Segurança:** todo dado financeiro pertence ao usuário autenticado. O `UserId` nunca vem do frontend; é extraído do token. Auditoria de segurança dos endpoints financeiros antes do lançamento público.
- **LGPD:** exportar meus dados, excluir meus dados financeiros, excluir minha conta, histórico de consentimentos quando aplicável, minimização de dados. Não coletar dados bancários sensíveis desnecessários.
- **Simplicidade:** cada tela responde a uma pergunta útil. Usuário experiente pode ignorar explicações; o iniciante pode aprender.
- **Sem integração bancária real:** nada de Open Finance automático, PIX, movimentação real de dinheiro, pagamento de boletos ou integração bancária direta nesta versão.

## Blocos planejados

### 1. Cartões de crédito — "Meus cartões"

Cadastro manual de cartões:

- Nome do cartão
- Banco/instituição
- Limite
- Dia de fechamento
- Dia de vencimento
- Últimos 4 dígitos (opcional)
- Cor/identificação visual (opcional)

**Não armazenar:** número completo, CVV, senha ou dados sensíveis desnecessários.

### 2. Faturas

Controle de faturas por cartão:

- Fatura atual, valor atual
- Limite utilizado e limite disponível
- Data de fechamento e vencimento
- Visualizar compras da fatura, parcelas e faturas futuras

### 3. Compra no crédito

Evoluir a regra existente de parcelamento:

- Quando **Saída + Cartão de crédito**, o usuário escolhe qual cartão foi utilizado.
- Se parcelado, associar cada parcela à fatura correta.

Exemplo: Notebook 12x de R$ 300 no cartão Nubank → 1/12 em setembro, 2/12 em outubro, 3/12 em novembro, etc.

### 4. Contas financeiras — "Minhas contas"

Cadastro manual de contas (sem integração bancária):

- Conta corrente, conta digital, dinheiro, carteira, poupança
- Ex.: Nubank (saldo R$ X), Itaú (saldo R$ Y), Dinheiro (R$ Z)

### 5. Transferências entre contas

Movimentação interna Conta A → Conta B. **Não** deve contar como nova entrada + nova saída no resumo geral. É apenas movimentação entre contas próprias. Modelagem adequada necessária.

### 6. Reserva de emergência — "Minha reserva"

- Meta da reserva, valor atual, número de meses desejado
- Mostrar "R$ X de R$ Y" e progresso
- Reaproveitar a infraestrutura de metas existente, evitando duplicação

### 7. Dívidas — "Minhas dívidas"

Cadastro manual:

- Nome, valor original, valor atual, número de parcelas
- Juros (opcional), vencimento, instituição, observações
- Mostrar total devido, total pago e restante

**Não** criar aconselhamento financeiro automático arriscado.

### 8. Patrimônio

Visão simples:

- **Ativos:** dinheiro, contas, reservas, outros bens informados manualmente
- **Passivos:** dívidas
- **Resultado:** patrimônio líquido

Explicação simples: "É o que você possui menos o que ainda deve."

### 9. Calendário financeiro

Visual mensal simples mostrando:

- Contas recorrentes, parcelas, faturas
- Entradas previstas, saídas previstas, vencimentos

### 10. Contas a pagar

Lançamento futuro (ex.: Internet R$ 100, vence 10/09) com status **Pendente** → **Pago**. Somente ao marcar como pago (ou conforme regra definida) refletir no movimento financeiro. Documentar bem a diferença entre **previsto** e **realizado**.

### 11. Contas a receber

Mesma ideia (salário, freelance, reembolso) com status **Previsto** → **Recebido**.

### 12. Previsto vs realizado

Dashboard simples:

- Planejado: R$ X
- Realizado: R$ Y
- Diferença: R$ Z

### 13. Relatórios melhorados

- Relatório mensal e anual
- Comparação entre meses e entre categorias
- Evolução do saldo, das entradas e das saídas

### 14. Novos gráficos

Além dos gráficos de pizza existentes:

- Linha: "Quanto entrou e saiu ao longo dos meses" (Jan/Fev/Mar/Abr)
- Linha: "Evolução de quanto sobrou"

Cada gráfico deve responder a uma pergunta útil. Não exagerar em gráficos.

### 15. Exportação melhorada

Evoluir o Excel:

- Mês, trimestre, ano, intervalo personalizado
- Planilha anual: Resumo, Entradas, Saídas, Categorias, Planejamento, Metas
- PDF como recurso futuro

### 16. Importação CSV

Upload manual com pré-visualização e confirmação:

- Data, descrição, valor, entrada/saída, categoria
- Categorização automática pode sugerir categorias
- **Nunca** importar silenciosamente sem confirmação

### 17. Importação OFX

Suporte a OFX (sem Open Finance). O usuário exporta o OFX do próprio banco e importa manualmente. Reduz complexidade regulatória e de integração.

### 18. Categorização inteligente melhorada

Evoluir o classificador local considerando:

- Descrição, estabelecimento, histórico
- Categoria usada anteriormente, forma de pagamento, preferências do usuário

Ex.: "Petz Rio Claro" → Pets. Se o usuário sempre usa "Mercado X" → Alimentação, priorizar o histórico pessoal.

### 19. Busca financeira natural

Pesquisa simples ("mercado", "ração", "faculdade", "compras de agosto"). Não precisa ser IA; pode usar filtros + pesquisa textual.

### 20. Central de aprendizado financeiro

Integrar o controle financeiro ao conteúdo educacional da Central:

- "Reserva de emergência" → "O que é reserva de emergência?" + [Aprender]
- "Cartão de crédito" → "Entenda como funciona a fatura"
- "Juros" → "Entenda juros do cartão"

### 21. Contexto educativo

Não transformar toda tela em aula. Usar "Saiba mais", "Entenda", "Como funciona?". O experiente ignora; o iniciante aprende.

### 22. Dashboard personalizável

Opção futura para o usuário escolher quais cards visualizar (saldo, metas, orçamento, cartões, faturas, reserva, dívidas). Sem drag-and-drop inicialmente, se não for necessário.

### 23. Privacidade e LGPD

- Exportar meus dados
- Excluir meus dados financeiros
- Excluir minha conta
- Histórico de consentimentos quando aplicável
- Minimização de dados
- Auditoria de segurança dos endpoints financeiros antes do lançamento público

### 24. Backup e produção

Preparação para produção:

- PostgreSQL gerenciado, backups, teste de restauração
- SSL, secrets, rate limiting, logs, monitoramento
- Ambientes: Development, Staging, Production

## Fora do escopo da V6

- Open Finance automático
- PIX
- Movimentação real de dinheiro
- Pagamento de boletos
- Integração bancária direta
- Investimentos em bolsa em tempo real
- Criptomoedas
- Recomendação de investimento por IA
- Microserviços
- Kubernetes

## Prioridades

| Prioridade | Itens |
|---|---|
| **P0 — Crítico** | Cartões, Faturas, Contas financeiras, Previsto vs realizado, Segurança |
| **P1 — Alta** | Calendário, Dívidas, Reserva, Relatórios anuais, Gráficos históricos |
| **P2 — Média** | CSV, OFX, Patrimônio, Busca avançada |
| **P3 — Futuro** | Dashboard personalizável, IA financeira, Open Finance |

## Implementação sugerida (fases)

| Fase | Escopo | Status |
|---|---|---|
| **V6.1** | Cartões, Faturas, Contas | ✅ Implementado |
| **V6.2** | Contas a pagar/receber, Calendário, Previsto vs realizado | ✅ Implementado |
| **V6.3** | Dívidas, Reserva, Patrimônio | ✅ Implementado |
| **V6.4** | Relatórios anuais, CSV, OFX | 🔶 Parcial (relatório anual pronto; CSV/OFX pendente) |

Assim evitamos fazer tudo de uma vez.

## O que foi implementado (V6.1–V6.3)

- **Cartões de crédito** (`/minha-conta/financas/cartoes`): CRUD de cartões (nome, banco, limite, dia de fechamento/vencimento, final, cor) e **faturas** calculadas por período de fechamento (valor, limite usado/disponível, vencimento).
- **Compra no crédito associada ao cartão**: no formulário de lançamento, ao escolher **Saída + Cartão de crédito**, aparece o seletor de cartão; o `creditCardId` é salvo na transação (inclusive nas parcelas). O backend valida a propriedade do cartão.
- **Contas financeiras** (`/minha-conta/financas/contas`): CRUD de contas (corrente, digital, dinheiro, carteira, poupança) com saldo, e **transferência entre contas** (não vira entrada/saída no resumo).
- **Contas a pagar/receber** (`/minha-conta/financas/contas-a-pagar`): lançamentos previstos com status Pendente → Pago/Recebido, e painel **Previsto vs Realizado** (entradas, saídas e sobra).
- **Calendário financeiro** (`/minha-conta/financas/calendario`): eventos por dia (entradas, saídas, previstos e faturas).
- **Dívidas** (`/minha-conta/financas/dividas`): CRUD de dívidas com total devido, original e já pago.
- **Reserva de emergência** (`/minha-conta/financas/reserva`): meta, valor atual, progresso e meses desejados.
- **Patrimônio líquido** (`/minha-conta/financas/reserva`): ativos (contas + reserva) − passivos (dívidas).
- **Relatório anual** (`/minha-conta/financas/relatorio-anual`): entradas/saídas/sobra do ano, evolução mês a mês (gráfico de barras) e maiores categorias de saída.

### Segurança
- Todos os endpoints V6 exigem autenticação e derivam o `UserId` do token (nunca do frontend).
- Validação de propriedade em cartões, contas, dívidas, reserva e contas a pagar.
- Testes de integração cobrem o isolamento entre usuários (usuário B não acessa/edita/exclui dados do usuário A).

### Pendente (V6.4)
- Importação CSV e OFX com confirmação manual.
- Exportação melhorada (trimestre/ano/intervalo) além do relatório anual já entregue.

## Evolução documentada (fora desta tarefa)

- **Resumo no card de acesso rápido da Home:** para usuário autenticado, o card "Minhas Finanças" poderá futuramente exibir Entradas do mês, Saídas do mês e Quanto sobrou, reutilizando o endpoint `GET /api/v1/finance/summary` já existente. Nesta correção da Home, o card foi deixado visualmente correto e sem resumo para não adicionar complexidade; o resumo fica como evolução da V6.
