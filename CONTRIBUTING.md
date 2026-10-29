# Contributing

English · [Português](CONTRIBUTING.pt-BR.md)

Thanks for considering it. This is a small tool on purpose — keep it small.

## Build

Requirements: Windows 10/11 and the .NET Framework 4.x that ships with it.
No SDK, no Visual Studio, no internet.

```powershell
./build.ps1
```

The binary lands in `bin/Fenestra.exe`.

## Test

```powershell
./tests/run-tests.ps1
```

The script builds nothing; run `build.ps1` first. It starts the server in
headless mode (`FENESTRA_NO_OPEN=1`), makes real HTTP requests against a
temporary fixture and reports pass/fail with a non-zero exit code.

## Code style

- **C# 5 only.** The compiler is the one from .NET Framework 4.8, so no
  interpolation, no `?.`, no expression-bodied members.
- **Zero dependencies.** Only the BCL. If a change needs NuGet, it needs a
  discussion first.
- **One file for now.** `src/Fenestra.cs` is intentionally a single file;
  splitting it is on the roadmap but not a requirement for PRs.
- Comments explain *why*, not *what*.

## Pull requests

- One thing per PR.
- Include a test if you touch HTTP handling, path resolution or port logic.
- Update `README.md` if behavior changes.
- Keep the diff readable; no drive-by reformatting.

## Good first issues

- `--root <folder>` to serve a folder different from the file's own.
- Windows ARM64 support.
- Code signing for the release binary (removes the SmartScreen warning).
- Portable mode (`fenestra.ini` next to the exe, no `%LOCALAPPDATA%`).
- More tests: HEAD, ETag, range requests, UTF-8 paths.
