# Agent Instructions

## Commits

- Always create small commits with only a subject line: no description, no body, no trailers.
- Create commits in between whenever it makes sense, so each commit stays small and isolated.

## Host-side only

All mods in this workspace MUST work host side only. Only the host has the mod installed; clients run the unmodified game.

- If networking functionality is required, it must work in such a way that only the host requires the mod installed.
- Everything sent to clients must be something unmodded clients already understand: vanilla messages, RPCs and synced fields, in their vanilla data layout.
- Not allowed: custom message types, extra serialized data, new SyncVars, or networking libraries such as R2API Networking. This also applies to optional extras that only clients with the mod would use.
- Allowed: host-side code that adds nothing to network traffic, e.g. a NetworkBehaviour that only decides which clients receive an object.

## Workspace

- `ShareSuite` is a third-party mod (submodule). Do not modify or upgrade it.
- `dec/<version>/` holds the decompiled game code and `prefabs/<version>/` the extracted game prefabs. Both are git-ignored reference material.
