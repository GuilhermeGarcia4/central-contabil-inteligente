# Avaliação de viabilidade — Open Finance

**Revisado em:** 24 de novembro de 2026
**Estado:** avaliação documental; nenhuma conexão, credencial, consentimento ou contratação foi iniciada.

## Resultado

A integração direta com o Open Finance não é pronta para este repositório. O código não identifica participação do Central Contábil Inteligente no Diretório de Participantes, certificados válidos, infraestrutura mTLS, APIs homologadas, entidade receptora habilitada, acordo com instituição participante ou jornada de consentimento. Não há evidência no projeto para confirmar esses requisitos. O modelo adequado para uma fase futura seria uma integração por participante/provedor habilitado, sujeita à validação jurídica, de segurança, custos, contrato e finalidades específicas.

O Banco Central descreve que somente instituições autorizadas podem participar e que o compartilhamento depende de consentimento do cliente. O fluxo requer identificação/autenticação e confirmação do compartilhamento. A instituição recebedora conecta-se à transmissora por APIs e obtém somente os dados autorizados. O Diretório formaliza a participação e integração das instituições; a regulação e os manuais também preveem requisitos de segurança, certificados ICP-Brasil e proteção dos dados.

## Dados e serviços

O Open Finance compartilha dados padronizados entre participantes, incluindo informações de contas, cartões, crédito e investimentos, mediante finalidade e consentimento. O escopo disponível é definido pelo ecossistema e pela versão de suas APIs; ele não oferece uma promessa universal de propostas de crédito personalizadas ou de aprovação. Encaminhamento de propostas é um serviço regulado com requisitos próprios e não equivale a uma lista aberta de ofertas para qualquer aplicativo.

## Bloqueios identificados no projeto

- Não foi encontrada prova de que a pessoa jurídica operadora esteja autorizada e registrada como participante/receptora.
- Não existe cadastro no Diretório, gestão de certificados, mTLS ou rotina de conformidade de API no projeto.
- Não existe jornada de consentimento Open Finance, gestão de escopos, expiração e revogação.
- Não foram identificados contrato com participante/provedor ou custos. Custos e condições comerciais permanecem **desconhecidos** e não foram estimados.
- Não há base para integrar credenciais bancárias diretamente; o projeto não deve coletar senhas bancárias.
- As mudanças desta rodada limitam-se a APIs públicas do Banco Central, simuladores com parâmetros digitados e prefill financeiro próprio mediante confirmação na sessão.

## Próximos passos para uma fase separada

1. Validar a entidade operadora, papel regulatório, eventual participação e elegibilidade com assessoria especializada e com o ecossistema oficial.
2. Solicitar propostas a provedores participantes habilitados, avaliando contrato, suboperadores, localização/retensão, suporte, SLAs, incidentes, custos e encerramento.
3. Definir finalidade, dados mínimos, base legal, telas de consentimento/revogação, controles de acesso, auditoria, criptografia, retenção e resposta a incidentes.
4. Só após aprovação específica, elaborar arquitetura, ambiente de testes e plano de homologação com APIs e certificados oficiais vigentes.

## Fontes oficiais

- [BCB — Perguntas frequentes sobre Open Finance](https://www.bcb.gov.br/meubc/faqs/s/open-finance)
- [BCB — Como os dados são transmitidos entre instituições](https://www.bcb.gov.br/meubc/faqs/p/como-os-dados-sao-transmitidos-de-uma-instituicao-para-outra)
- [BCB — Instrução Normativa 359 e Diretório de Participantes](https://www.bcb.gov.br/estabilidadefinanceira/exibenormativo?numero=359&tipo=Instru%C3%A7%C3%A3o%20Normativa%20BCB)
- [BCB — Resolução Conjunta nº 1](https://normativos.bcb.gov.br/Lists/Normativos/Attachments/51028/Res_Conj_0001_v8_L.pdf)
- [Open Finance Brasil — participantes e governança](https://openfinancebrasil.org.br/)
