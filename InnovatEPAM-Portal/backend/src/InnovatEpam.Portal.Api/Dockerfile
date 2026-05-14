# syntax=docker/dockerfile:1.7
# ------------------------------------------------------------------------------
# Multi-stage build for InnovatEpam.Portal.Api (.NET 8).
#
# Build context is expected to be the `backend/` directory (the folder that
# contains the solution + Directory.Build.props + global.json), so the COPY
# instructions can pull in every referenced project for a layered restore.
#
#   docker build -f src/InnovatEpam.Portal.Api/Dockerfile -t innovatepam-api .
#
# docker-compose.dev.yml wires this up automatically.
# ------------------------------------------------------------------------------

# ---------- 1. restore -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

# Copy solution + repo-level build props so `dotnet restore` sees the same
# graph CI does. Copying csproj files first keeps the restore layer cacheable.
COPY InnovatEpam.Portal.sln Directory.Build.props global.json ./
COPY src/InnovatEpam.Portal.Api/InnovatEpam.Portal.Api.csproj                     src/InnovatEpam.Portal.Api/
COPY src/InnovatEpam.Portal.Application/InnovatEpam.Portal.Application.csproj     src/InnovatEpam.Portal.Application/
COPY src/InnovatEpam.Portal.Domain/InnovatEpam.Portal.Domain.csproj               src/InnovatEpam.Portal.Domain/
COPY src/InnovatEpam.Portal.Infrastructure/InnovatEpam.Portal.Infrastructure.csproj src/InnovatEpam.Portal.Infrastructure/

RUN dotnet restore src/InnovatEpam.Portal.Api/InnovatEpam.Portal.Api.csproj

# ---------- 2. build + publish -----------------------------------------------
FROM restore AS publish
WORKDIR /src
COPY src/ ./src/

RUN dotnet publish src/InnovatEpam.Portal.Api/InnovatEpam.Portal.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- 3. runtime -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080

COPY --from=publish /app/publish ./

# .attachments is the default Attachments:RootPath; ensure the directory exists
# so the host bind-mount can land cleanly.
RUN mkdir -p /app/.attachments

ENTRYPOINT ["dotnet", "InnovatEpam.Portal.Api.dll"]
