# Implementação de conteúdos e ferramentas de educação financeira

## 1. Objetivo

Expandir o site existente, implementando uma área completa de educação financeira, com conteúdos informativos, ferramentas interativas, simuladores financeiros e comparação de propostas bancárias.

O objetivo é proporcionar uma experiência intuitiva, moderna e acessível, permitindo que os usuários adquiram conhecimento financeiro e tomem decisões mais conscientes.

Antes de iniciar, analise toda a estrutura atual do projeto, respeitando a arquitetura, as tecnologias, a identidade visual e as funcionalidades existentes.

## 2. Conteúdos de educação financeira

Criar páginas individuais e completas para os seguintes tópicos:

7. Tributação de altas rendas.
8. Portabilidade de dívidas.
9. Consórcio × Financiamento.
10. Empréstimos: entenda antes de contratar.
11. IOF e rendimentos.
12. IPTU e IPVA.
13. Imposto de Renda sem complicação.
14. Benefícios do Governo.
15. MEI sem complicação.
16. Cartão de crédito: use com cuidado.
17. Score de crédito.
18. Organização financeira pessoal.
19. Reserva de emergência.
20. FGTS e INSS.
22. Golpes financeiros e segurança digital.
23. Endividamento e renegociação de dívidas.
24. Juros: entenda antes de comprar ou contratar.
25. Planejamento financeiro familiar.
26. Educação financeira para jovens.

### Requisitos dos conteúdos

Cada página deverá conter:

- Título e descrição introdutória.
- Explicações completas em linguagem simples e acessível.
- Exemplos práticos com situações do cotidiano.
- Vantagens, desvantagens e riscos, quando aplicável.
- Dicas e recomendações.
- Tabelas comparativas, quando necessário.
- Perguntas frequentes (FAQ).
- Links para conteúdos relacionados.
- Referências de fontes oficiais e data da última atualização.
- Otimização para SEO.
- Layout responsivo para computadores, tablets e celulares.

Utilizar informações atualizadas, priorizando fontes oficiais, como Banco Central do Brasil, Receita Federal, Governo Federal, INSS, Caixa Econômica Federal e Sebrae.

Não utilizar informações fictícias, valores tributários desatualizados ou regras financeiras sem validação.

## 3. Central de educação financeira

Criar uma página central para reunir todos os conteúdos disponibilizados no site.

Organizar os conteúdos em categorias:

- Impostos e tributos.
- Crédito e empréstimos.
- Financiamentos e consórcios.
- Planejamento e organização financeira.
- Benefícios e direitos.
- Empreendedorismo.
- Segurança financeira.
- Educação financeira para jovens.

Implementar:

- Barra de pesquisa.
- Filtros por categoria.
- Cards de apresentação dos conteúdos.
- Artigos relacionados.
- Destaques para conteúdos relevantes.
- Navegação intuitiva entre as páginas.
- Integração com os conteúdos já existentes.

## 4. Calculadora financeira

Desenvolver uma central de calculadoras financeiras, utilizando como referência funcional a Calculadora do Cidadão do Banco Central do Brasil.

Referência oficial:
https://www.bcb.gov.br/meubc/calculadoradocidadao

A ferramenta deverá possuir identidade visual própria, integrada ao design do site.

### Calculadoras que deverão ser implementadas

- Juros simples.
- Juros compostos.
- Financiamento com prestações fixas.
- Financiamento pelo sistema SAC.
- Financiamento pelo sistema Price.
- Simulação de empréstimos.
- Aplicações financeiras com depósitos regulares.
- Rentabilidade de investimentos.
- Correção monetária.
- Reserva de emergência.
- Planejamento financeiro mensal.
- Quitação antecipada de dívidas.
- Portabilidade de dívidas.
- Comparação entre consórcio e financiamento.
- Custo Efetivo Total (CET).

### Funcionalidades das calculadoras

Permitir que o usuário informe, conforme o tipo de simulação:

- Valor inicial.
- Valor financiado.
- Valor de entrada.
- Taxa de juros mensal ou anual.
- Quantidade de parcelas.
- Prazo.
- Aportes mensais.
- Tarifas administrativas.
- Seguros.
- IOF.
- Outros encargos aplicáveis.

Apresentar os resultados de maneira clara, incluindo:

- Valor das parcelas.
- Total de juros pagos.
- Valor total da operação.
- Evolução do saldo devedor.
- Rentabilidade estimada.
- Tabelas detalhadas.
- Gráficos interativos.
- Demonstrativo de amortização.
- Possibilidade de exportar os resultados em PDF.

Todos os cálculos deverão utilizar fórmulas financeiras corretas, com testes automatizados e validação dos resultados.

## 5. Comparador de propostas bancárias

Implementar uma ferramenta que permita ao usuário comparar propostas de diferentes instituições financeiras.

O usuário deverá conseguir adicionar duas ou mais propostas para comparação simultânea.

### Dados de cada proposta

- Nome da instituição financeira.
- Tipo de operação.
- Valor solicitado.
- Valor de entrada.
- Taxa de juros mensal.
- Taxa de juros anual.
- Custo Efetivo Total (CET).
- Quantidade de parcelas.
- Valor das parcelas.
- IOF.
- Tarifas administrativas.
- Seguros obrigatórios ou opcionais.
- Demais encargos.
- Valor total a pagar.

### Resultados da comparação

Apresentar:

- Comparação lado a lado.
- Proposta com menor custo total.
- Proposta com menor CET.
- Proposta com menor parcela.
- Diferença financeira entre as propostas.
- Economia potencial.
- Gráficos comparativos.
- Tabela detalhada de custos.
- Alertas sobre encargos adicionais.
- Explicação sobre os critérios utilizados na comparação.

Não considerar automaticamente a menor parcela como a melhor proposta, pois prazos maiores podem resultar em custos totais superiores.

### Integração com instituições financeiras

Investigar a viabilidade de integração com:

- APIs oficiais do Banco Central.
- Dados públicos de taxas de juros.
- Open Finance Brasil.
- APIs oficiais de instituições financeiras, quando disponíveis e autorizadas.

Caso não seja possível obter propostas bancárias reais automaticamente, implementar inicialmente o preenchimento manual das propostas pelo usuário.

Taxas médias divulgadas pelo Banco Central deverão ser identificadas como referências de mercado, e não como ofertas individuais garantidas.

Não inventar taxas, propostas, integrações ou dados bancários.

## 6. Interface e experiência do usuário

Todas as novas funcionalidades deverão seguir a identidade visual existente no projeto.

Garantir:

- Design moderno e profissional.
- Responsividade completa.
- Acessibilidade.
- Navegação simplificada.
- Formatação monetária brasileira (R$).
- Idioma português do Brasil.
- Validação dos campos de entrada.
- Mensagens de erro claras.
- Indicadores de carregamento.
- Bom desempenho.
- Compatibilidade com os principais navegadores.
- Reutilização dos componentes existentes.

## 7. Segurança e conformidade

- Respeitar a Lei Geral de Proteção de Dados (LGPD).
- Não solicitar dados pessoais ou bancários desnecessários.
- Não armazenar informações financeiras sensíveis sem necessidade e autorização.
- Informar que as simulações possuem caráter educativo e estimativo.
- Não apresentar conteúdos como recomendações financeiras personalizadas.
- Identificar as fontes e datas de atualização dos dados.
- Não realizar contratações financeiras sem autorização expressa do usuário.

## 8. Etapas de desenvolvimento

Executar o desenvolvimento de forma organizada e incremental:

**Etapa 1:** Analisar a estrutura atual do projeto e elaborar o plano de implementação.

**Etapa 2:** Criar a central de educação financeira e sua estrutura de navegação.

**Etapa 3:** Implementar todos os conteúdos informativos solicitados.

**Etapa 4:** Desenvolver as calculadoras financeiras.

**Etapa 5:** Desenvolver o comparador de propostas bancárias.

**Etapa 6:** Investigar e implementar integrações externas viáveis.

**Etapa 7:** Revisar responsividade, acessibilidade, segurança, SEO e experiência do usuário.

**Etapa 8:** Executar testes completos e validar todas as funcionalidades.

## 9. Regras obrigatórias de execução

- Não remover ou comprometer funcionalidades existentes.
- Não modificar áreas que não estejam relacionadas ao objetivo.
- Não utilizar dados fictícios como informações reais.
- Não deixar funcionalidades incompletas, botões sem ação ou páginas com conteúdo provisório.
- Não realizar alterações destrutivas no banco de dados.
- Documentar todas as alterações realizadas.
- Corrigir erros encontrados durante o desenvolvimento.
- Executar testes e build ao final de cada etapa.
- Somente solicitar aprovação quando a etapa estiver funcional e compilando corretamente.
- Aguardar minha aprovação antes de iniciar a próxima etapa.
- Ao finalizar cada etapa, executar o projeto e deixá-lo rodando para que eu possa testar.
- Não considerar uma funcionalidade concluída sem evidências de funcionamento.

## 10. Resultado esperado

Entregar uma plataforma completa de educação financeira, integrada ao site existente, com conteúdos confiáveis, calculadoras funcionais e ferramentas de comparação financeira que auxiliem os usuários na tomada de decisões.

Inicie pela análise do projeto atual, identifique os componentes reutilizáveis, apresente o plano de implementação e execute a primeira etapa conforme as regras estabelecidas.
---

## Etapa 1 — análise concluída

### Estrutura atual e pontos de reutilização

- **Frontend:** Angular 22 com componentes standalone, rotas lazy-loaded e SCSS global em `frontend/src/styles.scss`. O shell de navegação e rodapé ficam em `frontend/src/app/app.component.ts`; a página inicial está em `frontend/src/app/features/home/home.component.ts`.
- **Artigos:** a rota `artigos/:slug` e `ArticleComponent` já exibem título, resumo, conteúdo simples/técnico e fontes do artigo obtido pela API. O backend já fornece endpoints de artigos e categorias e persiste o conteúdo no PostgreSQL. A central de educação pode reutilizar o modelo de artigo, os dados e os estilos existentes, acrescentando listagem, busca e filtro sem duplicar artigos no frontend.
- **Calculadoras:** `calculadoras/:slug` e `CalculatorComponent` já tratam formulário, validação, carregamento, erros, resultado, fórmula e regra de cálculo. A API registra calculadoras por slug; hoje a interface expõe juros compostos, férias e 13º. A próxima expansão deve criar calculadoras específicas e serviços de domínio com cálculos auditáveis, sem concentrar fórmulas na interface.
- **Finanças:** há componentes de planejamento, reserva e dívidas no módulo pessoal. Eles são referências úteis de experiência e apresentação, mas pertencem à área autenticada; as novas ferramentas educativas devem ser públicas e não gravar dados pessoais por padrão.
- **API e persistência:** ASP.NET Core 10, monólito modular, EF Core e PostgreSQL com pgvector. O frontend já encaminha `/api` e `/health` para a API pela configuração de proxy. Integrações externas devem ficar no backend, com fontes e timestamps explícitos.
- **Design e acessibilidade:** reaproveitar tipografia, cores, cartões, formulários, navegação, responsividade e padrões acessíveis existentes. O conteúdo deve manter português do Brasil e valores monetários em BRL.

### Lacunas observadas

- Ainda não há uma página central de educação financeira, busca ou filtros por categoria para os artigos.
- A tela atual de artigos oferece conteúdo simples/técnico, mas não estrutura FAQ, links relacionados, data de revisão e metadados de SEO como seções explícitas.
- Existem apenas três calculadoras públicas na interface; faltam SAC, Price, CET, portabilidade, consórcio versus financiamento e demais simuladores do escopo.
- Não existe comparador de propostas bancárias.
- A resposta de artigo pode mostrar “fonte pendente de validação” quando não há fonte cadastrada. Conteúdo novo deve ser publicado somente com referências oficiais verificadas; não preencher essa lacuna com dados fictícios.
- A aplicação depende de PostgreSQL para iniciar a API. Neste ambiente, o Docker Engine está indisponível e não há serviço PostgreSQL escutando em `localhost:5432`; portanto a interface pode ser iniciada, mas a experiência completa com API requer iniciar/configurar o banco.

### Plano técnico por etapa

1. **Central e navegação:** adicionar rota pública `/educacao-financeira`, busca textual, filtro de categoria e cards usando artigos/categorias existentes. Acrescentar entrada à navegação global e manter `/artigos/:slug` como destino do conteúdo.
2. **Conteúdo editorial:** mapear os tópicos pedidos às categorias já existentes, verificar fontes oficiais e registrar referência e data de revisão em cada conteúdo. Construir as páginas com o componente de artigo existente; completar FAQ, exemplos, riscos e links relacionados onde o modelo permitir, sem publicar regra tributária ou financeira sem validação.
3. **Calculadoras:** expandir a central de simuladores e a API com módulos independentes para cada fórmula. Documentar premissas, periodicidade, arredondamento, custos considerados e limitações; exibir tabelas e gráficos quando aplicável. Validar os resultados com casos conhecidos e testes automatizados antes de liberar cada simulador.
4. **Comparador:** começar com entrada manual de duas ou mais propostas. Comparar CET e custo total, além de parcela, encargos e prazo; explicitar dados ausentes e nunca classificar a menor parcela isoladamente como melhor oferta.
5. **Integrações:** pesquisar fontes públicas e APIs oficiais disponíveis. Implementar somente endpoints autorizados e estáveis; identificar taxas médias como referência de mercado. Manter preenchimento manual como funcionamento garantido.
6. **Qualidade e entrega:** revisar acessibilidade, responsividade, segurança, SEO e LGPD; executar testes e build, registrar resultados e deixar a aplicação em execução para avaliação. Cada etapa será apresentada para aprovação antes de começar a seguinte.

### Critérios de aceite da Etapa 1

- Estrutura, tecnologias, rotas e componentes reutilizáveis mapeados.
- Lacunas e dependências operacionais registradas.
- Etapas seguintes ordenadas com critérios de segurança e validação definidos.
- Build do frontend concluído e testes unitários já existentes aprovados nesta sessão: frontend 25/25; backend 93/93. Os testes de integração do backend estão configurados para serem ignorados sem `TEST_CONNECTION_STRING`.


### Inicialização local após a Etapa 1

- O frontend está em `http://localhost:4200` e a API em `http://localhost:5042`.
- O Docker Desktop foi iniciado e o PostgreSQL com pgvector está no serviço `postgres` do Compose. Como a porta `5432` já estava ocupada por outro projeto, `docker-compose.yml` agora permite configurar a porta publicada via `POSTGRES_PORT`; neste ambiente ela está em `5433` no `.env` local, ignorado pelo Git.
- A API aplicou as migrations e respondeu saudável em `/health`, com `database: connected`. O proxy do frontend para `/health` também respondeu HTTP 200.
- Credenciais de desenvolvimento foram geradas localmente e guardadas apenas em `.env`; não foram adicionadas ao repositório.

## Etapa 2 — central de educação financeira concluída

- Criada a rota pública `/educacao-financeira`, com título e descrição para mecanismos de busca.
- A central consome os artigos publicados em `/api/v1/articles`, pesquisa por título, resumo e categoria, e monta filtros a partir das categorias que realmente têm conteúdo elegível.
- Os cards levam à rota existente `/artigos/:slug`; mensagens de carregamento, erro com nova tentativa e estado vazio foram incluídos.
- O artigo marcado no banco como conteúdo de demonstração é omitido da biblioteca, para não apresentá-lo como material educativo revisado.
- A navegação global e a página inicial agora apontam para a central. A página também oferece acesso às três calculadoras já existentes.
- Atualizado o README para documentar a rota.
- Validação: `npm run build` concluído; `npm test` aprovado, 25 testes em 7 arquivos.

**Limite desta etapa:** o banco atual ainda não contém artigos reais aprovados para educação financeira; a central mostra o estado vazio até que a Etapa 3 cadastre conteúdo validado e fontes oficiais. Nenhum conteúdo fictício foi criado para preencher a página.

## Etapa 3 — conteúdos informativos concluídos

- Foram provisionados os 19 temas solicitados (os itens numerados 7–20 e 22–26) como artigos publicados nas categorias existentes: Financeiro, Contabilidade, MEI e Trabalhista.
- Cada artigo contém título, resumo, explicação simples e técnica, exemplo do cotidiano, orientações e riscos, perguntas frequentes e indicação de conteúdos relacionados. As FAQs são exibidas como elementos expansíveis; artigos também recebem título e descrição SEO, data de revisão, links relacionados clicáveis e fontes oficiais verificadas.
- O backend adiciona os artigos ausentes por slug, com `LastReviewedAt`, `NeedsReviewAt` em seis meses, versão e fontes oficiais ligadas ao conteúdo. O seed é aditivo e não sobrescreve artigos existentes.
- As afirmações com regras tributárias/benefícios atuais apontam para a Receita Federal, Banco Central, MDS, INSS, FGTS/Caixa e órgãos oficiais. Regras locais de IPTU/IPVA são identificadas como dependentes do município/estado; taxas e benefícios não são apresentados como universais quando dependem do perfil.
- O README documenta o catálogo publicado.
- Validação: API retornou os 19 slugs; cada artigo tem ao menos duas fontes e data de revisão; health da API e PostgreSQL saudáveis; frontend respondeu HTTP 200. Build .NET e Angular aprovados; 93 testes unitários .NET e 25 testes Angular aprovados. Os 19 testes de integração do backend permanecem ignorados por falta de `TEST_CONNECTION_STRING`.

## Etapas 5 e 6 — comparador e integração pública

- Criada a rota `/comparador-propostas`, ligada à navegação principal e à central de educação financeira. Cada usuário pode comparar duas ou mais propostas informadas manualmente, removê-las/adicioná-las e ver custo total, CET, parcela, encargos discriminados, prazo e economia aritmética potencial.
- O custo soma entrada e parcelas. IOF, tarifas, seguros e demais encargos só são acrescentados quando o usuário indica que não estão incluídos nas prestações. Menor parcela, menor CET e menor custo total aparecem como critérios distintos; CET ausente é indicado explicitamente.
- Os dados ficam apenas em memória no navegador; não são enviados ao backend nem gravados em armazenamento local. A tela alerta para não inserir identificadores, dados de conta ou credenciais.
- Integração pública implementada pela API com a série SGS 20748 do Banco Central: taxa média anual das novas operações de crédito pessoal livre para pessoas físicas. A API informa mês de referência, horário de consulta e fonte e mantém cache por até seis horas. A interface mantém o comparador manual disponível quando a fonte estiver indisponível.
- A série foi consultada com sucesso durante a implementação. O Portal de Dados Abertos descreve a taxa como média ponderada das novas operações, publicada mensalmente; portanto ela é exibida como referência de mercado e não como oferta individual.
- Open Finance e cotações de instituições específicas não foram conectados: este projeto não tem participante/iniciador habilitado nem fluxo de consentimento/contratação. Nenhuma cotação foi inventada.
- Referências oficiais: [dataset SGS 20748 — Banco Central](https://dadosabertos.bcb.gov.br/dataset/20748-taxa-media-de-juros-das-operacoes-de-credito-com-recursos-livres---pessoas-fisicas---credito-) e [API SGS](https://api.bcb.gov.br/dados/serie/bcdata.sgs.20748/dados/ultimos/1?formato=json).
- Validação final: `dotnet build` sem avisos/erros; `dotnet test` aprovou os 93 testes unitários; 19 testes de integração foram ignorados por ausência de `TEST_CONNECTION_STRING`; `npm run build` concluiu sem avisos após ajuste de template; `npm test` aprovou 25 testes. `/health` respondeu saudável com PostgreSQL conectado; `GET /api/v1/market-rates/personal-credit` respondeu com a série SGS e o mês de referência atual; a página web está disponível em `http://localhost:4200/comparador-propostas`.

## Etapas 4, 7 e 8 — calculadoras, revisão e validação

- Criada a central pública `/calculadoras` e os simuladores de juros simples e compostos, parcelas fixas/Price, SAC, empréstimo, investimento com aportes, rentabilidade, correção monetária com índice informado, reserva, orçamento mensal, quitação antecipada, portabilidade, consórcio versus financiamento e estimativa do CET. As calculadoras existentes de férias e 13º foram preservadas.
- As 15 calculadoras educativas (inclusive juros compostos) usam `POST /api/v1/calculations/financial-tools/{slug}`, sem persistir os valores. O endpoint anterior de juros compostos foi mantido para compatibilidade com o histórico de cálculos autenticado já existente. Entradas são validadas, fórmulas e premissas são exibidas e as simulações de amortização retornam cronograma. Para correção monetária, rentabilidade e custos, os dados são informados pelo usuário; não se apresentam taxas futuras inventadas.
- Resultados podem ser impressos ou salvos em PDF pela função de impressão do navegador. O detalhe tem título/descrição SEO por simulador; formulários usam rótulos, avisos de validação, regiões de resultado e layouts responsivos.
- A revisão verificou ausência de dados identificáveis nos formulários e chamadas, cálculo mantido sem persistência e mensagens de erro de validação da API. As folhas de estilo definem grades móveis e área de rolagem para tabelas extensas. A captura visual automatizada não foi possível porque o canal nativo de automação do Windows não estava disponível.
- Testes adicionados para juros simples, Price, SAC, aportes de investimento, CET e limite de prazo. Verificação de todos os endpoints financeiros com entradas de referência e do endpoint de referência do Banco Central concluída.
- Validação final: `dotnet build` sem avisos/erros; `dotnet test` aprovou 100 testes unitários; 19 testes de integração foram ignorados sem `TEST_CONNECTION_STRING`; `npm run build` e `npm test` aprovados (25 testes Angular). Os 15 slugs das calculadoras e o comparador foram exercitados por chamadas API com cenários de referência; API e PostgreSQL responderam saudáveis, API do Banco Central retornou referência; rotas `/calculadoras` e `/comparador-propostas` servidas pela aplicação após o restart final.
- Restart final concluído: API em `http://localhost:5042` e frontend em `http://localhost:4200`; após reiniciar, health e banco responderam saudáveis, as 15 calculadoras responderam, a consulta SGS 20748 respondeu e as duas rotas principais retornaram HTTP 200.

## Continuação — melhorias pendentes

O novo escopo e os critérios de aceite auditados estão em [docs/METAS.md](docs/METAS.md). As nove etapas desta continuação foram solicitadas; status, limitações e evidências devem ser atualizados nesse arquivo durante a execução.
