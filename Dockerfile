# ===============================
# Stage 1: Runtime 
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Segurança: roda como usuário não-root que já existe na imagem
USER app
WORKDIR /app

# Kestrel ouvindo em todas as interfaces na porta 5087
ENV ASPNETCORE_URLS=http://+:5087
# Documenta no Docker que o container expõe a porta 5087
EXPOSE 5087

# ===============================
# Stage 2: Build/Publish
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copia a solução e os .csproj para aproveitar o cache do Docker
COPY ["Fcg.Payment.sln", "./"]
COPY ["Fcg.Payment.Api/Fcg.Payment.API.csproj", "Fcg.Payment.Api/"]
COPY ["Fcg.Payment.Application/Fcg.Payment.Application.csproj", "Fcg.Payment.Application/"]
COPY ["Fcg.Payment.Domain/Fcg.Payment.Domain.csproj", "Fcg.Payment.Domain/"]
COPY ["Fcg.Payment.Infrastructure/Fcg.Payment.Infrastructure.csproj", "Fcg.Payment.Infrastructure/"]

# Restaura dependências do projeto de API (puxa o grafo inteiro)
RUN dotnet restore "Fcg.Payment.Api/Fcg.Payment.API.csproj"

# Copia o restante do código
COPY . .

# Publica a API (Release) para uma pasta única
WORKDIR "/src/Fcg.Payment.Api"
RUN dotnet publish "Fcg.Payment.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ===============================
# Stage 3: Final (imagem enxuta)
# ===============================
FROM base AS final
WORKDIR /app

# Copia artefatos publicados
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "Fcg.Payment.API.dll"]
