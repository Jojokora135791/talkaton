# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — слой с restore переиспользуется, пока не поменялись зависимости.
COPY Directory.Build.props ./
COPY src/Talkaton.Domain/Talkaton.Domain.csproj src/Talkaton.Domain/
COPY src/Talkaton.Infrastructure/Talkaton.Infrastructure.csproj src/Talkaton.Infrastructure/
COPY src/Talkaton.Api/Talkaton.Api.csproj src/Talkaton.Api/
RUN dotnet restore src/Talkaton.Api/Talkaton.Api.csproj

COPY src/ src/
RUN dotnet publish src/Talkaton.Api/Talkaton.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl нужен только для docker healthcheck — в aspnet-образе нет ни его, ни wget.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Talkaton.Api.dll"]
