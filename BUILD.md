# Guia de Build - Porto Segura (Backend / API)

Este documento descreve os passos necessários para configurar, compilar e executar o backend da aplicação **Porto Segura**, construído com a plataforma **.NET 9.0**.

## Pré-requisitos
Para desenvolvimento e execução local (Nativa):
- **.NET SDK 9.0** ou superior instalado em seu ambiente.
- IDE de sua preferência (Visual Studio, Rider ou VS Code com extensão C#).

Para execução conteinerizada:
- **Docker** instalado e em execução.

## 1. Executando Nativamente via .NET CLI

Acesse o diretório raiz do backend onde o arquivo `.sln` ou a pasta do projeto `.csproj` está localizada.

### Restaurar Dependências
O comando de restore fará o download dos pacotes NuGet necessários:
```bash
dotnet restore PortoSeguraAPI/PortoSeguraAPI.csproj
```

### Compilar o Projeto
Para verificar se não há erros e compilar o código fonte:
```bash
dotnet build PortoSeguraAPI/PortoSeguraAPI.csproj -c Release
```

### Executar a Aplicação
Inicie o servidor de desenvolvimento web (Kestrel) embutido no .NET:
```bash
dotnet run --project PortoSeguraAPI/PortoSeguraAPI.csproj
```
A API será iniciada e ficará disponível para requisições conforme as portas mapeadas nos arquivos `appsettings.json` ou nas propriedades do launchSettings.

## 2. Executando via Docker (Conteinerização)

O repositório já conta com um `Dockerfile` configurado usando *Multi-stage Build*, que garante uma imagem final leve baseada no `aspnet:9.0` e segura para produção.

### Construir a Imagem Docker
Na raiz do repositório do backend (onde o `Dockerfile` está localizado), execute o comando de build:

```bash
docker build -t portosegura-api .
```

### Rodar o Container
Após a imagem ser construída com sucesso, inicie o container mapeando a porta local para a porta exposta pelo container (a porta interna utilizada na imagem de produção do ASP.NET Core costuma ser `8080`):

```bash
docker run -d -p 8080:8080 --name portosegura-api-container portosegura-api
```
A API estará acessível no ambiente host em `http://localhost:8080`.

## 3. Banco de Dados e Migrations (Entity Framework Core)
A aplicação utiliza o Entity Framework Core para gerenciar o esquema do banco de dados. Para aplicar as migrações mais recentes ao seu banco de dados, utilize o comando:

```bash
dotnet ef database update --project PortoSeguraAPI/PortoSeguraAPI.csproj
```
*Aviso: Certifique-se de configurar corretamente sua Connection String no arquivo `appsettings.Development.json` ou pelas variáveis de ambiente adequadas antes de executar a migração.*
