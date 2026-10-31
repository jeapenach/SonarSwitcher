# Desinstala Sonar Switcher: quita el paquete, la exención de localhost y el certificado.
$ErrorActionPreference = 'Continue'
$here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$pkgName = '796a56cd-8e43-414a-8177-06b3cccc083d'

$pkg = Get-AppxPackage -Name $pkgName
if ($pkg) {
    & CheckNetIsolation.exe LoopbackExempt -d "-n=$($pkg.PackageFamilyName)" | Out-Null
    Remove-AppxPackage -Package $pkg.PackageFullName
}

$cer = Get-ChildItem -Path $here -Filter '*.cer' | Select-Object -First 1
if ($cer) {
    $thumb = (New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cer.FullName)).Thumbprint
    Remove-Item -Path "Cert:\LocalMachine\TrustedPeople\$thumb" -ErrorAction SilentlyContinue
}
exit 0
