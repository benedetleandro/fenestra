# Security Policy

## Supported versions

Fenestra is an alpha project. Only the latest commit on `main` is supported.

## Reporting a vulnerability

Please **do not open a public issue**. Use GitHub's private vulnerability
reporting ("Security" tab → "Report a vulnerability") on this repository.

You can expect an answer within a few days. If the report is valid, a fix and
a release will follow, and you will be credited unless you prefer otherwise.

## Threat model

Fenestra is a **local** tool. It runs an HTTP server bound to `127.0.0.1` and
serves the folder of the file you opened. It is not designed to be exposed to a
network, and it does not implement TLS, authentication or access control.

What is protected:

- **Binding**: the listener binds only to `127.0.0.1`, never `0.0.0.0`.
- **Path traversal**: requests are normalized and confined to the served root.
  `..`, `:`, `::$DATA` and absolute paths are rejected.
- **No remote code**: the server only reads and serves files. It does not
  execute anything from the served folder.

What is **not** protected, by design:

- Every file under the served root is readable by anything that can reach the
  port, including other local processes. Point Fenestra at a folder, not at
  your home directory.
- Dotfiles (`.env`, `.git`) are served if they live under the root.
- There is no authentication: a local process can request any file in the root.
- The `Referer`/`Origin` headers are not validated, so a malicious page could
  probe the local port (DNS rebinding is partially mitigated by binding to
  loopback only, but not fully).

## Scope

Out of scope: vulnerabilities that require the attacker to already have local
code execution, and issues caused by opening a folder that contains secrets.
