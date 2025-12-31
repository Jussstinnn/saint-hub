# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos solo el csproj y restauramos
COPY SaintHub.csproj ./
RUN dotnet restore SaintHub.csproj

# Copiamos el resto del proyecto
COPY . ./

# 🔴 PUBLICAMOS EXPLÍCITAMENTE EL CSPROJ
RUN dotnet publish SaintHub.csproj -c Release -o out

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SaintHub.dll"]
