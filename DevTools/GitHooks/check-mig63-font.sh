#!/bin/sh
# Stable safeguard: retain this check after the temporary MIG63 branch guard ends.
set -eu
root=$(git rev-parse --show-toplevel)
cd "$root"

legacy='Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset'
legacy_baseline='1d379a7eb64de1871194614f83bf61ac52e0d323677048fe237e3fbd3b399cc2'
static_asset='Assets/Font/Heavy/ShortCycle_Heavy_Static.asset'
dynamic_asset='Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset'
manifest='DevTools/Fonts/ShortCycleFontBuildManifest.json'

fail() { echo "MIG63 font guard: $*" >&2; exit 1; }
hash_stream() {
  if command -v sha256sum >/dev/null 2>&1; then sha256sum | cut -d ' ' -f 1
  elif command -v shasum >/dev/null 2>&1; then shasum -a 256 | cut -d ' ' -f 1
  else fail 'sha256sum or shasum is required'; fi
}
valid_hash() { [ "${#1}" = 64 ] && ! printf '%s' "$1" | LC_ALL=C grep -q '[^0-9a-f]'; }
lower() { printf '%s' "$1" | tr 'A-F' 'a-f'; }

approved=$(git config --local --get mig63.fontApprovedSha256 || true)
approved=$(lower "$approved")
reason=$(git config --local --get mig63.fontApprovalReason || true)
check_legacy_hash() {
  candidate=$(lower "$2")
  valid_hash "$candidate" || fail "$1 has an invalid SHA-256"
  [ "$candidate" = "$legacy_baseline" ] && return 0
  if [ "$candidate" = "$approved" ] && printf '%s' "$reason" | LC_ALL=C grep -q '[^[:space:]]'; then
    echo "MIG63 font guard: exact-hash exception for $1 ($candidate). Reason: $reason" >&2
    return 0
  fi
  fail "$1 drifted: $candidate. Preserve the changed asset and inspect it; do not commit atlas drift. An intentional legacy change requires explicit approval of this exact hash. See DevTools/GitHooks/README.md."
}
check_exact_hash() {
  candidate=$(lower "$2")
  expected=$(lower "$3")
  valid_hash "$candidate" || fail "$1 has an invalid SHA-256"
  valid_hash "$expected" || fail "$1 manifest SHA-256 is invalid"
  [ "$candidate" = "$expected" ] || fail "$1 drifted: $candidate (approved manifest: $expected)"
}
index_content_hash() {
  indexed_path=$1
  blob=$(git rev-parse --verify ":$indexed_path") || fail "index asset is missing/unmerged: $indexed_path"
  size=$(git cat-file -s "$blob")
  # Read small blobs as possible LFS pointers; never hash pointer text as font data.
  if [ "$size" -le 1024 ] && [ "$(git cat-file blob "$blob" | head -n 1)" = 'version https://git-lfs.github.com/spec/v1' ]; then
    pointer=$(git cat-file blob "$blob")
    oid=$(printf '%s\n' "$pointer" | sed -n 's/^oid sha256://p')
    valid_hash "$oid" || fail "index LFS pointer has an invalid oid: $indexed_path"
    printf '%s' "$oid"
  else
    git cat-file blob "$blob" | hash_stream
  fi
}
check_legacy_asset() {
  [ -f "$legacy" ] || fail "working file is missing: $legacy"
  check_legacy_hash 'legacy working file' "$(hash_stream < "$legacy")"
  check_legacy_hash 'legacy index content' "$(index_content_hash "$legacy")"
}
check_manifest_asset() {
  label=$1
  path=$2
  expected=$3
  [ -f "$path" ] || fail "working file is missing: $path"
  check_exact_hash "$label working file" "$(hash_stream < "$path")" "$expected"
  check_exact_hash "$label index content" "$(index_content_hash "$path")" "$expected"
}
manifest_value() {
  key=$1
  values=$(sed -n 's/^[[:space:]]*"'"$key"'"[[:space:]]*:[[:space:]]*"\([^"]*\)".*$/\1/p' "$manifest")
  [ "$(printf '%s\n' "$values" | sed '/^$/d' | wc -l | tr -d ' ')" = 1 ] ||
    fail "manifest must contain exactly one string field: $key"
  printf '%s' "$values"
}

if [ ! -f "$static_asset" ]; then
  # A committed static font cannot silently downgrade to legacy protection.
  git cat-file -e "HEAD:$static_asset" 2>/dev/null && fail "committed static font is missing"
  check_legacy_asset
  # Before PRE-1 assets exist, retain the reviewed legacy guard exactly.
  git cat-file -e ":$static_asset" 2>/dev/null &&
    fail "static font is staged but absent from the working tree: $static_asset"
  git cat-file -e ":$dynamic_asset" 2>/dev/null &&
    fail "dynamic fallback is staged before the static font: $dynamic_asset"
  git cat-file -e ":$manifest" 2>/dev/null &&
    fail "build manifest is staged before the static font: $manifest"
  [ ! -e "$dynamic_asset" ] || fail "dynamic fallback exists before static font: $dynamic_asset"
  [ ! -e "$manifest" ] || fail "build manifest exists before static font: $manifest"
  exit 0
fi

[ -f "$manifest" ] || fail "approved build manifest is missing: $manifest"
manifest_blob=$(git rev-parse --verify ":$manifest") || fail "index manifest is missing/unmerged: $manifest"
manifest_work_hash=$(hash_stream < "$manifest")
manifest_index_hash=$(git cat-file blob "$manifest_blob" | hash_stream)
[ "$manifest_work_hash" = "$manifest_index_hash" ] ||
  fail 'working and staged build manifests differ; review and stage one exact manifest'

[ "$(manifest_value approvalStatus)" = 'APPROVED' ] ||
  fail 'build manifest is not approved (approvalStatus must be APPROVED)'
[ -n "$(manifest_value approvedBy)" ] || fail 'approved manifest has no approvedBy value'
[ -n "$(manifest_value approvedAtUtc)" ] || fail 'approved manifest has no approvedAtUtc value'
# Explicit user-approved FONT-R policy: only Static is byte-protected.
# Older manifests keep their reviewed dual-font/legacy checks below.
if LC_ALL=C grep -q '"protectionMode"' "$manifest"; then
  [ "$(manifest_value protectionMode)" = 'STATIC_ONLY' ] || fail 'unknown protectionMode'
  [ "$(manifest_value staticAssetPath)" = "$static_asset" ] || fail 'manifest staticAssetPath is not the reviewed path'
  check_manifest_asset 'static font' "$static_asset" "$(manifest_value staticAssetSha256)"
  exit 0
fi
check_legacy_asset
[ -f "$dynamic_asset" ] || fail "dynamic fallback is missing: $dynamic_asset"
[ "$(manifest_value legacyAssetPath)" = "$legacy" ] || fail 'manifest legacyAssetPath is not the reviewed path'
[ "$(manifest_value staticAssetPath)" = "$static_asset" ] || fail 'manifest staticAssetPath is not the reviewed path'
[ "$(manifest_value dynamicAssetPath)" = "$dynamic_asset" ] || fail 'manifest dynamicAssetPath is not the reviewed path'

manifest_legacy_hash=$(lower "$(manifest_value legacyAssetSha256)")
manifest_static_hash=$(lower "$(manifest_value staticAssetSha256)")
manifest_dynamic_hash=$(lower "$(manifest_value dynamicEmptyAssetSha256)")
[ "$manifest_legacy_hash" = "$legacy_baseline" ] ||
  fail 'approved manifest does not preserve the reviewed legacy baseline hash'
check_manifest_asset 'static font' "$static_asset" "$manifest_static_hash"
check_manifest_asset 'empty dynamic fallback' "$dynamic_asset" "$manifest_dynamic_hash"
