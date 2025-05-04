# --- Etapa 1: Build ---
# Usar la imagen oficial del SDK de .NET 8 (ajusta la versión si usas otra) como base para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos .csproj y restaurar dependencias primero
# Esto aprovecha el cache de Docker. Si no cambian los .csproj, no se vuelve a descargar todo.
COPY ["TaskFlowApi/TaskFlowApi.csproj", "TaskFlowApi/"]
# Si tuvieras otros proyectos referenciados, cópialos también aquí
# COPY ["OtraLibreria/OtraLibreria.csproj", "OtraLibreria/"]
RUN dotnet restore "TaskFlowApi/TaskFlowApi.csproj"

# Copiar el resto del código fuente del proyecto principal
COPY ["TaskFlowApi/", "TaskFlowApi/"]
# Si tuvieras otros proyectos, copia su código también
# COPY ["OtraLibreria/", "OtraLibreria/"]

WORKDIR "/src/TaskFlowApi"
# Compilar y publicar la aplicación en modo Release
RUN dotnet publish "TaskFlowApi.csproj" -c Release -o /app/publish --no-restore

# --- Etapa 2: Final Runtime Image ---
# Usar la imagen de runtime de ASP.NET Core 8, que es más pequeña que el SDK
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Exponer el puerto en el que escuchará la API DENTRO del contenedor.
# Usaremos 8080 para HTTP y 8081 para HTTPS (puedes ajustar o quitar HTTPS si no lo configuraste)
EXPOSE 8080
EXPOSE 8081

# Configurar las URLs en las que escuchará Kestrel dentro del contenedor
# Escucha en ambos puertos, HTTP y HTTPS (si aplica)
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081

# Copiar la salida publicada de la etapa de 'build' a la imagen final
COPY --from=build /app/publish .

# Punto de entrada: Comando para ejecutar la aplicación cuando el contenedor inicie
ENTRYPOINT ["dotnet", "TaskFlowApi.dll"]