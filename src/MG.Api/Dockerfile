FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first for better layer caching
COPY ["MG.Api/MG.Api.csproj", "MG.Api/"]
COPY ["MG.Models/MG.Models.csproj", "MG.Models/"]
COPY ["MG.Services/MG.Services.csproj", "MG.Services/"]

# Restore dependencies
RUN dotnet restore "MG.Api/MG.Api.csproj"

# Copy all source code
COPY . .

# Build the project
WORKDIR "/src/MG.Api"
RUN dotnet build "MG.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MG.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MG.Api.dll"]
