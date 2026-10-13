param([Parameter(Mandatory=$true)][string]$VideoPath)
$ErrorActionPreference = 'Stop'
$resolvedVideo = (Resolve-Path -LiteralPath $VideoPath).Path
Set-Location "$PSScriptRoot/python"
& ../.venv/Scripts/python.exe -u app.py --video $resolvedVideo --no-udp
