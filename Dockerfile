FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj", "src/TravelPax.Workforce.Api/"]
COPY ["src/TravelPax.Workforce.Application/TravelPax.Workforce.Application.csproj", "src/TravelPax.Workforce.Application/"]
COPY ["src/TravelPax.Workforce.Domain/TravelPax.Workforce.Domain.csproj", "src/TravelPax.Workforce.Domain/"]
COPY ["src/TravelPax.Workforce.Contracts/TravelPax.Workforce.Contracts.csproj", "src/TravelPax.Workforce.Contracts/"]
COPY ["src/TravelPax.Workforce.Infrastructure/TravelPax.Workforce.Infrastructure.csproj", "src/TravelPax.Workforce.Infrastructure/"]

RUN dotnet restore "src/TravelPax.Workforce.Api/TravelPax.Workforce.Api.csproj"

COPY . .
WORKDIR "/src/src/TravelPax.Workforce.Api"
RUN dotnet publish "TravelPax.Workforce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TravelPax.Workforce.Api.dll"]
