@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -Command "& { $script = Get-Content -Raw -Encoding utf8 '%~f0'; $block = [regex]::Match($script, '(?ms)^#<PS_SCRIPT_START>.*#<PS_SCRIPT_END>').Value; Invoke-Expression $block; exit $LASTEXITCODE }"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Snapshot basariyla olusturuldu.
    exit /b 0
) else (
    echo.
    echo Snapshot olusturulurken hata olustu.
    pause >nul
    exit /b %ERRORLEVEL%
)

#<PS_SCRIPT_START>
$ErrorActionPreference = "Continue"

$projectRoot = Get-Location

$excludedPathPrefixes = @(
    '.git',
    '.config',
    '.playwright-cli',
    '.stitch',
    '.tools',
    'bin',
    'obj',
    'logs',
    'tmp',
    'output',
    'screenshoots',
    'keys',
    'wwwroot\lib',
    'wwwroot\images\certificates',
    'wwwroot\images\profiles',
    'Data\Migrations'
)

$excludedPathPrefixes = $excludedPathPrefixes | ForEach-Object { $_ -replace '/', '\' }

$excludedDirs = @(
    '.git',
    '.vs',
    '.idea',
    '.vscode',
    '__pycache__',
    'node_modules',
    'bin',
    'obj',
    'logs',
    'tmp',
    'temp',
    'output',
    'screenshoots',
    'keys',
    'coverage',
    'dist',
    'build',
    'vendor'
)

$excludedExtensions = @(
    '.png', '.jpg', '.jpeg', '.gif', '.bmp', '.svg', '.ico', '.webp',
    '.mp4', '.mov', '.avi', '.mp3', '.wav', '.ogg',
    '.exe', '.dll', '.pdb', '.obj', '.so', '.lib', '.a',
    '.zip', '.rar', '.7z', '.tar', '.gz',
    '.log', '.tmp', '.temp',
    '.pdf', '.doc', '.docx',
    '.db', '.sqlite', '.bak',
    '.map',
    '.txt'
)

$excludedFiles = @(
    'Thumbs.db',
    '.DS_Store',
    '.env',
    '.env.example',
    '.gitignore',
    '.dockerignore',
    'create_snapshot.bat',
    'appsettings.json',
    'appsettings.Development.json',
    'package-lock.json',
    'yarn.lock',
    'pnpm-lock.yaml'
)

$excludedFileNamePatterns = @(
    '^MentoraSnapshot_v\d+\.txt$',
    '^ProjectSnapshot_v\d+\.txt$',
    '^~\$.*',
    '.*\.Designer\.cs$',
    '^ApplicationDbContextModelSnapshot\.cs$'
)

$snapshotBasename = 'MentoraSnapshot_v'

$latestVersion = Get-ChildItem -Path $projectRoot -Filter "$($snapshotBasename)*.txt" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -match 'v(\d+)\.txt$') {
        [int]$matches[1]
    }
} | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum

if ($null -eq $latestVersion) { $latestVersion = 0 }
$nextVersion = $latestVersion + 1
$outputFile = Join-Path -Path $projectRoot -ChildPath "$($snapshotBasename)$($nextVersion).txt"

Write-Host "Snapshot olusturuluyor: $outputFile" -ForegroundColor Cyan

$outputStream = $null
$outputWriter = $null

try {
    $outputStream = New-Object System.IO.FileStream($outputFile, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
    $outputWriter = New-Object System.IO.StreamWriter($outputStream, [System.Text.Encoding]::UTF8)

    Get-ChildItem -Path $projectRoot -Recurse -File | ForEach-Object {
        $file = $_
        $includeFile = $true
        $relativePath = $file.FullName.Substring($projectRoot.Path.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
        $relativePathNormalized = $relativePath -replace '/', '\'

        if ($file.Extension -eq '.bat' -or $file.FullName -eq (Resolve-Path $outputFile).Path) {
            $includeFile = $false
        }

        if ($includeFile -and ($excludedFiles -contains $file.Name -or $excludedExtensions -contains $file.Extension)) {
            $includeFile = $false
        }

        if ($includeFile) {
            foreach ($pattern in $excludedFileNamePatterns) {
                if ($file.Name -match $pattern) {
                    $includeFile = $false
                    break
                }
            }
        }

        if ($includeFile) {
            foreach ($prefix in $excludedPathPrefixes) {
                if ($relativePathNormalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $includeFile = $false
                    break
                }
            }
        }

        if ($includeFile) {
            $pathSegments = $relativePathNormalized.Split('\')
            foreach ($segment in $pathSegments) {
                if ($excludedDirs -contains $segment) {
                    $includeFile = $false
                    break
                }
            }
        }

        if ($includeFile) {
            $header = @"

--------------------------------------------------------------------------------
DOSYA: $relativePath
--------------------------------------------------------------------------------

"@
            $outputWriter.Write($header)

            try {
                $stream = New-Object System.IO.FileStream($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8, $true)
                $content = $reader.ReadToEnd()
                $reader.Dispose()
                $stream.Dispose()
                $outputWriter.Write($content)
            } catch {
                $errorMessage = "HATA: $($file.FullName) dosyasi okunamadi. Sebep: $($_.Exception.Message)"
                Write-Host $errorMessage -ForegroundColor Red
                $outputWriter.Write($errorMessage)
            }
        }
    }
}
finally {
    if ($null -ne $outputWriter) { $outputWriter.Dispose() }
    if ($null -ne $outputStream) { $outputStream.Dispose() }
}

$errorCount = $Error.Count
if ($errorCount -eq 0) {
    Write-Host "Islem basariyla tamamlandi." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Islem tamamlandi ancak $errorCount hata olustu." -ForegroundColor Yellow
    exit 1
}

#<PS_SCRIPT_END>
