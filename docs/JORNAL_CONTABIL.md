# Jornal Contábil

## Primeira entrega

O Jornal Contábil é uma área pública e independente de `/novidades`. A página inicial mostra a notícia mais recente em destaque e até três outras atualizações. A rota `/jornal-contabil` permite buscar por palavras-chave, filtrar por categoria, ordenar por data e navegar por páginas.

Cada item preserva o título, o resumo fornecido pelo feed, a categoria editorial atribuída, o nome da fonte, a data de publicação original e o link direto para a publicação. Quando o feed não oferece resumo, a interface identifica essa ausência; não redige um substituto. Os resumos não são aconselhamento contábil, tributário ou jurídico individualizado.

## Fontes consultadas

| Fonte | Feed oficial | Estado verificado em 25/11/2026 |
| --- | --- | --- |
| Receita Federal | [RSS/Atom de notícias](https://www.gov.br/receitafederal/pt-br/assuntos/noticias/RSS) | HTTP 200; feed Atom/RSS 1.0 |
| Conselho Federal de Contabilidade | [RSS da categoria Notícias](https://cfc.org.br/category/noticias/feed/) | HTTP 200; RSS 2.0 |
| eSocial | [RSS da área de notícias](https://www.gov.br/esocial/pt-br/noticias/RSS) | HTTP 404 nesta verificação; não integrado |
| Portal do Empreendedor | [RSS de notícias](https://www.gov.br/empresas-e-negocios/pt-br/empreendedor/noticias/RSS) | Não confirmou feed acessível nesta verificação; não integrado |

O gov.br documenta que RSS precisa estar habilitado na pasta do portal e usa o sufixo `RSS` quando ativo ([orientação oficial](https://www.gov.br/gestaodeconteudo/pt-br/manuais-e-tutoriais/configuracoes-do-ambiente/habilitando-rss)). O endereço `/noticias/feed/` do CFC respondeu com feed de comentários, portanto a integração usa a categoria oficial de notícias.

## Operação e limites

- A API busca os feeds oficiais; o navegador não consulta terceiros e não armazena credenciais.
- O timeout por consulta é de 8 segundos. Respostas bem-sucedidas ficam no cache compartilhado já usado pelo sistema por 15 minutos.
- Se uma fonte falhar, o backend tenta devolver a última cópia salva e a resposta identifica a fonte indisponível. Outras fontes continuam aparecendo.
- Notícias sem data original ou link HTTP(S) válido são descartadas para não apresentar metadados inferidos ou URLs inseguras.
- As categorias do Jornal são atribuídas por palavras-chave do título e resumo. São rótulos de navegação, não afirmações sobre o conteúdo legal.
- Os feeds podem atualizar com atraso ou alterar seu formato; a página sempre expõe a fonte original para conferência.

## Evoluções possíveis (fora da primeira entrega)

- Calendário de obrigações fiscais e contábeis.
- Alertas para mudanças com prazos próximos.
- Preferências de temas por usuário.
- Ligações entre notícias, artigos, calculadoras e consultas do sistema.
- Inclusão de eSocial, Diário Oficial, Simples Nacional e outros órgãos quando houver API ou feed oficial acessível e contrato estável.
