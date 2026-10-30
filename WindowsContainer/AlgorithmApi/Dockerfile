# escape=`
# Stage 1 - Restore and Build
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022 AS build
WORKDIR /src

COPY packages/ ./packages/
COPY AlgorithmApi/ ./AlgorithmApi/

WORKDIR /src/AlgorithmApi
RUN nuget restore AlgorithmApi.csproj -PackagesDirectory ../packages
RUN msbuild AlgorithmApi.csproj `
    /p:Configuration=Release `
    /p:DeployOnBuild=true `
    /p:WebPublishMethod=FileSystem `
    /p:PublishUrl=C:\publish `
    /p:DeleteExistingFiles=True

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022
WORKDIR /inetpub/wwwroot
COPY --from=build C:\src\AlgorithmApi\obj\Release\Package\PackageTmp .
