FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlliumSativum.QueryServer/AlliumSativum.QueryServer.csproj", "AlliumSativum.QueryServer/"]
COPY ["AlliumSativum.QueryPlanner/AlliumSativum.QueryPlanner.csproj", "AlliumSativum.QueryPlanner/"]
COPY ["AlliumSativum.Worker.Sdk/AlliumSativum.Worker.Sdk.csproj", "AlliumSativum.Worker.Sdk/"]
COPY ["AlliumSativum.Worker.Proto/AlliumSativum.Worker.Proto.csproj", "AlliumSativum.Worker.Proto/"]
COPY ["AlliumSativum.Shared/AlliumSativum.Shared.csproj", "AlliumSativum.Shared/"]
COPY ["AlliumSativum.ServiceDefaults/AlliumSativum.ServiceDefaults.csproj", "AlliumSativum.ServiceDefaults/"]
COPY ["AlliumSativum.QueryExecutor/AlliumSativum.QueryExecutor.csproj", "AlliumSativum.QueryExecutor/"]
COPY ["AlliumSativum.QueryPerformance/AlliumSativum.QueryPerformance.csproj", "AlliumSativum.QueryPerformance/"]
RUN dotnet restore "AlliumSativum.QueryServer/AlliumSativum.QueryServer.csproj"
COPY . .
WORKDIR "/src/AlliumSativum.QueryServer"
RUN dotnet build "./AlliumSativum.QueryServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlliumSativum.QueryServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlliumSativum.QueryServer.dll"]
