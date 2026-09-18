# Build from the repo's Api/PointbreakLinksApi/ directory: docker build -f Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY Domain/*.csproj Domain/
COPY Application/*.csproj Application/
COPY Infrastructure/*.csproj Infrastructure/
COPY Identity.Domain/*.csproj Identity.Domain/
COPY Identity.Application/*.csproj Identity.Application/
COPY Identity.Infrastructure/*.csproj Identity.Infrastructure/
COPY WebAPI/*.csproj WebAPI/
COPY Application.Tests/*.csproj Application.Tests/
RUN dotnet restore WebAPI/WebAPI.csproj

COPY . .
RUN dotnet publish WebAPI/WebAPI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The image doesn't bake in a database, JWT secret, or CORS origins — all of that is expected
# to come from the environment (docker-compose.yml, or whatever orchestrator runs this in
# production), same as the documented "don't commit real secrets to appsettings.json" rule in
# PROJECT_MAP.md's quickstart.
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]
