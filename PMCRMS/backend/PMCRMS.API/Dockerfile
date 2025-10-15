# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY PMCRMS/backend/PMCRMS.API/PMCRMS.API.csproj ./PMCRMS.API/
RUN dotnet restore "./PMCRMS.API/PMCRMS.API.csproj"

# Copy everything else and build
COPY PMCRMS/backend/PMCRMS.API/. ./PMCRMS.API/
RUN dotnet build "./PMCRMS.API/PMCRMS.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "./PMCRMS.API/PMCRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install fontconfig and Devanagari fonts for PDF generation
RUN apt-get update && apt-get install -y \
    fontconfig \
    fonts-noto-core \
    fonts-noto-ui-core \
    fonts-indic \
    && fc-cache -f -v \
    && rm -rf /var/lib/apt/lists/*

# Create directories for logs and uploads
RUN mkdir -p /app/logs /app/uploads

EXPOSE 8080

COPY --from=publish /app/publish .

# Set environment variable for ASP.NET Core to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PMCRMS.API.dll"]
