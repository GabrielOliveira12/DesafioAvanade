FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/EstoqueService/EstoqueService.csproj", "src/EstoqueService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/EstoqueService/EstoqueService.csproj"
COPY . .
WORKDIR "/src/src/EstoqueService"
RUN dotnet build "EstoqueService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EstoqueService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EstoqueService.dll"]
