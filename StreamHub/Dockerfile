FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# ENV ASPNETCORE_ENVIRONMENT=Production
# ENV ASPNETCORE_HTTP_PORTS=http://+:9001
# ENV DetailedErrors=true

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["StreamHub/StreamHub.csproj", "StreamHub/"]
RUN dotnet restore "StreamHub/StreamHub.csproj"
COPY . .
WORKDIR "/src/StreamHub"
RUN dotnet build "StreamHub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "StreamHub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StreamHub.dll"]
