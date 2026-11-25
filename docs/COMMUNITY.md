# Comunidade

## Escopo

A Comunidade oferece discussões públicas em texto, respostas em ordem cronológica, busca, tema, reação “útil” e denúncias. Não oferece mensagens privadas, anexos ou chat em tempo real. O frontend exibe texto com interpolação Angular, sem interpretar HTML fornecido por usuários.

## Regras de convivência

- Não publique spam, assédio, conteúdo ilegal, CPF, e-mail, telefone, senha, dados bancários ou números de cartão.
- Não exponha informações pessoais suas ou de terceiros.
- Mantenha o respeito e não trate as respostas como aconselhamento contábil, jurídico ou tributário individualizado.
- Denúncias podem ser feitas em discussões e respostas; administradores e moderadores revisam o conteúdo e registram a decisão.

A API limita títulos a 140 caracteres, discussões a 5.000, respostas a 3.000 e detalhes/justificativas a 500. Valida categorias, bloqueia tags HTML e padrões comuns de CPF, contato e cartões. O limite não substitui a moderação nem detecta todos os dados sensíveis possíveis.

## Papéis e privacidade

O papel **Moderator** é provisionado pelo seed de identidade. Administradores podem conceder ou remover o papel no painel **Comunidade → Revisar denúncias**, informando o e-mail da conta já cadastrada. Essa operação é protegida pelo papel **Admin**; moderadores não podem gerenciar outros papéis. Uma sessão existente recebe o novo papel quando obtiver um JWT no próximo login/refresh.

Na Comunidade e na fila de moderação, os dados de perfil exibidos limitam-se ao nome público escolhido pelo usuário e ao papel (**Membro**, **Moderador** ou **Admin**). A fila não expõe e-mail, CPF ou dados de conta financeira. O conteúdo removido é ocultado para visitantes e preservado no banco para o histórico de moderação.

## API

- GET /api/v1/community: público; busca, tema e paginação.
- GET /api/v1/community/{id}: público; discussão e respostas.
- POST/PUT/DELETE /api/v1/community e /{id}: autenticado; criar/editar/remover a própria discussão.
- POST /api/v1/community/{id}/comments e PUT/DELETE /comments/{id}: autenticado; responder e administrar a própria resposta.
- POST /api/v1/community/{id}/useful: autenticado; alternar reação.
- POST /api/v1/community/{id}/reports e /comments/{id}/reports: autenticado; denunciar.
- GET /api/v1/community/moderation/reports, POST .../review e GET .../actions: Admin ou Moderator.
- POST /api/v1/community/moderation/{discussion|comment}/{id}/{hide|restore}: Admin ou Moderator; ocultar/restaurar e auditar conteúdo.
- PUT /api/v1/community/moderators: somente Admin; conceder/remover papel de moderador.

Escritas têm limites por identidade autenticada (12 por minuto; denúncias, 5 a cada 5 minutos). Os registros de denúncia, remoção lógica e auditoria são armazenados no PostgreSQL. Não há ferramenta de exportação de dados da comunidade nesta entrega.

## Banco e validação

As migrations AddCommunity e AddCommunityModerationAudit criam discussões, respostas, denúncias, reações, trilha de moderação, índices e a regra de que cada denúncia aponta para exatamente uma discussão ou resposta. A API aplica migrations no início, conforme o padrão existente. Antes de publicar, valide essa inicialização em homologação e confirme os papéis e a conta administrativa.
