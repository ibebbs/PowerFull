FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["lib/", "lib/"]
COPY ["PowerFull/PowerFull.csproj", "PowerFull/"]
COPY ["nuget.config", "PowerFull/"]
RUN dotnet restore "PowerFull/PowerFull.csproj"
COPY . .
WORKDIR "/src/PowerFull"
RUN dotnet build "PowerFull.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PowerFull.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PowerFull.dll"]