#Requires -Version 5.1
<#
.SYNOPSIS
    Локальный кластер PostgreSQL для проекта Snake (данные в Snake\Data\pgdata).

.DESCRIPTION
    Кластер поднимается под текущим пользователем на порту 5433 и слушает только 127.0.0.1.
    База: snake, пользователь: snake.

.EXAMPLE
    ./Data/db.ps1 start
    ./Data/db.ps1 status
    ./Data/db.ps1 psql
    ./Data/db.ps1 stop
#>
param(
    [ValidateSet('start', 'stop', 'status', 'psql')]
    [string]$Action = 'status'
)

$ErrorActionPreference = 'Stop'

$bin = if ($env:SNAKE_PG_BIN) { $env:SNAKE_PG_BIN } else { 'C:\Program Files\PostgreSQL\18\bin' }
$data = Join-Path $PSScriptRoot 'pgdata'
$log = Join-Path $PSScriptRoot 'pg.log'
$port = 5433
$user = 'snake'
$password = 'snake_dev_pw'
$database = 'snake'

if (-not (Test-Path (Join-Path $data 'PG_VERSION'))) {
    throw "Кластер не найден: $data. Сначала выполните initdb (см. Data/README.md)."
}

function Invoke-Psql {
    param([string]$Db, [string]$Sql)
    $env:PGPASSWORD = $password
    try {
        & "$bin\psql.exe" -h 127.0.0.1 -p $port -U $user -d $Db -tAc $Sql
    }
    finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
}

switch ($Action) {
    'start' {
        # Start-Process с редиректом: иначе pg_ctl держит унаследованный stdout и блокирует оболочку.
        $out = Join-Path $PSScriptRoot 'pgctl.out'
        $err = Join-Path $PSScriptRoot 'pgctl.err'
        $proc = Start-Process -FilePath "$bin\pg_ctl.exe" `
            -ArgumentList @('-D', $data, '-l', $log, 'start') `
            -RedirectStandardOutput $out -RedirectStandardError $err `
            -PassThru -NoNewWindow
        $proc.WaitForExit(30000) | Out-Null
        Start-Sleep -Seconds 2
        if ((Invoke-Psql -Db 'postgres' -Sql 'select 1') -eq '1') {
            Write-Host "PostgreSQL запущен: 127.0.0.1:$port, база $database"
        }
        else {
            Write-Host "Не удалось запустить. Смотрите $log"
            Get-Content $log -Tail 10 -ErrorAction SilentlyContinue
        }
    }
    'stop' {
        & "$bin\pg_ctl.exe" -D $data stop -m fast
    }
    'status' {
        if ((Invoke-Psql -Db 'postgres' -Sql 'select 1') -eq '1') {
            Write-Host "PostgreSQL работает: 127.0.0.1:$port"
            $played = Invoke-Psql -Db $database -Sql 'select count(*) from game_results'
            Write-Host "Партий в БД: $played"
        }
        else {
            Write-Host "PostgreSQL не отвечает на 127.0.0.1:$port"
        }
    }
    'psql' {
        $env:PGPASSWORD = $password
        & "$bin\psql.exe" -h 127.0.0.1 -p $port -U $user -d $database
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
}
