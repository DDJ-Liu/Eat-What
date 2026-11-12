"""Byte-exact, source-only compiler evidence. No checkout, Git writes or Unity calls."""
import argparse
import difflib
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import zipfile


SCHEMA = "code-source-closure/1"
MAX_FILE = 64 * 1024 * 1024


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def write_json(path, value):
    path.write_bytes((json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n").encode("utf-8"))


def relative_name(value):
    if not isinstance(value, str) or not value or "\\" in value or ":" in value:
        raise ValueError("Unsafe closure relative path")
    p = PurePosixPath(value)
    if p.is_absolute() or p.as_posix() != value or any(
            part in (".", "..", ".git") or part.endswith((".", " ")) or
            re.fullmatch(r"(?i)(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?", part)
            for part in value.split("/")):
        raise ValueError("Unsafe closure relative path: " + value)
    return value


def no_links(path):
    for parent in (path, *path.parents):
        if parent.is_symlink() or (parent.exists() and getattr(parent.stat(), "st_file_attributes", 0) & 0x400):
            raise ValueError("Closure paths must not traverse reparse points")


def output_path(root, path, fresh=False):
    path = Path(os.path.abspath(path))
    no_links(path)
    path.relative_to(root / ".ai-workspace" / "outputs")
    if path == root / ".ai-workspace" / "outputs":
        raise ValueError("Output root is not a task directory")
    if fresh and path.exists():
        raise ValueError("Destination must be new; existing files are never overwritten")
    return path


def git(root, *args):
    p = subprocess.run(["git", *args], cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if p.returncode:
        raise ValueError("Read-only Git command failed: " + p.stderr.decode("utf-8", "replace"))
    return p.stdout


def lfs_pointer(data):
    if not re.match(rb"\Aversion https://git-lfs.github.com/spec/v1\r?\n", data):
        return None
    m = re.search(rb"(?m)^oid sha256:([0-9a-f]{64})\r?$", data)
    n = re.search(rb"(?m)^size ([0-9]+)\r?$", data)
    if not m or not n:
        raise ValueError("Malformed Git LFS pointer")
    return {"oid": m[1].decode(), "size": int(n[1])}


def encode(base, target, is_lfs=False):
    # LFS content is kept literally, never regenerated from its Git pointer.
    if is_lfs or base is None:
        return {"kind": "literal-lfs" if is_lfs else "literal"}, target
    if base == target:
        return {"kind": "git-blob"}, b""
    for mode, transformed in (("lf", base.replace(b"\r\n", b"\n")),
                              ("crlf", base.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n"))):
        if transformed == target:
            return {"kind": "eol", "mode": mode}, b""
    before, after = base.splitlines(keepends=True), target.splitlines(keepends=True)
    offsets = [0]
    for line in before:
        offsets.append(offsets[-1] + len(line))
    tape, ops = bytearray(), []
    for tag, a, b, c, d in difflib.SequenceMatcher(None, before, after, autojunk=False).get_opcodes():
        if tag == "equal":
            ops.append(["base", offsets[a], offsets[b] - offsets[a]])
        elif tag in ("replace", "insert"):
            data = b"".join(after[c:d])
            ops.append(["payload", len(tape), len(data)])
            tape.extend(data)
    if len(tape) >= len(target):
        return {"kind": "literal"}, target
    return {"kind": "delta", "ops": ops}, bytes(tape)


def decode(record, base, payload):
    kind = record["kind"]
    if kind in ("literal", "literal-lfs"):
        return payload
    if base is None:
        raise ValueError("Required Git blob is missing")
    if kind == "git-blob":
        return base
    if kind == "eol":
        result = base.replace(b"\r\n", b"\n")
        if record["mode"] not in ("lf", "crlf"):
            raise ValueError("Unknown EOL transform")
        return result if record["mode"] == "lf" else result.replace(b"\n", b"\r\n")
    if kind != "delta":
        raise ValueError("Unknown closure storage kind")
    result = bytearray()
    for source, start, count in record["ops"]:
        if source not in ("base", "payload"):
            raise ValueError("Unknown delta source")
        data = base if source == "base" else payload
        if type(start) is not int or type(count) is not int or start < 0 or count < 0 or start + count > len(data):
            raise ValueError("Invalid delta byte range")
        result.extend(data[start:start + count])
        if len(result) > MAX_FILE:
            raise ValueError("Restored file exceeds limit")
    return bytes(result)


class ClosureEvidence:
    def __init__(self, root, directory):
        self.root = Path(root).resolve()
        self.directory = output_path(self.root, directory, fresh=True)
        self.head = git(self.root, "rev-parse", "HEAD").decode().strip()
        self.tree = {}
        for item in git(self.root, "ls-tree", "-r", "-z", self.head).split(b"\0"):
            if item:
                info, path = item.split(b"\t", 1)
                if info.split()[1] == b"blob":
                    self.tree[path.decode("utf-8")] = info.split()[2].decode()
        self.directory.mkdir(parents=True)
        self.manifest = {"schema": SCHEMA, "gitHead": self.head, "files": [], "objects": {},
                         "context": "context.zip", "payload": "payload.zip",
                         "externalCompilerAndReferenceBinaries": "identities only; not source restoration"}
        with zipfile.ZipFile(self.directory / "context.zip", "w", zipfile.ZIP_DEFLATED) as z:
            p = subprocess.run(["git", "status", "--porcelain=v1", "-uall"], cwd=self.root,
                               stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            if p.returncode:
                raise ValueError("Cannot capture worktree status")
            z.writestr("git-status.txt", p.stdout)
            z.writestr("git-status.stderr.txt", p.stderr)
        with zipfile.ZipFile(self.directory / "payload.zip", "w", zipfile.ZIP_DEFLATED):
            pass
        self.names = set()
        self.blobs = {}

    def capture(self, stage, entries):
        relative_name(stage)
        diffs, result = [], []
        with zipfile.ZipFile(self.directory / "payload.zip", "a", zipfile.ZIP_DEFLATED) as payload_zip:
            for entry in entries:
                source = Path(os.path.abspath(entry["path"]))
                no_links(source)
                source.relative_to(self.root)
                repo_path = relative_name(entry["repoPath"])
                restore_path = relative_name(entry["restorePath"])
                if restore_path.casefold() in self.names:
                    raise ValueError("Duplicate closure destination")
                raw = source.read_bytes()
                if len(raw) > MAX_FILE or lfs_pointer(raw):
                    raise ValueError("Oversized or unhydrated source input: " + str(source))
                key = sha(raw)
                if entry.get("expectedSha256", key).upper() != key:
                    raise ValueError("Source differs from declared original snapshot")
                if key not in self.manifest["objects"]:
                    oid = self.tree.get(repo_path)
                    base = self.blobs.setdefault(oid, git(self.root, "cat-file", "blob", oid)) if oid else None
                    pointer = lfs_pointer(base) if base is not None else None
                    record, tape = encode(base, raw, bool(pointer))
                    if decode(record, base, tape) != raw:
                        raise ValueError("Evidence encoding is not byte-exact")
                    record.update({"sha256": key, "bytes": len(raw)})
                    if oid and record["kind"] in ("git-blob", "delta", "eol"):
                        record["baseBlob"] = oid
                    if pointer:
                        record["gitLfsPointer"] = pointer
                        record["gitLfsContentMatchesDeclaredOid"] = key.lower() == pointer["oid"] and len(raw) == pointer["size"]
                    if tape or record["kind"] in ("literal", "literal-lfs", "delta"):
                        payload_key = sha(tape)
                        record["payloadKey"] = payload_key
                        if payload_key not in payload_zip.namelist():
                            payload_zip.writestr(payload_key, tape)
                    self.manifest["objects"][key] = record
                    if base is not None and not pointer and base != raw and b"\0" not in base + raw:
                        diffs.extend(difflib.unified_diff(base.decode("utf-8", "replace").splitlines(True),
                                     raw.decode("utf-8", "replace").splitlines(True),
                                     fromfile="HEAD/" + repo_path, tofile=restore_path))
                file_record = {"repoPath": repo_path, "restorePath": restore_path, "sha256": key, "bytes": len(raw)}
                self.manifest["files"].append(file_record)
                self.names.add(restore_path.casefold())
                result.append({"path": str(source), "bytes": len(raw), "sha256": key,
                               "closureManifest": str(self.directory / "manifest.json"), "restorePath": restore_path})
        with zipfile.ZipFile(self.directory / "context.zip", "a", zipfile.ZIP_DEFLATED) as z:
            z.writestr(stage + ".diff", "".join(diffs).encode("utf-8"))
        write_json(self.directory / "manifest.json", self.manifest)
        return result


def restore(root, manifest_path, destination):
    root = Path(root).resolve()
    manifest_path = Path(manifest_path).resolve()
    no_links(manifest_path)
    manifest = json.loads(manifest_path.read_bytes())
    if manifest.get("schema") != SCHEMA or not re.fullmatch(r"[0-9a-f]{40,64}", manifest["gitHead"]):
        raise ValueError("Unsupported closure manifest")
    destination = output_path(root, destination, fresh=True)
    names, blobs, materialized = set(), {}, {}
    with zipfile.ZipFile(manifest_path.parent / "payload.zip") as z:
        if len(z.namelist()) != len(set(z.namelist())):
            raise ValueError("Duplicate payload member")
        for key, record in manifest["objects"].items():
            if not re.fullmatch(r"[0-9A-F]{64}", key) or key != record["sha256"] or not 0 <= record["bytes"] <= MAX_FILE:
                raise ValueError("Invalid object identity")
            oid, payload_key = record.get("baseBlob"), record.get("payloadKey")
            if oid and not re.fullmatch(r"[0-9a-f]{40,64}", oid):
                raise ValueError("Invalid Git object identity")
            base = blobs.setdefault(oid, git(root, "cat-file", "blob", oid)) if oid else None
            payload = b""
            if payload_key:
                if not re.fullmatch(r"[0-9A-F]{64}", payload_key) or z.getinfo(payload_key).file_size > MAX_FILE:
                    raise ValueError("Invalid payload identity/size")
                payload = z.read(payload_key)
                if sha(payload) != payload_key:
                    raise ValueError("Payload SHA mismatch")
            raw = decode(record, base, payload)
            if len(raw) != record["bytes"] or sha(raw) != key:
                raise ValueError("Restored source SHA mismatch")
            materialized[key] = raw
        for entry in manifest["files"]:
            name = relative_name(entry["restorePath"])
            relative_name(entry["repoPath"])
            if name.casefold() in names or entry["sha256"] not in materialized or len(materialized[entry["sha256"]]) != entry["bytes"]:
                raise ValueError("Duplicate or inconsistent closure file")
            names.add(name.casefold())
        if any(any(parent.as_posix().casefold() in names for parent in PurePosixPath(name).parents
                   if parent.as_posix() != ".") for name in names):
            raise ValueError("Closure file/directory collision")
        # Validate every object and every destination before the first output write.
        destination.mkdir(parents=True)
        for entry in manifest["files"]:
            path = destination / entry["restorePath"]
            path.parent.mkdir(parents=True, exist_ok=True)
            with path.open("xb") as f:
                f.write(materialized[entry["sha256"]])
    return {"files": len(names), "allHashesMatched": True, "destination": str(destination)}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("action", choices=("pack", "restore"))
    p.add_argument("--root", required=True)
    p.add_argument("--manifest", required=True)
    p.add_argument("--output", required=True)
    args = p.parse_args()
    if args.action == "pack":
        entries = json.loads(Path(args.manifest).read_bytes())
        store = ClosureEvidence(args.root, args.output)
        store.capture("selected", entries)
        print(json.dumps({"files": len(entries), "manifest": str(store.directory / "manifest.json")}))
    else:
        print(json.dumps(restore(args.root, args.manifest, args.output)))


if __name__ == "__main__":
    main()
