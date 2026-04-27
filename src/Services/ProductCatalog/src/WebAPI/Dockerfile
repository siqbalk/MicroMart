# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/ProductCatalog/src/WebAPI/MicroMart.ProductCatalog.csproj", "src/Services/ProductCatalog/src/WebAPI/"]
COPY ["MicroMart.ProductCatalog.Application/MicroMart.ProductCatalog.Application.csproj", "MicroMart.ProductCatalog.Application/"]
COPY ["MicroMart.ProductCatalog.Domain/MicroMart.ProductCatalog.Domain.csproj", "MicroMart.ProductCatalog.Domain/"]
COPY ["src/Shared/MicroMart.Shared.Core/MicroMart.Shared.Core.csproj", "src/Shared/MicroMart.Shared.Core/"]
COPY ["MicroMart.ProductCatalog.Infrastructure/MicroMart.ProductCatalog.Infrastructure.csproj", "MicroMart.ProductCatalog.Infrastructure/"]
RUN dotnet restore "./src/Services/ProductCatalog/src/WebAPI/MicroMart.ProductCatalog.csproj"
COPY . .
WORKDIR "/src/src/Services/ProductCatalog/src/WebAPI"
RUN dotnet build "./MicroMart.ProductCatalog.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MicroMart.ProductCatalog.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MicroMart.ProductCatalog.dll"]