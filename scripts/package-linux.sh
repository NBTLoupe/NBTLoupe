#!/bin/bash
set -eu
cd "$(dirname "$0")/.."

VERSION=$1
RID=$2
DATE=$(date +%F)
APP_DIR="publish/NBTLoupe.AppDir"

sh scripts/fetch-assets.sh icons/linux

mkdir -p "$APP_DIR/usr/bin" "$APP_DIR/usr/share/applications" "$APP_DIR/usr/share/metainfo" "$APP_DIR/usr/share/icons"

cp -R ASSETS/icons/linux "$APP_DIR/usr/share/icons/hicolor"
cp "$APP_DIR/usr/share/icons/hicolor/256x256/apps/NBTLoupe.png" "$APP_DIR/"

mv "publish/$RID/NBTLoupe" "publish/$RID"/*.so "$APP_DIR/usr/bin/"
chmod +x "$APP_DIR/usr/bin/NBTLoupe"

cp templates/linux/AppRun "$APP_DIR/AppRun"
chmod +x "$APP_DIR/AppRun"

cp templates/linux/com.mallardluna.nbtloupe.desktop "$APP_DIR/usr/share/applications/com.mallardluna.nbtloupe.desktop"
cp templates/linux/com.mallardluna.nbtloupe.desktop "$APP_DIR/com.mallardluna.nbtloupe.desktop"

sed -e "s/{{VERSION}}/$VERSION/g" -e "s/{{DATE}}/$DATE/g" templates/linux/com.mallardluna.nbtloupe.appdata.xml.template > "$APP_DIR/usr/share/metainfo/com.mallardluna.nbtloupe.appdata.xml"

cd publish
curl -fLo ./appimagetool.AppImage "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-$(uname -m).AppImage"
chmod +x ./appimagetool.AppImage
./appimagetool.AppImage NBTLoupe.AppDir "NBTLoupe-v$VERSION-RELEASE-$RID.AppImage"
