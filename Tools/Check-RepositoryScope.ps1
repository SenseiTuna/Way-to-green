param([switch]$PrePush)
$ErrorActionPreference = 'Stop'

function Assert-Paths([string[]]$Paths) {
    $blocked = @($Paths | Where-Object {
        $_ -match '^(Docs|Concepts|Evidence|Builds)/' -or
        $_ -eq 'BASLANGIC_PROMPTU.txt' -or
        ($_ -match '^[^/]+\.md$' -and $_ -notin @('README.md', 'THIRD_PARTY_NOTICES.md')) -or
        $_ -match '(^|/)(\.env(?:\..*)?|\.codex|\.agents|\.aws)(/|$)' -or
        $_ -match '^KiyiDongusu/(Library|Temp|UserSettings|Logs|Build|Builds)/'
    })
    if ($blocked.Count -gt 0) {
        $blocked | Sort-Object -Unique | ForEach-Object { Write-Output "BLOCKED: $_" }
        throw 'Internal files must remain local. Push cancelled.'
    }
}

if ($PrePush) {
    $updates = [Console]::In.ReadToEnd() -split '\r?\n' | Where-Object { $_.Trim() }
    foreach ($update in $updates) {
        $fields = $update -split '\s+'
        $localSha = $fields[1]
        $remoteSha = $fields[3]
        if ($localSha -match '^0+$') { continue }
        $commits = if ($remoteSha -match '^0+$') { @(git rev-list $localSha) } else { @(git rev-list $localSha "^$remoteSha") }
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect outgoing history. Push cancelled.' }
        foreach ($commit in $commits) {
            $paths = @(git ls-tree -r --name-only $commit)
            if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect outgoing commit. Push cancelled.' }
            Assert-Paths $paths
        }
    }
} else {
    $paths = @(git ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Git index.' }
    Assert-Paths $paths
    Write-Output 'Repository scope: PASS'
}
