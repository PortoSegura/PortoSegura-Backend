# Etapa de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. O caminho de origem agora aponta para dentro da pasta correta
COPY ["PortoSeguraAPI/PortoSeguraAPI.csproj", "PortoSeguraAPI/"]
RUN dotnet restore "PortoSeguraAPI/PortoSeguraAPI.csproj"

# 2. Copia o resto dos arquivos do repositório
COPY . .

# 3. Faz o publish apontando para o caminho atualizado
RUN dotnet publish "PortoSeguraAPI/PortoSeguraAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa de Produção 
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PortoSeguraAPI.dll"]