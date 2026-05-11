FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CVDesktopEditor.slnx ./
COPY server/CVDesktopEditor.Api/CVDesktopEditor.Api.csproj server/CVDesktopEditor.Api/
RUN dotnet restore server/CVDesktopEditor.Api/CVDesktopEditor.Api.csproj

COPY database database
COPY server/CVDesktopEditor.Api server/CVDesktopEditor.Api
RUN dotnet publish server/CVDesktopEditor.Api/CVDesktopEditor.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet CVDesktopEditor.Api.dll
