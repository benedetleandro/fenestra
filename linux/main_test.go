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
	write(t, simple, "<!doctype html><h1>hi</h1><img src=\"./a.png\">")
	if needsServer(simple) {
		t.Error("plain HTML should not need a server")
	}

	app := filepath.Join(dir, "app.html")
	write(t, app, `<script type="module" src="/assets/app.js"></script>`)
	if !needsServer(app) {
		t.Error("an ES module should need a server")
	}

	fetch := filepath.Join(dir, "fetch.html")
	write(t, fetch, "<script>fetch('./data.json')</script>")
	if !needsServer(fetch) {
		t.Error("fetch should need a server")
	}
}

func TestIsInside(t *testing.T) {
	root := filepath.Join("C:", "root")
	if !isInside(root, filepath.Join(root, "a", "b")) {
		t.Error("a child should be inside")
	}
	if isInside(root, filepath.Join(root, "..", "outside")) {
		t.Error(".. should not be inside")
	}
}

func TestHostAllowed(t *testing.T) {
	allowed := []string{"127.0.0.1", "127.0.0.1:8080", "localhost", "localhost:51000"}
	for _, h := range allowed {
		if !hostAllowed(h) {
			t.Errorf("hostAllowed(%q) = false, want true", h)
		}
	}

	blocked := []string{"evil.example", "evil.example:80", "127.0.0.1.evil.com", "", "0.0.0.0:80"}
	for _, h := range blocked {
		if hostAllowed(h) {
			t.Errorf("hostAllowed(%q) = true, want false", h)
		}
	}
}

func TestResolvesInside(t *testing.T) {
	base := t.TempDir()
	root := filepath.Join(base, "root")
	if err := os.MkdirAll(root, 0o755); err != nil {
		t.Fatal(err)
	}

	outside := filepath.Join(base, "secret.txt")
	write(t, outside, "SECRET")
	inside := filepath.Join(root, "ok.txt")
	write(t, inside, "ok")

	if !resolvesInside(root, inside) {
		t.Error("a regular file inside the root should pass")
	}
	if resolvesInside(root, outside) {
		t.Error("a file outside the root should be rejected")
	}

	link := filepath.Join(root, "leak.txt")
	if err := os.Symlink(outside, link); err != nil {
		t.Skipf("symlink not available: %v", err)
	}
	if resolvesInside(root, link) {
		t.Error("a symlink pointing outside the root should be rejected")
	}
}

func TestURLPath(t *testing.T) {
	if got := urlPath(""); got != "" {
		t.Errorf("urlPath(empty) = %q", got)
	}
	if got := urlPath("index.html"); got != "index.html" {
		t.Errorf("urlPath(index.html) = %q", got)
	}
	if got := urlPath(`folder with space/a.html`); !strings.Contains(got, "%20") {
		t.Errorf("urlPath should escape spaces, got %q", got)
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

	resp, _ = get("/route/anything")
	if resp.StatusCode != 200 {
		t.Errorf("SPA fallback = %d", resp.StatusCode)
	}

	resp, _ = get("/nothing.png")
	if resp.StatusCode != 404 {
		t.Errorf("missing file = %d", resp.StatusCode)
	}

	// Traversal: the handler gets the raw path, without the client cleaning it.
	req := httptest.NewRequest("GET", "/", nil)
	req.URL.Path = "/../secret.txt"
	rec := httptest.NewRecorder()
	handler(root, "index.html").ServeHTTP(rec, req)
	if strings.Contains(rec.Body.String(), "SECRET") {
		t.Fatal("traversal served a file outside the root")
	}
}

func TestSourceEntry(t *testing.T) {
	dir := t.TempDir()
	src := filepath.Join(dir, "index.html")
	write(t, src, `<script type="module" src="/src/main.tsx"></script>`)
	if !looksLikeSourceEntry(src) {
		t.Error("Vite entry should be detected")
	}
	if _, ok := buildForSource(src); ok {
		t.Error("no build exists yet")
	}

	if err := os.MkdirAll(filepath.Join(dir, "dist"), 0o755); err != nil {
		t.Fatal(err)
	}
	build := filepath.Join(dir, "dist", "index.html")
	write(t, build, `<script type="module" src="/assets/index.js"></script>`)
	if looksLikeSourceEntry(build) {
		t.Error("a built index should not look like a source entry")
	}

	got, ok := buildForSource(src)
	if !ok || got != filepath.Join(dir, "dist") {
		t.Errorf("buildForSource = %q, %v", got, ok)
	}
}

func TestHTTPFeatures(t *testing.T) {
	root := t.TempDir()
	write(t, filepath.Join(root, "index.html"), "<h1>fixture</h1>")
	write(t, filepath.Join(root, "a.js"), "console.log(1)")

	srv := httptest.NewServer(handler(root, "index.html"))
	defer srv.Close()
	client := srv.Client()

	resp, err := client.Get(srv.URL + "/a.js")
	if err != nil {
		t.Fatal(err)
	}
	resp.Body.Close()

	etag := resp.Header.Get("ETag")
	if etag == "" {
		t.Fatal("missing ETag")
	}
	if got := resp.Header.Get("Accept-Ranges"); got != "bytes" {
		t.Errorf("Accept-Ranges = %q, want bytes", got)
	}

	req, _ := http.NewRequest("GET", srv.URL+"/a.js", nil)
	req.Header.Set("If-None-Match", etag)
	resp, err = client.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusNotModified {
		t.Errorf("If-None-Match = %d, want 304", resp.StatusCode)
	}

	req, _ = http.NewRequest("GET", srv.URL+"/a.js", nil)
	req.Header.Set("Range", "bytes=0-3")
	resp, err = client.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	body, _ := io.ReadAll(resp.Body)
	resp.Body.Close()
	if resp.StatusCode != http.StatusPartialContent {
		t.Errorf("Range = %d, want 206", resp.StatusCode)
	}
	if string(body) != "cons" {
		t.Errorf("Range body = %q, want %q", body, "cons")
	}
	if cr := resp.Header.Get("Content-Range"); cr != "bytes 0-3/14" {
		t.Errorf("Content-Range = %q, want bytes 0-3/14", cr)
	}

	req, _ = http.NewRequest("GET", srv.URL+"/a.js", nil)
	req.Header.Set("Range", "bytes=999-")
	resp, err = client.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusRequestedRangeNotSatisfiable {
		t.Errorf("unsatisfiable Range = %d, want 416", resp.StatusCode)
	}

	req, _ = http.NewRequest("GET", srv.URL+"/", nil)
	req.Host = "evil.example"
	resp, err = client.Do(req)
	if err != nil {
		t.Fatal(err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusForbidden {
		t.Errorf("host tampering = %d, want 403", resp.StatusCode)
	}
}
