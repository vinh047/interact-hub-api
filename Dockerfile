# ==========================================
# STAGE 1: BUILD BACKEND VỚI .NET SDK
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Chui vào folder con để copy đúng file .csproj
COPY ["InteractHub.Api/InteractHub.Api.csproj", "InteractHub.Api/"]

# 2. Restore các thư viện
RUN dotnet restore "InteractHub.Api/InteractHub.Api.csproj"

# 3. Copy toàn bộ mã nguồn còn lại ở thư mục gốc vào
COPY . .

# 4. Trỏ thư mục làm việc vào đúng folder chứa code API để build
WORKDIR "/src/InteractHub.Api"
RUN dotnet publish "InteractHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 2: CHẠY APP VỚI .NET RUNTIME
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Mở port mặc định của .NET 10 Docker
EXPOSE 8080

# Copy các file đã build từ Stage 1 sang
COPY --from=build /app/publish .

# Khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "InteractHub.Api.dll"]