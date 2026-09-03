FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/POS.Api/POS.Api.csproj", "src/POS.Api/"]
COPY ["src/POS.Application/POS.Application.csproj", "src/POS.Application/"]
COPY ["src/POS.Domain/POS.Domain.csproj", "src/POS.Domain/"]
COPY ["src/POS.Infrastructure/POS.Infrastructure.csproj", "src/POS.Infrastructure/"]
COPY ["src/POS.Contracts/POS.Contracts.csproj", "src/POS.Contracts/"]
RUN dotnet restore "src/POS.Api/POS.Api.csproj"
COPY . .
WORKDIR "/src/src/POS.Api"
RUN dotnet build "POS.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "POS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "POS.Api.dll"]
