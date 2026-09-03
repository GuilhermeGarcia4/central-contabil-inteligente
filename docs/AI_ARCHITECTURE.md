# Arquitetura de IA e Knowledge — V3

## Visão geral

A V3 mantém o monólito modular. IA é uma camada opcional sobre conteúdo editorial, fontes, vigência e calculadoras determinísticas. Artigos e calculadoras continuam funcionando quando o provider externo falha.

```text
                    USER
                      │
                      ▼
                 Angular
                      │
                      ▼
               Assistant API
                      │
          ┌───────────┴────────────┐
          │                        │
          ▼                        ▼
    Intent Router            Hybrid Search
          │                        │
          │               ┌────────┴────────┐
          │               ▼                 ▼
          │          PostgreSQL          pgvector
          │
          ▼
     Tool Router
          │
      ┌───┴───────────┐
      ▼               ▼
Calculators        Knowledge
      │               │
      └───────┬───────┘
              ▼
             LLM
              │
              ▼
     Answer + Sources
```

## Indexação e busca híbrida

`KnowledgeIndexingWorker` consome uma fila interna limitada. `KnowledgeIndexingService` aceita artigos publicados sem revisão rejeitada e calculadoras ativas, normaliza o texto, calcula SHA-256 e evita reindexação quando `ContentHash` não mudou. Cada chunk mantém documento, artigo, fonte, URL, jurisdição, vigência e verificação.

A migration inicial cria `vector`, coluna `vector(384)`, índice HNSW cosseno e índice GIN full-text em português. A dimensão é validada por `AI:EmbeddingDimensions`; alterá-la exige migration explícita. Cada chunk registra provider, modelo e dimensão. A busca vetorial compara somente embeddings com a mesma identidade da consulta, e a fila reindexa documentos quando essa identidade muda. Assim, alternar modelos não mistura espaços vetoriais nem apaga conteúdo.

## RAG, providers e tools

`IEmbeddingService` e `IChatCompletionService` desacoplam fornecedores. `IAssistantProvider` e `IEmbeddingProvider` especializam os contratos externos. `ConfigurableChatCompletionService` e `ConfigurableEmbeddingService` selecionam Gemini, OpenAI ou fallback local pela configuração. O fallback gera embeddings determinísticos e respostas fundamentadas.

- `AI_PROVIDER=gemini`: Gemini REST oficial, `x-goog-api-key`, `generateContent` e `embedContent`.
- `AI_PROVIDER=openai`: endpoints OpenAI de chat completions e embeddings.
- ausente, inválido, sem chave ou com falha: modo `local-grounded`.

O Gemini usa `gemini-3.7-flash` e `gemini-embedding-2` por padrão. O embedding solicita explicitamente 384 dimensões e diferencia `RETRIEVAL_DOCUMENT` de `RETRIEVAL_QUERY`. O modelo fica centralizado em configuração.

1. validação e minimização;
2. intenção por regras baratas;
3. busca híbrida e filtros;
4. ferramenta determinística quando aplicável;
5. contexto limitado e separado;
6. chat configurável ou resposta local;
7. fontes, confiança categórica e disclaimer;
8. telemetria sem pergunta e registro anonimizado de lacunas.

O tool router oferece somente calculadoras conhecidas. Não oferece SQL, shell, arquivos, administração, fetch arbitrário ou alteração de regras.

## Segurança

- Prompt versionado em `Modules/AI/Infrastructure/system-prompt.pt-BR.txt`.
- Contexto recuperado é **DADO NÃO CONFIÁVEL**, nunca instrução.
- Separação SYSTEM/USER/RETRIEVED_CONTEXT/TOOL_RESULT e teste de prompt injection.
- CPF e e-mail minimizados antes de chamadas externas.
- URLs de providers exigem HTTPS ou localhost; não há ingestão de URL arbitrária.
- API keys ficam somente no backend.
- Falhas registram provider e status, nunca a chave nem o corpo potencialmente sensível.
- Rate limit distinto para visitante, usuário e administrador.
- `AI:MaxRetrievedDocuments`, `AI:MaxChunks` e `AI:MaxContextCharacters` limitam contexto.

## Observabilidade

`AIRequest` registra provider, modelo, tokens estimados, duração, status e intenção, nunca o texto. `UnansweredQuestion` agrega a pergunta normalizada. `/admin/knowledge` gerencia indexação; `/admin/ai` mostra uso, latência, erros e lacunas.
