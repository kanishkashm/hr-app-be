FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj", "src/TravelPax.Workforce.Api/"]
COPY ["src/TravelPax.Workforce.Application/TravelPax.Workforce.Application.csproj", "src/TravelPax.Workforce.Application/"]
COPY ["src/TravelPax.Workforce.Domain/TravelPax.Workforce.Domain.csproj", "src/TravelPax.Workforce.Domain/"]
COPY ["src/TravelPax.Workforce.Infrastructure/TravelPax.Workforce.Infrastructure.csproj", "src/TravelPax.Workforce.Infrastructure/"]
COPY ["src/TravelPax.Workforce.Contracts/TravelPax.Workforce.Contracts.csproj", "src/TravelPax.Workforce.Contracts/"]
RUN dotnet restore "src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj"

COPY . .
RUN dotnet publish "src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TravelPax.Workforce.Api.dll"]
