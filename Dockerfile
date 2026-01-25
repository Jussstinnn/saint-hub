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

# ✅ Uploads: Render/Docker suelen correr con user no-root y el filesystem es case-sensitive.
# Creamos la carpeta y damos permisos para que los uploads nuevos (WEBP/AVIF/JPG/PNG) se guarden bien.
USER root
RUN mkdir -p /app/wwwroot/uploads/products && chmod -R 777 /app/wwwroot/uploads

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SaintHub.dll"]
