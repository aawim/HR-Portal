#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"
ARCH="${2:-amd64}"
RID="linux-x64"
if [[ "$ARCH" == "arm64" ]]; then RID="linux-arm64"; fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST_DIR="$SCRIPT_DIR/../dist/$RID"
PKG_DIR="$SCRIPT_DIR/../dist/deb_${VERSION}_${ARCH}"

rm -rf "$PKG_DIR"
mkdir -p "$PKG_DIR/DEBIAN" \
         "$PKG_DIR/opt/dhr-attendance-connector" \
         "$PKG_DIR/etc/systemd/system" \
         "$PKG_DIR/usr/share/applications"

cp -r "$DIST_DIR/worker/"* "$PKG_DIR/opt/dhr-attendance-connector/"
cp -r "$DIST_DIR/app/"* "$PKG_DIR/opt/dhr-attendance-connector/"
cp "$SCRIPT_DIR/dhr-attendance-connector.service" "$PKG_DIR/etc/systemd/system/"

cat > "$PKG_DIR/DEBIAN/control" <<EOF
Package: dhr-attendance-connector
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: Digital HR (DHR) Maldives <support@dhr.gov.mv>
Description: DHR Attendance Connector - Biometric device collector and secure sync service
 Lightweight cross-platform connector for local biometric attendance terminals.
EOF

cat > "$PKG_DIR/DEBIAN/postinst" <<'EOF'
#!/usr/bin/env bash
set -e
if ! id -u dhr-connector >/dev/null 2>&1; then
    useradd --system --no-create-home --shell /usr/sbin/nologin dhr-connector
fi
mkdir -p /var/lib/dhr-attendance-connector
chown -R dhr-connector:dhr-connector /var/lib/dhr-attendance-connector
chmod 700 /var/lib/dhr-attendance-connector
chmod +x /opt/dhr-attendance-connector/DHR.AttendanceConnector.Worker
chmod +x /opt/dhr-attendance-connector/DHR.AttendanceConnector.App
systemctl daemon-reload
systemctl enable --now dhr-attendance-connector.service
EOF
chmod 755 "$PKG_DIR/DEBIAN/postinst"

dpkg-deb --build "$PKG_DIR" "$SCRIPT_DIR/../dist/dhr-attendance-connector_${VERSION}_${ARCH}.deb"
echo "Built DEB package: $SCRIPT_DIR/../dist/dhr-attendance-connector_${VERSION}_${ARCH}.deb"
