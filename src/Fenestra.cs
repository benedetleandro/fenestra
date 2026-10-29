using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Fenestra
{
    internal static class Program
    {
        private static string _root;
        private static string _entry;
        private static string _defaultDoc;
        private static string _lockFile;
        private static long _lastRequest = DateTime.UtcNow.Ticks;
        private static System.Threading.Timer _idleTimer;

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0) { Usage(); return; }

                string a = args[0];
                if (Eq(a, "--install")) { Install(args.Length > 1 ? args[1] : null); return; }
                if (Eq(a, "--uninstall")) { Uninstall(); return; }
                if (Eq(a, "--version"))
                {
                    MessageBox.Show("Fenestra 0.4.2", "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (Eq(a, "--browsers")) { ShowBrowsers(); return; }

                if (Eq(a, "--browser"))
                {
                    if (args.Length < 2) { Usage(); return; }
                    bool ok = SetBrowser(args[1], args.Length <= 2);
                    if (ok && args.Length > 2) Serve(args[2], false);
                    return;
                }

                if (Eq(a, "--serve"))
                {
                    if (args.Length < 2) { Usage(); return; }
                    Serve(args[1], true);
                    return;
                }

                Serve(a, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open this application.\r\n\r\n" + ex.Message,
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------------------------------------------------------- serve

        private static void Serve(string target, bool force)
        {
            string full = Path.GetFullPath(target);

            // HTML simples abre direto no Edge por file://, sem servidor.
            // So quem precisa de origem HTTP (modulos ES, caminhos absolutos,
            // fetch, service worker) passa pelo servidor.
            if (!force && File.Exists(full) && IsHtml(full) && !NeedsServer(full))
            {
                OpenInBrowser(full);
                return;
            }

            if (File.Exists(full))
            {
                _root = Path.GetDirectoryName(full);
                _entry = full.Substring(_root.Length).TrimStart('\\');
            }
            else if (Directory.Exists(full))
            {
                _root = full;
                _entry = null;
            }
            else
            {
                throw new FileNotFoundException("Path not found: " + target);
            }

            _root = _root.TrimEnd('\\');
            _defaultDoc = FindDefaultDoc(_root);

            // Abrir o index pela raiz, nao por /index.html: routers de SPA
            // costumam tratar /index.html como rota inexistente.
            if (_defaultDoc != null && _entry != null &&
                string.Equals(_entry, _defaultDoc, StringComparison.OrdinalIgnoreCase))
            {
                _entry = null;
            }

            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenestra");
            Directory.CreateDirectory(Path.Combine(dataDir, "sessions"));

            string key = Hash(_root);
            _lockFile = Path.Combine(dataDir, "sessions", key + ".txt");

            int existing = ReadLockPort();
            if (existing > 0 && Ping(existing))
            {
                OpenBrowser(existing);
                return;
            }

            TcpListener listener = StartListener(existing);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            WriteLock(port);

            Thread accept = new Thread(delegate() { AcceptLoop(listener); });
            accept.IsBackground = true;
            accept.Start();

            Log("serving " + _root + " at 127.0.0.1:" + port);

            _idleTimer = new System.Threading.Timer(IdleCheck, null, 60000, 60000);

            Process browser = OpenBrowser(port);
            WatchBrowser(browser);

            while (true) Thread.Sleep(1000);
        }

        private static bool IsHtml(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".html" || ext == ".htm" || ext == ".xhtml";
        }

        // Decide se o arquivo precisa de origem HTTP. Se nao precisar,
        // o Edge abre o proprio arquivo e o Fenestra nem sobe servidor.
        private static bool NeedsServer(string file)
        {
            try
            {
                FileInfo info = new FileInfo(file);
                if (info.Length > 4L * 1024 * 1024) return true;

                string text = File.ReadAllText(file).ToLowerInvariant();

                if (text.IndexOf("type=\"module\"", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("type='module'", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("crossorigin", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("rel=\"manifest\"", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("rel='manifest'", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("src=\"/", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("src='/", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("href=\"/", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("href='/", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("fetch(", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("xmlhttprequest", StringComparison.Ordinal) >= 0) return true;
                if (text.IndexOf("serviceworker", StringComparison.Ordinal) >= 0) return true;

                return false;
            }
            catch { return true; }
        }

        private static void OpenInBrowser(string file)
        {
            Launch(file);
        }

        private static string FindDefaultDoc(string root)
        {
            string[] candidates = { "index.html", "index.htm", "index.xhtml", "default.html", "Default.html" };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(Path.Combine(root, candidates[i]))) return candidates[i];
            }
            return null;
        }

        private static TcpListener StartListener(int preferred)
        {
            // A porta anterior e reaproveitada: origem estavel preserva
            // localStorage, cookies e service worker entre sessoes.
            if (preferred > 0)
            {
                TcpListener reuse = TryBind(preferred);
                if (reuse != null) return reuse;
            }

            int basePort = 51000 + (int)(Hash32(_root) % 12000);
            for (int i = 0; i < 400; i++)
            {
                int port = basePort + i;
                if (port > 65000) port = 51000 + (port % 12000);
                TcpListener l = TryBind(port);
                if (l != null) return l;
            }

            TcpListener fallback = new TcpListener(IPAddress.Loopback, 0);
            fallback.Start();
            return fallback;
        }

        private static TcpListener TryBind(int port)
        {
            try
            {
                TcpListener l = new TcpListener(IPAddress.Loopback, port);
                l.Start();
                return l;
            }
            catch (SocketException) { return null; }
        }

        private static void AcceptLoop(TcpListener listener)
        {
            while (true)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch { return; }

                ThreadPool.QueueUserWorkItem(delegate(object state) { Handle((TcpClient)state); }, client);
            }
        }

        private static void Handle(TcpClient client)
        {
            try
            {
                using (client)
                {
                    client.ReceiveTimeout = 8000;
                    client.SendTimeout = 20000;

                    NetworkStream ns = client.GetStream();
                    StreamReader reader = new StreamReader(ns, Encoding.ASCII, false, 4096, true);

                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) return;

                    int guard = 0;
                    string header;
                    while ((header = reader.ReadLine()) != null && header.Length > 0)
                    {
                        if (++guard > 200) break;
                    }

                    _lastRequest = DateTime.UtcNow.Ticks;

                    string[] parts = line.Split(' ');
                    if (parts.Length < 2) { SendText(ns, 400, "Bad request"); return; }

                    string method = parts[0].ToUpperInvariant();
                    bool head = method == "HEAD";
                    if (method != "GET" && !head) { SendText(ns, 405, "Method not allowed"); return; }

                    string target = parts[1];
                    int q = target.IndexOf('?');
                    if (q >= 0) target = target.Substring(0, q);

                    if (target == "/__fenestra/ping")
                    {
                        Respond(ns, 200, "text/plain; charset=utf-8",
                            Encoding.ASCII.GetBytes("fenestra-ok"), head);
                        return;
                    }

                    string rel = PercentDecode(target);
                    if (rel.IndexOf('\0') >= 0 || rel.IndexOf(':') >= 0)
                    {
                        SendText(ns, 403, "Access denied");
                        return;
                    }

                    rel = rel.Replace('/', '\\').TrimStart('\\');

                    string full;
                    try { full = Path.GetFullPath(Path.Combine(_root, rel)); }
                    catch { SendText(ns, 400, "Invalid path"); return; }

                    if (!IsInside(_root, full))
                    {
                        SendText(ns, 403, "Access denied");
                        return;
                    }

                    if (Directory.Exists(full))
                    {
                        if (_defaultDoc != null)
                        {
                            string index = Path.Combine(full, _defaultDoc);
                            if (File.Exists(index)) { ServeFile(ns, index, head); return; }
                        }

                        byte[] listing = Encoding.UTF8.GetBytes(BuildListing(full, target));
                        Respond(ns, 200, "text/html; charset=utf-8", listing, head);
                        return;
                    }

                    if (File.Exists(full)) { ServeFile(ns, full, head); return; }

                    // SPA: rota sem extensao cai no index.html
                    if (Path.GetExtension(full).Length == 0 && _defaultDoc != null)
                    {
                        string index = Path.Combine(_root, _defaultDoc);
                        if (File.Exists(index)) { ServeFile(ns, index, head); return; }
                    }

                    Send404(ns, target, head);
                }
            }
            catch { }
        }

        private static void ServeFile(NetworkStream ns, string path, bool head)
        {
            try
            {
                byte[] body = File.ReadAllBytes(path);
                Respond(ns, 200, Mime(path), body, head);
            }
            catch (Exception ex)
            {
                SendText(ns, 500, "Could not read the file: " + ex.Message);
            }
        }

        private static void Respond(NetworkStream ns, int status, string contentType, byte[] body, bool head)
        {
            string reason;
            if (status == 200) reason = "OK";
            else if (status == 403) reason = "Forbidden";
            else if (status == 404) reason = "Not Found";
            else if (status == 405) reason = "Method Not Allowed";
            else if (status == 400) reason = "Bad Request";
            else reason = "Internal Server Error";

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
            sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
            sb.Append("Content-Length: ").Append(body.Length).Append("\r\n");
            sb.Append("Cache-Control: no-store, no-cache, must-revalidate\r\n");
            sb.Append("Pragma: no-cache\r\n");
            sb.Append("X-Fenestra: 1\r\n");
            sb.Append("Connection: close\r\n\r\n");

            byte[] headBytes = Encoding.ASCII.GetBytes(sb.ToString());
            ns.Write(headBytes, 0, headBytes.Length);
            if (!head && body.Length > 0) ns.Write(body, 0, body.Length);
            ns.Flush();
        }

        private static void SendText(NetworkStream ns, int status, string message)
        {
            string html = "<!doctype html><meta charset=\"utf-8\"><title>Fenestra</title>" +
                          "<body style=\"font:15px/1.5 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:40px\">" +
                          "<h1 style=\"font-weight:600\">" + status + "</h1><p>" + Html(message) + "</p></body>";
            Respond(ns, status, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), false);
        }

        private static void Send404(NetworkStream ns, string target, bool head)
        {
            string html = "<!doctype html><meta charset=\"utf-8\"><title>Not found</title>" +
                          "<body style=\"font:15px/1.5 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:40px;max-width:800px;margin:auto\">" +
                          "<h1 style=\"font-weight:600\">404</h1><p>Could not find <code style=\"color:#9cc4ff\">" + Html(target) + "</code> at:</p>" +
                          "<p style=\"color:#8a8a90;word-break:break-all\">" + Html(_root) + "</p>" +
                          "<p><a style=\"color:#9cc4ff\" href=\"/\">Back to the root</a></p></body>";
            Respond(ns, 404, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), head);
        }

        private static string BuildListing(string dir, string urlBase)
        {
            if (!urlBase.EndsWith("/")) urlBase += "/";

            List<string> dirs = new List<string>();
            List<string> files = new List<string>();

            try
            {
                foreach (string d in Directory.GetDirectories(dir)) dirs.Add(Path.GetFileName(d));
                foreach (string f in Directory.GetFiles(dir)) files.Add(Path.GetFileName(f));
            }
            catch { }

            dirs.Sort(StringComparer.OrdinalIgnoreCase);
            files.Sort(StringComparer.OrdinalIgnoreCase);

            StringBuilder sb = new StringBuilder();
            sb.Append("<!doctype html><html lang=\"pt-BR\"><meta charset=\"utf-8\"><title>Fenestra</title>");
            sb.Append("<style>body{font:15px/1.6 system-ui,sans-serif;background:#0f0f10;color:#e8e8ea;padding:36px;max-width:960px;margin:auto}");
            sb.Append("a{color:#9cc4ff;text-decoration:none}a:hover{text-decoration:underline}li{margin:5px 0}");
            sb.Append("h1{font-weight:600;margin:0 0 4px}p.path{color:#8a8a90;word-break:break-all;margin:0 0 26px}");
            sb.Append("ul{list-style:none;padding:0}span.dir{color:#ffd479}</style>");
            sb.Append("<h1>Fenestra</h1><p class=\"path\">").Append(Html(urlBase)).Append("</p><ul>");

            if (urlBase != "/")
                sb.Append("<li><a href=\"../\">..</a></li>");

            for (int i = 0; i < dirs.Count; i++)
            {
                sb.Append("<li><span class=\"dir\">&#128193;</span> <a href=\"")
                  .Append(Html(Uri.EscapeDataString(dirs[i]))).Append("/\">")
                  .Append(Html(dirs[i])).Append("/</a></li>");
            }

            for (int i = 0; i < files.Count; i++)
            {
                sb.Append("<li><a href=\"")
                  .Append(Html(Uri.EscapeDataString(files[i]))).Append("\">")
                  .Append(Html(files[i])).Append("</a></li>");
            }

            sb.Append("</ul></body></html>");
            return sb.ToString();
        }

        private static string Mime(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".html": case ".htm": case ".xhtml": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": case ".mjs": return "text/javascript; charset=utf-8";
                case ".json": case ".map": return "application/json; charset=utf-8";
                case ".webmanifest": return "application/manifest+json; charset=utf-8";
                case ".xml": return "application/xml; charset=utf-8";
                case ".txt": return "text/plain; charset=utf-8";
                case ".csv": return "text/csv; charset=utf-8";
                case ".svg": return "image/svg+xml";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".avif": return "image/avif";
                case ".ico": return "image/x-icon";
                case ".woff": return "font/woff";
                case ".woff2": return "font/woff2";
                case ".ttf": return "font/ttf";
                case ".otf": return "font/otf";
                case ".eot": return "application/vnd.ms-fontobject";
                case ".wasm": return "application/wasm";
                case ".mp4": return "video/mp4";
                case ".webm": return "video/webm";
                case ".mp3": return "audio/mpeg";
                case ".wav": return "audio/wav";
                case ".ogg": return "audio/ogg";
                case ".pdf": return "application/pdf";
                case ".zip": return "application/zip";
                default: return "application/octet-stream";
            }
        }

        // -------------------------------------------------------------- helpers

        private static bool IsInside(string root, string full)
        {
            string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string f = Path.GetFullPath(full);
            if (string.Equals(f, r, StringComparison.OrdinalIgnoreCase)) return true;
            return f.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string PercentDecode(string s)
        {
            if (s.IndexOf('%') < 0) return s;

            List<byte> bytes = new List<byte>();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '%' && i + 2 < s.Length)
                {
                    int hi = HexVal(s[i + 1]);
                    int lo = HexVal(s[i + 2]);
                    if (hi >= 0 && lo >= 0)
                    {
                        bytes.Add((byte)((hi << 4) | lo));
                        i += 2;
                        continue;
                    }
                }

                byte[] encoded = Encoding.UTF8.GetBytes(c.ToString());
                for (int k = 0; k < encoded.Length; k++) bytes.Add(encoded[k]);
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        private static uint Hash32(string text)
        {
            uint h = 2166136261;
            string lower = text.ToLowerInvariant();
            for (int i = 0; i < lower.Length; i++)
            {
                h ^= lower[i];
                h *= 16777619;
            }
            return h;
        }

        private static string Hash(string text)
        {
            return Hash32(text).ToString("x8");
        }

        private static string Html(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string UrlPath(string relative)
        {
            string[] parts = relative.Split('\\');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append('/');
                sb.Append(Uri.EscapeDataString(parts[i]));
            }
            return sb.ToString();
        }

        private static void Log(string message)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenestra");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "fenestra.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + "\r\n");
            }
            catch { }
        }

        // -------------------------------------------------------------- browser

        private static Process OpenBrowser(int port)
        {
            string url = "http://127.0.0.1:" + port + "/";
            if (_entry != null) url += UrlPath(_entry);

            // Usado pelos testes e pelo CI: sobe o servidor sem abrir navegador.
            if (Environment.GetEnvironmentVariable("FENESTRA_NO_OPEN") == "1")
            {
                Log("FENESTRA_NO_OPEN=1, headless server at " + url);
                return null;
            }

            return Launch(url);
        }

        private static Process Launch(string target)
        {
            string exe = ResolveBrowser();
            if (exe == null)
            {
                Log("no known browser; using the system default");
                try { Process.Start(target); } catch { }
                return null;
            }

            // Abre uma aba normal: sem --app, sem perfil separado, sem janela
            // propria. O Fenestra so abre a porta; quem mostra o HTML e o
            // navegador escolhido.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exe;
            psi.Arguments = "\"" + target + "\"";
            psi.UseShellExecute = false;

            Log("opening " + exe + " " + target);
            return Process.Start(psi);
        }

        private static string[] BrowserCandidates(string key)
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            switch (key)
            {
                case "edge":
                    return new string[] {
                        Path.Combine(pf86, "Microsoft", "Edge", "Application", "msedge.exe"),
                        Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe"),
                        Path.Combine(local, "Microsoft", "Edge", "Application", "msedge.exe")
                    };
                case "chrome":
                    return new string[] {
                        Path.Combine(pf, "Google", "Chrome", "Application", "chrome.exe"),
                        Path.Combine(pf86, "Google", "Chrome", "Application", "chrome.exe"),
                        Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe")
                    };
                case "firefox":
                    return new string[] {
                        Path.Combine(pf, "Mozilla Firefox", "firefox.exe"),
                        Path.Combine(pf86, "Mozilla Firefox", "firefox.exe")
                    };
                case "brave":
                    return new string[] {
                        Path.Combine(pf, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                        Path.Combine(pf86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                        Path.Combine(local, "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
                    };
                case "vivaldi":
                    return new string[] {
                        Path.Combine(pf, "Vivaldi", "Application", "vivaldi.exe"),
                        Path.Combine(local, "Vivaldi", "Application", "vivaldi.exe")
                    };
                case "opera":
                    return new string[] {
                        Path.Combine(local, "Programs", "Opera", "opera.exe"),
                        Path.Combine(pf, "Opera", "opera.exe"),
                        Path.Combine(pf86, "Opera", "opera.exe")
                    };
                case "chromium":
                    return new string[] {
                        Path.Combine(pf, "Chromium", "Application", "chrome.exe"),
                        Path.Combine(local, "Chromium", "Application", "chrome.exe")
                    };
                default:
                    return new string[0];
            }
        }

        private static string BrowserExeName(string key)
        {
            if (key == "edge") return "msedge.exe";
            if (key == "chromium") return "chrome.exe";
            return key + ".exe";
        }

        private static string FindBrowser(string key)
        {
            string[] paths = BrowserCandidates(key);
            for (int i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i])) return paths[i];
            }

            string exeName = BrowserExeName(key);

            string[] roots = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exeName,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + exeName
            };

            for (int r = 0; r < roots.Length; r++)
            {
                try
                {
                    using (RegistryKey k = Registry.LocalMachine.OpenSubKey(roots[r]))
                    {
                        if (k != null)
                        {
                            string value = k.GetValue("") as string;
                            if (value != null && File.Exists(value)) return value;
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(roots[r]))
                    {
                        if (k != null)
                        {
                            string value = k.GetValue("") as string;
                            if (value != null && File.Exists(value)) return value;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string BrowserPrefPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenestra");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "browser.txt");
        }

        private static string ReadBrowserPref()
        {
            try
            {
                string path = BrowserPrefPath();
                if (!File.Exists(path)) return "edge";
                string value = File.ReadAllText(path).Trim().ToLowerInvariant();
                return value.Length == 0 ? "edge" : value;
            }
            catch { return "edge"; }
        }

        private static string ResolveBrowser()
        {
            string pref = ReadBrowserPref();
            if (pref == "default") return null;

            string exe = FindBrowser(pref);
            if (exe != null) return exe;

            // Preferencia indisponivel: cai para o primeiro navegador achado.
            string[] order = { "edge", "chrome", "firefox", "brave", "vivaldi", "opera", "chromium" };
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == pref) continue;
                string candidate = FindBrowser(order[i]);
                if (candidate != null) return candidate;
            }

            return null;
        }

        private static bool SetBrowser(string name, bool showMessage)
        {
            name = (name ?? "").Trim().ToLowerInvariant();

            string[] known = { "edge", "chrome", "firefox", "brave", "vivaldi", "opera", "chromium", "default" };
            bool valid = false;
            for (int i = 0; i < known.Length; i++) { if (known[i] == name) { valid = true; break; } }

            if (!valid)
            {
                MessageBox.Show(
                    "Unknown browser: " + name + "\r\n\r\n" +
                    "Options: edge, chrome, firefox, brave, vivaldi, opera, chromium, default",
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (name != "default" && FindBrowser(name) == null)
            {
                MessageBox.Show("Could not find " + name + " installed on this machine.",
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try { File.WriteAllText(BrowserPrefPath(), name); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save the preference: " + ex.Message,
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (showMessage)
            {
                MessageBox.Show(
                    "Fenestra browser: " + name + "\r\n\r\n" +
                    "Applies to every double-click from now on.",
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return true;
        }

        private static void ShowBrowsers()
        {
            string[] keys = { "edge", "chrome", "firefox", "brave", "vivaldi", "opera", "chromium" };
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < keys.Length; i++)
            {
                string exe = FindBrowser(keys[i]);
                sb.Append(keys[i]).Append(": ").Append(exe == null ? "not found" : exe).Append("\r\n");
            }

            sb.Append("\r\nCurrent: ").Append(ReadBrowserPref()).Append("\r\n\r\n");
            sb.Append("Trocar: Fenestra.exe --browser chrome");

            MessageBox.Show(sb.ToString(), "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void WatchBrowser(Process browser)
        {
            if (browser == null) return;

            Thread t = new Thread(delegate()
            {
                try
                {
                    // Se sair em menos de 6s, foi entrega para uma instancia ja aberta:
                    // nao derruba o servidor, deixa o timeout ocioso cuidar.
                    if (browser.WaitForExit(6000)) return;
                    browser.WaitForExit();
                }
                catch { }

                Shutdown("browser closed");
            });
            t.IsBackground = true;
            t.Start();
        }

        private static void IdleCheck(object state)
        {
            try
            {
                long last = Interlocked.Read(ref _lastRequest);
                TimeSpan idle = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - last);
                if (idle.TotalMinutes > 240) Shutdown("ocioso");
            }
            catch { }
        }

        private static void Shutdown(string reason)
        {
            Log("encerrando: " + reason);
            Environment.Exit(0);
        }

        private static bool Ping(int port)
        {
            try
            {
                using (TcpClient c = new TcpClient())
                {
                    c.ReceiveTimeout = 900;
                    c.SendTimeout = 900;
                    c.Connect(IPAddress.Loopback, port);

                    NetworkStream ns = c.GetStream();
                    byte[] request = Encoding.ASCII.GetBytes(
                        "GET /__fenestra/ping HTTP/1.0\r\nHost: 127.0.0.1\r\n\r\n");
                    ns.Write(request, 0, request.Length);

                    byte[] buffer = new byte[512];
                    int n = ns.Read(buffer, 0, buffer.Length);
                    string text = Encoding.ASCII.GetString(buffer, 0, n);
                    return text.IndexOf("fenestra-ok", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch { return false; }
        }

        private static int ReadLockPort()
        {
            try
            {
                if (!File.Exists(_lockFile)) return 0;
                string text = File.ReadAllText(_lockFile).Trim();
                int bar = text.IndexOf('|');
                if (bar < 0) return 0;
                int port;
                if (!int.TryParse(text.Substring(0, bar), out port)) return 0;
                return port;
            }
            catch { return 0; }
        }

        private static void WriteLock(int port)
        {
            try
            {
                File.WriteAllText(_lockFile, port + "|" + Process.GetCurrentProcess().Id);
            }
            catch { }
        }

        // ------------------------------------------------------------- install

        private static void Install(string sample)
        {
            string exe = Application.ExecutablePath;

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Fenestra.html"))
            {
                k.SetValue("", "Fenestra (local app)");
                using (RegistryKey icon = k.CreateSubKey("DefaultIcon"))
                {
                    icon.SetValue("", "\"" + exe + "\",0");
                }
                using (RegistryKey cmd = k.CreateSubKey(@"shell\open\command"))
                {
                    cmd.SetValue("", "\"" + exe + "\" \"%1\"");
                }
            }

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\Fenestra.exe\shell\open\command"))
            {
                k.SetValue("", "\"" + exe + "\" \"%1\"");
            }

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\Fenestra.exe\SupportedTypes"))
            {
                k.SetValue(".html", "");
                k.SetValue(".htm", "");
            }

            AddOpenWith(".html");
            AddOpenWith(".htm");

            MessageBox.Show(
                "Fenestra registered.\r\n\r\n" +
                "One time only, to make the double-click work:\r\n\r\n" +
                "1. Right-click the index.html\r\n" +
                "2. Open with > Choose another app\r\n" +
                "3. Select \"Fenestra (local app)\"\r\n" +
                "4. Check \"Always use this app\"\r\n\r\n" +
                "After that, any .html opens: Fenestra starts the port\r\n" +
                "when needed and your browser shows the page.\r\n\r\n" +
                "To undo: Fenestra.exe --uninstall",
                "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void AddOpenWith(string ext)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
                @"Software\Classes\" + ext + @"\OpenWithProgids"))
            {
                k.SetValue("Fenestra.html", new byte[0], RegistryValueKind.Binary);
            }
        }

        private static void Uninstall()
        {
            TryDeleteKey(@"Software\Classes\Fenestra.html");
            TryDeleteKey(@"Software\Classes\Applications\Fenestra.exe");
            TryDeleteValue(@"Software\Classes\.html\OpenWithProgids", "Fenestra.html");
            TryDeleteValue(@"Software\Classes\.htm\OpenWithProgids", "Fenestra.html");

            MessageBox.Show("Fenestra removed from the .html handler.", "Fenestra",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void TryDeleteKey(string path)
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(path, false); } catch { }
        }

        private static void TryDeleteValue(string path, string name)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(path, true))
                {
                    if (k != null) k.DeleteValue(name, false);
                }
            }
            catch { }
        }

        private static void Usage()
        {
            MessageBox.Show(
                "Fenestra 0.4.2\r\n\r\n" +
                "Usage:\r\n" +
                "  Fenestra.exe \"C:\\path\\index.html\"\r\n" +
                "  Fenestra.exe \"C:\\path\\folder\"\r\n" +
                "  Fenestra.exe --serve \"C:\\path\\index.html\"   force the server\r\n" +
                "  Fenestra.exe --browser chrome \"C:\\path\\index.html\"\r\n" +
                "  Fenestra.exe --browser chrome              set the preferred browser\r\n" +
                "  Fenestra.exe --browsers                    list detected browsers\r\n\r\n" +
                "  Browsers: edge (default), chrome, firefox, brave, vivaldi, opera,\r\n" +
                "  chromium, default\r\n\r\n" +
                "  --install [file]      register the .html handler\r\n" +
                "  --uninstall           remove it\r\n",
                "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
