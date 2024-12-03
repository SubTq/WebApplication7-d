# Pobieranie obrazu bazowego .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Kopiowanie pliku projektu .csproj
COPY WebApplication7-masterAktualizacja/WebApplication7/*.csproj ./

# Przywracanie zależności
RUN dotnet restore

# Kopiowanie pozostałych plików projektu
COPY WebApplication7-masterAktualizacja/WebApplication7/ ./

# Budowanie aplikacji
RUN dotnet publish -c Release -o out

# Pobieranie obrazu runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-env /app/out .

# Ustawienie domyślnej komendy uruchamiającej aplikację
ENTRYPOINT ["dotnet", "WebApplication7.dll"]
