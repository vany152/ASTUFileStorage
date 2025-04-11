FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# устанавливаем curl для проверки состояния через healthcheck
USER root
RUN apt-get update && apt-get install -y curl

ARG INTERNAL_HTTP_PORT=80
ARG INTERNAL_HTTPS_PORT=443
EXPOSE ${INTERNAL_HTTP_PORT}
EXPOSE ${INTERNAL_HTTPS_PORT}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["FileStorage/FileStorage.csproj", "FileStorage/"]
RUN dotnet restore "FileStorage/FileStorage.csproj"
COPY . . 
WORKDIR "/src/FileStorage"
RUN dotnet build "FileStorage.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FileStorage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

ARG PEM_CERTIFICATE_FILE_HOST_PATH
USER root
WORKDIR /usr/local/share/ca-certificates
COPY ["${PEM_CERTIFICATE_FILE_HOST_PATH}", "./"]
RUN update-ca-certificates

USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStorage.dll"]
