$ErrorActionPreference = 'Stop'
Set-Location "$PSScriptRoot/python"
& ../.venv/Scripts/python.exe mock_sender.py @args
