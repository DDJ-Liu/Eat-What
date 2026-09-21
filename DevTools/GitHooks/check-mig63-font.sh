#!/bin/sh
# Stable safeguard: retain this check after the temporary MIG63 branch guard ends.
set -eu
root=$(git rev-parse --show-toplevel)
cd "$root"
asset='Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset'
baseline='1d379a7eb64de1871194614f83bf61ac52e0d323677048fe237e3fbd3b399cc2'
fail() { echo "MIG63 font guard: $*" >&2; exit 1; }
hash_stream() {
  if command -v sha256sum >/dev/null 2>&1; then sha256sum | cut -d ' ' -f 1
  elif command -v shasum >/dev/null 2>&1; then shasum -a 256 | cut -d ' ' -f 1
  else fail 'sha256sum or shasum is required'; fi
}
valid_hash() { [ "${#1}" = 64 ] && ! printf '%s' "$1" | LC_ALL=C grep -q '[^0-9a-f]'; }
approved=$(git config --local --get mig63.fontApprovedSha256 || true)
approved=$(printf '%s' "$approved" | tr 'A-F' 'a-f')
reason=$(git config --local --get mig63.fontApprovalReason || true)
check_hash() {
  valid_hash "$2" || fail "$1 has an invalid SHA-256"
  [ "$2" = "$baseline" ] && return 0
  if [ "$2" = "$approved" ] && printf '%s' "$reason" | LC_ALL=C grep -q '[^[:space:]]'; then
    echo "MIG63 font guard: exact-hash exception for $1 ($2). Reason: $reason" >&2
    return 0
  fi
  fail "$1 drifted: $2. Preserve the changed asset and inspect it; do not commit atlas drift. An intentional change requires explicit approval of this exact hash. See DevTools/GitHooks/README.md."
}
[ -f "$asset" ] || fail "working file is missing: $asset"
check_hash 'working file' "$(hash_stream < "$asset")"
blob=$(git rev-parse --verify ":$asset") || fail "index asset is missing/unmerged: $asset"
size=$(git cat-file -s "$blob")
# Read small blobs as possible LFS pointers; never hash pointer text as font data.
if [ "$size" -le 1024 ] && [ "$(git cat-file blob "$blob" | head -n 1)" = 'version https://git-lfs.github.com/spec/v1' ]; then
  pointer=$(git cat-file blob "$blob")
  oid=$(printf '%s\n' "$pointer" | sed -n 's/^oid sha256://p')
  check_hash 'index LFS content' "$oid"
else
  check_hash 'index content' "$(git cat-file blob "$blob" | hash_stream)"
fi
