FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["../BassTest/BassTest.csproj", "../BassTest/"]
RUN dotnet restore "../BassTest/BassTest.csproj"
COPY . .
WORKDIR "/src/../BassTest"
RUN dotnet build "BassTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BassTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BassTest.dll"]
