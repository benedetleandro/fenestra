# Releasing

English · [Português](RELEASING.pt-BR.md)

Fenestra is alpha. Releases are **milestones**, not every commit.

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
5. The release workflow builds and publishes the assets.
6. Update the Homebrew tap with the new URLs and SHA-256.
7. Close the issues and the milestone.

## Versioning

`0.x` while alpha. Breaking changes bump the minor version. Fixes go into the
next milestone, not into a new tag. `1.0.0` is reserved for the first stable
release.
