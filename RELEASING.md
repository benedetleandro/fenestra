# Releasing

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)


Fenestra is 1.x. Releases are **milestones**, not every commit.

## Policy

- `main` always builds. Changes accumulate under **Unreleased** in
  [`CHANGELOG.md`](CHANGELOG.md).
- A release happens only when the maintainer asks for one, or at a milestone —
  never because a fix was merged.
- One tag per release (`vX.Y.Z`). Never a tag per change.
- The tag must match `VERSION`; CI enforces it.
- Patch releases exist only to fix a broken release. A normal fix waits for the
  next milestone.

## Steps

1. Move the `Unreleased` entries into a new version section in `CHANGELOG.md`.
2. Bump `VERSION` and the site version strings (`docs/`); the `version` CI job
   fails if they disagree.
3. Commit as `Release X.Y.Z`.
4. Tag and push: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. Update `docs/version.json` with the new version, notes URL, download URL and manual URL. `sha256` may stay empty until the assets exist; the update page hides it when empty.
6. The release workflow builds `Fenestra.exe`, the Linux and macOS binaries, `manual.html`, and `SHA256SUMS`.
7. Test the update tab before tagging: run the app with a local `version.json` that advertises the new version and confirm `/__fenestra/update` opens once.
8. Update the Homebrew tap with the new URLs and SHA-256.
9. Close the issues and the milestone.

## Versioning

`1.x` is stable. Breaking changes bump the major version. Fixes go into the
next milestone, not into a new tag.
