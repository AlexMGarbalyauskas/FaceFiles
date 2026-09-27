FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FaceFiles.csproj", "."]
RUN dotnet restore "FaceFiles.csproj"

COPY . .
RUN dotnet publish "FaceFiles.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet FaceFiles.dll --urls http://0.0.0.0:${PORT:-10000}"]
