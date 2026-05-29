FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GlobalGo.sln ./
COPY GlobalGo.Api/GlobalGo.Api.csproj GlobalGo.Api/
COPY GlobalGo.Application/GlobalGo.Application.csproj GlobalGo.Application/
COPY GlobalGo.Domain/GlobalGo.Domain.csproj GlobalGo.Domain/
COPY GlobalGo.Infrastructure/GlobalGo.Infrastructure.csproj GlobalGo.Infrastructure/
RUN dotnet restore GlobalGo.Api/GlobalGo.Api.csproj

COPY . .
RUN dotnet publish GlobalGo.Api/GlobalGo.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GlobalGo.Api.dll"]
