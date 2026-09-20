#!/bin/sh
# MIG63 temporary commit guard. Removal/phase changes require lifecycle ledger entry.
branch=$(git symbolic-ref --quiet --short HEAD) || exit 1
case "$branch" in
  main|backup/*) echo "MIG63: direct commits on main/backup are blocked." >&2; exit 1 ;;
esac
phase=$(git config --local --get mig63.phase) || phase=preparing
git -c core.quotePath=false diff --cached --name-only --diff-filter=ACMRD |
while IFS= read -r path; do
  case "$branch" in
    content/p0p1-layout)
      case "$phase" in handoff|closed) continue ;; esac
      case "$path" in
        *.cs|*.cs.meta|*.asmdef|*.asmdef.meta|*.shader|*.shader.meta|*.shadergraph|*.shadergraph.meta|*.hlsl|*.hlsl.meta|*.mat|*.mat.meta|ProjectSettings/*|Packages/*|Assets/Settings/*|Assets/Font/*|Assets/Spine/*|Assets/Plugins/*|.gitignore)
          echo "MIG63: CONTENT file ownership/freeze rejects $path" >&2; exit 1 ;;
      esac ;;
    migration/unity-6.3)
      case "$phase" in integrating|handoff|closed) continue ;; esac
      case "$path" in
        *.unity|*.prefab) echo "MIG63: scene/prefab save is frozen before M4: $path" >&2; exit 1 ;;
      esac ;;
  esac
done
