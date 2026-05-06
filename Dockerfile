# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the csproj from the current folder and restore
COPY Lasamify.csproj .
RUN dotnet restore

# Copy everything else from the current folder and publish
COPY . .
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Ensure the database is included
COPY lasamify.db .

# Vercel likes port 3000
ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000

ENTRYPOINT ["dotnet", "Lasamify.dll"]