# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./WebAppApiPhim/
WORKDIR /src/WebAppApiPhim
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/out .
COPY appsettings.json .

COPY appsettings.Production.json .

EXPOSE 80
ENTRYPOINT ["dotnet", "WebAppApiPhim.dll"]