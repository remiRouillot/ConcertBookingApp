#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM registry.access.redhat.com/ubi8/dotnet-80 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM registry.access.redhat.com/ubi8/dotnet-80 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ConcertBookingApp.csproj", "."]
RUN dotnet restore "./ConcertBookingApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ConcertBookingApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConcertBookingApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConcertBookingApp.dll"]