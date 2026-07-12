# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the project file first (better layer caching)
COPY *.csproj .
RUN dotnet restore

# Copy the rest of the source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the application (smaller image, no SDK needed)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8888
ENV ASPNETCORE_URLS=http://+:8888

ENTRYPOINT ["dotnet", "PowerPlantChallenge.dll"]