$ErrorActionPreference = "Stop"

$resourceRoot = Join-Path $PSScriptRoot "..\Resources"
$markers = @(
    [char]0x00C3, # Ã
    [char]0x00C2, # Â
    [char]0x20AC, # €
    [char]0x00C6, # Æ
    [char]0x00C5, # Å
    [char]0x201A, # ‚
    [char]0x201E, # „
    [char]0x0192  # ƒ
)

$badValues = New-Object System.Collections.Generic.List[string]

Get-ChildItem -Path $resourceRoot -Filter "*.resx" -File | ForEach-Object {
    [xml]$xml = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8

    foreach ($data in $xml.root.data) {
        $value = [string]$data.value
        if ([string]::IsNullOrEmpty($value)) {
            continue
        }

        foreach ($marker in $markers) {
            if ($value.Contains($marker)) {
                $badValues.Add(("{0}: {1}" -f $_.Name, $data.name))
                break
            }
        }
    }
}

if ($badValues.Count -gt 0) {
    Write-Error ("Resource encoding looks broken in these keys: " + ($badValues -join "; "))
    exit 1
}
