Implemented in the unreleased branch state (no tag, per the new release policy).

- Go: response writes go through `writeString`, which logs failures; lock, log,
  preference and desktop-database errors are checked and logged; a missing HOME
  is a clear fatal error instead of a relative path. gosec now reports zero
  issues (with the reviewed G115/G204/G304/G703 exclusions).
- C#: the 11 empty `catch { }` blocks now log `ignored error: <message>`.
- Tests: Windows suite and Go tests pass.

Ships at the 0.7.0 milestone.
