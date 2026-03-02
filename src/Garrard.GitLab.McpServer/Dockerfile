# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY git-tool.sln ./
COPY LICENSE ./
COPY src/Garrard.GitLab/Garrard.GitLab.csproj src/Garrard.GitLab/
COPY src/Garrard.GitLab.McpServer/Garrard.GitLab.McpServer.csproj src/Garrard.GitLab.McpServer/
COPY src/Garrard.GitLab.Sample/Garrard.GitLab.Sample.csproj src/Garrard.GitLab.Sample/
COPY tests/Garrard.GitLab.Tests/Garrard.GitLab.Tests.csproj tests/Garrard.GitLab.Tests/
COPY tests/Garrard.GitLab.McpServer.Tests/Garrard.GitLab.McpServer.Tests.csproj tests/Garrard.GitLab.McpServer.Tests/
RUN dotnet restore src/Garrard.GitLab.McpServer/Garrard.GitLab.McpServer.csproj

COPY src/Garrard.GitLab/ src/Garrard.GitLab/
COPY src/Garrard.GitLab.McpServer/ src/Garrard.GitLab.McpServer/
RUN dotnet publish src/Garrard.GitLab.McpServer/Garrard.GitLab.McpServer.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Garrard.GitLab.McpServer.dll"]
