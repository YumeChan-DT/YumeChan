FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

WORKDIR /app


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY /src /src 
WORKDIR /src/YumeChan.ConsoleRunner
RUN dotnet restore "YumeChan.ConsoleRunner.csproj"
RUN dotnet build "YumeChan.ConsoleRunner.csproj" --no-restore -c Release -o /app/build


FROM build AS publish

RUN dotnet publish "YumeChan.ConsoleRunner.csproj" -c Release -o /app/publish


FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YumeChan.ConsoleRunner.dll"]
