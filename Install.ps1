param(
    [Parameter(Mandatory=$true)] [string]   $installPath,
    [Parameter(Mandatory=$true)] [string]   $toolsPath,
    [Parameter(Mandatory=$true)]            $package,
    [Parameter(Mandatory=$true)]            $project
)

$assemblyName = "IT.BTS.PC.Base64Decoder"

$packageRoot = $(Get-Item $toolsPath).Parent.FullName
$sourceFile = "$packageRoot\lib\net45\$assemblyName.dll"
gacutil /if $sourcefile