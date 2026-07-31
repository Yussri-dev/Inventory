param(
    [string]$SourceHost = "localhost",
    [int]$SourcePort = 5432,
    [string]$DestinationHost = "localhost",
    [int]$DestinationPort = 55432,
    [string]$Database = "InventoryMarkt_DB",
    [string]$User = "postgres",
    [string]$PostgreSqlToolsPath = "C:\Program Files\PostgreSQL\17\bin",
    [string]$SourceDumpFile = ".\catalog-and-identity-source.dump",
    [string]$DestinationBackupFile = ".\catalog-and-identity-destination-before-import.dump"
)

$ErrorActionPreference = "Stop"

$psql = Join-Path $PostgreSqlToolsPath "psql.exe"
$pgDump = Join-Path $PostgreSqlToolsPath "pg_dump.exe"
$pgRestore = Join-Path $PostgreSqlToolsPath "pg_restore.exe"

foreach ($tool in @($psql, $pgDump, $pgRestore)) {
    if (-not (Test-Path $tool)) {
        throw "Outil PostgreSQL introuvable : $tool"
    }
}

# Les champs TenantId, PasswordHash et SecurityStamp sont inclus
# automatiquement parce que la table AspNetUsers est copiée en entier.
$tableArguments = @(
    '--table=public."Tenants"',
    '--table=public."AspNetRoles"',
    '--table=public."AspNetRoleClaims"',
    '--table=public."AspNetUsers"',
    '--table=public."AspNetUserRoles"',
    '--table=public."AspNetUserClaims"',
    '--table=public."AspNetUserLogins"',
    '--table=public."AspNetUserTokens"',
    '--table=public."ProductCategory"',
    '--table=public."ProductCatalogs"',
    '--table=public."PackComponents"'
)

function ConvertTo-PlainText {
    param([Security.SecureString]$SecureValue)

    return [System.Net.NetworkCredential]::new(
        "",
        $SecureValue
    ).Password
}

function Invoke-Psql {
    param(
        [Parameter(Mandatory)]
        [string]$Password,

        [Parameter(Mandatory)]
        [string]$HostName,

        [Parameter(Mandatory)]
        [int]$Port,

        [Parameter(Mandatory)]
        [string]$Sql
    )

    $previousPassword = $env:PGPASSWORD

    try {
        $env:PGPASSWORD = $Password

        $Sql |
            & $psql `
                -X `
                -v ON_ERROR_STOP=1 `
                -h $HostName `
                -p $Port `
                -U $User `
                -d $Database

        if ($LASTEXITCODE -ne 0) {
            throw "psql a échoué avec le code $LASTEXITCODE."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

function Invoke-PgDump {
    param(
        [Parameter(Mandatory)]
        [string]$Password,

        [Parameter(Mandatory)]
        [string]$HostName,

        [Parameter(Mandatory)]
        [int]$Port,

        [Parameter(Mandatory)]
        [string]$OutputFile
    )

    $previousPassword = $env:PGPASSWORD

    try {
        $env:PGPASSWORD = $Password

        $arguments = @(
            "-h", $HostName,
            "-p", $Port,
            "-U", $User,
            "-d", $Database,
            "--format=custom",
            "--data-only",
            "--no-owner",
            "--no-privileges",
            "--file", $OutputFile
        ) + $tableArguments

        & $pgDump @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "pg_dump a échoué avec le code $LASTEXITCODE."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

function Invoke-PgRestore {
    param(
        [Parameter(Mandatory)]
        [string]$Password,

        [Parameter(Mandatory)]
        [string]$HostName,

        [Parameter(Mandatory)]
        [int]$Port,

        [Parameter(Mandatory)]
        [string]$InputFile
    )

    $previousPassword = $env:PGPASSWORD

    try {
        $env:PGPASSWORD = $Password

        & $pgRestore `
            -h $HostName `
            -p $Port `
            -U $User `
            -d $Database `
            --data-only `
            --no-owner `
            --no-privileges `
            --disable-triggers `
            --exit-on-error `
            $InputFile

        if ($LASTEXITCODE -ne 0) {
            throw "pg_restore a échoué avec le code $LASTEXITCODE."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

$sourceDumpPath =
    [System.IO.Path]::GetFullPath(
        (Join-Path (Get-Location) $SourceDumpFile)
    )

$destinationBackupPath =
    [System.IO.Path]::GetFullPath(
        (Join-Path (Get-Location) $DestinationBackupFile)
    )

Write-Host ""
Write-Host "Source PostgreSQL 17 : $SourceHost`:$SourcePort/$Database" -ForegroundColor Cyan
Write-Host "Destination Docker   : $DestinationHost`:$DestinationPort/$Database" -ForegroundColor Cyan
Write-Host ""

$sourceSecurePassword =
    Read-Host "Mot de passe PostgreSQL 17 (source)" -AsSecureString

$destinationSecurePassword =
    Read-Host "Mot de passe PostgreSQL Docker (destination)" -AsSecureString

$sourcePassword =
    ConvertTo-PlainText $sourceSecurePassword

$destinationPassword =
    ConvertTo-PlainText $destinationSecurePassword

$countsSql = @'
SELECT
    (SELECT COUNT(*) FROM public."Tenants") AS tenants,
    (SELECT COUNT(*) FROM public."AspNetUsers") AS users,
    (SELECT COUNT(*) FROM public."AspNetRoles") AS roles,
    (SELECT COUNT(*) FROM public."AspNetUserRoles") AS user_roles,
    (SELECT COUNT(*) FROM public."AspNetUserClaims") AS user_claims,
    (SELECT COUNT(*) FROM public."ProductCategory") AS categories,
    (SELECT COUNT(*) FROM public."ProductCatalogs") AS catalogs,
    (SELECT COUNT(*) FROM public."PackComponents") AS pack_components;
'@

Write-Host ""
Write-Host "Contenu de la source :" -ForegroundColor Yellow

Invoke-Psql `
    -Password $sourcePassword `
    -HostName $SourceHost `
    -Port $SourcePort `
    -Sql $countsSql

Write-Host ""
Write-Host "Contenu actuel de Docker :" -ForegroundColor Yellow

Invoke-Psql `
    -Password $destinationPassword `
    -HostName $DestinationHost `
    -Port $DestinationPort `
    -Sql $countsSql

Write-Host ""
Write-Host "Cette opération remplacera dans Docker :" -ForegroundColor Yellow
Write-Host "  - les tenants" -ForegroundColor Yellow
Write-Host "  - tous les utilisateurs et rôles ASP.NET Identity" -ForegroundColor Yellow
Write-Host "  - les catégories, catalogues et composants de packs" -ForegroundColor Yellow
Write-Host ""
Write-Host "PasswordHash, SecurityStamp et TenantId seront conservés via AspNetUsers." -ForegroundColor Cyan
Write-Host "Les ventes, stocks, clients, achats et autres données métier ne sont pas exportés." -ForegroundColor Cyan
Write-Host ""

$confirmation =
    Read-Host 'Tape exactement REMPLACER pour continuer'

if ($confirmation -cne "REMPLACER") {
    Write-Host "Opération annulée." -ForegroundColor Yellow
    exit 0
}

foreach ($file in @($sourceDumpPath, $destinationBackupPath)) {
    if (Test-Path $file) {
        Remove-Item $file -Force
    }
}

Write-Host ""
Write-Host "1/5 Sauvegarde des données actuelles de Docker..." -ForegroundColor Cyan

Invoke-PgDump `
    -Password $destinationPassword `
    -HostName $DestinationHost `
    -Port $DestinationPort `
    -OutputFile $destinationBackupPath

Write-Host "Sauvegarde Docker : $destinationBackupPath" -ForegroundColor Green

Write-Host ""
Write-Host "2/5 Export depuis PostgreSQL 17..." -ForegroundColor Cyan

Invoke-PgDump `
    -Password $sourcePassword `
    -HostName $SourceHost `
    -Port $SourcePort `
    -OutputFile $sourceDumpPath

Write-Host "Export source : $sourceDumpPath" -ForegroundColor Green

Write-Host ""
Write-Host "3/5 Suppression contrôlée des anciennes données dans Docker..." -ForegroundColor Cyan

$deleteSql = @'
BEGIN;

DELETE FROM public."AspNetUserTokens";
DELETE FROM public."AspNetUserLogins";
DELETE FROM public."AspNetUserClaims";
DELETE FROM public."AspNetUserRoles";
DELETE FROM public."AspNetRoleClaims";
DELETE FROM public."AspNetUsers";
DELETE FROM public."AspNetRoles";

DELETE FROM public."PackComponents";
DELETE FROM public."ProductCatalogs";
DELETE FROM public."ProductCategory";

DELETE FROM public."Tenants";

COMMIT;
'@

try {
    Invoke-Psql `
        -Password $destinationPassword `
        -HostName $DestinationHost `
        -Port $DestinationPort `
        -Sql $deleteSql
}
catch {
    Write-Host ""
    Write-Host "La suppression a échoué, probablement parce que des données métier Docker référencent déjà ces tenants ou catalogues." -ForegroundColor Red
    Write-Host "Aucune importation ne sera lancée." -ForegroundColor Red
    throw
}

Write-Host ""
Write-Host "4/5 Import dans PostgreSQL Docker..." -ForegroundColor Cyan

try {
    Invoke-PgRestore `
        -Password $destinationPassword `
        -HostName $DestinationHost `
        -Port $DestinationPort `
        -InputFile $sourceDumpPath
}
catch {
    Write-Host ""
    Write-Host "L'import source a échoué. Tentative de restauration de la sauvegarde Docker..." -ForegroundColor Red

    try {
        Invoke-PgRestore `
            -Password $destinationPassword `
            -HostName $DestinationHost `
            -Port $DestinationPort `
            -InputFile $destinationBackupPath

        Write-Host "La sauvegarde Docker a été restaurée." -ForegroundColor Yellow
    }
    catch {
        Write-Host "La restauration automatique a également échoué." -ForegroundColor Red
    }

    throw
}

Write-Host ""
Write-Host "5/5 Réinitialisation des séquences Identity..." -ForegroundColor Cyan

$sequenceSql = @'
DO $$
DECLARE
    sequence_name text;
BEGIN
    sequence_name := pg_get_serial_sequence('public."AspNetUserClaims"', 'Id');

    IF sequence_name IS NOT NULL THEN
        EXECUTE format(
            'SELECT setval(%L, COALESCE((SELECT MAX("Id") FROM public."AspNetUserClaims"), 1), (SELECT COUNT(*) > 0 FROM public."AspNetUserClaims"))',
            sequence_name
        );
    END IF;

    sequence_name := pg_get_serial_sequence('public."AspNetRoleClaims"', 'Id');

    IF sequence_name IS NOT NULL THEN
        EXECUTE format(
            'SELECT setval(%L, COALESCE((SELECT MAX("Id") FROM public."AspNetRoleClaims"), 1), (SELECT COUNT(*) > 0 FROM public."AspNetRoleClaims"))',
            sequence_name
        );
    END IF;
END
$$;
'@

Invoke-Psql `
    -Password $destinationPassword `
    -HostName $DestinationHost `
    -Port $DestinationPort `
    -Sql $sequenceSql

Write-Host ""
Write-Host "Vérification finale dans Docker :" -ForegroundColor Yellow

Invoke-Psql `
    -Password $destinationPassword `
    -HostName $DestinationHost `
    -Port $DestinationPort `
    -Sql $countsSql

Write-Host ""
Write-Host "Copie terminée avec succès." -ForegroundColor Green
Write-Host "Les anciens mots de passe restent valides grâce à la copie de PasswordHash." -ForegroundColor Green
Write-Host "Redémarre ensuite l'API avec : docker compose restart api" -ForegroundColor Cyan

$sourcePassword = $null
$destinationPassword = $null