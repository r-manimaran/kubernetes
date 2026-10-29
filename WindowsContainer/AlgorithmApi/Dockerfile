# Stage 1 — Restore and Build
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
WORKDIR /src

COPY packages/ ./packages/
COPY AlgorithmApi/ ./AlgorithmApi/

WORKDIR /src/AlgorithmApi
RUN nuget restore AlgorithmApi.csproj -PackagesDirectory ../packages
RUN msbuild AlgorithmApi.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:PublishUrl=C:\publish

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
WORKDIR /inetpub/wwwroot
COPY --from=build C:\publish .
