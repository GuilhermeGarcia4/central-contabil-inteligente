# Plano de metas — educação financeira e simuladores

**Revisado:** 24/11/2026. Continuação do [`plano meta.md`](../plano%20meta.md). A implementação foi executada e validada nesta rodada. Recursos de mercado são referências estatísticas; nenhum resultado deve ser apresentado como oferta garantida ou recomendação individual.

## Etapa 1 — Comparador de propostas: concluída

- O comparador consulta as médias mensais do BCB por instituição/modalidade, cacheadas por seis horas. Filtra por modalidade e nome de instituição; exibe instituição, mês, taxas mensais/anuais e fonte. A opção “Usar como referência” preenche a proposta e calcula parcela Price quando principal e prazo estão disponíveis.
- CET informado e estimativa matemática aparecem separados. A estimativa anualiza o fluxo com custos explicitamente modelados e não se apresenta como CET contratual.
- Exibe custo de parcelas, excedente estimado sobre o principal líquido, entrada/custos não incluídos, total, CET e prestação. Avisa quando modalidade, principal ou prazo diferem e permite criar cópias com principal igual sem substituir propostas originais.
- Duplicação, remoção, limpeza e modo manual continuam disponíveis quando a consulta falha.
- Fonte: [BCB — taxas de juros por instituição financeira](https://dadosabertos.bcb.gov.br/dataset/taxas-de-juros-de-operacoes-de-credito). Taxas médias não são ofertas; custo individual depende de perfil e contrato.

## Etapa 2 — Calculadoras do Cidadão: concluída

- Parcelas fixas/Price resolvem parcela (cálculo direto), principal, taxa mensal e prazo. Taxa e prazo inversos validam os limites e não aceitam soluções inexistentes nas condições implementadas.
- Aportes regulares calculam valor futuro e resolvem aporte, prazo ou taxa, com depósito no início ou fim do período, resumo de capital/rendimento e cronograma.
- Correção monetária consulta IPCA, INPC, IGP-M, TR mensal (série 7811 com início no dia 1º), poupança (série 196 desde 2012, aniversário dia 1º) ou Selic mensal para intervalo escolhido; compõe apenas meses publicados completos. O modo manual de taxa acumulada permanece disponível. A interface não interpola meses parciais nem estima divulgações futuras.
- As unidades/periodicidades das séries aparecem com a fonte. Catálogo e dados vêm do SGS/Olinda do Banco Central.

## Etapa 3 — Simuladores adicionais: implementada com premissas informadas

- Renegociação calcula saldo com desconto, entrada, custos, taxa, prazo, parcela, total e diferença contra o saldo original.
- Rendimento líquido calcula capitalização e deduz IR, IOF e taxas **informados pelo usuário**. Não seleciona regime fiscal nem infere alíquota; o usuário deve confirmar tributos para a modalidade e prazo do investimento.
- Cartão compara um cenário simplificado de pagamento parcial/rotativo com empréstimo Price e limita encargos acumulados ao principal original nas condições legais aplicáveis a operações elegíveis iniciadas a partir de 03/01/2024. A taxa continua sendo uma premissa informada; o teto legal incide sobre encargos totais, não sobre taxa mensal. Verifique a [orientação do BCB](https://www.bcb.gov.br/meubc/faqs/p/limitacao-dos-juros-e-encargos-financeiros-no-saldo-devedor-da-fatura-do-cartao).
- Consórcio inclui lance livre/embutido, reajuste anual e comparação de valores presentes conforme taxa de desconto digitada. Não promete data de contemplação.
- Limite: os cenários não reproduzem contratos completos, datas específicas, todos os métodos de cobrança, seguros ou tributos; isso está indicado nas telas.

## Etapa 4 — Indicadores econômicos: concluída

- `/indicadores-economicos` consulta seis séries SGS: IPCA, INPC, IGP-M, TR mensal (série 7811, início no dia 1º), poupança desde 2012 (série 196, início no dia 1º) e Selic acumulada no mês.
- Mostra unidade, periodicidade, referência, data da consulta e ligação à série oficial, com tabela e gráfico histórico. Datas inválidas, fonte indisponível e períodos sem observações têm tratamento de erro.

## Etapa 5 — Simulações, histórico e exportação: concluída

- Imprimir/salvar como PDF mostra data/hora do cálculo, parâmetros, resultado, fórmula, premissas e cronograma retornado.
- Simulações educativas só são persistidas quando a pessoa autenticada escolhe **Salvar no meu histórico** e confirma. API protegida por autenticação permite listar, ver, renomear, duplicar, recalcular com a fórmula vigente e excluir; consultas e exclusões filtram pelo usuário autenticado.
- Visitantes continuam calculando sem login e sem armazenamento local ou gravação automática.

## Etapa 6 — Educação financeira: concluída

- Verificação via API encontrou 20 artigos publicados; os 20 têm conteúdo simples/técnico, revisão registrada e ao menos duas fontes. O antigo artigo demonstrativo sobre juros compostos foi atualizado com conteúdo educativo e duas fontes oficiais do BCB.
- Cada artigo aponta para calculadora relacionada, comparador e glossário e traz quiz opcional local, sem cadastro. `/glossario` inclui termos ligados às calculadoras e aos indicadores.
- Datas/regras dependentes de atualização devem ser novamente conferidas na fonte antes da publicação de alterações futuras.

## Etapa 7 — Integração opt-in com finanças pessoais: concluída no escopo definido

- Botões nas calculadoras de reserva, renegociação e aportes consultam os dados próprios de reserva, dívida ou meta **só após confirmação explícita**. Campos de simulação são preenchidos em memória; salvar no histórico é uma ação separada.
- Endpoints financeiros existentes são autenticados; a interface não solicita CPF, senha bancária ou dados de Open Finance. Assistente recebeu orientação para mencionar ferramentas, fontes e limites sem fabricar números ou garantias.

## Etapa 8 — Open Finance: avaliação documental concluída; integração não iniciada

- Relatório: [`OPEN_FINANCE_VIABILITY.md`](OPEN_FINANCE_VIABILITY.md). Resume APIs/serviços, consentimento, diretório, certificados e segurança; identifica requisitos ausentes no projeto e custos comerciais desconhecidos; descreve próximos passos.
- Nenhuma conexão bancária, credencial, consentimento ou contratação foi criada. Qualquer integração real depende de elegibilidade, participante/provedor e aprovação específica.

## Etapa 9 — Qualidade e smoke tests: concluída com limitação de integração

- `dotnet build CentralContabil.slnx --no-restore`: aprovado, zero avisos/erros.
- `dotnet test CentralContabil.slnx --no-restore`: 107 testes unitários aprovados; 19 testes de integração ignorados porque `TEST_CONNECTION_STRING` não está configurada.
- `npm run build`: aprovado, sem avisos; `npm test -- --watch=false`: 25 testes aprovados.
- Smoke HTTP confirmou saúde da API/PostgreSQL, consulta de taxas (6 modalidades, 38 linhas; referência 2026-10), filtro de modalidade, catálogo de seis indicadores, série IPCA, inversões de Price/aportes, simuladores novos, 20 artigos revisados e HTTP 200 nas rotas públicas e na rota de histórico autenticado.
- Sem credenciais foram feitas as chamadas CRUD autenticadas de histórico ou prefill; a verificação de isolamento dessa nova tabela requer a suíte de integração ligada a um banco de teste dedicado.

## Estado final

**Etapas implementadas; etapa 3 mantém tributação informada manualmente; integração Open Finance permanece apenas documental.** A API está em `http://localhost:5042` e o frontend em `http://localhost:4200`.
