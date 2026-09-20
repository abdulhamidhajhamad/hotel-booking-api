# syntax=docker/dockerfile:1.7

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/HotelBooking.Domain/HotelBooking.Domain.csproj                 HotelBooking.Domain/
COPY src/HotelBooking.Application/HotelBooking.Application.csproj       HotelBooking.Application/
COPY src/HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj HotelBooking.Infrastructure/
COPY src/HotelBooking.Presentation/HotelBooking.Presentation.csproj     HotelBooking.Presentation/
RUN dotnet restore HotelBooking.Presentation/HotelBooking.Presentation.csproj

COPY src/ .
RUN dotnet publish HotelBooking.Presentation/HotelBooking.Presentation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "HotelBooking.Presentation.dll"]