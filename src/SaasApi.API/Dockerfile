FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/SaasApi.API/SaasApi.API.csproj", "src/SaasApi.API/"]
COPY ["src/SaasApi.Application/SaasApi.Application.csproj", "src/SaasApi.Application/"]
COPY ["src/SaasApi.Domain/SaasApi.Domain.csproj", "src/SaasApi.Domain/"]
COPY ["src/SaasApi.Infrastructure/SaasApi.Infrastructure.csproj", "src/SaasApi.Infrastructure/"]
RUN dotnet restore "src/SaasApi.API/SaasApi.API.csproj"
COPY . .
RUN dotnet build "src/SaasApi.API/SaasApi.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/SaasApi.API/SaasApi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SaasApi.API.dll"]
