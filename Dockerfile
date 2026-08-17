# ===== Etapa 1: build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia só o csproj primeiro para aproveitar cache de camadas do Docker
COPY LatamPriceChecker.csproj .
RUN dotnet restore "LatamPriceChecker.csproj"

# Copia o restante do código e publica
COPY . .
RUN dotnet publish "LatamPriceChecker.csproj" -c Release -o /app/publish --no-restore

# ===== Etapa 2: runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Porta que o Kestrel vai escutar dentro do container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LatamPriceChecker.dll"]
