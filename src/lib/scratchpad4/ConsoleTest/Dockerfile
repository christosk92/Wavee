FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["../ConsoleTest/ConsoleTest.csproj", "../ConsoleTest/"]
RUN dotnet restore "../ConsoleTest/ConsoleTest.csproj"
COPY . .
WORKDIR "/src/../ConsoleTest"
RUN dotnet build "ConsoleTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleTest.dll"]
