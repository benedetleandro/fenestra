package main

import (
	"io"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func write(t *testing.T, name, body string) {
	t.Helper()
	if err := os.WriteFile(name, []byte(body), 0o644); err != nil {
		t.Fatal(err)
	}
}

func TestMimeFor(t *testing.T) {
	cases := map[string]string{
		"a.html":  "text/html; charset=utf-8",
		"a.js":    "text/javascript; charset=utf-8",
		"a.css":   "text/css; charset=utf-8",
		"a.json":  "application/json; charset=utf-8",
		"a.woff2": "font/woff2",
		"a.wasm":  "application/wasm",
		"a.bin":   "application/octet-stream",
	}
	for name, want := range cases {
		if got := mimeFor(name); got != want {
			t.Errorf("mimeFor(%q) = %q, want %q", name, got, want)
		}
	}
}

func TestNeedsServer(t *testing.T) {
	dir := t.TempDir()

	simple := filepath.Join(dir, "simple.html")
	write(t, simple, "<!doctype html><h1>oi</h1><img src=\"./a.png\">")
	if needsServer(simple) {
		t.Error("HTML simples nao deveria precisar de servidor")
	}

	app := filepath.Join(dir, "app.html")
	write(t, app, `<script type="module" src="/assets/app.js"></script>`)
	if !needsServer(app) {
		t.Error("modulo ES deveria precisar de servidor")
	}

	fetch := filepath.Join(dir, "fetch.html")
	write(t, fetch, "<script>fetch('./data.json')</script>")
	if !needsServer(fetch) {
		t.Error("fetch deveria precisar de servidor")
	}
}

func TestIsInside(t *testing.T) {
	root := filepath.Join("C:", "root")
	if !isInside(root, filepath.Join(root, "a", "b")) {
		t.Error("filho deveria estar dentro")
	}
	if isInside(root, filepath.Join(root, "..", "fora")) {
		t.Error(".. nao deveria estar dentro")
	}
}

func TestURLPath(t *testing.T) {
	if got := urlPath(""); got != "" {
		t.Errorf("urlPath(vazio) = %q", got)
	}
	if got := urlPath("index.html"); got != "index.html" {
		t.Errorf("urlPath(index.html) = %q", got)
	}
	if got := urlPath(`pasta com espaço/a.html`); !strings.Contains(got, "%20") {
		t.Errorf("urlPath deveria escapar espaco, veio %q", got)
	}
}

func TestHandler(t *testing.T) {
	base := t.TempDir()
	root := filepath.Join(base, "public")
	if err := os.MkdirAll(filepath.Join(root, "assets"), 0o755); err != nil {
		t.Fatal(err)
	}

	write(t, filepath.Join(root, "index.html"), "<!doctype html><title>fixture</title><h1>fixture</h1>")
	write(t, filepath.Join(root, "assets", "app.js"), "console.log(1)")
	write(t, filepath.Join(root, "assets", "app.css"), "body{}")
	write(t, filepath.Join(root, "data.json"), `{"ok":true}`)
	write(t, filepath.Join(base, "secret.txt"), "SECRET")

	srv := httptest.NewServer(handler(root, "index.html"))
	defer srv.Close()

	get := func(p string) (*http.Response, string) {
		t.Helper()
		resp, err := http.Get(srv.URL + p)
		if err != nil {
			t.Fatal(err)
		}
		defer resp.Body.Close()
		body, _ := io.ReadAll(resp.Body)
		return resp, string(body)
	}

	resp, body := get("/")
	if resp.StatusCode != 200 || !strings.Contains(body, "fixture") {
		t.Errorf("/ = %d, body %q", resp.StatusCode, body)
	}
	if ct := resp.Header.Get("Content-Type"); !strings.HasPrefix(ct, "text/html") {
		t.Errorf("/ content-type = %q", ct)
	}

	resp, _ = get("/assets/app.js")
	if ct := resp.Header.Get("Content-Type"); !strings.HasPrefix(ct, "text/javascript") {
		t.Errorf("js content-type = %q", ct)
	}

	resp, _ = get("/assets/app.css")
	if ct := resp.Header.Get("Content-Type"); !strings.HasPrefix(ct, "text/css") {
		t.Errorf("css content-type = %q", ct)
	}

	resp, _ = get("/data.json")
	if ct := resp.Header.Get("Content-Type"); !strings.HasPrefix(ct, "application/json") {
		t.Errorf("json content-type = %q", ct)
	}

	resp, _ = get("/rota/qualquer")
	if resp.StatusCode != 200 {
		t.Errorf("SPA fallback = %d", resp.StatusCode)
	}

	resp, _ = get("/nada.png")
	if resp.StatusCode != 404 {
		t.Errorf("arquivo ausente = %d", resp.StatusCode)
	}

	// Traversal: o handler recebe o caminho cru, sem o cliente limpar.
	req := httptest.NewRequest("GET", "/", nil)
	req.URL.Path = "/../secret.txt"
	rec := httptest.NewRecorder()
	handler(root, "index.html").ServeHTTP(rec, req)
	if strings.Contains(rec.Body.String(), "SECRET") {
		t.Fatal("traversal serviu um arquivo fora da raiz")
	}
}
