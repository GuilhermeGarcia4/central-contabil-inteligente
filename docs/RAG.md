# RAG da Central Contábil

O fluxo permanece: pergunta → detecção de intenção → busca híbrida → conteúdo aprovado → ferramentas determinísticas → LLM → resposta com fontes. Vigência, jurisdição, vínculo com artigo e fonte oficial continuam armazenados no documento; o provider de IA não recebe acesso a SQL.

## Embeddings e pgvector

O PostgreSQL continua usando pgvector com coluna `vector(384)`. Gemini Embedding 2 suporta dimensão configurável, então o backend solicita 384 valores em vez de alterar a coluna existente. Cada chunk registra `EmbeddingProvider`, `EmbeddingModel` e `EmbeddingDimensions`. Consultas vetoriais filtram por essa identidade.

Ao alternar `AI_PROVIDER`, o worker detecta que os embeddings existentes pertencem a outro espaço vetorial e reindexa somente os chunks. Documentos, artigos, fontes, referências legais e demais dados não são removidos. Enquanto um provider estiver indisponível, a busca lexical e o embedding local mantêm o sistema funcional.

Para solicitar reindexação manual, um administrador pode usar `POST /api/v1/admin/knowledge/reindex`. Não altere `AI__EmbeddingDimensions=384` sem uma migration explícita da coluna pgvector e uma reindexação completa.

## Minimização de dados

CPF e e-mail são removidos antes de chamadas externas. O backend calcula resumos financeiros e resultados de ferramentas; o modelo recebe apenas a pergunta minimizada, trechos recuperados limitados e o resumo estruturado necessário. Transações completas e SQL livre não são enviados.
