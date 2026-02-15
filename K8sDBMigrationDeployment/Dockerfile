# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /src
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TaskManagementApi/TaskManagementApi.csproj", "TaskManagementApi/"]
RUN dotnet restore "TaskManagementApi/TaskManagementApi.csproj"
COPY TaskManagementApi/ TaskManagementApi/
WORKDIR "/src/TaskManagementApi"
RUN dotnet build "TaskManagementApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TaskManagementApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - for running the API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManagementApi.dll"]

# Migration stage - for running EF Core migrations
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migration
WORKDIR /src

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef --version 10.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy the project files (needed for EF Core)
COPY ["TaskManagementApi/TaskManagementApi.csproj", "TaskManagementApi/"]
RUN dotnet restore "TaskManagementApi/TaskManagementApi.csproj"

# Copy source code and build
COPY TaskManagementApi/ TaskManagementApi/
WORKDIR "/src/TaskManagementApi"
RUN dotnet build "TaskManagementApi.csproj" -c Release -o /app/build

# Set working directory and entry point
WORKDIR /src/TaskManagementApi
ENTRYPOINT ["dotnet", "ef", "database", "update"]