param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\Sql\NaceRev21.sql')
)

$ErrorActionPreference = 'Stop'
$sourceUrl = 'https://siniflama.tuik.gov.tr/Classifications/GetSiniflamaSatir?surumId=1438&seviye=4&kod='
$downloadedRecords = Invoke-RestMethod -Uri $sourceUrl
$records = @($downloadedRecords.Where({ $_.duzey -eq 4 }))

if ($records.Count -ne 651) {
    throw "TÜİK kaynağından 651 sınıf bekleniyordu; $($records.Count) kayıt geldi. Dosya üretilmedi."
}

function ConvertTo-SqlUnicodeLiteral([string]$Value) {
    if ($null -eq $Value) { return 'NULL' }
    return "N'$($Value.Replace("'", "''"))'"
}

$duplicateCodes = $records | Group-Object kod | Where-Object Count -gt 1
if ($duplicateCodes) {
    throw "Kaynakta yinelenen NACE kodu bulundu: $($duplicateCodes.Name -join ', ')"
}

$rows = for ($i = 0; $i -lt $records.Count; $i++) {
    $record = $records[$i]
    $suffix = if ($i -eq $records.Count - 1) { '' } else { ',' }
    "    ($(ConvertTo-SqlUnicodeLiteral $record.kod), $(ConvertTo-SqlUnicodeLiteral $record.tanim), 1)$suffix"
}

$generatedAt = Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'
$sql = @"
/*
    NACE Rev. 2.1 - dört haneli sınıflar (651 kayıt)
    Kaynak: Türkiye İstatistik Kurumu Sınıflama Sunucusu
    Sürüm: Avrupa Topluluğunda Ekonomik Faaliyetlerin İstatistiki Sınıflaması, NACE Rev. 2.1
    Kaynak sürüm kimliği: 1438
    Kaynak adresi: $sourceUrl
    Üretim zamanı: $generatedAt

    Betik tekrar çalıştırılabilir; Kod alanında mevcut olan kayıtları atlar.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

INSERT INTO dbo.Nace (Kod, Ad, Aktif)
SELECT kaynak.Kod, kaynak.Ad, kaynak.Aktif
FROM (VALUES
$($rows -join "`r`n")
) AS kaynak (Kod, Ad, Aktif)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Nace AS mevcut
    WHERE mevcut.Kod = kaynak.Kod
);

DECLARE @EklenenKayitSayisi int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT @EklenenKayitSayisi AS EklenenKayitSayisi,
       (SELECT COUNT(*) FROM dbo.Nace WHERE Aktif = 1) AS ToplamAktifNaceSayisi;
"@

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($OutputPath),
    $sql,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Output "Üretildi: $([System.IO.Path]::GetFullPath($OutputPath)) ($($records.Count) kayıt)"
