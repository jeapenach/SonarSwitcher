# Permite que el widget (app UWP en sandbox) hable con 127.0.0.1 (GG y Sonar).
# Ejecutar en PowerShell DESPUÉS de implementar el widget desde Visual Studio.
# El nombre interno del paquete es el GUID de Package.appxmanifest (Identity Name).
$id = "796a56cd-8e43-414a-8177-06b3cccc083d"
$pkg = Get-AppxPackage -Name $id | Select-Object -First 1
if (-not $pkg) { Write-Host "No encontré el paquete $id. Implementa primero el proyecto desde Visual Studio."; exit 1 }
$pfn = $pkg.PackageFamilyName
Write-Host "Paquete:" $pfn
CheckNetIsolation.exe LoopbackExempt -a -n="$pfn"
Write-Host "Exenciones activas (debe aparecer tu paquete):"
CheckNetIsolation.exe LoopbackExempt -s | Select-String $id
