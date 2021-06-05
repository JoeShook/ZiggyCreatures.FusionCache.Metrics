#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["examples/EventCountersPluginExampleDotNetCore/EventCountersPluginExampleDotNetCore.csproj", "examples/EventCountersPluginExampleDotNetCore/"]
RUN dotnet restore "examples/EventCountersPluginExampleDotNetCore/EventCountersPluginExampleDotNetCore.csproj"
COPY . .
WORKDIR "/src/examples/EventCountersPluginExampleDotNetCore"
RUN dotnet build "EventCountersPluginExampleDotNetCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventCountersPluginExampleDotNetCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventCountersPluginExampleDotNetCore.dll"]