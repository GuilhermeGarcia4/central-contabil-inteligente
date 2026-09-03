# Controle Financeiro Pessoal — V4

## Escopo

O módulo permite que usuários autenticados mantenham receitas, despesas, categorias próprias e recorrências mensais. As telas ficam em `/minha-conta/financas` e `/minha-conta/financas/lancamentos`; a API fica em `/api/v1/finance`.

Não há integração bancária, pagamento, OFX, Open Finance ou envio de descrições a serviços externos.

## Domínio e persistência

- `FinancialTransaction`: lançamento com `Amount` positivo em `numeric(18,2)`; `Type` define receita ou despesa.
- `FinancialCategory`: categoria global (`UserId = null`) ou pessoal. Categorias globais não podem ser alteradas pelo usuário.
- `FinancialRecurrence`: modelo extensível de recorrência; na V4 somente `Monthly` é aceito.
- `FinancialCategoryPreference`: associação de texto normalizado e categoria, isolada por usuário.

A migration `AddPersonalFinanceModule` cria as tabelas, constraints de valor positivo, categorias iniciais e índices em usuário, data, categoria, recorrência e preferências.

## Segurança

O `UserId` nunca faz parte dos DTOs de escrita. Todos os endpoints extraem o identificador de `ClaimTypes.NameIdentifier`; consultas, alterações e exclusões filtram pelo usuário autenticado. IDs pertencentes a outra conta retornam `404`, evitando IDOR/BOLA. Categorias pessoais e preferências seguem o mesmo isolamento.

## Resumo e cálculos

O backend executa `SUM`, `COUNT` e `GROUP BY` no banco para o mês solicitado. O saldo é `receitas - despesas`; comprometimento é `despesas / receitas * 100` somente quando há receita. Comparações retornam `null` quando o mês anterior não oferece base, evitando divisão por zero.

## Recorrência

Ao marcar um lançamento como recorrente, a primeira ocorrência é salva e a regra recebe a próxima data mensal. Ao consultar um período, ocorrências vencidas são materializadas de forma controlada. O índice único `(RecurrenceId, TransactionDate)` impede duplicidade. Scheduler e frequências semanal/anual/custom ficam para evolução futura.

## Gráficos

O Angular usa `DonutChartComponent`, um componente próprio baseado em `conic-gradient`, sem dependência externa. Cada gráfico possui legenda textual com categoria, valor e percentual; portanto a cor não é a única forma de transmitir dados. O layout se adapta a telas pequenas e mostra empty state quando não há dados.

## Categorização automática

`ICategorySuggestionService` isola o contrato. A implementação V4, `RuleBasedCategorySuggestionService`, roda somente no backend local e segue esta prioridade:

1. preferência exata do próprio usuário;
2. expressões e palavras-chave locais;
3. até três sugestões ordenadas por confiança;
4. nenhuma sugestão quando não há evidência suficiente.

O texto é convertido para minúsculas, tem acentos e pontuação removidos e espaços normalizados. Termos são comparados por palavras/expressões, evitando substrings acidentais. Confiança de pelo menos `0,75` permite preencher o campo no formulário, mas o usuário sempre pode trocar a categoria antes de salvar.

O Angular aguarda 400 ms após a digitação e cancela requisições anteriores com `switchMap`. Ao salvar, a escolha confirmada é lembrada apenas para aquela conta. Uma correção nunca altera regras globais.

Uma evolução híbrida poderá consultar um classificador externo apenas quando as regras locais forem insuficientes, após análise separada de LGPD, consentimento, minimização e fornecedor.

## API

- `GET /summary` e `/dashboard`
- `GET|POST /transactions`, `GET|PUT|DELETE /transactions/{id}`
- `GET|POST /categories`, `PUT|DELETE /categories/{id}`
- `GET|POST /recurrences`, `PUT|DELETE /recurrences/{id}`
- `POST /category-suggestion`

Todos exigem JWT.
# V5 — planejamento financeiro pessoal

Além do histórico e dashboard da V4, a V5 inclui orçamento mensal por categoria, metas com aportes, compromissos parcelados, gestão de recorrências, previsão, relatório mensal e exportação XLSX.

## Parcelamento

O parcelamento está disponível somente para uma **Saída** paga com **Cartão de crédito**. O formulário solicita o valor total, pelo menos duas parcelas e a data da primeira parcela. A API cria um plano e um lançamento por mês. A divisão é feita em centavos: eventuais centavos restantes são distribuídos nas primeiras parcelas, preservando exatamente o total informado.

Excluir ou editar um lançamento altera apenas aquela parcela. A API não apaga todo o plano de forma implícita.

## Exportação Excel

`GET /api/v1/finance/export/excel` aceita mês/ano ou intervalo de datas, tipo, categoria, forma de pagamento, texto e valores mínimo/máximo. O arquivo possui as abas `Resumo`, `Entradas`, `Saídas` e `Todos os lançamentos`, com datas e valores em tipos nativos do Excel.

Biblioteca: `ClosedXML` 0.105.1, selecionada por oferecer geração XLSX sem depender de Microsoft Office instalado.

## Migração

Aplicar sem recriar o banco:

```powershell
dotnet ef database update --project backend/src/CentralContabil.Api --startup-project backend/src/CentralContabil.Api
```

Migração V5: `AddFinancialPlanningV5`. Ela adiciona tabelas e colunas, preservando os dados da V4.
