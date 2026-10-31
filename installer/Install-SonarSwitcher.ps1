# Instala Sonar Switcher: confía en el certificado, instala el paquete y concede la exención de localhost.
# Requiere permisos de administrador (el instalador .exe ya los pide).
$ErrorActionPreference = 'Stop'
$here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$pkgName = '796a56cd-8e43-414a-8177-06b3cccc083d'   # Identity Name de Package.appxmanifest
Start-Transcript -Path (Join-Path $here 'install.log') -Force | Out-Null

try {
    $cer  = Get-ChildItem -Path $here -Filter '*.cer'  | Select-Object -First 1
    $msix = Get-ChildItem -Path $here -Filter '*.msix' | Select-Object -First 1
    if (-not $cer -or -not $msix) { throw 'Faltan el paquete (.msix) o el certificado (.cer) junto al script.' }

    # 1) Si hay una versión de desarrollo (registrada desde Visual Studio), quitarla: no puede convivir con el paquete firmado.
    $existing = Get-AppxPackage -Name $pkgName
    if ($existing -and $existing.IsDevelopmentMode) {
        Write-Host 'Quitando la versión de desarrollo instalada...'
        Remove-AppxPackage -Package $existing.PackageFullName
    }

    # 2) Confiar en el certificado que firma el paquete (almacén "Personas de confianza" del equipo).
    Write-Host 'Confiando en el certificado...'
    Import-Certificate -FilePath $cer.FullName -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null

    # 3) Instalar el paquete con sus dependencias (.NET Native y VCLibs).
    $deps = @()
    $depDir = Join-Path $here 'Dependencies'
    if (Test-Path $depDir) { $deps = @(Get-ChildItem -Path $depDir -Filter '*.appx' | ForEach-Object { $_.FullName }) }
    Write-Host 'Instalando el widget...'
    if ($deps.Count -gt 0) {
        Add-AppxPackage -Path $msix.FullName -DependencyPath $deps -ForceUpdateFromAnyVersion -ForceApplicationShutdown
    } else {
        Add-AppxPackage -Path $msix.FullName -ForceUpdateFromAnyVersion -ForceApplicationShutdown
    }

    # 4) Exención de loopback: sin ella el widget no puede hablar con GG/Sonar en 127.0.0.1.
    $pkg = Get-AppxPackage -Name $pkgName
    if (-not $pkg) { throw 'El paquete no aparece instalado después de la instalación.' }
    Write-Host 'Concediendo acceso a localhost...'
    & CheckNetIsolation.exe LoopbackExempt -a "-n=$($pkg.PackageFamilyName)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "CheckNetIsolation falló (código $LASTEXITCODE)." }

    Write-Host 'Listo. Abre Game Bar con Win + G y busca "Sonar Switcher".'
    Stop-Transcript | Out-Null
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Stop-Transcript | Out-Null
    exit 1
}
