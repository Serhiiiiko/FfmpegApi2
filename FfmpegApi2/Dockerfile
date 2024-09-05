FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ffmpeg \
        python3 \
        python3-venv \
        curl \
        wget \
        unzip \
        libgfortran5 && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FfmpegApi2/FfmpegApi2.csproj", "FfmpegApi2/"]
RUN dotnet restore "./FfmpegApi2/FfmpegApi2.csproj"
COPY . .
WORKDIR "/src/FfmpegApi2"
RUN dotnet build "./FfmpegApi2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./FfmpegApi2.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FfmpegApi2.dll"]