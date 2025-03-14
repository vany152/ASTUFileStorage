FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# устанавливаем curl для проверки состояния через healthcheck
RUN apt-get update && apt-get install -y curl
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FileStorage/FileStorage.csproj", "FileStorage/"]
RUN dotnet restore "FileStorage/FileStorage.csproj"
WORKDIR "/src/FileStorage"
COPY FileStorage/ . 
RUN dotnet build "FileStorage.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FileStorage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStorage.dll"]
