# EF Core scaffold: генерация сущностей и DbContext из PostgreSQL в проект .Bus, папка Models/

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Set-Location $repoRoot

# Строка из конфигурации (appsettings.json) — не хардкодим в коде
$conn = "Name=DefaultConnection"
$provider = "Npgsql.EntityFrameworkCore.PostgreSQL"
$project = "UniversityMethodologicalDepartment.Bus"
$outputDir = "Entities"
$contextName = "AppDbContext"

# Только схема public (без auth, storage, extensions и др. служебных схем Supabase)
$schemas = "public"

# Без --startup-project: для design-time используется сам .Bus (WinUI-проект не грузим).
dotnet ef dbcontext scaffold $conn $provider `
  --project $project `
  --output-dir $outputDir `
  --context $contextName `
  --schema $schemas `
  --force
