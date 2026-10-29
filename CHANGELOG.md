# Changelog

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
