#!/usr/bin/env bash
set -euo pipefail
# Bundles pinned upstream AdminLTE/Bootstrap/HTMX releases as app-owned static assets.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
npm install --no-audit --no-fund
TARGET="$ROOT/src/PeopleHub/wwwroot/vendor"
mkdir -p "$TARGET/fonts"
cp node_modules/admin-lte/dist/css/adminlte.min.css "$TARGET/adminlte.min.css"
cp node_modules/admin-lte/dist/js/adminlte.min.js "$TARGET/adminlte.min.js"
cp node_modules/bootstrap/dist/css/bootstrap.min.css "$TARGET/bootstrap.min.css"
cp node_modules/bootstrap/dist/js/bootstrap.bundle.min.js "$TARGET/bootstrap.bundle.min.js"
cp node_modules/bootstrap-icons/font/bootstrap-icons.min.css "$TARGET/bootstrap-icons.css"
cp node_modules/bootstrap-icons/font/fonts/* "$TARGET/fonts/"
cp node_modules/htmx.org/dist/htmx.min.js "$TARGET/htmx.min.js"
printf 'Static vendor files prepared in %s\n' "$TARGET"
