# syntax=docker/dockerfile:1
# ------------------------------------------------------------------------------
# RaceDay MVC web application (Part 3)
# Multi-stage build: the SDK image compiles the app, the much smaller ASP.NET
# runtime image runs it as a non-root user.
# Build context = repository root:   docker build -t raceday-web .
# ------------------------------------------------------------------------------

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the (slow) restore step is cached
# until the dependencies actually change.
COPY RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/
RUN dotnet restore RaceDay.Web/RaceDay.Web.csproj

COPY RaceDay.Web/ RaceDay.Web/
RUN dotnet publish RaceDay.Web/RaceDay.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Where the MVC app finds the RaceDay API. Override at run time with  -e Api__BaseUrl=...
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Api__BaseUrl=http://host.docker.internal:5100

EXPOSE 8080
COPY --from=build /app/publish .

# Never run as root inside the container (the "app" user is built into the .NET 8 images).
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
