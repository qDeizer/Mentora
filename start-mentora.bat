@echo off
setlocal EnableExtensions

cd /d "%~dp0"
title Mentora - Baslat

if exist ".env" (
    echo [INFO] .env yukleniyor...
    for /f "usebackq tokens=1,* delims==" %%A in (`findstr /v "^#" ".env" ^| findstr /v "^$"`) do (
        set "%%A=%%B"
    )
)

echo =====================================================
echo Mentora LOCAL startup script
echo =====================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [HATA] .NET SDK bulunamadi. Once .NET 8 SDK kurmalisin.
    pause
    exit /b 1
)

echo [0/4] Eski Mentora surecleri kapatiliyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$targets = Get-CimInstance Win32_Process | Where-Object { ($_.Name -ieq 'PsikologProje_Void.exe') -or ($_.Name -ieq 'dotnet.exe' -and $_.CommandLine -match 'PsikologProje_Void(\\.csproj|\\.dll)') }; foreach ($p in $targets) { try { Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop; Write-Host ('[INFO] Kapatildi PID=' + $p.ProcessId) } catch {} }"
taskkill /F /IM PsikologProje_Void.exe >nul 2>nul
timeout /t 1 /nobreak >nul

if not exist "appsettings.Development.json" (
    if exist "appsettings.Development.sample.json" (
        copy /Y "appsettings.Development.sample.json" "appsettings.Development.json" >nul
        echo [INFO] appsettings.Development.json olusturuldu.
    ) else (
        echo [UYARI] appsettings.Development.sample.json bulunamadi.
        echo [UYARI] Varsayilan appsettings.json ile devam edilecek.
    )
)

echo [1/3] Proje derleniyor (restore dahil)...
dotnet build -c Debug
if errorlevel 1 goto :error

echo [2/3] Uygulama baslatiliyor...
echo [INFO] Cikis yapmak icin CTRL+C kullan.
echo [INFO] Adres: http://localhost:5000
echo.
dotnet run --no-build --project "PsikologProje_Void.csproj"
if errorlevel 1 goto :error

goto :eof

:error
echo.
echo [HATA] Startup asamalarindan biri basarisiz oldu.
pause
exit /b 1
