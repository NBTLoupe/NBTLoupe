#  Copyright (c) 2024-2026 MallardLuna
#
#  This Source Code Form is subject to the terms of the Mozilla Public
#  License, v. 2.0. If a copy of the MPL was not distributed with this
#  file, You can obtain one at https://mozilla.org/MPL/2.0/.

param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Rid
)

$ErrorActionPreference = 'Stop'

Set-Location "$PSScriptRoot\.."

$SourceFiles = "publish/$Rid/NBTLoupe.exe", "publish/$Rid/*.dll"
$Destination = "publish/NBTLoupe-v$Version-RELEASE-$Rid.zip"

Compress-Archive -Path $SourceFiles -DestinationPath $Destination

Compress-Archive -Path "publish/$Rid/NBTLoupe.pdb" -DestinationPath "publish/NBTLoupe-v$Version-SYMBOLS-$Rid.zip"
