# Điền chuỗi kết nối database Supabase vào appsettings.Local.json và .env (hai file này không bị commit).
# Mật khẩu được gõ trực tiếp vào cửa sổ này, không hiện lên màn hình và không lưu ở đâu khác.
#
# Cách chạy (từ thư mục Backend):   pwsh scripts/set-supabase-connection.ps1
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent),
    [string]$PoolerHost,
    [string]$Password
)

$ErrorActionPreference = 'Stop'
$localFile = Join-Path $Root 'RacehorseTransport.API\appsettings.Local.json'
$envFile = Join-Path $Root '.env'

if (-not (Test-Path $localFile)) { throw "Không thấy $localFile. Copy appsettings.Local.example.json thành appsettings.Local.json trước." }

$local = Get-Content $localFile -Raw | ConvertFrom-Json
$projectRef = ([uri]$local.Supabase.Url).Host.Split('.')[0]

Write-Host "Project Supabase: $projectRef"
Write-Host "Lấy host ở dashboard: nút Connect -> mục 'Session pooler' (dạng aws-...pooler.supabase.com)."
Write-Host "KHÔNG dùng host db.$projectRef.supabase.co của mục 'Direct connection': nó chỉ có IPv6." -ForegroundColor Yellow

if (-not $PoolerHost) { $PoolerHost = (Read-Host 'Host Session pooler').Trim() }
if ($PoolerHost -notmatch 'pooler\.supabase\.com$') { throw "Host '$PoolerHost' không phải host Session pooler (phải kết thúc bằng pooler.supabase.com)." }
if (-not $Password) { $Password = Read-Host 'Mật khẩu database' -MaskInput }
if ([string]::IsNullOrWhiteSpace($Password)) { throw 'Mật khẩu trống.' }

# Trong chuỗi kết nối, giá trị có ký tự đặc biệt phải đặt trong nháy đơn (nháy đơn bên trong thì gấp đôi)
$quotedPassword = "'" + $Password.Replace("'", "''") + "'"
$connection = "Host=$PoolerHost;Port=5432;Database=postgres;Username=postgres.$projectRef;Password=$quotedPassword;SSL Mode=Require;Trust Server Certificate=true"

# 1. appsettings.Local.json
$local.ConnectionStrings.Supabase = $connection
$local | ConvertTo-Json -Depth 10 | Set-Content $localFile -Encoding utf8
Write-Host "Đã ghi ConnectionStrings:Supabase vào appsettings.Local.json" -ForegroundColor Green

# 2. .env (nếu có): thay cả dòng ConnectionStrings__DefaultConnection
if (Test-Path $envFile) {
    $envValue = '"' + $connection.Replace('\', '\\').Replace('"', '\"') + '"'
    $lines = Get-Content $envFile | ForEach-Object {
        if ($_ -like 'ConnectionStrings__DefaultConnection=*') { "ConnectionStrings__DefaultConnection=$envValue" } else { $_ }
    }
    Set-Content $envFile $lines -Encoding utf8
    Write-Host "Đã ghi ConnectionStrings__DefaultConnection vào .env" -ForegroundColor Green
}

Write-Host ""
Write-Host "Chạy thử:  dotnet run --project RacehorseTransport.API -- --Database:Use Supabase"
Write-Host "Rồi mở http://localhost:5080/health (database phải là true)."
