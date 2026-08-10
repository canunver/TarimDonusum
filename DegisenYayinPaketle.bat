@echo off
setlocal

set "KAYNAK=c:\sources\asp.net\tarimdonusum\bin\release\net8.0\publish"
set "DEGISIKLIK_TARIHI=2026-08-10 11:00"

if not defined KAYNAK (
    set /p "KAYNAK=Publish klasorunun tam yolunu girin: "
)

if not defined DEGISIKLIK_TARIHI (
    set /p "DEGISIKLIK_TARIHI=Baslangic tarihini girin (yyyy-MM-dd HH:mm): "
)

if not exist "%KAYNAK%\" (
    echo HATA: Publish klasoru bulunamadi: %KAYNAK%
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop';" ^
    "$kaynak = [IO.Path]::GetFullPath($env:KAYNAK).TrimEnd('\');" ^
    "$tarih = [DateTime]::ParseExact($env:DEGISIKLIK_TARIHI, 'yyyy-MM-dd HH:mm', [Globalization.CultureInfo]::InvariantCulture);" ^
    "$zaman = Get-Date -Format 'yyyyMMdd_HHmmss';" ^
    "$hedef = Join-Path ('..') ('DegisenYayin_' + $zaman);" ^
    "$zip = $hedef + '.zip';" ^
    "$dosyalar = @(Get-ChildItem -LiteralPath $kaynak -Recurse -File | Where-Object { $_.LastWriteTime -gt $tarih });" ^
    "if ($dosyalar.Count -eq 0) { Write-Host 'Belirtilen tarihten sonra degisen dosya bulunamadi.'; exit 2 };" ^
    "foreach ($dosya in $dosyalar) {" ^
    "  $goreliYol = $dosya.FullName.Substring($kaynak.Length).TrimStart('\');" ^
    "  $hedefDosya = Join-Path $hedef $goreliYol;" ^
    "  New-Item -ItemType Directory -Path (Split-Path $hedefDosya -Parent) -Force | Out-Null;" ^
    "  Copy-Item -LiteralPath $dosya.FullName -Destination $hedefDosya -Force;" ^
    "};" ^
    "Compress-Archive -Path (Join-Path $hedef '*') -DestinationPath $zip -Force;" ^
    "Write-Host ('Paket hazirlandi: ' + $zip);" ^
    "Write-Host ('Dosya sayisi: ' + $dosyalar.Count)"

set "SONUC=%ERRORLEVEL%"
if "%SONUC%"=="2" exit /b 0
if not "%SONUC%"=="0" (
    echo HATA: Paket olusturulamadi.
    exit /b %SONUC%
)

echo Islem tamamlandi.
exit /b 0
