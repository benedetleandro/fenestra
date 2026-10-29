#!/bin/bash
# Fenestra — macOS build.
# Double-click it, or run: bash build.command
set -euo pipefail
cd "$(dirname "$0")"

VERSION=$(sed -n 's/^const version = "\([^"]*\)".*/\1/p' main.go)
[ -n "$VERSION" ] || VERSION="0.0.0"

trap 'echo; echo "FAILED at line $LINENO. Copy the message above and send it."; exit 1' ERR

echo "Fenestra — macOS build"
echo

if ! command -v go >/dev/null 2>&1; then
  cat <<'MSG'
Go not found.

  1. Download the official installer: https://go.dev/dl/
  2. Install it (no warning: the installer is signed by Google)
  3. Run this script again
MSG
  read -r -p "Open go.dev/dl now? [y/N] " answer || true
  case "${answer:-}" in
    s|S|y|Y) open "https://go.dev/dl/" ;;
  esac
  exit 1
fi

echo "Go: $(go version)"
echo

echo "building fenestra..."
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
chmod +x fenestra
echo "ok: $(pwd)/fenestra"
echo

if command -v osacompile >/dev/null 2>&1; then
  echo "building Fenestra.app..."

  if [ -d "Fenestra.app" ]; then
    rm -rf "Fenestra.app"
  fi

  if osacompile -o "Fenestra.app" droplet.applescript 2>/tmp/fenestra-osacompile.log; then
    cp fenestra "Fenestra.app/Contents/Resources/fenestra"
    chmod +x "Fenestra.app/Contents/Resources/fenestra"

    PLIST="Fenestra.app/Contents/Info.plist"
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes array" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0 dict" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:CFBundleTypeName string HTML" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:CFBundleTypeRole string Viewer" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:LSHandlerRank string Alternate" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:LSItemContentTypes array" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:LSItemContentTypes:0 string public.html" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :CFBundleDocumentTypes:0:LSItemContentTypes:1 string public.xhtml" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier dev.fenestra.app" "$PLIST" 2>/dev/null || /usr/libexec/PlistBuddy -c "Add :CFBundleIdentifier string dev.fenestra.app" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$PLIST" 2>/dev/null || /usr/libexec/PlistBuddy -c "Add :CFBundleShortVersionString string $VERSION" "$PLIST" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION" "$PLIST" 2>/dev/null || /usr/libexec/PlistBuddy -c "Add :CFBundleVersion string $VERSION" "$PLIST" 2>/dev/null || true

    if [ -f "icon.png" ] && command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
      ICONSET_DIR="$(mktemp -d)/Fenestra.iconset"
      mkdir -p "$ICONSET_DIR"
      for s in 16 32 128 256 512; do
        sips -z "$s" "$s" icon.png --out "$ICONSET_DIR/icon_${s}x${s}.png" >/dev/null 2>&1 || true
        sips -z "$((s * 2))" "$((s * 2))" icon.png --out "$ICONSET_DIR/icon_${s}x${s}@2x.png" >/dev/null 2>&1 || true
      done
      if iconutil -c icns "$ICONSET_DIR" -o /tmp/fenestra.icns 2>/dev/null; then
        cp /tmp/fenestra.icns "Fenestra.app/Contents/Resources/droplet.icns"
        echo "icon applied"
      fi
      rm -rf "$(dirname "$ICONSET_DIR")"
    fi

    # Editing Info.plist invalidates the signature osacompile created.
    # Without re-signing, macOS refuses to open the app.
    if command -v codesign >/dev/null 2>&1; then
      if codesign --force --deep --sign - "Fenestra.app" 2>/tmp/fenestra-codesign.log; then
        echo "ad-hoc signature re-applied"
      else
        echo "warning: codesign failed; see /tmp/fenestra-codesign.log"
      fi
    fi

    LSREGISTER="/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister"
    if [ -x "$LSREGISTER" ]; then
      "$LSREGISTER" -f "$(pwd)/Fenestra.app" >/dev/null 2>&1 || true
    fi
    touch "Fenestra.app"

    echo "ok: $(pwd)/Fenestra.app"
  else
    echo "osacompile failed. See /tmp/fenestra-osacompile.log"
    echo "The ./fenestra binary was built and works on its own."
  fi
  echo
else
  echo "osacompile not found; only the binary was built."
  echo
fi

cat <<'MSG'
Done.

  Test:   ./fenestra ~/path/dist/index.html
  Finder: drag Fenestra.app to /Applications
          right-click an index.html > Open with > Fenestra > Always

If macOS complains the first time, go to
System Settings > Privacy & Security > Open Anyway.
MSG
