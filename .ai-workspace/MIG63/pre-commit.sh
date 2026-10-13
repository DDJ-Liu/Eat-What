#!/bin/sh
root=$(git rev-parse --show-toplevel) || exit 1
sh "$root/DevTools/GitHooks/check-mig63-font.sh" || exit 1
# Permanent branch guard; migration-specific path freezes were retired at E2.
branch=$(git symbolic-ref --quiet --short HEAD) || exit 1
case "$branch" in
  main|backup/*) echo "MIG63: direct commits on main/backup are blocked." >&2; exit 1 ;;
esac
