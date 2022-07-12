FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src

COPY . ./
RUN dotnet restore src
RUN dotnet publish src -c Release -o out

ENTRYPOINT [ "dotnet", "/src/out/frankie-bot.dll" ]