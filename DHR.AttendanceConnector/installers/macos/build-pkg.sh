#!/usr/bin/env bash
set -euo pipefail

# macOS PKG build template for DHR Attendance Connector
# Requires macOS with Xcode command line tools (pkgbuild, codesign, notarytool).
# Set DEVELOPER_ID_APP and DEVELOPER_ID_INSTALLER env vars for production signing & notarization.

VERSION="${1:-1.0.0}"
RID="${2:-osx-arm64}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST_DIR="$SCRIPT_DIR/../dist/$RID"
STAGE_DIR="$SCRIPT_DIR/../dist/macos-stage-$RID"

rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR/Library/Application Support/DHR/AttendanceConnector"
mkdir -p "$STAGE_DIR/Library/LaunchDaemons"

cp -r "$DIST_DIR/worker/"* "$STAGE_DIR/Library/Application Support/DHR/AttendanceConnector/"
cp -r "$DIST_DIR/app/"* "$STAGE_DIR/Library/Application Support/DHR/AttendanceConnector/"
cp "$SCRIPT_DIR/mv.gov.dhr.attendanceconnector.plist" "$STAGE_DIR/Library/LaunchDaemons/"

if [[ -n "${DEVELOPER_ID_APP:-}" ]]; then
    codesign --force --options runtime --sign "$DEVELOPER_ID_APP" "$STAGE_DIR/Library/Application Support/DHR/AttendanceConnector/DHR.AttendanceConnector.Worker"
    codesign --force --options runtime --sign "$DEVELOPER_ID_APP" "$STAGE_DIR/Library/Application Support/DHR/AttendanceConnector/DHR.AttendanceConnector.App"
else
    echo "WARNING: DEVELOPER_ID_APP not set; binaries will be UNSIGNED."
fi

PKG_OUT="$SCRIPT_DIR/../dist/DHR-Attendance-Connector-$VERSION-$RID.pkg"
pkgbuild --root "$STAGE_DIR" \
         --identifier "mv.gov.dhr.attendanceconnector" \
         --version "$VERSION" \
         "$PKG_OUT"

echo "Built macOS package: $PKG_OUT"
