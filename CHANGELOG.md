# Changelog

## Unreleased

- The default follows the live http/https association (the real system browser), so changing it in Windows is picked up on the next open; `--install` still captures the previous `.html` handler as a fallback.
- Double-click Fenestra with no file to open the browser settings screen and pick the default browser; `--settings` does the same.

## 0.7.0

- Linux and macOS now build from a single Go module (`linux/`), with the
  platform behavior selected at runtime; the macOS kit compiles the same source.
- Handle ignored errors in Go and C# (`writeString`, lock/log/preference errors,
  HOME failures, logged catches); gosec is clean.
- Pin GitHub Actions by SHA, name the timing constants, drop the unused field.
- Release policy: milestones, not per-change tags. See [RELEASING.md](RELEASING.md).

## 0.6.1

- `--no-prompt` skips the first-run browser picker; `--serve` skips it too, so
  scripts and services never block on a dialog.
- `FuzzHandlerPaths` exercises request-path handling with traversal, NUL,
  hidden-file and encoded inputs; 34k runs locally with no crash.

## 0.6.0

- First run asks which browser to use and saves the answer: a native dialog on
  Windows, a terminal menu on Linux and macOS, and an `osascript` dialog when the
  macOS app is opened from the Finder.
- With no preference and no way to ask (headless, CI, no TTY), the system browser
  is used; `--browser` still wins and a missing browser falls back to the system.
- The site documents the first-run picker and where the preference lives.

## 0.5.3

- Hidden files and directories (`.env`, `.git/`) return 403 and are omitted from
  listings; `.well-known/` keeps working.
- The single-instance ping verifies which root the running server serves, so a
  stale lock cannot open a different folder. Linux hashes paths case-sensitively.
- The Windows reparse-point check fails closed on unexpected errors.
- `--port` writes the session lock, so a second launch reuses the server.
- `--uninstall` refreshes the desktop database and suggests the preferred browser.
- Port selection uses named constants, a pure `candidatePort`, and logs the
  ephemeral fallback.
- New tests: root-aware ping, hidden files, port candidates, lock round-trip,
  `extractPort`; CI runs `go test -race`.

## 0.5.2

- Fix the single-instance ping reading only the first packet.

## 0.5.1

- A source entry (one that references `/src/`) now serves the sibling `dist/`,
  `build/` or `out/` build, and explains what to do when there is none.

## 0.5.0

- Host header validation (DNS rebinding), symlink confinement, ETag/304, Range,
  `--port`, single-source `VERSION`, and an automated release workflow.

## 0.4.2

- Every user-facing message in English (dialogs, CLI, HTTP errors, macOS scripts).
