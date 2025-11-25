# build stage utilise .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier tout le dossier du projet
COPY . .

# Restore et publish
RUN dotnet restore "Panier.API.csproj"
RUN dotnet publish "Panier.API.csproj" -c Release -o /app/publish

# runtime stage runtime .NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Panier.API.dll"]
