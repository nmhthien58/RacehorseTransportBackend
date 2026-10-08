# Image chạy API. Railway tự build file này khi deploy từ GitHub.

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Chép riêng các file .csproj trước để lớp "restore" được cache lại khi chỉ sửa code
COPY RacehorseTransport.API/RacehorseTransport.API.csproj RacehorseTransport.API/
COPY RacehorseTransport.Services/RacehorseTransport.Services.csproj RacehorseTransport.Services/
COPY RacehorseTransport.Repositories/RacehorseTransport.Repositories.csproj RacehorseTransport.Repositories/
RUN dotnet restore RacehorseTransport.API/RacehorseTransport.API.csproj

COPY . .
RUN dotnet publish RacehorseTransport.API/RacehorseTransport.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway cấp cổng qua biến PORT; chạy ở máy thì mặc định 8080
ENTRYPOINT ["sh", "-c", "exec dotnet RacehorseTransport.API.dll --urls http://0.0.0.0:${PORT:-8080}"]
