package main

import (
	_ "embed"
	"fmt"
	"net/http"
	"os"
	"path/filepath"
	"strings"
)

//go:embed whatsnew.html
var whatsnewHTML []byte

var pendingWhatsNew string

func initWhatsNew() {
	path := filepath.Join(configDir(), "last-run-version")
	data, err := os.ReadFile(path)
	if err != nil {
		_ = os.MkdirAll(configDir(), 0o750)
		_ = os.WriteFile(path, []byte(version), 0o600)
		return
	}
	last := strings.TrimSpace(string(data))
	if last != "" && last != version {
		pendingWhatsNew = version
	}
}

func maybeOpenWhatsNew(port int) {
	if pendingWhatsNew == "" {
		return
	}
	if os.Getenv("FENESTRA_NO_UPDATE_TAB") == "1" || os.Getenv("FENESTRA_NO_OPEN") == "1" {
		return
	}
	launch(fmt.Sprintf("http://127.0.0.1:%d/__fenestra/whatsnew", port))
	_ = os.MkdirAll(configDir(), 0o750)
	_ = os.WriteFile(filepath.Join(configDir(), "last-run-version"), []byte(pendingWhatsNew), 0o600)
	pendingWhatsNew = ""
}

func serveWhatsNew(w http.ResponseWriter) {
	if pendingWhatsNew == "" {
		http.Error(w, "no what's new page", http.StatusNotFound)
		return
	}
	repl := strings.NewReplacer(
		"{{VERSION}}", pendingWhatsNew,
		"{{NOTES}}", "https://github.com/benedetleandro/fenestra/releases/tag/v"+pendingWhatsNew,
	)
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Header().Set("Cache-Control", "no-store")
	_, _ = w.Write([]byte(repl.Replace(string(whatsnewHTML))))
}
