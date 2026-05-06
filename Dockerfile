# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Lasamify/Lasamify.csproj", "Lasamify/"]
RUN dotnet restore "Lasamify/Lasamify.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Lasamify"
RUN dotnet build "Lasamify.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "Lasamify.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# IMPORTANT: Copy your SQLite database into the image
COPY Lasamify/lasamify.db .

# Set the port for Vercel (Vercel usually expects port 3000 or 8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Lasamify.dll"]