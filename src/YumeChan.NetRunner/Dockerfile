FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS deps

# Install Node.js and NPM for TypeScript compilation dependencies
RUN apt-get update -qq && apt-get install nodejs -qq

#RUN apt-get update -yq && apt-get upgrade -yq \
#    && apt-get install -yq curl git nano \
#    && curl -sL https://deb.nodesource.com/setup_8.x | bash - \
#    && apt-get install -yq nodejs build-essential \
#    && npm install -g npm && npm install


FROM deps AS build
COPY /src /src
WORKDIR /src/YumeChan.NetRunner
RUN dotnet restore "YumeChan.NetRunner.csproj"
RUN dotnet build "YumeChan.NetRunner.csproj" --no-restore -c Release -o /app/build

FROM build AS publish
WORKDIR /src/YumeChan.NetRunner
RUN dotnet publish "YumeChan.NetRunner.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YumeChan.NetRunner.dll"]
