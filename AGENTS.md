# Agent Instructions

## Commits

- Always create small commits with only a subject line: no description, no body, no trailers.
- Create commits in between whenever it makes sense, so each commit stays small and isolated.

## Host-side only

All mods in this workspace MUST work host side only. Only the host has the mod installed; clients run the unmodified game.

- If networking functionality is required, it must work in such a way that only the host requires the mod installed.
- No special networking code must be written (no custom network messages, no new networked components or SyncVars, no networking libraries such as R2API Networking). Changes reach clients only through what vanilla clients already understand.

## Workspace

- `ShareSuite` is a third-party mod (submodule). Do not modify or upgrade it.
- `dec/<version>/` holds the decompiled game code and `prefabs/<version>/` the extracted game prefabs. Both are git-ignored reference material.
