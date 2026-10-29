// Fenestra for macOS.
//
// A tiny static file server that opens local HTML builds in a real browser.
// Built by the user, on the user's machine: see build.command.
package main

import (
	"fmt"
	"html"
	"io"
	"net"
	"net/http"
	"os"
	"os/exec"
	"os/signal"
	"path"
	"path/filepath"
	"runtime"
	"strconv"
	"strings"
	"sync/atomic"
	"syscall"
	"time"
)

const version = "0.4.2"

var lastRequest int64 = time.Now().UnixNano()

func main() {
	args := os.Args[1:]
	if len(args) == 0 {
		usage()
		os.Exit(1)
	}

	switch args[0] {
	case "--version":
		fmt.Println("Fenestra " + version + " (" + runtime.GOOS + ")")
	case "--help", "-h":
		usage()
	case "--browsers":
		listBrowsers()
	case "--install":
		install()
	case "--uninstall":
		uninstall()
	case "--browser":
		if len(args) < 2 {
			usage()
			os.Exit(1)
		}
		if !setBrowser(args[1]) {
			os.Exit(1)
		}
		if len(args) > 2 {
			run(args[2], false)
		}
	case "--serve":
		if len(args) < 2 {
			usage()
			os.Exit(1)
		}
		run(args[1], true)
	default:
		run(args[0], false)
	}
}

func usage() {
	fmt.Print(`Fenestra ` + version + ` (macos)

Usage:
  fenestra "~/projects/app/dist/index.html"
  fenestra "~/projects/app/dist"
  fenestra --serve "~/index.html"          force the server
  fenestra --browser firefox               set the preferred browser
  fenestra --browser chrome "~/app/index.html"
  fenestra --browsers                      list detected browsers
  fenestra --install                       Finder instructions
  fenestra --uninstall                     undo instructions

Browsers: edge, chrome, chromium, firefox, brave, vivaldi, opera, safari, default
(edge is the default; "default" hands off to the system browser)
`)
}

// ------------------------------------------------------------------ run

func run(target string, force bool) {
	abs, err := filepath.Abs(target)
	if err != nil {
		fatal(err)
	}

	info, err := os.Stat(abs)
	if err != nil {
		fatal(fmt.Errorf("path not found: %s", target))
	}

	var root, entry string
	if info.IsDir() {
		root = abs
	} else {
		root = filepath.Dir(abs)
		entry = filepath.Base(abs)
	}
	root = filepath.Clean(root)

	defaultDoc := findDefaultDoc(root)
	if entry == defaultDoc {
		entry = ""
	}

	// Plain HTML opens directly in the browser, no server.
	if !force && entry != "" && isHTML(entry) && !needsServer(filepath.Join(root, entry)) {
		launch(abs)
		return
	}

	lock := lockPath(root)

	if port, ok := readLock(lock); ok && ping(port) {
		launch(fmt.Sprintf("http://127.0.0.1:%d/%s", port, urlPath(entry)))
		return
	}

	ln, port := listen(root)
	writeLock(lock, port)

	srv := &http.Server{Handler: handler(root, defaultDoc)}
	go func() {
		if err := srv.Serve(ln); err != nil && err != http.ErrServerClosed {
			fatal(err)
		}
	}()
	logf("serving %s at 127.0.0.1:%d", root, port)

	go idleWatch()

	url := fmt.Sprintf("http://127.0.0.1:%d/%s", port, urlPath(entry))
	cmd := launch(url)
	if cmd != nil {
		go func() {
			done := make(chan error, 1)
			go func() { done <- cmd.Wait() }()
			select {
			case <-done:
				// Exited in under 6s: it handed off to a running instance.
				return
			case <-time.After(6 * time.Second):
				<-done
				os.Exit(0)
			}
		}()
	}

	waitSignal()
}

func waitSignal() {
	ch := make(chan os.Signal, 1)
	signal.Notify(ch, os.Interrupt, syscall.SIGTERM)
	<-ch
}

func idleWatch() {
	for {
		time.Sleep(time.Minute)
		last := time.Unix(0, atomic.LoadInt64(&lastRequest))
		if time.Since(last) > 4*time.Hour {
			os.Exit(0)
		}
	}
}

// ---------------------------------------------------------------- server

func handler(root, defaultDoc string) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		atomic.StoreInt64(&lastRequest, time.Now().UnixNano())

		if r.URL.Path == "/__fenestra/ping" {
			io.WriteString(w, "fenestra-ok")
			return
		}

		if r.Method != http.MethodGet && r.Method != http.MethodHead {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}

		if hasDotDot(r.URL.Path) {
			http.Error(w, "access denied", http.StatusForbidden)
			return
		}

		clean := path.Clean("/" + r.URL.Path)
		rel := strings.TrimPrefix(clean, "/")
		full := filepath.Join(root, filepath.FromSlash(rel))

		if strings.Contains(rel, ":") || strings.ContainsRune(rel, 0) || !isInside(root, full) {
			http.Error(w, "access denied", http.StatusForbidden)
			return
		}

		if st, err := os.Stat(full); err == nil && st.IsDir() {
			if defaultDoc != "" {
				idx := filepath.Join(full, defaultDoc)
				if fi, err := os.Stat(idx); err == nil && !fi.IsDir() {
					serveFile(w, r, idx)
					return
				}
			}
			listDir(w, full, clean)
			return
		}

		if st, err := os.Stat(full); err == nil && !st.IsDir() {
			serveFile(w, r, full)
			return
		}

		// SPA: extensionless routes fall back to index.html.
		if filepath.Ext(full) == "" && defaultDoc != "" {
			idx := filepath.Join(root, defaultDoc)
			if fi, err := os.Stat(idx); err == nil && !fi.IsDir() {
				serveFile(w, r, idx)
				return
			}
		}

		notFound(w, r.URL.Path, root)
	})
}

func serveFile(w http.ResponseWriter, r *http.Request, name string) {
	f, err := os.Open(name)
	if err != nil {
		http.Error(w, "could not read the file", http.StatusInternalServerError)
		return
	}
	defer f.Close()

	st, err := f.Stat()
	if err != nil || st.IsDir() {
		http.Error(w, "could not read the file", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", mimeFor(name))
	w.Header().Set("Cache-Control", "no-store, no-cache, must-revalidate")
	w.Header().Set("X-Fenestra", version)
	http.ServeContent(w, r, st.Name(), st.ModTime(), f)
}

func listDir(w http.ResponseWriter, dir, urlPath string) {
	entries, err := os.ReadDir(dir)
	if err != nil {
		http.Error(w, "could not list the directory", http.StatusInternalServerError)
		return
	}

	var b strings.Builder
	b.WriteString("<!doctype html><html lang=\"pt-BR\"><meta charset=\"utf-8\"><title>Fenestra</title>")
	b.WriteString("<style>body{font:15px/1.6 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:36px;max-width:960px;margin:auto}")
	b.WriteString("a{color:#9cc4ff;text-decoration:none}a:hover{text-decoration:underline}li{margin:5px 0}")
	b.WriteString("h1{font-weight:600}p{color:#8a8a90;word-break:break-all}</style>")
	b.WriteString("<h1>Fenestra</h1><p>" + html.EscapeString(urlPath) + "</p><ul>")

	if urlPath != "/" {
		b.WriteString("<li><a href=\"../\">..</a></li>")
	}

	for _, e := range entries {
		name := e.Name()
		if e.IsDir() {
			b.WriteString("<li><a href=\"" + urlPathEscape(name) + "/\">" + html.EscapeString(name) + "/</a></li>")
		} else {
			b.WriteString("<li><a href=\"" + urlPathEscape(name) + "\">" + html.EscapeString(name) + "</a></li>")
		}
	}

	b.WriteString("</ul>")
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	io.WriteString(w, b.String())
}

func notFound(w http.ResponseWriter, target, root string) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.WriteHeader(http.StatusNotFound)
	io.WriteString(w, "<!doctype html><meta charset=\"utf-8\"><title>404</title>"+
		"<body style=\"font:15px/1.6 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:40px\">"+
		"<h1>404</h1><p>Could not find <code>"+html.EscapeString(target)+"</code> at:</p>"+
		"<p style=\"color:#8a8a90;word-break:break-all\">"+html.EscapeString(root)+"</p></body>")
}

// ---------------------------------------------------------------- paths

func findDefaultDoc(root string) string {
	for _, name := range []string{"index.html", "index.htm", "index.xhtml", "default.html", "Default.html"} {
		if st, err := os.Stat(filepath.Join(root, name)); err == nil && !st.IsDir() {
			return name
		}
	}
	return ""
}

func isHTML(name string) bool {
	switch strings.ToLower(filepath.Ext(name)) {
	case ".html", ".htm", ".xhtml":
		return true
	}
	return false
}

// needsServer decides whether the file needs an HTTP origin.
func needsServer(file string) bool {
	st, err := os.Stat(file)
	if err != nil || st.Size() > 4*1024*1024 {
		return true
	}

	data, err := os.ReadFile(file)
	if err != nil {
		return true
	}
	text := strings.ToLower(string(data))

	needles := []string{
		"type=\"module\"", "type='module'", "crossorigin",
		"rel=\"manifest\"", "rel='manifest'",
		"src=\"/", "src='/", "href=\"/", "href='/",
		"fetch(", "xmlhttprequest", "serviceworker",
	}
	for _, n := range needles {
		if strings.Contains(text, n) {
			return true
		}
	}
	return false
}

func hasDotDot(p string) bool {
	for _, part := range strings.Split(p, "/") {
		if part == ".." {
			return true
		}
	}
	return false
}

func isInside(root, full string) bool {
	r := filepath.Clean(root)
	f := filepath.Clean(full)
	if r == f {
		return true
	}
	return strings.HasPrefix(f, r+string(os.PathSeparator))
}

func urlPath(entry string) string {
	if entry == "" {
		return ""
	}
	parts := strings.Split(filepath.ToSlash(entry), "/")
	for i, p := range parts {
		parts[i] = urlPathEscape(p)
	}
	return strings.Join(parts, "/")
}

func urlPathEscape(s string) string {
	return strings.ReplaceAll(pathEscape(s), "%2F", "/")
}

func pathEscape(s string) string {
	var b strings.Builder
	for _, r := range s {
		switch {
		case r >= 'a' && r <= 'z', r >= 'A' && r <= 'Z', r >= '0' && r <= '9',
			r == '-', r == '_', r == '.', r == '~', r == '/':
			b.WriteRune(r)
		default:
			buf := []byte(string(r))
			for _, c := range buf {
				fmt.Fprintf(&b, "%%%02X", c)
			}
		}
	}
	return b.String()
}

// ---------------------------------------------------------------- mime

func mimeFor(name string) string {
	switch strings.ToLower(filepath.Ext(name)) {
	case ".html", ".htm", ".xhtml":
		return "text/html; charset=utf-8"
	case ".css":
		return "text/css; charset=utf-8"
	case ".js", ".mjs":
		return "text/javascript; charset=utf-8"
	case ".json", ".map":
		return "application/json; charset=utf-8"
	case ".webmanifest":
		return "application/manifest+json; charset=utf-8"
	case ".xml":
		return "application/xml; charset=utf-8"
	case ".txt":
		return "text/plain; charset=utf-8"
	case ".csv":
		return "text/csv; charset=utf-8"
	case ".svg":
		return "image/svg+xml"
	case ".png":
		return "image/png"
	case ".jpg", ".jpeg":
		return "image/jpeg"
	case ".gif":
		return "image/gif"
	case ".webp":
		return "image/webp"
	case ".avif":
		return "image/avif"
	case ".ico":
		return "image/x-icon"
	case ".woff":
		return "font/woff"
	case ".woff2":
		return "font/woff2"
	case ".ttf":
		return "font/ttf"
	case ".otf":
		return "font/otf"
	case ".wasm":
		return "application/wasm"
	case ".mp4":
		return "video/mp4"
	case ".webm":
		return "video/webm"
	case ".mp3":
		return "audio/mpeg"
	case ".wav":
		return "audio/wav"
	case ".ogg":
		return "audio/ogg"
	case ".pdf":
		return "application/pdf"
	case ".zip":
		return "application/zip"
	default:
		return "application/octet-stream"
	}
}

// ---------------------------------------------------------------- ports

func hash32(s string) uint32 {
	h := uint32(2166136261)
	for _, c := range strings.ToLower(s) {
		h ^= uint32(c)
		h *= 16777619
	}
	return h
}

func listen(root string) (net.Listener, int) {
	base := 51000 + int(hash32(root)%12000)
	for i := 0; i < 400; i++ {
		p := base + i
		if p > 65000 {
			p = 51000 + (p % 12000)
		}
		if ln, err := net.Listen("tcp", fmt.Sprintf("127.0.0.1:%d", p)); err == nil {
			return ln, p
		}
	}
	ln, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fatal(err)
	}
	return ln, ln.Addr().(*net.TCPAddr).Port
}

func lockPath(root string) string {
	return filepath.Join(dataDir(), "sessions", fmt.Sprintf("%08x.txt", hash32(root)))
}

func readLock(file string) (int, bool) {
	data, err := os.ReadFile(file)
	if err != nil {
		return 0, false
	}
	parts := strings.SplitN(strings.TrimSpace(string(data)), "|", 2)
	if len(parts) != 2 {
		return 0, false
	}
	port, err := strconv.Atoi(parts[0])
	if err != nil || port <= 0 {
		return 0, false
	}
	return port, true
}

func writeLock(file string, port int) {
	os.MkdirAll(filepath.Dir(file), 0o755)
	os.WriteFile(file, []byte(fmt.Sprintf("%d|%d", port, os.Getpid())), 0o644)
}

func ping(port int) bool {
	client := http.Client{Timeout: 900 * time.Millisecond}
	resp, err := client.Get(fmt.Sprintf("http://127.0.0.1:%d/__fenestra/ping", port))
	if err != nil {
		return false
	}
	defer resp.Body.Close()
	body, err := io.ReadAll(io.LimitReader(resp.Body, 64))
	return err == nil && strings.Contains(string(body), "fenestra-ok")
}

// ---------------------------------------------------------------- browser

type browser struct {
	key string
	app string
}

func browserList() []browser {
	return []browser{
		{"edge", "Microsoft Edge"},
		{"chrome", "Google Chrome"},
		{"chromium", "Chromium"},
		{"firefox", "Firefox"},
		{"brave", "Brave Browser"},
		{"vivaldi", "Vivaldi"},
		{"opera", "Opera"},
		{"safari", "Safari"},
	}
}

func appName(key string) string {
	for _, b := range browserList() {
		if b.key == key {
			return b.app
		}
	}
	return ""
}

func lookup(key string) (string, bool) {
	home, _ := os.UserHomeDir()
	for _, b := range browserList() {
		if b.key != key {
			continue
		}
		for _, base := range []string{"/Applications", filepath.Join(home, "Applications"), "/System/Applications"} {
			p := filepath.Join(base, b.app+".app")
			if _, err := os.Stat(p); err == nil {
				return p, true
			}
		}
	}
	return "", false
}

func readBrowserPref() string {
	data, err := os.ReadFile(filepath.Join(configDir(), "browser"))
	if err != nil {
		return "edge"
	}
	value := strings.ToLower(strings.TrimSpace(string(data)))
	if value == "" {
		return "edge"
	}
	return value
}

func setBrowser(name string) bool {
	name = strings.ToLower(strings.TrimSpace(name))

	valid := name == "default"
	for _, b := range browserList() {
		if b.key == name {
			valid = true
		}
	}
	if !valid {
		fmt.Fprintf(os.Stderr, "unknown browser: %s\n", name)
		return false
	}

	if name != "default" {
		if _, ok := lookup(name); !ok {
			fmt.Fprintf(os.Stderr, "could not find %s installed on this machine\n", name)
			return false
		}
	}

	if err := os.MkdirAll(configDir(), 0o755); err != nil {
		fmt.Fprintln(os.Stderr, err)
		return false
	}
	if err := os.WriteFile(filepath.Join(configDir(), "browser"), []byte(name+"\n"), 0o644); err != nil {
		fmt.Fprintln(os.Stderr, err)
		return false
	}

	fmt.Printf("Fenestra browser: %s\n", name)
	return true
}

func listBrowsers() {
	for _, b := range browserList() {
		if p, ok := lookup(b.key); ok {
			fmt.Printf("%-9s %s\n", b.key, p)
		} else {
			fmt.Printf("%-9s not found\n", b.key)
		}
	}
	fmt.Printf("\ncurrent: %s\n", readBrowserPref())
}

// launch opens target in the configured browser. Returns the process when it
// was started by us (so the caller can exit when it closes).
func launch(target string) *exec.Cmd {
	if os.Getenv("FENESTRA_NO_OPEN") == "1" {
		logf("FENESTRA_NO_OPEN=1, not opening a browser")
		return nil
	}

	name := readBrowserPref()
	if name != "default" {
		if _, ok := lookup(name); ok {
			cmd := exec.Command("open", "-a", appName(name), target)
			if err := cmd.Start(); err == nil {
				logf("opening %s %s", appName(name), target)
				return cmd
			}
		}
	}

	// Fallback: first known browser, then the system default.
	for _, b := range browserList() {
		if _, ok := lookup(b.key); ok {
			cmd := exec.Command("open", "-a", b.app, target)
			if err := cmd.Start(); err == nil {
				logf("opening %s %s", b.app, target)
				return cmd
			}
		}
	}

	cmd := exec.Command("open", target)
	if err := cmd.Start(); err == nil {
		logf("opening open %s", target)
		return cmd
	}

	fmt.Fprintf(os.Stderr, "could not open a browser for %s\n", target)
	return nil
}

// ---------------------------------------------------------------- install

func install() {
	fmt.Println("On macOS the .html association is set from the Finder:")
	fmt.Println()
	fmt.Println("  1. Drag Fenestra.app to /Applications")
	fmt.Println("  2. Right-click an index.html > Open with > Fenestra")
	fmt.Println("  3. Check \"Always use this app\"")
	fmt.Println()
	fmt.Println("Fenestra.app is created by ./build.command.")
}

func uninstall() {
	fmt.Println("To undo: right-click an .html > Open with > choose another app.")
}

// ---------------------------------------------------------------- misc

func dataDir() string {
	home, _ := os.UserHomeDir()
	return filepath.Join(home, "Library", "Application Support", "Fenestra")
}

func configDir() string {
	return dataDir()
}

func logf(format string, args ...any) {
	dir := dataDir()
	os.MkdirAll(dir, 0o755)
	f, err := os.OpenFile(filepath.Join(dir, "fenestra.log"), os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o644)
	if err != nil {
		return
	}
	defer f.Close()
	fmt.Fprintf(f, "%s  %s\n", time.Now().Format("2006-01-02 15:04:05"), fmt.Sprintf(format, args...))
}

func fatal(err error) {
	fmt.Fprintln(os.Stderr, "fenestra:", err)
	os.Exit(1)
}
