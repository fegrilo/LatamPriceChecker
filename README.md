# LatamPriceChecker

Monitor de preços do shop-search do Ragnarok Online LATAM (GnJoy), com alerta via Discord.

Os itens monitorados agora são armazenados em **PostgreSQL** e gerenciados por uma **API REST**, em vez de ficarem fixos no código (`AppConfig`).

## Pré-requisitos

- .NET 8 SDK
- PostgreSQL (ou Docker, veja abaixo)

## Subindo o PostgreSQL localmente (opcional, via Docker)

```bash
docker compose up -d
```

Isso sobe um Postgres em `localhost:5432` com o banco `latam_price_checker`.

## Configuração

Edite `appsettings.json` (ou, preferencialmente, use `dotnet user-secrets` / variáveis de ambiente para não commitar segredos):

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=latam_price_checker;Username=postgres;Password=SUASENHA"
dotnet user-secrets set "Discord:WebhookUrl" "https://discord.com/api/webhooks/SEU_WEBHOOK"
```

> ⚠️ **Nunca** commite a URL real do webhook do Discord nem a senha do banco no `appsettings.json`.

## Rodando

```bash
dotnet restore
dotnet run
```

Com a API rodando, abra `http://localhost:5080/swagger` (ou a porta que o `dotnet run` mostrar) para testar os endpoints pelo Swagger UI.

Na primeira execução, o schema do banco é criado automaticamente (`EnsureCreated`). Para produção, considere migrar para EF Core Migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

E trocar `db.Database.EnsureCreated()` por `db.Database.Migrate()` no `Program.cs`.

## API — Itens monitorados

Implementada com **Controllers** (`Controllers/ItemsController.cs`, estilo clássico `[ApiController]`/`ControllerBase`).

Base: `/api/items`

| Método | Rota             | Descrição                     | Body                                              |
|--------|------------------|--------------------------------|----------------------------------------------------|
| GET    | `/api/items`     | Lista todos os itens           | —                                                    |
| GET    | `/api/items/{id}`| Busca um item por id           | —                                                    |
| POST   | `/api/items`     | Cria um novo item              | `{ "searchWord": "string", "targetPrice": 123 }`   |
| PUT    | `/api/items/{id}`| Atualiza um item existente     | `{ "searchWord": "string", "targetPrice": 123 }`   |
| DELETE | `/api/items/{id}`| Remove um item                 | —                                                    |

### Exemplo

```bash
curl -X POST http://localhost:5000/api/items \
  -H "Content-Type: application/json" \
  -d '{"searchWord": "Báculo Adulter Fides", "targetPrice": 1000000}'
```

O background service verifica os itens cadastrados no banco a cada `Monitor:CheckIntervalMinutes` minutos (padrão: 10) e envia um alerta no Discord quando encontra um preço igual ou abaixo do `targetPrice`.
