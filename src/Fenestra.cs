using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private static int _forcedPort;
        private static bool _noPrompt;
        private static System.Threading.Timer _idleTimer;

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                args = ExtractPort(args);
                if (args.Length == 0) { ShowBrowserSettings(); return; }

                string a = args[0];
                if (Eq(a, "--settings")) { ShowBrowserSettings(); return; }
                if (Eq(a, "--install")) { Install(args.Length > 1 ? args[1] : null); return; }
                if (Eq(a, "--uninstall")) { Uninstall(); return; }
                if (Eq(a, "--version"))
                {
                    MessageBox.Show("Fenestra " + BuildInfo.Version, "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private static string[] ExtractPort(string[] args)
        {
            List<string> rest = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (Eq(args[i], "--no-prompt"))
                {
                    _noPrompt = true;
                    continue;
                }
                if (Eq(args[i], "--port") && i + 1 < args.Length)
                {
                    int port;
                    if (!int.TryParse(args[i + 1], out port) || port < 1 || port > 65535)
                    {
                        throw new ArgumentException("Invalid port: " + args[i + 1]);
                    }
                    _forcedPort = port;
                    i++;
                    continue;
                }
                rest.Add(args[i]);
            }
            return rest.ToArray();
        }

        private static void EnsureBrowserChoice(bool force)
        {
            if (force || _noPrompt) return;
            if (Environment.GetEnvironmentVariable("FENESTRA_NO_OPEN") == "1") return;

            if (!File.Exists(HandlerPrefPath())) CaptureHandler();
            if (File.Exists(BrowserPrefPath())) return;

            try { File.WriteAllText(BrowserPrefPath(), "default"); }
            catch (Exception ex) { Log("could not save browser choice: " + ex.Message); }
            Log("first run: default browser preference written");
        }

        private static string SystemDefaultBrowserExe()
        {
            string progId = null;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.html\UserChoice"))
                {
                    if (k != null) progId = k.GetValue("ProgId") as string;
                }
            }
            catch (Exception ex) { Log("html UserChoice: " + ex.Message); }

            if (string.IsNullOrEmpty(progId))
            {
                try
                {
                    using (RegistryKey k = Registry.ClassesRoot.OpenSubKey(".html"))
                    {
                        if (k != null) progId = k.GetValue("") as string;
                    }
                }
                catch (Exception ex) { Log("html class: " + ex.Message); }
            }
            if (string.IsNullOrEmpty(progId)) return null;

            string command = null;
            try
            {
                using (RegistryKey k = Registry.ClassesRoot.OpenSubKey(progId + @"\shell\open\command"))
                {
                    if (k != null) command = k.GetValue("") as string;
                }
            }
            catch (Exception ex) { Log("html command: " + ex.Message); }

            string exe = CommandExe(command);
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) return exe;

            Log("html handler not resolved: " + progId);
            return null;
        }

        private static string CommandExe(string command)
        {
            if (string.IsNullOrEmpty(command)) return null;
            command = command.Trim();
            if (command.StartsWith("\""))
            {
                int end = command.IndexOf('"', 1);
                return end > 1 ? command.Substring(1, end - 1) : null;
            }
            int space = command.IndexOf(' ');
            return space > 0 ? command.Substring(0, space) : command;
        }

        private static string KeyForExe(string exe)
        {
            if (string.IsNullOrEmpty(exe)) return null;
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            string[] keys = { "edge", "chrome", "chromium", "firefox", "brave", "vivaldi", "opera" };
            for (int i = 0; i < keys.Length; i++)
            {
                string known = BrowserExeName(keys[i]);
                if (string.IsNullOrEmpty(known)) continue;
                if (name == Path.GetFileNameWithoutExtension(known).ToLowerInvariant()) return keys[i];
            }
            return null;
        }

        private static void ShowBrowserSettings()
        {
            string current = ReadBrowserPref();
            string chosen = ShowBrowserPicker(false, current);
            if (chosen == null) return;

            try { File.WriteAllText(BrowserPrefPath(), chosen); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save the preference: " + ex.Message,
                    "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Log("browser preference changed: " + chosen);
            string label = chosen == "default" ? "the system default browser" : chosen;
            MessageBox.Show(
                "Fenestra will use " + label + ".\r\n\r\n" +
                "Double-click any .html to open it in that browser.",
                "Fenestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string ShowBrowserPicker(bool firstRun, string current)
        {
            string[] keys = { "default", "edge", "chrome", "firefox", "brave", "vivaldi", "opera", "chromium" };
            string[] labels = { "System default browser", "Microsoft Edge", "Google Chrome", "Firefox", "Brave", "Vivaldi", "Opera", "Chromium" };
            string sysExe = ReadHandlerPref();
            if (sysExe == null) sysExe = SystemDefaultBrowserExe();
            if (sysExe != null && !IsSelf(sysExe))
            {
                labels[0] = "System default (" + Path.GetFileNameWithoutExtension(sysExe) + ")";
            }

            using (Form form = new Form())
            {
                form.Text = firstRun ? "Fenestra - choose your browser" : "Fenestra - browser settings";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ClientSize = new Size(420, 320);
                form.Font = new Font("Segoe UI", 9f);

                Label title = new Label();
                title.Text = firstRun
                    ? "Which browser should open local sites?"
                    : "Which browser should Fenestra use?";
                title.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                title.SetBounds(18, 16, 384, 24);
                form.Controls.Add(title);

                Label hint = new Label();
                hint.Text = "This is the browser that opens local sites. It applies to every double-click.";
                hint.SetBounds(18, 44, 384, 34);
                form.Controls.Add(hint);

                ListBox list = new ListBox();
                list.SetBounds(18, 86, 384, 170);
                list.IntegralHeight = false;

                List<string> itemKeys = new List<string>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] != "default" && FindBrowser(keys[i]) == null) continue;
                    itemKeys.Add(keys[i]);
                    list.Items.Add(labels[i]);
                    if (keys[i] == current) list.SelectedIndex = list.Items.Count - 1;
                }
                if (list.SelectedIndex < 0) list.SelectedIndex = 0;
                form.Controls.Add(list);

                Button ok = new Button();
                ok.Text = firstRun ? "Use this browser" : "Save";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(196, 272, 120, 30);
                form.Controls.Add(ok);

                Button cancel = new Button();
                cancel.Text = firstRun ? "System default" : "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.SetBounds(322, 272, 80, 30);
                form.Controls.Add(cancel);

                form.AcceptButton = ok;
                form.CancelButton = cancel;

                DialogResult result = form.ShowDialog();
                if (result != DialogResult.OK || list.SelectedIndex < 0) return null;
                return itemKeys[list.SelectedIndex];
            }
        }

        private static void Serve(string target, bool force)
        {
            string full = Path.GetFullPath(target);

            EnsureBrowserChoice(force);

            if (File.Exists(full) && IsHtml(full) && LooksLikeSourceEntry(full))
            {
                string build = FindBuild(full);
                if (build != null)
                {
                    Log("source entry detected; using " + build);
                    full = build;
                }
                else
                {
                    throw new FileNotFoundException(
                        "This looks like a source entry (it references /src/).\r\n\r\n" +
                        "Build the project first (for example: npm run build) or open dist/index.html.");
                }
            }

            // Plain HTML opens directly in the browser through file://, no server.
            // Only files that need an HTTP origin (ES modules, absolute paths,
            // fetch, service worker) go through the server.
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

            // Open index from the root, not /index.html: SPA routers
            // usually treat /index.html as a missing route.
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

            TcpListener listener;
            int port;

            if (_forcedPort != 0)
            {
                listener = TryBind(_forcedPort);
                if (listener == null)
                {
                    throw new InvalidOperationException("Port " + _forcedPort + " is not available.");
                }
                port = _forcedPort;
                WriteLock(port);
            }
            else
            {
                int existing = ReadLockPort();
                if (existing > 0 && Ping(existing, _root))
                {
                    OpenBrowser(existing);
                    return;
                }

                listener = StartListener(existing);
                port = ((IPEndPoint)listener.LocalEndpoint).Port;

                WriteLock(port);
            }

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

        private static bool LooksLikeSourceEntry(string path)
        {
            try
            {
                string text = File.ReadAllText(path);
                return Regex.IsMatch(text, @"<script[^>]+src\s*=\s*[""'](?:\./|/)?src/", RegexOptions.IgnoreCase);
            }
            catch { return false; }
        }

        private static string FindBuild(string sourceFile)
        {
            string dir = Path.GetDirectoryName(sourceFile);
            string[] names = { "dist", "build", "out" };
            for (int i = 0; i < names.Length; i++)
            {
                string candidate = Path.Combine(Path.Combine(dir, names[i]), "index.html");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        // Decides whether the file needs an HTTP origin. If it does not,
        // the browser opens the file itself and Fenestra never starts a server.
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

        private const int PortMin = 51000;
        private const int PortSpan = 12000;
        private const int PortTries = 400;

        private static int CandidatePort(int i)
        {
            return PortMin + (int)((Hash32(_root) + (uint)i) % PortSpan);
        }

        private static TcpListener StartListener(int preferred)
        {
            // The previous port is reused: a stable origin preserves
            // localStorage, cookies and the service worker across sessions.
            if (preferred > 0)
            {
                TcpListener reuse = TryBind(preferred);
                if (reuse != null) return reuse;
            }

            for (int i = 0; i < PortTries; i++)
            {
                TcpListener l = TryBind(CandidatePort(i));
                if (l != null) return l;
            }

            TcpListener fallback = new TcpListener(IPAddress.Loopback, 0);
            fallback.Start();
            int port = ((IPEndPoint)fallback.LocalEndpoint).Port;
            Log("no stable port available; using ephemeral port " + port + " (the origin changes)");
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

                    Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    int guard = 0;
                    string header;
                    while ((header = reader.ReadLine()) != null && header.Length > 0)
                    {
                        if (++guard > 200) break;
                        int colon = header.IndexOf(':');
                        if (colon > 0)
                        {
                            string key = header.Substring(0, colon).Trim();
                            if (!headers.ContainsKey(key)) headers[key] = header.Substring(colon + 1).Trim();
                        }
                    }

                    _lastRequest = DateTime.UtcNow.Ticks;

                    string hostHeader;
                    if (!headers.TryGetValue("Host", out hostHeader) || !HostAllowed(hostHeader))
                    {
                        SendText(ns, 403, "Access denied");
                        return;
                    }

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
                            Encoding.ASCII.GetBytes("fenestra-ok\n" + _root), head);
                        return;
                    }

                    string rel = PercentDecode(target);
                    if (rel.IndexOf('\0') >= 0 || rel.IndexOf(':') >= 0 || IsHidden(rel))
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

                    if (HasReparsePoint(_root, full))
                    {
                        SendText(ns, 403, "Access denied");
                        return;
                    }

                    if (Directory.Exists(full))
                    {
                        if (_defaultDoc != null)
                        {
                            string index = Path.Combine(full, _defaultDoc);
                            if (File.Exists(index)) { ServeFile(ns, index, head, headers); return; }
                        }

                        byte[] listing = Encoding.UTF8.GetBytes(BuildListing(full, target));
                        Respond(ns, 200, "text/html; charset=utf-8", listing, head);
                        return;
                    }

                    if (File.Exists(full)) { ServeFile(ns, full, head, headers); return; }

                    // SPA: extensionless routes fall back to index.html
                    if (Path.GetExtension(full).Length == 0 && _defaultDoc != null)
                    {
                        string index = Path.Combine(_root, _defaultDoc);
                        if (File.Exists(index)) { ServeFile(ns, index, head, headers); return; }
                    }

                    Send404(ns, target, head);
                }
            }
            catch (Exception ex) { Log("ignored error: " + ex.Message); }
        }

        private static void ServeFile(NetworkStream ns, string path, bool head, Dictionary<string, string> headers)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists) { SendText(ns, 404, "Not found"); return; }

                long length = fi.Length;
                DateTime lastWrite = fi.LastWriteTimeUtc;
                string etag = "\"" + length.ToString("x") + "-" + lastWrite.Ticks.ToString("x") + "\"";
                string lastModified = lastWrite.ToString("R", CultureInfo.InvariantCulture);

                string ifNoneMatch;
                if (headers != null && headers.TryGetValue("If-None-Match", out ifNoneMatch))
                {
                    string candidate = ifNoneMatch.Trim();
                    if (candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) candidate = candidate.Substring(2);
                    candidate = candidate.Trim('"');
                    if (candidate == "*" || candidate == etag.Trim('"'))
                    {
                        SendNotModified(ns, etag, lastModified);
                        return;
                    }
                }

                string ifModifiedSince;
                if (headers != null && headers.TryGetValue("If-Modified-Since", out ifModifiedSince))
                {
                    DateTime since;
                    DateTime lastWriteSecond = new DateTime(
                        lastWrite.Ticks - (lastWrite.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
                    if (DateTime.TryParse(ifModifiedSince, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out since) &&
                        lastWriteSecond <= since)
                    {
                        SendNotModified(ns, etag, lastModified);
                        return;
                    }
                }

                long start = 0;
                long end = length - 1;
                bool partial = false;

                string range;
                if (headers != null && headers.TryGetValue("Range", out range) &&
                    range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) &&
                    range.IndexOf(',') < 0)
                {
                    long rs, re;
                    if (TryParseRange(range, length, out rs, out re))
                    {
                        start = rs;
                        end = re;
                        partial = true;
                    }
                    else
                    {
                        SendRangeNotSatisfiable(ns, length, etag, lastModified);
                        return;
                    }
                }

                long count = end - start + 1;
                if (count < 0) count = 0;
                if (count > int.MaxValue) { SendText(ns, 500, "File too large"); return; }

                int size = (int)count;
                byte[] body = new byte[size];
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(start, SeekOrigin.Begin);
                    int read = 0;
                    while (read < body.Length)
                    {
                        int n = fs.Read(body, read, body.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    if (read != body.Length) Array.Resize(ref body, read);
                }

                StringBuilder extra = new StringBuilder();
                extra.Append("ETag: ").Append(etag).Append("\r\n");
                extra.Append("Last-Modified: ").Append(lastModified).Append("\r\n");
                extra.Append("Accept-Ranges: bytes\r\n");
                if (partial)
                {
                    extra.Append("Content-Range: bytes ").Append(start).Append('-').Append(end)
                         .Append('/').Append(length).Append("\r\n");
                }

                Respond(ns, partial ? 206 : 200, Mime(path), body, head, extra.ToString());
            }
            catch (Exception ex)
            {
                SendText(ns, 500, "Could not read the file: " + ex.Message);
            }
        }

        private static void Respond(NetworkStream ns, int status, string contentType, byte[] body, bool head, string extra = "")
        {
            string reason;
            if (status == 200) reason = "OK";
            else if (status == 206) reason = "Partial Content";
            else if (status == 304) reason = "Not Modified";
            else if (status == 403) reason = "Forbidden";
            else if (status == 404) reason = "Not Found";
            else if (status == 405) reason = "Method Not Allowed";
            else if (status == 400) reason = "Bad Request";
            else if (status == 416) reason = "Range Not Satisfiable";
            else reason = "Internal Server Error";

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
            sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
            sb.Append("Content-Length: ").Append(body.Length).Append("\r\n");
            sb.Append("Cache-Control: no-cache\r\n");
            sb.Append("X-Fenestra: 1\r\n");
            if (extra.Length > 0) sb.Append(extra);
            sb.Append("Connection: close\r\n\r\n");

            byte[] headBytes = Encoding.ASCII.GetBytes(sb.ToString());
            ns.Write(headBytes, 0, headBytes.Length);
            if (!head && body.Length > 0) ns.Write(body, 0, body.Length);
            ns.Flush();
        }

        private static void SendNotModified(NetworkStream ns, string etag, string lastModified)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 304 Not Modified\r\n");
            sb.Append("ETag: ").Append(etag).Append("\r\n");
            sb.Append("Last-Modified: ").Append(lastModified).Append("\r\n");
            sb.Append("Cache-Control: no-cache\r\n");
            sb.Append("Connection: close\r\n\r\n");
            byte[] bytes = Encoding.ASCII.GetBytes(sb.ToString());
            ns.Write(bytes, 0, bytes.Length);
            ns.Flush();
        }

        private static void SendRangeNotSatisfiable(NetworkStream ns, long length, string etag, string lastModified)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 416 Range Not Satisfiable\r\n");
            sb.Append("Content-Range: bytes */").Append(length).Append("\r\n");
            sb.Append("Content-Length: 0\r\n");
            sb.Append("ETag: ").Append(etag).Append("\r\n");
            sb.Append("Last-Modified: ").Append(lastModified).Append("\r\n");
            sb.Append("Connection: close\r\n\r\n");
            byte[] bytes = Encoding.ASCII.GetBytes(sb.ToString());
            ns.Write(bytes, 0, bytes.Length);
            ns.Flush();
        }

        private static bool HostAllowed(string value)
        {
            string host = value;
            int colon = host.LastIndexOf(':');
            if (colon >= 0) host = host.Substring(0, colon);
            host = host.Trim('[', ']');
            return host == "127.0.0.1" || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHidden(string rel)
        {
            foreach (string part in rel.Split('/', '\\'))
            {
                if (part.StartsWith(".") && part != ".well-known") return true;
            }
            return false;
        }

        private static bool HasReparsePoint(string root, string full)
        {
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
            string rel = full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
            string current = root;
            foreach (string part in rel.Split(Path.DirectorySeparatorChar))
            {
                if (part.Length == 0) continue;
                current = Path.Combine(current, part);
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
                }
                catch (FileNotFoundException) { return false; }
                catch (DirectoryNotFoundException) { return false; }
                catch { return true; }
            }
            return false;
        }

        private static bool TryParseRange(string header, long length, out long start, out long end)
        {
            start = 0;
            end = 0;
            string spec = header.Substring(6).Trim();
            int dash = spec.IndexOf('-');
            if (dash < 0) return false;
            string first = spec.Substring(0, dash).Trim();
            string second = spec.Substring(dash + 1).Trim();

            if (first.Length == 0)
            {
                long suffix;
                if (!long.TryParse(second, out suffix) || suffix <= 0) return false;
                if (suffix > length) suffix = length;
                start = length - suffix;
                end = length - 1;
            }
            else
            {
                if (!long.TryParse(first, out start) || start < 0 || start >= length) return false;
                if (second.Length == 0) end = length - 1;
                else if (!long.TryParse(second, out end) || end < start) return false;
                if (end >= length) end = length - 1;
            }
            return true;
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
                foreach (string d in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(d);
                    if (!name.StartsWith(".")) dirs.Add(name);
                }
                foreach (string f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f);
                    if (!name.StartsWith(".")) files.Add(name);
                }
            }
            catch (Exception ex) { Log("ignored error: " + ex.Message); }

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
            catch (Exception ex) { Log("ignored error: " + ex.Message); }
        }

        // -------------------------------------------------------------- browser

        private static Process OpenBrowser(int port)
        {
            string url = "http://127.0.0.1:" + port + "/";
            if (_entry != null) url += UrlPath(_entry);

            // Used by tests and CI: starts the server without opening a browser.
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
                // Follow the .html handler exactly, not the http default.
                string handler = SystemDefaultBrowserExe();
                if (handler != null && !IsSelf(handler))
                {
                    Log("opening the .html handler " + handler);
                    try
                    {
                        ProcessStartInfo h = new ProcessStartInfo();
                        h.FileName = handler;
                        h.Arguments = "\"" + target + "\"";
                        h.UseShellExecute = false;
                        return Process.Start(h);
                    }
                    catch (Exception ex) { Log("html handler failed: " + ex.Message); }
                }

                Log("no .html handler; using the shell default");
                try { Process.Start(target); } catch (Exception ex) { Log("ignored error: " + ex.Message); }
                return null;
            }

            // Opens a normal tab: no --app, no separate profile, no custom
            // window. Fenestra only opens the port; the chosen browser shows
            // the HTML.
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
                catch (Exception ex) { Log("ignored error: " + ex.Message); }

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
                catch (Exception ex) { Log("ignored error: " + ex.Message); }
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
                if (!File.Exists(path)) return "default";
                string value = File.ReadAllText(path).Trim().ToLowerInvariant();
                return value.Length == 0 ? "default" : value;
            }
            catch { return "default"; }
        }

        private static string HandlerPrefPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenestra");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "handler.txt");
        }

        private static string ReadHandlerPref()
        {
            try
            {
                string path = HandlerPrefPath();
                if (!File.Exists(path)) return null;
                string value = File.ReadAllText(path).Trim();
                return value.Length == 0 ? null : value;
            }
            catch { return null; }
        }

        private static void SaveHandlerPref(string exe)
        {
            try { File.WriteAllText(HandlerPrefPath(), exe); }
            catch (Exception ex) { Log("handler preference: " + ex.Message); }
        }

        private static bool IsSelf(string exe)
        {
            try
            {
                return string.Equals(Path.GetFullPath(exe), Path.GetFullPath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        // Captures which browser owned .html before Fenestra became the handler.
        private static void CaptureHandler()
        {
            string exe = SystemDefaultBrowserExe();
            if (exe == null || IsSelf(exe)) return;
            SaveHandlerPref(exe);
            Log("captured html handler: " + exe);
        }

        private static string ResolveBrowser()
        {
            string pref = ReadBrowserPref();
            if (pref != "default")
            {
                string exe = FindBrowser(pref);
                if (exe != null) return exe;
                Log("browser " + pref + " not found; falling back");
            }

            string handler = ReadHandlerPref();
            if (handler != null && File.Exists(handler) && !IsSelf(handler)) return handler;

            string system = SystemDefaultBrowserExe();
            if (system != null && !IsSelf(system)) return system;

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
                    // If it exits in under 6s, it handed off to a running instance:
                    // do not stop the server, let the idle timeout handle it.
                    if (browser.WaitForExit(6000)) return;
                    browser.WaitForExit();
                }
                catch (Exception ex) { Log("ignored error: " + ex.Message); }

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
            catch (Exception ex) { Log("ignored error: " + ex.Message); }
        }

        private static void Shutdown(string reason)
        {
            Log("stopping: " + reason);
            Environment.Exit(0);
        }

        private static bool Ping(int port, string root)
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

                    StringBuilder response = new StringBuilder();
                    byte[] buffer = new byte[512];
                    while (true)
                    {
                        int n = ns.Read(buffer, 0, buffer.Length);
                        if (n <= 0) break;
                        response.Append(Encoding.ASCII.GetString(buffer, 0, n));
                    }

                    string raw = response.ToString();
                    int split = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (split < 0) return false;
                    string body = raw.Substring(split + 4).TrimEnd('\r', '\n');
                    return body == "fenestra-ok\n" + root;
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
            catch (Exception ex) { Log("ignored error: " + ex.Message); }
        }

        // ------------------------------------------------------------- install

        private static void Install(string sample)
        {
            string exe = Application.ExecutablePath;

            // Remember the browser that is the .html handler right now.
            CaptureHandler();

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
            try { Registry.CurrentUser.DeleteSubKeyTree(path, false); } catch (Exception ex) { Log("ignored error: " + ex.Message); }
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
            catch (Exception ex) { Log("ignored error: " + ex.Message); }
        }

        private static void Usage()
        {
            MessageBox.Show(
                "Fenestra " + BuildInfo.Version + "\r\n\r\n" +
                "Usage:\r\n" +
                "  Fenestra.exe \"C:\\path\\index.html\"\r\n" +
                "  Fenestra.exe \"C:\\path\\folder\"\r\n" +
                "  Fenestra.exe --serve \"C:\\path\\index.html\"   force the server\r\n" +
                "  Fenestra.exe --port 8080 \"C:\\path\\index.html\" use a specific port\r\n" +
                "  Fenestra.exe --no-prompt \"C:\\path\\index.html\" skip the browser prompt\r\n" +
                "  Fenestra.exe --browser chrome \"C:\\path\\index.html\"\r\n" +
                "  Fenestra.exe --browser chrome              set the preferred browser\r\n" +
                "  Fenestra.exe --browsers                    list detected browsers\r\n\r\n" +
                "  Browsers: default (system), edge, chrome, firefox, brave, vivaldi, opera,\r\n" +
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
