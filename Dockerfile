# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
# COPY *.sln .
COPY snsrpi-device/*.csproj .
RUN dotnet restore

# copy everything else and build app
COPY snsrpi-device/ .
COPY snsrpi-device/config /config
WORKDIR /src
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["./snsrpi-device"]