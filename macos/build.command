#!/bin/bash
# Fenestra — build para macOS.
# Duplo clique neste arquivo, ou rode: bash build.command
set -euo pipefail
cd "$(dirname "$0")"

echo "Fenestra — build para macOS"
echo

if ! command -v go >/dev/null 2>&1; then
  cat <<'MSG'
Go nao encontrado.

  1. Baixe o instalador oficial: https://go.dev/dl/
  2. Instale (nao aparece aviso: o instalador e assinado pela Google)
  3. Rode este script de novo
MSG
  read -r -p "Abrir go.dev/dl agora? [s/N] " answer
  case "$answer" in
    s|S|y|Y) open "https://go.dev/dl/" ;;
  esac
  exit 1
fi

echo "compilando fenestra..."
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
chmod +x fenestra
echo "ok: $(pwd)/fenestra"
echo

if command -v osacompile >/dev/null 2>&1; then
  echo "montando Fenestra.app..."
  rm -rf "Fenestra.app"
  osacompile -o "Fenestra.app" droplet.applescript
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
  touch "Fenestra.app"
  echo "ok: $(pwd)/Fenestra.app"
  echo
else
  echo "osacompile nao encontrado; so o binario foi gerado."
  echo
fi

cat <<'MSG'
Pronto.

  Teste:   ./fenestra ~/caminho/dist/index.html
  Finder:  arraste Fenestra.app para /Applications
           botao direito num index.html > Abrir com > Fenestra > Sempre

Se o macOS reclamar na primeira vez, va em
Ajustes > Privacidade e Seguranca > Abrir assim mesmo.
MSG
