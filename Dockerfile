FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["InventarioCPR/InventarioCPR.csproj", "InventarioCPR/"]
RUN dotnet restore "InventarioCPR/InventarioCPR.csproj"

COPY . .
RUN dotnet publish "InventarioCPR/InventarioCPR.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:DebugType=None \
    /p:DebugSymbols=false

# A server-rendered page is unusable without the Blazor browser runtime.
RUN test -s /app/publish/wwwroot/_framework/blazor.web.js

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 10000

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
ENTRYPOINT ["dotnet", "InventarioCPR.dll"]
