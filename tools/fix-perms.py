import io

for p in ("linux/main.go", "macos/main.go"):
    with io.open(p, encoding="utf-8", newline="") as fh:
        s = fh.read()

    s = s.replace(
        'os.WriteFile(file, []byte(fmt.Sprintf("%d|%d", port, os.Getpid())), 0o644)',
        'os.WriteFile(file, []byte(fmt.Sprintf("%d|%d", port, os.Getpid())), 0o600)')
    s = s.replace(
        'os.WriteFile(browserPrefPath(), []byte(name+"\\n"), 0o644)',
        'os.WriteFile(browserPrefPath(), []byte(name+"\\n"), 0o600)')
    s = s.replace(
        'os.WriteFile(filepath.Join(configDir(), "browser"), []byte(name+"\\n"), 0o644)',
        'os.WriteFile(filepath.Join(configDir(), "browser"), []byte(name+"\\n"), 0o600)')
    s = s.replace(
        'os.OpenFile(filepath.Join(dir, "fenestra.log"), os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o644)',
        'os.OpenFile(filepath.Join(dir, "fenestra.log"), os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o600)')
    s = s.replace(
        'os.WriteFile(file, []byte(entry), 0o755)',
        'os.WriteFile(file, []byte(entry), 0o644)')

    with io.open(p, "w", encoding="utf-8", newline="") as fh:
        fh.write(s)

print("ok")
