#!/bin/bash

#  Copyright (c) 2024-2026 MallardLuna
#
#  This Source Code Form is subject to the terms of the Mozilla Public
#  License, v. 2.0. If a copy of the MPL was not distributed with this
#  file, You can obtain one at https://mozilla.org/MPL/2.0/.

set -eu
cd "$(dirname "$0")/.."

VERSION=$1
RID=$2
APP_DIR="publish/dmg/NBTLoupe.app"

mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

cp ASSETS/icons/macos/NBTLoupe.icns "$APP_DIR/Contents/Resources/"
mv "publish/$RID/NBTLoupe" "publish/$RID"/*.dylib "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/NBTLoupe"

sed -e "s/{{VERSION}}/$VERSION/g" templates/macos/Info.plist.template > "$APP_DIR/Contents/Info.plist"

cd publish
ln -s /Applications dmg/Applications
diskutil image create from dmg "NBTLoupe-v$VERSION-RELEASE-$RID.dmg" --volumeName "NBTLoupe v$VERSION" -f UDZO

ditto -c -k --norsrc --keepParent "$RID/NBTLoupe.dSYM" "NBTLoupe-v$VERSION-SYMBOLS-$RID.zip"