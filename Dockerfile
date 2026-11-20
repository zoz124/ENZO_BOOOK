# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so layer caching survives source-only changes.
COPY BookNest.sln ./
COPY src/BookNest.Core/BookNest.Core.csproj src/BookNest.Core/
COPY src/BookNest.Infrastructure/BookNest.Infrastructure.csproj src/BookNest.Infrastructure/
COPY src/BookNest.Web/BookNest.Web.csproj src/BookNest.Web/
RUN dotnet restore src/BookNest.Web/BookNest.Web.csproj

COPY src/ src/
RUN dotnet publish src/BookNest.Web/BookNest.Web.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Writable paths for logs and uploaded covers (mounted as a volume in compose).
RUN mkdir -p /app/logs /app/wwwroot/uploads/covers

EXPOSE 8080

ENTRYPOINT ["dotnet", "BookNest.Web.dll"]
