$ErrorActionPreference = 'Stop'
Set-Location "$PSScriptRoot/python"
& ../.venv/Scripts/python.exe -u app.py --no-preview @args
