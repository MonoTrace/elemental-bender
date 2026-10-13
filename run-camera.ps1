$ErrorActionPreference = 'Stop'
Set-Location "$PSScriptRoot/python"
& ../.venv/Scripts/python.exe app.py @args
