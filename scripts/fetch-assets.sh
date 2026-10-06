#!/bin/sh
set -eu
cd "$(dirname "$0")/.."

missing=""
for dir in "$@"; do
    [ -d "ASSETS/$dir" ] || missing="$missing $dir"
done

if [ -z "$missing" ]; then
    exit 0
fi

if [ ! -e ASSETS/.git ]; then
    git -c "submodule.ASSETS.update=!git sparse-checkout set --cone$missing && git checkout" submodule update --init --filter=blob:none ASSETS
else
    git -C ASSETS sparse-checkout add $missing
fi
