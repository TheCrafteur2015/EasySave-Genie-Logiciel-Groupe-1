FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LogServer/LogServer.csproj", "LogServer/"]
RUN dotnet restore "LogServer/LogServer.csproj"
COPY . .
WORKDIR "/src/LogServer"
RUN dotnet build "LogServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LogServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

ENV ASPNETCORE_URLS=http://+:5000
ENV LogDirectory=/app/logs

ENTRYPOINT ["dotnet", "LogServer.dll"]
