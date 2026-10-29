// Fenestra for Linux.
//
// A tiny static file server that opens local HTML builds in a real browser.
// Same idea as the Windows build, but with no registry, no WinForms and no
// .NET: a single static binary.
package main

import (
	"bufio"
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
	"regexp"
	"runtime"
	"strconv"
	"strings"
	"sync/atomic"
	"syscall"
	"time"
)

var version = "0.0.0-dev"

var lastRequest int64 = time.Now().UnixNano()
var forcedPort int

func main() {
	args := extractPort(os.Args[1:])
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
	fmt.Print(`Fenestra ` + version + ` (linux)

Usage:
  fenestra "~/projects/app/dist/index.html"
  fenestra "~/projects/app/dist"
  fenestra --serve "~/index.html"          force the server
  fenestra --port 8080 "~/index.html"      use a specific port
  fenestra --browser firefox               set the preferred browser
  fenestra --browser chrome "~/app/index.html"
  fenestra --browsers                      list detected browsers
  fenestra --install                       register the .html handler
  fenestra --uninstall                     remove it

Browsers: edge, chrome, chromium, firefox, brave, vivaldi, opera, default
("default" uses the system browser; edge is opt-in)
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

	chooseBrowserOnFirstRun()

	var root, entry string
	if info.IsDir() {
		root = abs
	} else {
		root = filepath.Dir(abs)
		entry = filepath.Base(abs)
	}
	root = filepath.Clean(root)

	if entry != "" && isHTML(entry) {
		sourceFile := filepath.Join(root, entry)
		if buildDir, ok := buildForSource(sourceFile); ok {
			root = buildDir
			entry = "index.html"
			logf("source entry detected; using %s", buildDir)
		} else if looksLikeSourceEntry(sourceFile) {
			fatal(fmt.Errorf("%s looks like a source entry (it references /src/); run the build first or open dist/index.html", target))
		}
	}

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

	var ln net.Listener
	var port int

	if forcedPort != 0 {
		var err error
		ln, err = net.Listen("tcp", fmt.Sprintf("127.0.0.1:%d", forcedPort))
		if err != nil {
			fatal(fmt.Errorf("port %d is not available: %v", forcedPort, err))
		}
		port = forcedPort
		writeLock(lock, port)
	} else {
		if p, ok := readLock(lock); ok && ping(p, root) {
			launch(fmt.Sprintf("http://127.0.0.1:%d/%s", p, urlPath(entry)))
			return
		}
		ln, port = listen(root)
		writeLock(lock, port)
	}

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

func extractPort(args []string) []string {
	out := make([]string, 0, len(args))
	for i := 0; i < len(args); i++ {
		if args[i] == "--port" && i+1 < len(args) {
			n, err := strconv.Atoi(args[i+1])
			if err != nil || n < 1 || n > 65535 {
				fatal(fmt.Errorf("invalid port: %s", args[i+1]))
			}
			forcedPort = n
			i++
			continue
		}
		out = append(out, args[i])
	}
	return out
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

		if !hostAllowed(r.Host) {
			http.Error(w, "access denied", http.StatusForbidden)
			return
		}

		if r.URL.Path == "/__fenestra/ping" {
			io.WriteString(w, "fenestra-ok\n"+root)
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

		if isHidden(rel) {
			http.Error(w, "access denied", http.StatusForbidden)
			return
		}

		if strings.Contains(rel, ":") || strings.ContainsRune(rel, 0) || !isInside(root, full) {
			http.Error(w, "access denied", http.StatusForbidden)
			return
		}

		if !resolvesInside(root, full) {
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
	w.Header().Set("Cache-Control", "no-cache")
	w.Header().Set("ETag", fmt.Sprintf("\"%x-%x\"", st.Size(), st.ModTime().UnixNano()))
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
	b.WriteString("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Fenestra</title>")
	b.WriteString("<style>body{font:15px/1.6 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:36px;max-width:960px;margin:auto}")
	b.WriteString("a{color:#9cc4ff;text-decoration:none}a:hover{text-decoration:underline}li{margin:5px 0}")
	b.WriteString("h1{font-weight:600}p{color:#8a8a90;word-break:break-all}</style>")
	b.WriteString("<h1>Fenestra</h1><p>" + html.EscapeString(urlPath) + "</p><ul>")

	if urlPath != "/" {
		b.WriteString("<li><a href=\"../\">..</a></li>")
	}

	for _, e := range entries {
		name := e.Name()
		if strings.HasPrefix(name, ".") {
			continue
		}
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

// isHidden blocks dotfiles and dot-directories, except .well-known.
func isHidden(rel string) bool {
	for _, part := range strings.Split(rel, "/") {
		if strings.HasPrefix(part, ".") && part != ".well-known" {
			return true
		}
	}
	return false
}

var sourceEntryRe = regexp.MustCompile(`(?i)<script[^>]+src\s*=\s*["'](?:\./|/)?src/`)

func looksLikeSourceEntry(file string) bool {
	data, err := os.ReadFile(file)
	if err != nil {
		return false
	}
	return sourceEntryRe.Match(data)
}

func buildForSource(file string) (string, bool) {
	if !looksLikeSourceEntry(file) {
		return "", false
	}
	dir := filepath.Dir(file)
	for _, name := range []string{"dist", "build", "out"} {
		candidate := filepath.Join(dir, name)
		if fi, err := os.Stat(filepath.Join(candidate, "index.html")); err == nil && !fi.IsDir() {
			return candidate, true
		}
	}
	return "", false
}

func isInside(root, full string) bool {
	r := filepath.Clean(root)
	f := filepath.Clean(full)
	if r == f {
		return true
	}
	return strings.HasPrefix(f, r+string(os.PathSeparator))
}

// hostAllowed rejects DNS-rebinding attempts: only loopback hostnames pass.
func hostAllowed(h string) bool {
	host := h
	if i := strings.LastIndex(h, ":"); i >= 0 {
		host = h[:i]
	}
	host = strings.Trim(host, "[]")
	return host == "127.0.0.1" || host == "localhost"
}

// resolvesInside rejects symlinks that point outside the served root.
func resolvesInside(root, full string) bool {
	realRoot, err := filepath.EvalSymlinks(root)
	if err != nil {
		realRoot = root
	}
	realFull, err := filepath.EvalSymlinks(full)
	if err != nil {
		return true // does not exist: let the normal 404 flow handle it
	}
	return isInside(realRoot, realFull)
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
	for _, c := range s {
		h ^= uint32(c)
		h *= 16777619
	}
	return h
}

// hashForPath lowercases only where the file system is case-insensitive.
func hashForPath(s string) uint32 {
	if runtime.GOOS == "linux" {
		return hash32(s)
	}
	return hash32(strings.ToLower(s))
}

const (
	portMin   = 51000
	portSpan  = 12000
	portTries = 400
)

func candidatePort(root string, i int) int {
	return portMin + int((hashForPath(root)+uint32(i))%portSpan)
}

func listen(root string) (net.Listener, int) {
	for i := 0; i < portTries; i++ {
		p := candidatePort(root, i)
		if ln, err := net.Listen("tcp", fmt.Sprintf("127.0.0.1:%d", p)); err == nil {
			return ln, p
		}
	}
	ln, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fatal(err)
	}
	port := ln.Addr().(*net.TCPAddr).Port
	logf("no stable port available; using ephemeral port %d (the origin changes)", port)
	return ln, port
}

func lockPath(root string) string {
	return filepath.Join(dataDir(), "sessions", fmt.Sprintf("%08x.txt", hashForPath(root)))
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

func ping(port int, root string) bool {
	client := http.Client{Timeout: 900 * time.Millisecond}
	resp, err := client.Get(fmt.Sprintf("http://127.0.0.1:%d/__fenestra/ping", port))
	if err != nil {
		return false
	}
	defer resp.Body.Close()
	body, err := io.ReadAll(io.LimitReader(resp.Body, 4096))
	return err == nil && string(body) == "fenestra-ok\n"+root
}

// ---------------------------------------------------------------- browser

type browser struct {
	key   string
	bins  []string
	found string
}

func browserList() []browser {
	return []browser{
		{"edge", []string{"microsoft-edge", "microsoft-edge-stable"}, ""},
		{"chrome", []string{"google-chrome", "google-chrome-stable"}, ""},
		{"chromium", []string{"chromium", "chromium-browser"}, ""},
		{"firefox", []string{"firefox", "firefox-esr"}, ""},
		{"brave", []string{"brave-browser", "brave"}, ""},
		{"vivaldi", []string{"vivaldi", "vivaldi-stable"}, ""},
		{"opera", []string{"opera"}, ""},
	}
}

func lookup(key string) (string, bool) {
	for _, b := range browserList() {
		if b.key != key {
			continue
		}
		for _, name := range b.bins {
			if p, err := exec.LookPath(name); err == nil {
				return p, true
			}
		}
	}
	return "", false
}

func readBrowserPref() string {
	data, err := os.ReadFile(filepath.Join(configDir(), "browser"))
	if err != nil {
		return "default"
	}
	value := strings.ToLower(strings.TrimSpace(string(data)))
	if value == "" {
		return "default"
	}
	return value
}

func browserPrefPath() string {
	return filepath.Join(configDir(), "browser")
}

func saveBrowserPref(name string) {
	if err := os.MkdirAll(configDir(), 0o755); err == nil {
		if err := os.WriteFile(browserPrefPath(), []byte(name+"\n"), 0o644); err != nil {
			logf("browser preference: %v", err)
		}
	}
}

func browserLabel(key string) string {
	switch key {
	case "edge":
		return "Microsoft Edge"
	case "chrome":
		return "Google Chrome"
	case "chromium":
		return "Chromium"
	case "firefox":
		return "Firefox"
	case "brave":
		return "Brave"
	case "vivaldi":
		return "Vivaldi"
	case "opera":
		return "Opera"
	case "safari":
		return "Safari"
	}
	return key
}

func isTTY() bool {
	fi, err := os.Stdin.Stat()
	return err == nil && (fi.Mode()&os.ModeCharDevice) != 0
}

func chooseBrowserOnFirstRun() {
	if os.Getenv("FENESTRA_NO_OPEN") == "1" {
		return
	}
	if _, err := os.Stat(browserPrefPath()); err == nil {
		return
	}
	if !isTTY() {
		saveBrowserPref("default")
		return
	}

	keys := []string{"default"}
	labels := []string{"System default browser"}
	for _, b := range browserList() {
		if _, ok := lookup(b.key); ok {
			keys = append(keys, b.key)
			labels = append(labels, browserLabel(b.key))
		}
	}

	fmt.Println("Fenestra — which browser should open local sites?")
	for i, label := range labels {
		fmt.Printf("  %d) %s\n", i+1, label)
	}
	fmt.Print("Choose [1]: ")

	line, _ := bufio.NewReader(os.Stdin).ReadString('\n')
	line = strings.TrimSpace(line)
	choice := 1
	if line != "" {
		if n, err := strconv.Atoi(line); err == nil && n >= 1 && n <= len(keys) {
			choice = n
		}
	}
	saveBrowserPref(keys[choice-1])
	logf("first-run browser choice: %s", keys[choice-1])
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
		if bin, ok := lookup(name); ok {
			cmd := exec.Command(bin, target)
			if err := cmd.Start(); err == nil {
				logf("opening %s %s", bin, target)
				return cmd
			}
		} else {
			logf("browser %s not found; using the system default", name)
		}
	}

	cmd := exec.Command("xdg-open", target)
	if err := cmd.Start(); err == nil {
		logf("opening xdg-open %s", target)
		return cmd
	}

	fmt.Fprintf(os.Stderr, "could not open a browser for %s\n", target)
	return nil
}

// ---------------------------------------------------------------- install

func install() {
	exe, err := os.Executable()
	if err != nil {
		fatal(err)
	}

	dir := appsDir()
	if err := os.MkdirAll(dir, 0o755); err != nil {
		fatal(err)
	}

	entry := fmt.Sprintf(`[Desktop Entry]
Type=Application
Name=Fenestra
Comment=Open local HTML over localhost
Exec="%s" %%f
Terminal=false
MimeType=text/html;
Categories=Development;
`, exe)

	file := filepath.Join(dir, "fenestra.desktop")
	if err := os.WriteFile(file, []byte(entry), 0o755); err != nil {
		fatal(err)
	}

	exec.Command("update-desktop-database", dir).Run()

	if out, err := exec.Command("xdg-mime", "default", "fenestra.desktop", "text/html").CombinedOutput(); err != nil {
		fmt.Fprintf(os.Stderr, "xdg-mime failed: %v\n%s\n", err, out)
		fmt.Println("Create the .desktop entry manually, or run: xdg-mime default fenestra.desktop text/html")
		return
	}

	fmt.Println("Fenestra registered as the text/html handler.")
	fmt.Println("To undo: fenestra --uninstall")
}

func uninstall() {
	file := filepath.Join(appsDir(), "fenestra.desktop")
	if err := os.Remove(file); err != nil && !os.IsNotExist(err) {
		fatal(err)
	}
	exec.Command("update-desktop-database", appsDir()).Run()

	fmt.Println("Fenestra removed. Set your default browser back with:")
	if desktop := desktopFileFor(readBrowserPref()); desktop != "" {
		fmt.Printf("  xdg-mime default %s text/html\n", desktop)
	} else {
		fmt.Println("  xdg-mime default <your-browser>.desktop text/html")
	}
}

func desktopFileFor(key string) string {
	switch key {
	case "edge":
		return "microsoft-edge.desktop"
	case "chrome":
		return "google-chrome.desktop"
	case "chromium":
		return "chromium.desktop"
	case "firefox":
		return "firefox.desktop"
	case "brave":
		return "brave-browser.desktop"
	case "vivaldi":
		return "vivaldi-stable.desktop"
	case "opera":
		return "opera.desktop"
	}
	return ""
}

// ---------------------------------------------------------------- misc

func dataDir() string {
	if x := os.Getenv("XDG_DATA_HOME"); x != "" {
		return filepath.Join(x, "fenestra")
	}
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".local", "share", "fenestra")
}

func configDir() string {
	if x := os.Getenv("XDG_CONFIG_HOME"); x != "" {
		return filepath.Join(x, "fenestra")
	}
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".config", "fenestra")
}

func appsDir() string {
	if x := os.Getenv("XDG_DATA_HOME"); x != "" {
		return filepath.Join(x, "applications")
	}
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".local", "share", "applications")
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
