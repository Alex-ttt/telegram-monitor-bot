FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TelegramBotExample/TelegramBotExample.csproj", "TelegramBotExample/"]
RUN dotnet restore "TelegramBotExample/TelegramBotExample.csproj"
COPY . .
WORKDIR "/src/TelegramBotExample"
RUN dotnet build "TelegramBotExample.csproj" -c Release -o /app/build

# Set the .NET runtime location
ENV DOTNET_ROOT=/usr/lib/dotnet

FROM build AS publish
RUN dotnet publish "TelegramBotExample.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TelegramBotExample.dll"]
