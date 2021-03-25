#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine3.12 AS base
WORKDIR /app

ENV \
    # Set the keep alive url
    KEEP_ALIVE_URL= \
    # Set the default delay
    KEEP_ALIVE_DELAY=5 \
    # Is the Url a site map
    URL_IS_SITEMAP=0 

VOLUME ["/keepaliveconfig"]

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.302-alpine3.12 AS build
WORKDIR /src
COPY ["KeepAlive/KeepAlive.csproj", "KeepAlive/"]
RUN dotnet restore "KeepAlive/KeepAlive.csproj"
COPY . .
WORKDIR "/src/KeepAlive"
RUN dotnet build "KeepAlive.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KeepAlive.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeepAlive.dll"]

