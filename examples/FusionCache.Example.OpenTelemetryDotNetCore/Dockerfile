#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["examples/FusionCache.Example.OpenTelemetryDotNetCore/FusionCache.Example.OpenTelemetryDotNetCore.csproj", "examples/FusionCache.Example.OpenTelemetryDotNetCore/"]
RUN dotnet restore "examples/FusionCache.Example.OpenTelemetryDotNetCore/FusionCache.Example.OpenTelemetryDotNetCore.csproj"
COPY . .
WORKDIR "/src/examples/FusionCache.Example.OpenTelemetryDotNetCore"
RUN dotnet build "FusionCache.Example.OpenTelemetryDotNetCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FusionCache.Example.OpenTelemetryDotNetCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FusionCache.Example.OpenTelemetryDotNetCore.dll"]