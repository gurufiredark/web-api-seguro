# Segfy Policies API

API REST para cadastro e consulta de apólices de seguro automóvel, desenvolvida em C# com ASP.NET Core e SQLite.

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- xUnit
- Swagger

## Como executar

Na raiz do projeto, restaure as dependências:

```bash
dotnet restore
```

Execute a API:

```bash
dotnet run --project src/Segfy.Policies.Api
```

Acesse o Swagger em:

```text
http://localhost:5259/swagger
```

O banco SQLite será criado automaticamente no arquivo `policies.db` na primeira execução.

## Como executar o front-end

Em outro terminal, instale as dependencias do app React:

```bash
cd frontend/Segfy.Policies.Web
npm install
```

Execute o Vite:

```bash
npm run dev
```

Acesse:

```text
http://127.0.0.1:5173
```

Durante o desenvolvimento, o Vite redireciona chamadas para `/api` para a API em:

```text
http://localhost:5259
```

## Como testar

```bash
dotnet test
```

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/policies` | Cadastra uma apólice |
| `GET` | `/api/policies` | Lista todas as apólices |
| `GET` | `/api/policies/{id}` | Consulta uma apólice por ID |
| `PUT` | `/api/policies/{id}` | Atualiza uma apólice |
| `DELETE` | `/api/policies/{id}` | Remove uma apólice |
| `GET` | `/api/policies/expiring-soon` | Lista apólices que vencem nos próximos 30 dias |

## Exemplo de cadastro

```json
{
  "insuredDocument": "12345678901",
  "vehiclePlate": "ABC1D23",
  "monthlyPremium": 199.90,
  "startDate": "2026-07-01",
  "endDate": "2027-07-01",
  "status": "Ativa"
}
```

O número da apólice é gerado automaticamente no formato:

```text
SEG-YYYY-XXXX
```

Exemplo:

```text
SEG-2026-0001
```

## Regras implementadas

- CPF/CNPJ deve conter 11 ou 14 dígitos.
- Placa deve conter 7 caracteres alfanuméricos.
- Valor do prêmio mensal deve ser maior que zero.
- Data de término deve ser maior ou igual à data de início.
- Status permitido: `Ativa`, `Cancelada` ou `Expirada`.
- Número da apólice é sequencial por ano.

## Consulta SQL solicitada

Consulta para listar apólices que vencem nos próximos 30 dias no SQLite:

```sql
SELECT *
FROM Policies
WHERE EndDate BETWEEN DATE('now') AND DATE('now', '+30 days')
ORDER BY EndDate, PolicyNumber;
```

## Estrutura

```text
src/
  Segfy.Policies.Api/
    Controllers/
    Data/
    Dtos/
    Models/
    Services/
frontend/
  Segfy.Policies.Web/
tests/
  Segfy.Policies.Tests/
```
