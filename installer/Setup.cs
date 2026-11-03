using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

static class Setup
{
    class Browser
    {
        public string Key;
        public string Label;
        public string Path;
    }

    [STAThread]
    static void Main(string[] args)
    {
        bool silent = Array.IndexOf(args, "--silent") >= 0;
        List<Browser> found = Detect();

        if (silent)
        {
            Install("default", found, true);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Form form = new Form();
        form.Text = "Fenestra Setup";
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new Size(460, 430);
        form.Font = new Font("Segoe UI", 9f);

        Label title = new Label();
        title.Text = "Install Fenestra";
        title.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        title.SetBounds(18, 14, 420, 26);
        form.Controls.Add(title);

        Label hint = new Label();
        hint.Text = "Fenestra opens local HTML builds on 127.0.0.1.\r\n" +
                    "Choose the browser it should use:";
        hint.SetBounds(18, 46, 420, 36);
        form.Controls.Add(hint);

        Panel panel = new Panel();
        panel.SetBounds(18, 88, 424, 250);
        panel.AutoScroll = true;
        form.Controls.Add(panel);

        List<RadioButton> radios = new List<RadioButton>();
        int y = 4;
        for (int i = 0; i < found.Count; i++)
        {
            AddRadio(panel, ref y, radios, found[i].Key, found[i].Label);
        }
        if (radios.Count == 0)
        {
            AddRadio(panel, ref y, radios, "default", "No browser detected");
        }
        radios[0].Checked = true;

        Label footer = new Label();
        footer.Text = "Installs to " + TargetDir() + "\r\n" +
                      "and registers Fenestra for .html and .htm.";
        footer.SetBounds(18, 344, 424, 32);
        form.Controls.Add(footer);

        Button install = new Button();
        install.Text = "Install";
        install.SetBounds(258, 384, 90, 30);
        form.Controls.Add(install);

        Button cancel = new Button();
        cancel.Text = "Cancel";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.SetBounds(354, 384, 90, 30);
        form.Controls.Add(cancel);
        form.CancelButton = cancel;

        install.Click += delegate
        {
            string key = "default";
            for (int i = 0; i < radios.Count; i++)
            {
                if (radios[i].Checked) key = (string)radios[i].Tag;
            }
            try
            {
                Install(key, found, false);
                form.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Install failed: " + ex.Message, "Fenestra Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.Run(form);
    }

    static void AddRadio(Panel panel, ref int y, List<RadioButton> radios, string key, string label)
    {
        RadioButton rb = new RadioButton();
        rb.Text = label;
        rb.Tag = key;
        rb.SetBounds(8, y, 390, 24);
        panel.Controls.Add(rb);
        radios.Add(rb);
        y += 28;
    }

    static string TargetDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenestra");
    }

    static List<Browser> Detect()
    {
        List<Browser> list = new List<Browser>();
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Add(list, "edge", "Microsoft Edge", Path.Combine(pf86, @"Microsoft\Edge\Application\msedge.exe"));
        Add(list, "chrome", "Google Chrome", Path.Combine(pf, @"Google\Chrome\Application\chrome.exe"));
        Add(list, "chrome", "Google Chrome", Path.Combine(pf86, @"Google\Chrome\Application\chrome.exe"));
        Add(list, "chrome", "Google Chrome", Path.Combine(local, @"Google\Chrome\Application\chrome.exe"));
        Add(list, "firefox", "Mozilla Firefox", Path.Combine(pf, @"Mozilla Firefox\firefox.exe"));
        Add(list, "firefox", "Mozilla Firefox", Path.Combine(pf86, @"Mozilla Firefox\firefox.exe"));
        Add(list, "brave", "Brave", Path.Combine(pf, @"BraveSoftware\Brave-Browser\Application\brave.exe"));
        Add(list, "vivaldi", "Vivaldi", Path.Combine(pf, @"Vivaldi\Application\vivaldi.exe"));
        Add(list, "vivaldi", "Vivaldi", Path.Combine(local, @"Vivaldi\Application\vivaldi.exe"));
        Add(list, "opera", "Opera", Path.Combine(pf, @"Opera\opera.exe"));
        Add(list, "opera", "Opera", Path.Combine(local, @"Programs\Opera\opera.exe"));
        Add(list, "chromium", "Chromium", Path.Combine(pf, @"Chromium\Application\chrome.exe"));
        return list;
    }

    static void Add(List<Browser> list, string key, string label, string path)
    {
        if (!File.Exists(path)) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Key == key) return;
        }
        Browser b = new Browser();
        b.Key = key;
        b.Label = label;
        b.Path = path;
        list.Add(b);
    }

    static void Install(string key, List<Browser> found, bool quiet)
    {
        string dir = TargetDir();
        Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "Fenestra.exe");

        Extract("Fenestra.exe", exe);
        Extract("manual.template.html", Path.Combine(dir, "manual.template.html"));

        string settings = Path.Combine(dir, "settings.json");
        if (key == "default")
        {
            if (File.Exists(settings)) File.Delete(settings);
        }
        else
        {
            File.WriteAllText(settings, "{\n  \"browser\": \"" + key + "\"\n}\n");
        }

        string manual = Path.Combine(dir, "manual.html");
        File.WriteAllText(manual, Manual(found));

        Register(exe);

        if (quiet) return;

        MessageBox.Show(
            "Almost done.\r\n\r\n" +
            "In the Open with window, select Fenestra and check\r\n" +
            "\"Always use this app\".\r\n\r\n" +
            "Then the manual opens.",
            "Fenestra Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        FinishAssociation();

        MessageBox.Show("Fenestra installed.\r\n\r\n" +
            "The manual opens now; use it to change the browser at any time.",
            "Fenestra Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);

        Process.Start(exe, "\"" + manual + "\"");
    }

    static void FinishAssociation()
    {
        try
        {
            string sample = Path.Combine(Path.GetTempPath(), "fenestra-sample.html");
            File.WriteAllText(sample, "<!doctype html><title>Fenestra</title>");
            Process.Start("rundll32.exe",
                "shell32.dll,OpenAs_RunDLL \"" + sample + "\"");
        }
        catch { }
    }

    static string Manual(List<Browser> found)
    {
        string html = ReadResource("manual.template.html");
        System.Text.StringBuilder items = new System.Text.StringBuilder();
        for (int i = 0; i < found.Count; i++)
        {
            items.Append(Card(found[i].Key, found[i].Label));
        }
        return html.Replace("{{BROWSERS}}", items.ToString());
    }

    static string Card(string key, string label)
    {
        string ico = key == "chromium" ? "C" : "";
        return "<a class=\"b-card\" href=\"fenestra://browser/" + key + "\">" +
            "<span class=\"b-ico " + key + "\">" + ico + "</span>" +
            "<span class=\"b-txt\"><strong>" + label + "</strong>" +
            "<span class=\"b-use\"></span></span></a>";
    }

    static void Extract(string name, string target)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (s == null) throw new Exception("Missing resource: " + name);
            using (FileStream f = File.Create(target)) { s.CopyTo(f); }
        }
    }

    static string ReadResource(string name)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (s == null) throw new Exception("Missing resource: " + name);
            using (StreamReader r = new StreamReader(s)) { return r.ReadToEnd(); }
        }
    }

    static void Register(string exe)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Fenestra.html"))
        {
            k.SetValue("", "Fenestra (local app)");
            using (RegistryKey cmd = k.CreateSubKey(@"shell\open\command"))
            {
                cmd.SetValue("", "\"" + exe + "\" \"%1\"");
            }
        }

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
            @"Software\Classes\Applications\Fenestra.exe\shell\open\command"))
        {
            k.SetValue("", "\"" + exe + "\" \"%1\"");
        }
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
            @"Software\Classes\Applications\Fenestra.exe"))
        {
            k.SetValue("FriendlyAppName", "Fenestra");
            k.SetValue("ApplicationCompany", "Fenestra");
        }
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
            @"Software\Classes\Applications\Fenestra.exe\SupportedTypes"))
        {
            k.SetValue(".html", "");
            k.SetValue(".htm", "");
        }

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.html\OpenWithProgids"))
        {
            k.SetValue("Fenestra.html", "");
        }
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.htm\OpenWithProgids"))
        {
            k.SetValue("Fenestra.html", "");
        }

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Fenestra\Capabilities"))
        {
            k.SetValue("ApplicationName", "Fenestra");
            k.SetValue("ApplicationDescription", "Open local HTML over localhost");
            k.SetValue("ApplicationIcon", "\"" + exe + "\",0");
            using (RegistryKey fa = k.CreateSubKey("FileAssociations"))
            {
                fa.SetValue(".html", "Fenestra.html");
                fa.SetValue(".htm", "Fenestra.html");
            }
        }
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
        {
            k.SetValue("Fenestra", @"Software\Fenestra\Capabilities");
        }

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\fenestra"))
        {
            k.SetValue("", "URL:Fenestra");
            k.SetValue("URL Protocol", "");
            using (RegistryKey cmd = k.CreateSubKey(@"shell\open\command"))
            {
                cmd.SetValue("", "\"" + exe + "\" \"%1\"");
            }
        }
    }
}
