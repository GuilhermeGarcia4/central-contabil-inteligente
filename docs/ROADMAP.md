# Roadmap

## V1 — implementado

- Monólito modular, PostgreSQL, Identity/JWT, artigos, fontes, regras versionadas, calculadoras, favoritos, histórico, auditoria e administração.

## V2 — implementado

- Consulta CNPJ/NCM, integrações legislativas resilientes, cache JSONB, busca universal, painel de integrações e login Google.

## V3 — implementado

- pgvector, Knowledge, busca híbrida, assistente fundamentado, tools determinísticas, telemetria e painéis administrativos.

## V4 — Controle Financeiro Pessoal — implementado

- Dashboard mensal, receitas, despesas, saldo e comprometimento da renda.
- Categorias globais e pessoais, histórico, filtros e gráficos acessíveis.
- Recorrências mensais controladas.
- Isolamento obrigatório de dados entre usuários.
- Categorização automática baseada em regras e preferências do próprio usuário.

## V5 — Planejamento Financeiro — implementado

- Orçamentos e limites mensais por categoria.
- Metas, cartões, parcelamentos e dívidas.
- Reserva de emergência e relatórios/exportações.
- Reconhecimento local de estabelecimentos.
- Classificação híbrida opcional com IA, condicionada a avaliação de LGPD e usada apenas com baixa confiança das regras locais.

## V6 — Finanças mais completas e acompanhamento pessoal — em implementação

Objetivo: transformar o Controle Financeiro em uma ferramenta que permita ao usuário acompanhar melhor sua vida financeira sem deixar o sistema complicado para iniciantes.

- Cartões de crédito, faturas e compra no crédito associada ao cartão. ✅
- Contas financeiras e transferências internas entre contas. ✅
- Reserva de emergência, dívidas e patrimônio líquido. ✅
- Calendário financeiro, contas a pagar/receber e previsto vs realizado. ✅
- Relatórios anuais, gráficos históricos e exportação melhorada (mês/trimestre/ano/intervalo). 🔶 (anual pronto; exportação ampliada pendente)
- Importação CSV e OFX com confirmação manual. ⬜
- Categorização inteligente melhorada e busca financeira natural. ⬜
- Central de aprendizado financeiro e contexto educativo. ⬜
- Privacidade/LGPD (exportar e excluir dados) e preparação para produção. ⬜

Detalhamento completo em `docs/V6_PLAN.md`.

## V7 — Inteligência Financeira e Assistente — implementado

Objetivo: dar ao usuário inteligência sobre as próprias finanças e um assistente que responde perguntas financeiras com base nos dados reais da conta.

- **V7.1 — Assistente inteligente (Q&A financeiro):** o assistente agora responde perguntas sobre as finanças do usuário (ex.: "quanto entrou este mês?", "para onde foi meu dinheiro?") usando a infraestrutura V3 + `FinancialAssistantTools`. O detector de intenção prioriza termos financeiros antes do cálculo. ✅
- **V7.2 — Inteligência financeira:** "Entenda meu mês" (entradas, saídas, saldo, taxa de poupança, maior saída e insights), resumo semanal e mensal, e geração de insights (orçamento, parcelas futuras). ✅
- **V7.3 — Importação CSV/OFX com prévia e confirmação:** upload, detecção automática de delimitador (`;`/`,`), sugestão de categoria, detecção de duplicados e confirmação manual antes de gravar. ✅
- **V7.4 — Alertas e resumos:** Central de alertas (orçamento, metas, parcelas, contas a pagar, faturas) e preferências de perfil de explicação e de alertas. ✅
- **V7.5 — Documentação:** este roadmap e a documentação de apoio. ✅

Detalhamento completo em `docs/V7_PLAN.md`.

## V8 — Visão financeira e personalização — implementado

- Overview agregado por intervalo, Entradas, Saídas e Quanto sobrou. ✅
- Donuts, principais categorias e paleta acessível. ✅
- Comparação com período anterior e evolução mensal. ✅
- Cores isoladas por usuário e restauração automática. ✅
- Planejamento, compromissos e metas no overview. ✅
- Seleção e ordenação das seções: evolução posterior.

## Pendências operacionais contínuas

- Curadoria e revisão profissional das regras trabalhistas demonstrativas.
- Ampliação do corpus oficial da base de conhecimento.
- Avaliações regulatórias versionadas e alertas de vigência.
- Evolução de privacidade para histórico opcional de conversas.
