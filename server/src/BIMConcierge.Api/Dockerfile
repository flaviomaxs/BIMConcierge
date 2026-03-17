FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BIMConcierge.Api/BIMConcierge.Api.csproj", "src/BIMConcierge.Api/"]
RUN dotnet restore "src/BIMConcierge.Api/BIMConcierge.Api.csproj"
COPY . .
WORKDIR "/src/src/BIMConcierge.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BIMConcierge.Api.dll"]
