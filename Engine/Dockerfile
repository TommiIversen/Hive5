# Brug det ønskede base image
FROM 2mmilab/ffmpeg6python312gstreamerdeck AS base

# Installer nødvendige værktøjer og .NET runtime
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://dot.net/v1/dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm dotnet-install.sh

# Bekræft installation
RUN dotnet --version

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Engine/Engine.csproj", "Engine/"]
RUN dotnet restore "Engine/Engine.csproj"
COPY . .
WORKDIR "/src/Engine"
RUN dotnet build "Engine.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish image
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Engine.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image med base og runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Engine.dll"]
