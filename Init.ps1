$pcPath = "C:\Program Files (x86)\Microsoft BizTalk Server 2013 R2\Pipeline Components\"
if (Test-Path $pcPath){
    $currentLocation = Get-Location
    xcopy  $currentLocation"\lib\net45\IT.BTS.PC.Base64Decoder.dll" "C:\Program Files (x86)\Microsoft BizTalk Server 2013 R2\Pipeline Components\"
}
else{
    Write-Output "Pipeline component path not found"
}