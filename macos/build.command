#!/bin/bash
# Fenestra — build para macOS.
# Duplo clique, ou rode: bash build.command
set -euo pipefail
cd "$(dirname "$0")"

trap 'echo; echo "FALHOU na linha $LINENO. Copie a mensagem acima e me mande."; exit 1' ERR

echo "Fenestra — build para macOS"
echo

if ! command -v go >/dev/null 2>&1; then
  cat <<'MSG'
Go nao encontrado.

  1. Baixe o instalador oficial: https://go.dev/dl/
  2. Instale (nao aparece aviso: o instalador e assinado pela Google)
  3. Rode este script de novo
MSG
  read -r -p "Abrir go.dev/dl agora? [s/N] " answer || true
  case "${answer:-}" in
    s|S|y|Y) open "https://go.dev/dl/" ;;
  esac
  exit 1
fi

echo "Go: $(go version)"
echo

echo "compilando fenestra..."
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
chmod +x fenestra
echo "ok: $(pwd)/fenestra"
echo

if command -v osacompile >/dev/null 2>&1; then
  echo "montando Fenestra.app..."

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

    # Editar o Info.plist invalida a assinatura que o osacompile criou.
    # Sem re-assinar, o macOS recusa abrir o app ("assinatura invalida").
    if command -v codesign >/dev/null 2>&1; then
      if codesign --force --deep --sign - "Fenestra.app" 2>/tmp/fenestra-codesign.log; then
        echo "assinatura ad-hoc refeita"
      else
        echo "aviso: codesign falhou; veja /tmp/fenestra-codesign.log"
      fi
    fi

    LSREGISTER="/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister"
    if [ -x "$LSREGISTER" ]; then
      "$LSREGISTER" -f "$(pwd)/Fenestra.app" >/dev/null 2>&1 || true
    fi
    touch "Fenestra.app"

    echo "ok: $(pwd)/Fenestra.app"
  else
    echo "osacompile falhou. Veja /tmp/fenestra-osacompile.log"
    echo "O binario ./fenestra foi gerado e funciona sozinho."
  fi
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
