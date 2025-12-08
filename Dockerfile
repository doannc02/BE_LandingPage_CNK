FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/NunchakuClub.API/NunchakuClub.API.csproj", "src/NunchakuClub.API/"]
COPY ["src/NunchakuClub.Application/NunchakuClub.Application.csproj", "src/NunchakuClub.Application/"]
COPY ["src/NunchakuClub.Infrastructure/NunchakuClub.Infrastructure.csproj", "src/NunchakuClub.Infrastructure/"]
COPY ["src/NunchakuClub.Domain/NunchakuClub.Domain.csproj", "src/NunchakuClub.Domain/"]
RUN dotnet restore "src/NunchakuClub.API/NunchakuClub.API.csproj"

COPY . .
WORKDIR "/src/src/NunchakuClub.API"
RUN dotnet build "NunchakuClub.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NunchakuClub.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NunchakuClub.API.dll"]
