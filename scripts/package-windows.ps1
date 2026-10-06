param(
    [string]$Version,
    [string]$Rid
)

Set-Location "$PSScriptRoot\.."

$SourceFiles = "publish/$Rid/NBTLoupe.exe", "publish/$Rid/*.dll"
$Destination = "publish/NBTLoupe-v$Version-RELEASE-$Rid.zip"

Compress-Archive -Path $SourceFiles -DestinationPath $Destination
