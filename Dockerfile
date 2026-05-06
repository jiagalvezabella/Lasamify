FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the csproj and restore (no subfolders needed)
COPY Lasamify.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
COPY lasamify.db .

# Permission fix for SQLite on Linux
RUN chmod 666 lasamify.db

ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000

ENTRYPOINT ["dotnet", "Lasamify.dll"]