#!/bin/bash
set -eu
cd "$(dirname "$0")/.."

VERSION=$1
RID=$2
APP_DIR="publish/dmg/NBTLoupe.app"

sh scripts/fetch-assets.sh icons/macos

mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

cp ASSETS/icons/macos/NBTLoupe.icns "$APP_DIR/Contents/Resources/"
mv "publish/$RID/NBTLoupe" "publish/$RID"/*.dylib "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/NBTLoupe"

sed -e "s/{{VERSION}}/$VERSION/g" templates/macos/Info.plist.template > "$APP_DIR/Contents/Info.plist"

cd publish
ln -s /Applications dmg/Applications
diskutil image create from dmg "NBTLoupe-v$VERSION-RELEASE-$RID.dmg" --volumeName "NBTLoupe v$VERSION" -f UDZO
