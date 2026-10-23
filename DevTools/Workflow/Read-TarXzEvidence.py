"""TK-WF-01 internal tar.xz reader. JSON stdin/stdout; never extracts or writes."""
import base64
import hashlib
import io
import json
import lzma
import os
import pathlib
import re
import stat
import sys
import tarfile

MAX_PACKED = 10_000_000
MAX_TEXT = 2 * 1024 * 1024
MAX_MANIFEST = 32 * 1024 * 1024
MAX_FILES = 10_000
MAX_UNPACKED = 512 * 1024 * 1024
MAX_DECODED = 576 * 1024 * 1024
MAX_HEADER = 64 * 1024
CHUNK = 64 * 1024


def require(condition, message):
    if not condition:
        raise ValueError(message)


def relative(value):
    require(isinstance(value, str) and 0 < len(value) <= 4096, 'Invalid archive path')
    require('\\' not in value and ':' not in value and not any(ord(c) < 32 for c in value), 'Unsafe archive path')
    parts = value.split('/')
    require(all(p not in ('', '.', '..') for p in parts), 'Absolute/traversal archive path rejected')
    return value


def local(root, value):
    path = root.joinpath(*relative(value).split('/'))
    for parent in (path, *path.parents):
        if parent.exists() or parent.is_symlink():
            info = parent.lstat()
            require(not stat.S_ISLNK(info.st_mode) and not (getattr(info, 'st_file_attributes', 0) & 0x400), 'Reparse path rejected')
    require(path.resolve().is_relative_to(root.resolve()), 'Path escapes project')
    return path


def integer(value, maximum, label):
    require(type(value) is int and 0 <= value <= maximum, 'Invalid/bounded ' + label)
    return value


def digest(value):
    require(isinstance(value, str) and re.fullmatch(r'[0-9A-Fa-f]{64}', value), 'Invalid SHA256')
    return value.upper()


class BoundedXZ:
    """Single XZ stream with bounded decoder memory, output, and read allocations."""
    def __init__(self, packed, limit):
        self.data = packed
        self.pos = 0
        self.total = 0
        self.limit = limit
        self.decoder = lzma.LZMADecompressor(format=lzma.FORMAT_XZ, memlimit=128 * 1024 * 1024)

    def read(self, size):
        require(0 <= size <= CHUNK, 'Oversized decoder read')
        output = bytearray()
        while len(output) < size:
            if self.decoder.eof:
                require(not self.decoder.unused_data and self.pos == len(self.data), 'Trailing/concatenated XZ stream rejected')
                break
            block = b''
            if self.decoder.needs_input:
                require(self.pos < len(self.data), 'Truncated XZ stream')
                block = self.data[self.pos:self.pos + CHUNK]
                self.pos += len(block)
            decoded = self.decoder.decompress(block, max_length=size - len(output))
            self.total += len(decoded)
            require(self.total <= self.limit, 'Total decompression budget exceeded')
            output.extend(decoded)
        return bytes(output)


class BoundedTarInfo(tarfile.TarInfo):
    # Bound extension headers before stdlib allocates their bodies/recurses.
    def _proc_pax(self, archive):
        require(0 <= self.size <= MAX_HEADER, 'PAX header exceeds bound')
        return super()._proc_pax(archive)

    def _proc_gnulong(self, archive):
        require(0 <= self.size <= MAX_HEADER, 'GNU header exceeds bound')
        return super()._proc_gnulong(archive)

    def _proc_sparse(self, archive):
        raise ValueError('Sparse TAR entries rejected')

    def _proc_gnusparse_00(self, *args):
        raise ValueError('Sparse TAR entries rejected')

    _proc_gnusparse_01 = _proc_gnusparse_00
    _proc_gnusparse_10 = _proc_gnusparse_00


def read_request(request):
    root = pathlib.Path(request['projectRoot'])
    require(root.is_absolute(), 'Project root must be absolute')
    manifest_name = relative(request['manifestPath'])
    archive_name = relative(request['archivePath'])
    require(manifest_name.startswith('.ai-workspace/archive/') and archive_name.startswith('.ai-workspace/archive/'), 'Archive storage required')
    manifest_path = local(root, manifest_name)
    require(manifest_path.stat().st_size <= MAX_MANIFEST, 'Manifest exceeds bound')
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    require(manifest.get('schemaVersion') == 3, 'Expected schemaVersion 3')
    task_id = request['taskId']
    require(re.fullmatch(r'AI-\d{6}', task_id) is not None, 'AI task identity required')
    archive_path = local(root, archive_name)
    require(archive_path.parent == manifest_path.parent, 'Archive must belong to manifest directory')
    packages = [p for p in manifest['packages'] if p.get('archiveFile') == archive_path.name]
    require(len(packages) == 1, 'Archive package must be unique')
    package = packages[0]
    unit = package.get('unitId')
    require(isinstance(unit, str) and re.fullmatch(r'[A-Za-z0-9_-]{1,120}', unit), 'Invalid unit identity')
    require(package.get('taskId') == task_id and package.get('status') == 'READY', 'Package is not READY for task')
    require(package.get('format') == 'tar.xz' and archive_path.name == unit + '.tar.xz', 'Explicit tar.xz format/file required')
    require(sum(p.get('unitId') == unit for p in manifest['packages']) == 1, 'Duplicate unit identity')
    packed_size = integer(package.get('packedBytes'), MAX_PACKED, 'packed bytes')
    unpacked_size = integer(package.get('unpackedBytes'), MAX_UNPACKED, 'unpacked bytes')
    require(archive_path.is_file() and archive_path.stat().st_size == packed_size, 'Packed length differs from manifest')
    with archive_path.open('rb') as stream:
        packed = stream.read(MAX_PACKED + 1)
    require(len(packed) == packed_size, 'Packed file drift/limit')
    archive_sha = hashlib.sha256(packed).hexdigest().upper()
    require(archive_sha == digest(package.get('archiveSha256')), 'Archive SHA differs from manifest')
    entries = {}
    folded = set()
    for entry in manifest['files']:
        if entry.get('archiveUnitId') != unit or entry.get('disposition') != 'archive':
            continue
        require(entry.get('taskId') == task_id, 'Cross-task unit member rejected')
        name = relative(entry.get('originalPath'))
        require(name.casefold() not in folded, 'Duplicate/ambiguous manifest path')
        folded.add(name.casefold())
        integer(entry.get('bytes'), MAX_UNPACKED, 'entry bytes')
        digest(entry.get('sha256'))
        entries[name] = entry
        require(len(entries) <= MAX_FILES, 'Entry count exceeds bound')
    require(entries and sum(e['bytes'] for e in entries.values()) == unpacked_size, 'Declared total differs from member lengths')
    result = {'ok': True, 'format': 'tar.xz', 'archiveSha256': archive_sha,
              'packedBytes': packed_size, 'unpackedBytes': unpacked_size, 'entries': len(entries),
              'verification': 'Package SHA/length and unit manifest checked; member data checked only when read'}
    action = request.get('action')
    require(action in ('inspect', 'read'), 'Unknown reader action')
    if action == 'inspect':
        return result
    wanted = relative(request['entryPath'])
    matches = [e for e in manifest['files'] if e.get('originalPath', '').casefold() == wanted.casefold()
               and e.get('taskId') == task_id and e.get('disposition') == 'archive'
               and (wanted != '__archive__/manifest.json' or e.get('archiveUnitId') == unit)]
    require(len(matches) == 1 and matches[0].get('archiveUnitId') == unit, 'Requested member is not unique in this READY unit')
    wanted = matches[0]['originalPath']
    require(wanted in entries and entries[wanted]['bytes'] <= MAX_TEXT, 'Requested text exceeds 2MiB bound')
    wanted_sha = digest(entries[wanted]['sha256'])
    limit = min(MAX_DECODED, unpacked_size + len(entries) * 4096 + 10240)
    reader = BoundedXZ(packed, limit)
    members = {}
    verified_roots = set()
    content = None
    with tarfile.open(fileobj=reader, mode='r|', tarinfo=BoundedTarInfo, encoding='utf-8', errors='strict') as archive:
        for info in archive:
            name = relative(info.name)
            require(name in entries and name not in members, 'Unknown/duplicate TAR member')
            require(info.sparse is None and not any(k.startswith('GNU.sparse') for k in info.pax_headers), 'Sparse TAR entries rejected')
            expected = entries[name]
            require(info.isreg() or info.islnk(), 'Symlink/special TAR member rejected')
            if info.islnk():
                target = relative(info.linkname)
                require(info.size == 0 and target in entries, 'Unknown/cross-unit hardlink target')
                require(expected['bytes'] == entries[target]['bytes'] and digest(expected['sha256']) == digest(entries[target]['sha256']), 'Hardlink manifest content differs')
                members[name] = target
            else:
                require(info.size == expected['bytes'], 'TAR member length differs from manifest')
                members[name] = None
                if digest(expected['sha256']) == wanted_sha and expected['bytes'] == entries[wanted]['bytes']:
                    # At most one bounded content buffer; aliases share content, never filesystem links.
                    source = archive.extractfile(info)
                    with source:
                        data = source.read(MAX_TEXT + 1)
                    require(len(data) == expected['bytes'] and hashlib.sha256(data).hexdigest().upper() == wanted_sha, 'Requested content SHA/length mismatch')
                    content = data
                    verified_roots.add(name)
        require(members.keys() == entries.keys(), 'TAR/manifest member set differs')
        # Consume TAR padding and the XZ footer, also checking integrity and total limits.
        while True:
            tail = archive.fileobj.read(CHUNK)
            if not tail:
                break
            require(not any(tail), 'Non-padding data after TAR end')
    roots = {}
    for name in members:
        current, chain, seen = name, [], set()
        while current not in roots:
            require(current not in seen, 'Cyclic hardlink rejected')
            seen.add(current)
            chain.append(current)
            target = members[current]
            if target is None:
                roots[current] = current
                break
            current = target
        for alias in chain:
            roots[alias] = roots[current]
    require(roots[wanted] in verified_roots and content is not None, 'Requested content root not verified')
    result.update(entryPath=wanted, sha256=wanted_sha, bytes=len(content),
                  contentBase64=base64.b64encode(content).decode('ascii'),
                  verification='Package SHA, complete member/link structure, requested content SHA/length verified')
    return result


def main():
    try:
        raw = sys.stdin.buffer.read(65537)
        require(len(raw) <= 65536, 'Request exceeds bound')
        result = read_request(json.loads(raw.decode('utf-8-sig')))
        print(json.dumps(result, ensure_ascii=True, separators=(',', ':')))
    except Exception as error:
        print(json.dumps({'ok': False, 'error': str(error)[:2000]}, ensure_ascii=True))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
