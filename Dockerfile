FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["httpdump/httpdump.csproj", "httpdump/"]
RUN dotnet restore "httpdump/httpdump.csproj"
COPY . .
WORKDIR "/src/httpdump"

RUN dotnet build "httpdump.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "httpdump.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS http://*:80

ENTRYPOINT ["dotnet", "httpdump.dll"]
