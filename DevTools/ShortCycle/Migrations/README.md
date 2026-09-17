# ShortCycle one-time migrations

This directory retains source-level history for completed, one-time ShortCycle scene migrations.
Files here are outside Unity's `Assets` tree, so they cannot remain active Editor menu or automatic entry points.

- `ShortCycleSceneMigrationV2.cs`: historical AI-000045 migration implementation. It was retired from `Assets/Editor/ShortCycle` by AI-000047 after the target scene, prefabs, data wiring, and verification helpers had been migrated. Do not run it against production assets; use it only as an audit reference.
