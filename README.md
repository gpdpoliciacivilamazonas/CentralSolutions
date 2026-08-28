# CentralSolutions

CentralSolutions Ã© uma aplicaÃ§Ã£o web em .NET 8 para controle de chamados por setor. Ela substitui o controle em planilhas por uma interface web organizada, com cadastro, ediÃ§Ã£o, consulta, exclusÃ£o e filtros de chamados.

A aplicaÃ§Ã£o utiliza ASP.NET Core MVC, Entity Framework Core e banco de dados SQLite.

## Funcionalidades

- Cadastro de chamados por setor.
- Cadastro e manutenÃ§Ã£o de setores no banco de dados.
- Registro do tipo de chamado.
- DefiniÃ§Ã£o do tÃ©cnico responsÃ¡vel.
- Registro da resoluÃ§Ã£o.
- Controle da situaÃ§Ã£o do chamado: `Em aberto`, `NÃ£o` ou `Sim`.
- Filtro por setor e situaÃ§Ã£o.
- Interface web responsiva com Bootstrap.
- Banco SQLite local.
- Suporte para execuÃ§Ã£o em Docker com volume persistente.

## Tecnologias

- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- SQLite
- Bootstrap
- Docker

## Estrutura principal

```text
CentralSolutions/
â”œâ”€â”€ Controllers/
â”‚   â”œâ”€â”€ DepartmentsController.cs
â”‚   â””â”€â”€ SupportTicketsController.cs
â”œâ”€â”€ Data/
â”‚   â””â”€â”€ ApplicationDbContext.cs
â”œâ”€â”€ Models/
â”‚   â”œâ”€â”€ Department.cs
â”‚   â””â”€â”€ SupportTicket.cs
â”œâ”€â”€ Views/
â”‚   â”œâ”€â”€ Home/
â”‚   â”œâ”€â”€ Departments/
â”‚   â”œâ”€â”€ Shared/
â”‚   â””â”€â”€ SupportTickets/
â”œâ”€â”€ wwwroot/
â”‚   â”œâ”€â”€ css/
â”‚   â””â”€â”€ img/
â”œâ”€â”€ Migrations/
â”œâ”€â”€ Dockerfile
â”œâ”€â”€ Program.cs
â””â”€â”€ appsettings.json
```

## Como rodar localmente

### 1. Restaurar dependÃªncias

Na raiz da soluÃ§Ã£o, execute:

```powershell
dotnet restore
```

### 2. Aplicar o banco SQLite

O projeto usa uma ferramenta local do Entity Framework. Se estiver em uma mÃ¡quina nova, restaure as ferramentas:

```powershell
dotnet tool restore
```

Depois aplique as migrations:

```powershell
dotnet tool run dotnet-ef database update --project CentralSolutions\CentralSolutions.csproj
```

Isso cria ou atualiza o banco SQLite configurado na connection string:

```json
"DefaultConnection": "Data Source=central_solutions.db"
```

### 3. Executar a aplicaÃ§Ã£o

```powershell
dotnet run --project CentralSolutions\CentralSolutions.csproj
```

Depois acesse a URL exibida no terminal.

Normalmente serÃ¡ algo como:

```text
https://localhost:5001
```

ou

```text
http://localhost:5000
```

## Como rodar no Visual Studio

1. Abra `CentralSolutions.sln`.
2. Defina `CentralSolutions` como projeto de inicializaÃ§Ã£o.
3. Pressione `F5` ou clique em **Iniciar**.
4. Acesse o menu **Chamados**.

## GestÃ£o de setores

Os setores nÃ£o ficam hardcoded no cÃ³digo. Eles sÃ£o armazenados na tabela `departments` do SQLite e carregados dinamicamente nas telas de chamados.

Pelo sistema, acesse o menu **Setores** para:

- buscar setores;
- cadastrar novos setores;
- editar nomes existentes;
- ativar ou inativar setores;
- excluir setores.

Setores inativos deixam de aparecer para novos chamados, mas chamados antigos mantÃªm o nome do setor que foi salvo no registro.

TambÃ©m Ã© possÃ­vel atualizar setores diretamente por SQL. Exemplos:

```sql
UPDATE departments
SET Name = 'NOVO NOME', UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'NOME ANTIGO';
```

```sql
INSERT INTO departments (Name, IsActive, CreatedAt)
VALUES ('NOVO SETOR', 1, CURRENT_TIMESTAMP);
```

Se tambÃ©m quiser renomear setores jÃ¡ gravados em chamados existentes, atualize a tabela `support_tickets`:

```sql
UPDATE support_tickets
SET Department = 'NOVO NOME'
WHERE Department = 'NOME ANTIGO';
```

## Como rodar com Docker

A aplicaÃ§Ã£o jÃ¡ possui os arquivos necessÃ¡rios para containerizaÃ§Ã£o:

- `CentralSolutions/Dockerfile`
- `docker-compose.yml`
- `.dockerignore`

No Docker, o banco SQLite fica em um volume persistente, para nÃ£o ser perdido ao recriar o container.

Caminho do banco dentro do container:

```text
/app/data/central_solutions.db
```

### 1. Instalar/abrir Docker Desktop

Antes de executar os comandos, confirme que o Docker Desktop estÃ¡ instalado e em execuÃ§Ã£o.

Verifique no terminal:

```powershell
docker --version
```

### 2. Subir a aplicaÃ§Ã£o

Na raiz da soluÃ§Ã£o, execute:

```powershell
docker compose up --build
```

A aplicaÃ§Ã£o ficarÃ¡ disponÃ­vel em:

```text
http://localhost:8080
```

### 3. Rodar em segundo plano

```powershell
docker compose up --build -d
```

### 4. Ver logs

```powershell
docker compose logs -f
```

### 5. Parar a aplicaÃ§Ã£o

```powershell
docker compose down
```

### 6. Parar e apagar o banco/volume

Use apenas se quiser remover tambÃ©m os dados persistidos:

```powershell
docker compose down -v
```

## ConfiguraÃ§Ã£o Docker

O `docker-compose.yml` define a connection string por variÃ¡vel de ambiente:

```yaml
ConnectionStrings__DefaultConnection: Data Source=/app/data/central_solutions.db
```

Esse formato sobrescreve a configuraÃ§Ã£o do `appsettings.json` dentro do container.

O volume usado para persistÃªncia Ã©:

```yaml
volumes:
  centralsolutions-data:
```

## Migrations no Docker

A aplicaÃ§Ã£o executa automaticamente as migrations ao iniciar:

```csharp
dbContext.Database.Migrate();
```

Assim, ao subir o container pela primeira vez, a tabela de chamados Ã© criada automaticamente no SQLite.

## ObservaÃ§Ãµes

- O banco local usado fora do Docker Ã© diferente do banco usado dentro do Docker.
- Para preservar dados no Docker, nÃ£o remova o volume `centralsolutions-data`.
- Para trocar o caminho do banco, altere a connection string no `appsettings.json` ou no `docker-compose.yml`.
