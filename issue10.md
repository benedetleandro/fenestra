Addressed in the unreleased state (no tag, per the release policy).

- All 19 action references are now pinned by commit SHA, with the tag kept as a comment.
- Named constants: `handoffDelay`, `idleAfter`, `maxHTMLSize`.
- Removed the unused `found` field from the Linux browser struct.
- `.gitignore` already covers `dist/`, `*.test`; `build.ps1` is English; CHANGELOG exists.

Declined with reason: bumping `go.mod` from 1.21 (keeps the build usable on older toolchains) and the `urlPathEscape` `%2F` replacement (it restores path separators escaped by `url.PathEscape`, it is not a no-op).
