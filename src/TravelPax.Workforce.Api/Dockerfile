# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj", "src/TravelPax.Workforce.Api/"]
COPY ["src/TravelPax.Workforce.Application/TravelPax.Workforce.Application.csproj", "src/TravelPax.Workforce.Application/"]
COPY ["src/TravelPax.Workforce.Domain/TravelPax.Workforce.Domain.csproj", "src/TravelPax.Workforce.Domain/"]
COPY ["src/TravelPax.Workforce.Contracts/TravelPax.Workforce.Contracts.csproj", "src/TravelPax.Workforce.Contracts/"]
COPY ["src/TravelPax.Workforce.Infrastructure/TravelPax.Workforce.Infrastructure.csproj", "src/TravelPax.Workforce.Infrastructure/"]
RUN dotnet restore "./src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj"
COPY . .
WORKDIR "/src/src/TravelPax.Workforce.Api"
RUN dotnet build "./TravelPax.Workforce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TravelPax.Workforce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelPax.Workforce.Api.dll"]