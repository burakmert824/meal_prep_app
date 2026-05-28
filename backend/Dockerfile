# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer-cached NuGet restore
COPY MealPrepper.Core/MealPrepper.Core.csproj MealPrepper.Core/
COPY MealPrepper.Infrastructure/MealPrepper.Infrastructure.csproj MealPrepper.Infrastructure/
COPY MealPrepper.API/MealPrepper.API.csproj MealPrepper.API/

RUN dotnet restore MealPrepper.API/MealPrepper.API.csproj

# Copy the rest of the source and publish
COPY MealPrepper.Core/ MealPrepper.Core/
COPY MealPrepper.Infrastructure/ MealPrepper.Infrastructure/
COPY MealPrepper.API/ MealPrepper.API/

RUN dotnet publish MealPrepper.API/MealPrepper.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MealPrepper.API.dll"]
