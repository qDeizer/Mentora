@echo off
setlocal EnableExtensions

cd /d "%~dp0"
title Mentora - Docker Baslat

echo =====================================================
echo Mentora Docker startup script
echo =====================================================
echo.

where docker >nul 2>nul
if errorlevel 1 (
    echo [HATA] Docker bulunamadi. Once Docker Desktop kurmalisin.
    pause
    exit /b 1
)

docker info >nul 2>nul
if errorlevel 1 (
    echo [HATA] Docker servisi calismiyor. Docker Desktop acik olmali.
    pause
    exit /b 1
)

if not exist ".env" (
    if exist ".env.example" (
        copy /Y ".env.example" ".env" >nul
        echo [INFO] .env dosyasi .env.example uzerinden olusturuldu.
    ) else (
        echo [HATA] .env.example bulunamadi.
        pause
        exit /b 1
    )
)

echo [1/3] Docker image ve containerlar hazirlaniyor...
docker compose up -d --build
if errorlevel 1 goto :error

echo [2/3] Uygulamanin ayaga kalkmasi bekleniyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ok = $false; for($i = 0; $i -lt 90; $i++){ try { $r = Invoke-WebRequest -Uri 'http://localhost:8080/health/ready' -UseBasicParsing -TimeoutSec 2; if($r.StatusCode -eq 200){ $ok = $true; break } } catch {}; Start-Sleep -Seconds 1 }; if($ok){ Write-Host '[OK] Mentora hazir.'; exit 0 } else { Write-Host '[UYARI] health/ready henuz 200 donmedi. Yine de aciliyor...'; exit 0 }"

echo [3/3] Tarayici aciliyor...
start "" "http://localhost:8080"

echo.
echo [BILGI] Durdurmak icin: stop-mentora-docker.bat
echo [BILGI] Loglar icin: docker compose logs -f app
echo.
exit /b 0

:error
echo.
echo [HATA] Docker startup asamalarindan biri basarisiz oldu.
echo [BILGI] Sorun tespiti icin: docker compose logs --tail=200
pause
exit /b 1
