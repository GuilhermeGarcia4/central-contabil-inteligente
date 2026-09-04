# Deploy e Google OAuth

## Fluxo de autenticação

O Angular inicia o login diretamente no backend. O backend cria o `Challenge("Google")`, e o handler do ASP.NET Core informa ao Google o `CallbackPath` pertencente ao próprio backend:

```text
Netlify
  -> Render /api/v1/auth/google/start
  -> Google
  -> Render /signin-google
  -> Render /api/v1/auth/google/complete
  -> Netlify /minha-conta?googleLogin=success
```

No retorno, a API localiza ou cria o usuário, associa o login Google, garante a role `User`, gera os tokens da Central Contábil, grava o refresh token em cookie `HttpOnly` e redireciona para o frontend definido por `FRONTEND_ORIGIN`. O Angular então usa `/api/v1/auth/refresh` para obter um novo access token; nenhum JWT é colocado na URL.

## Google Cloud Console

Crie um OAuth Client ID do tipo **Web application**.

Em **Authorized redirect URIs**, cadastre exatamente:

- Desenvolvimento: `http://localhost:5042/signin-google`
- Produção: `https://SEU-BACKEND.onrender.com/signin-google`

Substitua `SEU-BACKEND` pelo subdomínio real do Web Service no Render. Não cadastre `https://SEU-FRONTEND.netlify.app/signin-google`: o Angular não processa o callback do Google.

**Authorized JavaScript origins:** nenhuma é necessária para este fluxo. A aplicação não usa Google Identity Services nem o SDK JavaScript do Google no frontend; ela usa o handler OAuth do ASP.NET Core no backend.

## Render

Configure as seguintes Environment Variables no Web Service:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Authentication__Google__ClientId`: Client ID do Google
- `Authentication__Google__ClientSecret`: Client Secret do Google
- `FRONTEND_ORIGIN=https://SEU-FRONTEND.netlify.app`
- `JWT_KEY`: chave aleatória com pelo menos 32 caracteres
- `ConnectionStrings__Default`: conexão PostgreSQL de produção

O Render fornece `RENDER_EXTERNAL_URL` automaticamente. A API usa essa origem pública para montar `https://SEU-BACKEND.onrender.com/signin-google`, inclusive atrás do proxy TLS do Render. Não coloque o Client Secret no Netlify, no Angular ou no repositório.

## Netlify

Configure a variável de build:

- `API_ORIGIN=https://SEU-BACKEND.onrender.com`

O `prebuild` grava essa origem no `runtime-config.js` publicado. Em desenvolvimento, deixe `API_ORIGIN` ausente: o Angular usa URLs relativas e o proxy local encaminha `/api` para `http://localhost:5042`.

Depois de alterar `API_ORIGIN`, faça um novo deploy do frontend.

## Desenvolvimento local

Use os nomes hierárquicos do ASP.NET Core em User Secrets ou variáveis de ambiente:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "SEU_CLIENT_ID" --project backend/src/CentralContabil.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU_CLIENT_SECRET" --project backend/src/CentralContabil.Api
```

Em variáveis de ambiente, os mesmos nomes são:

- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`

Com a API em `http://localhost:5042` e o Angular em `http://localhost:4200`, o callback continua sendo `http://localhost:5042/signin-google`.
