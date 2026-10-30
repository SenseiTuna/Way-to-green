param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsJson = '{}'
)
$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$sessionPath = Join-Path $workspacePath 'Evidence\mcp-session.txt'
$headers = @{ Accept = 'application/json, text/event-stream' }
if (Test-Path $sessionPath) { $headers['Mcp-Session-Id'] = (Get-Content $sessionPath -Raw).Trim() }
$rpc = @{ jsonrpc = '2.0'; id = 1; method = $Method; params = ($ParamsJson | ConvertFrom-Json) } | ConvertTo-Json -Depth 30 -Compress
$response = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/mcp' -Method POST -ContentType 'application/json' -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($rpc)) -TimeoutSec 60
$dataLines = $response.Content -split "`n" | Where-Object { $_.StartsWith('data: ') }
if ($dataLines) { foreach ($line in $dataLines) { $line.Substring(6) } } else { $response.Content }
