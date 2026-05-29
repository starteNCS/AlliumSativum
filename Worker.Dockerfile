FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlliumSativum.Worker/AlliumSativum.Worker.csproj", "AlliumSativum.Worker/"]
COPY ["AlliumSativum.Connectors.PostgreSQL/AlliumSativum.Connectors.PostgreSQL.csproj", "AlliumSativum.Connectors.PostgreSQL/"]
COPY ["AlliumSativum.Connectors.Shared/AlliumSativum.Connectors.Shared.csproj", "AlliumSativum.Connectors.Shared/"]
COPY ["AlliumSativum.Shared/AlliumSativum.Shared.csproj", "AlliumSativum.Shared/"]
COPY ["AlliumSativum.Worker.Sdk/AlliumSativum.Worker.Sdk.csproj", "AlliumSativum.Worker.Sdk/"]
COPY ["AlliumSativum.Worker.Proto/AlliumSativum.Worker.Proto.csproj", "AlliumSativum.Worker.Proto/"]
COPY ["AlliumSativum.Connectors.JsonServer/AlliumSativum.Connectors.JsonServer.csproj", "AlliumSativum.Connectors.JsonServer/"]
COPY ["AlliumSativum.ServiceDefaults/AlliumSativum.ServiceDefaults.csproj", "AlliumSativum.ServiceDefaults/"]
RUN dotnet restore "AlliumSativum.Worker/AlliumSativum.Worker.csproj"
COPY . .
WORKDIR "/src/AlliumSativum.Worker"
RUN dotnet build "./AlliumSativum.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlliumSativum.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlliumSativum.Worker.dll"]
