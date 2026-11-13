#!/bin/sh
root=$(git rev-parse --show-toplevel) || exit 1
sh "$root/DevTools/GitHooks/check-mig63-font.sh" || exit 1
# Permanent branch guard; migration-specific path freezes were retired at E2.
branch=$(git symbolic-ref --quiet --short HEAD) || exit 1
case "$branch" in
  main|backup/*) echo "MIG63: direct commits on main/backup are blocked." >&2; exit 1 ;;
esac
# Source/generated boundary and actual staged Git blob size, after permanent guards.
checker="$root/DevTools/GitHooks/check-repo-content.py"
if [ ! -f "$checker" ]; then
  echo "Repository content checker is missing." >&2
  exit 1
fi
if command -v python3 >/dev/null 2>&1; then
  python3 "$checker" --repo "$root" || exit 1
elif command -v python >/dev/null 2>&1; then
  python "$checker" --repo "$root" || exit 1
else
  echo "Python 3 is required for the repository content checker." >&2
  exit 1
fi
