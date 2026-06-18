@echo off
setlocal EnableExtensions

cd /d "%~dp0"
title Mentora - Docker Durdur

echo =====================================================
echo Mentora Docker stop script
echo =====================================================
echo.

where docker >nul 2>nul
if errorlevel 1 (
    echo [HATA] Docker bulunamadi.
    pause
    exit /b 1
)

docker info >nul 2>nul
if errorlevel 1 (
    echo [HATA] Docker servisi calismiyor. Docker Desktop acik olmali.
    pause
    exit /b 1
)

docker compose down --remove-orphans
if errorlevel 1 (
    echo [HATA] Containerlar durdurulamadi.
    pause
    exit /b 1
)

echo [OK] Mentora Docker containerlari durduruldu.
exit /b 0
