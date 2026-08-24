# CentralSolutions

CentralSolutions é uma aplicação web em .NET 8 para controle de chamados por setor. Ela substitui o controle em planilhas por uma interface web organizada, com cadastro, edição, consulta, exclusão e filtros de chamados.

A aplicação utiliza ASP.NET Core MVC, Entity Framework Core e banco de dados SQLite.

## Funcionalidades

- Cadastro de chamados por setor.
- Registro do tipo de chamado.
- Definição do técnico responsável.
- Registro da resolução.
- Controle da situação do chamado: `Em aberto`, `Não` ou `Sim`.
- Filtro por setor e situação.
- Interface web responsiva com Bootstrap.
- Banco SQLite local.
- Suporte para execução em Docker com volume persistente.

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
├── Controllers/
│   └── SupportTicketsController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   └── SupportTicket.cs
├── Views/
│   ├── Home/
│   ├── Shared/
│   └── SupportTickets/
├── wwwroot/
│   ├── css/
│   └── img/
├── Migrations/
├── Dockerfile
├── Program.cs
└── appsettings.json
```

## Como rodar localmente

### 1. Restaurar dependências

Na raiz da solução, execute:

```powershell
dotnet restore
```

### 2. Aplicar o banco SQLite

O projeto usa uma ferramenta local do Entity Framework. Se estiver em uma máquina nova, restaure as ferramentas:

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

### 3. Executar a aplicação

```powershell
dotnet run --project CentralSolutions\CentralSolutions.csproj
```

Depois acesse a URL exibida no terminal.

Normalmente será algo como:

```text
https://localhost:5001
```

ou

```text
http://localhost:5000
```

## Como rodar no Visual Studio

1. Abra `CentralSolutions.sln`.
2. Defina `CentralSolutions` como projeto de inicialização.
3. Pressione `F5` ou clique em **Iniciar**.
4. Acesse o menu **Chamados**.

## Como rodar com Docker

A aplicação já possui os arquivos necessários para containerização:

- `CentralSolutions/Dockerfile`
- `docker-compose.yml`
- `.dockerignore`

No Docker, o banco SQLite fica em um volume persistente, para não ser perdido ao recriar o container.

Caminho do banco dentro do container:

```text
/app/data/central_solutions.db
```

### 1. Instalar/abrir Docker Desktop

Antes de executar os comandos, confirme que o Docker Desktop está instalado e em execução.

Verifique no terminal:

```powershell
docker --version
```

### 2. Subir a aplicação

Na raiz da solução, execute:

```powershell
docker compose up --build
```

A aplicação ficará disponível em:

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

### 5. Parar a aplicação

```powershell
docker compose down
```

### 6. Parar e apagar o banco/volume

Use apenas se quiser remover também os dados persistidos:

```powershell
docker compose down -v
```

## Configuração Docker

O `docker-compose.yml` define a connection string por variável de ambiente:

```yaml
ConnectionStrings__DefaultConnection: Data Source=/app/data/central_solutions.db
```

Esse formato sobrescreve a configuração do `appsettings.json` dentro do container.

O volume usado para persistência é:

```yaml
volumes:
  centralsolutions-data:
```

## Migrations no Docker

A aplicação executa automaticamente as migrations ao iniciar:

```csharp
dbContext.Database.Migrate();
```

Assim, ao subir o container pela primeira vez, a tabela de chamados é criada automaticamente no SQLite.

## Observações

- O banco local usado fora do Docker é diferente do banco usado dentro do Docker.
- Para preservar dados no Docker, não remova o volume `centralsolutions-data`.
- Para trocar o caminho do banco, altere a connection string no `appsettings.json` ou no `docker-compose.yml`.
