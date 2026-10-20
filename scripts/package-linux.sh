#!/bin/bash

#  Copyright (c) 2024-2026 MallardLuna
#  This Source Code Form is subject to the terms of the Mozilla Public
#  License, v. 2.0. If a copy of the MPL was not distributed with this
#  file, You can obtain one at https://mozilla.org/MPL/2.0/.

set -eu
cd "$(dirname "$0")/.."

VERSION=$1
RID=$2
DATE=$(date +%F)
APP_RUNTIME="https://github.com/AppImage/type2-runtime/releases/download/continuous"
declare -A arches=([linux-x64]=x86_64 [linux-arm64]=aarch64)
APP_ARCH=${arches[$RID]:?unsupported RID: $RID}
APP_DIR="publish/NBTLoupe.AppDir"
APP_OUT="NBTLoupe-v$VERSION-RELEASE-$RID.AppImage"

mkdir -p "$APP_DIR/usr/bin" "$APP_DIR/usr/share/applications" "$APP_DIR/usr/share/metainfo" "$APP_DIR/usr/share/icons"

cp -R ASSETS/icons/linux "$APP_DIR/usr/share/icons/hicolor"
cp "$APP_DIR/usr/share/icons/hicolor/256x256/apps/NBTLoupe.png" "$APP_DIR/"
ln -s NBTLoupe.png "$APP_DIR/.DirIcon"

mv "publish/$RID/NBTLoupe" "publish/$RID"/*.so "$APP_DIR/usr/bin/"
chmod +x "$APP_DIR/usr/bin/NBTLoupe"

cp templates/linux/AppRun "$APP_DIR/AppRun"
chmod +x "$APP_DIR/AppRun"

cp templates/linux/com.mallardluna.nbtloupe.desktop "$APP_DIR/usr/share/applications/com.mallardluna.nbtloupe.desktop"
cp templates/linux/com.mallardluna.nbtloupe.desktop "$APP_DIR/com.mallardluna.nbtloupe.desktop"

sed -e "s/{{VERSION}}/$VERSION/g" -e "s/{{DATE}}/$DATE/g" templates/linux/com.mallardluna.nbtloupe.appdata.xml.template > "$APP_DIR/usr/share/metainfo/com.mallardluna.nbtloupe.appdata.xml"

cd publish

curl -fLo "runtime-$APP_ARCH" "$APP_RUNTIME/runtime-$APP_ARCH"
curl -fLo "runtime-$APP_ARCH.sig" "$APP_RUNTIME/runtime-$APP_ARCH.sig"
if ! sha256sum --check --ignore-missing ../templates/linux/type2-runtime.sha256; then
    echo "::error::The AppImage Runtime hash doesn't match! Maybe there was an update?"
    exit 1
fi

export GNUPGHOME="$(mktemp -d)/gnupg"
mkdir -m 700 "$GNUPGHOME"
gpg --batch --import ../templates/linux/type2-runtime-signing-pubkey.asc
gpg --batch --verify "runtime-$APP_ARCH.sig" "runtime-$APP_ARCH"

mksquashfs NBTLoupe.AppDir payload.squashfs -root-owned -noappend -comp zstd
cat "runtime-$APP_ARCH" payload.squashfs > "$APP_OUT"
chmod +x "$APP_OUT"

"./$APP_OUT" --appimage-version

zip -j "NBTLoupe-v$VERSION-SYMBOLS-$RID.zip" "$RID/NBTLoupe.dbg"
