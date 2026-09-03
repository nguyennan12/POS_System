FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Enable polling file watcher for Windows/WSL2 Docker host file changes
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/POS.Api/POS.Api.csproj", "--urls", "http://0.0.0.0:8080", "--non-interactive"]
