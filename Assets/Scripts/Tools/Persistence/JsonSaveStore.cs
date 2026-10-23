using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
namespace EatWhat.Tools.Persistence
{
    public sealed class JsonSaveStore
    {
        private const int MaxFileBytes = 16777273;
        private readonly string root;
        private readonly ISaveCodec codec;
        private readonly EnvelopeAuthenticator authenticator;
        private readonly Action<string, string, string> replace;
        private readonly Dictionary<string, string> observed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string RootPath { get { return root; } }
        public JsonSaveStore(string root, ISaveCodec codec, EnvelopeAuthenticator authenticator)
            : this(root, codec, authenticator, (temp, target, backup) => File.Replace(temp, target, backup)) { }
        // Internal filesystem seam for deterministic atomic-failure regression; production always uses File.Replace.
        internal JsonSaveStore(string root, ISaveCodec codec, EnvelopeAuthenticator authenticator, Action<string, string, string> replace)
        {
            if (string.IsNullOrWhiteSpace(root) || codec == null || authenticator == null || replace == null) throw new ArgumentException("Store root, codec and authenticator are required.");
            this.root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (this.root.Length < 4) throw new ArgumentException("A non-root directory is required.");
            this.codec = codec; this.authenticator = authenticator; this.replace = replace;
        }
        public bool IsWriteBlocked(string slot) { return blocked.Contains(slot ?? string.Empty); }
        public void BlockSlot(string slot) { blocked.Add(slot ?? string.Empty); }
        public bool TryRead(string slot, out SaveEnvelope envelope, out string diagnostic)
        {
            envelope = null; diagnostic = null; string path;
            if (!TryPath(slot, out path)) { diagnostic = "Invalid save slot or root path."; return false; }
            try
            {
                if (!File.Exists(path))
                {
                    if (Directory.Exists(path)) throw new IOException("Slot is a directory.");
                    observed[slot] = string.Empty; return false; // Normal first-run defaults, no warning.
                }
                var bytes = ReadBounded(path); string json;
                if (!codec.TryDecode(bytes, out json, out diagnostic)) throw new FormatException("Save codec rejected data.");
                var parsed = SaveEnvelope.Parse(json);
                if (parsed.FormatVersion != SaveEnvelope.CurrentFormatVersion || parsed.SchemaVersion != SaveEnvelope.CurrentSchemaVersion || !authenticator.Verify(parsed))
                    throw new FormatException("Save version or authentication mismatch.");
                observed[slot] = Digest(bytes); envelope = parsed; diagnostic = null; return true;
            }
            catch (Exception error) when (ExpectedFailure(error))
            { blocked.Add(slot); diagnostic = "Stored save is invalid or unavailable; automatic writes to this slot are blocked."; return false; }
        }
        public bool Write(string slot, SaveEnvelope envelope, out string diagnostic)
        {
            diagnostic = null; string path;
            if (!TryPath(slot, out path)) { diagnostic = "Invalid save slot or root path."; return false; }
            if (blocked.Contains(slot)) { diagnostic = "Save slot is blocked; original data is preserved."; return false; }
            string temp = null; byte[] encoded = null;
            try
            {
                if (envelope == null || envelope.FormatVersion != SaveEnvelope.CurrentFormatVersion || envelope.SchemaVersion != SaveEnvelope.CurrentSchemaVersion)
                    throw new FormatException("Cannot write an unsupported save version.");
                if (!observed.ContainsKey(slot))
                { SaveEnvelope ignored; TryRead(slot, out ignored, out diagnostic); if (diagnostic != null || blocked.Contains(slot)) return false; }
                if (!StillObserved(slot, path)) throw new IOException("Save changed since it was read.");
                var signed = authenticator.Sign(envelope); var json = signed.ToJson();
                var reparsed = SaveEnvelope.Parse(json);
                if (!authenticator.Verify(reparsed) || reparsed.AuthenticationJson() != signed.AuthenticationJson())
                    throw new FormatException("Save cannot be authenticated after serialization.");
                // Verify our exact JSON roundtrip before creating a directory or stage file.
                encoded = codec.Encode(json);
                if (encoded.Length > MaxFileBytes) throw new FormatException("Save is too large.");
                if (observed[slot] == Digest(encoded)) { diagnostic = null; return true; }
                Directory.CreateDirectory(root); CheckAncestors(path);
                temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(encoded, 0, encoded.Length); stream.Flush(true); }
                CheckAncestors(path);
                if (!StillObserved(slot, path)) throw new IOException("Save changed before replacement.");
                if (observed[slot].Length == 0) File.Move(temp, path);
                else replace(temp, path, path + ".backup-" + Guid.NewGuid().ToString("N"));
                temp = null; observed[slot] = Digest(encoded); diagnostic = null; return true;
            }
            catch (Exception error) when (ExpectedFailure(error))
            { blocked.Add(slot); diagnostic = "Save write failed; existing original or atomic backup is preserved and further writes are blocked."; return false; }
            finally
            {
                // Only remove our unique, unchanged temporary file. Never delete backups or the destination.
                if (temp != null && encoded != null)
                    try { CheckAncestors(temp); if (File.Exists(temp) && Digest(ReadBounded(temp)) == Digest(encoded)) File.Delete(temp); }
                    catch (Exception error) when (ExpectedFailure(error)) { }
            }
        }
        private bool StillObserved(string slot, string path)
        {
            var expected = observed[slot];
            return expected.Length == 0 ? !File.Exists(path) && !Directory.Exists(path) : File.Exists(path) && Digest(ReadBounded(path)) == expected;
        }
        private bool TryPath(string slot, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(slot) || slot.Length > 64) return false;
            foreach (var c in slot) if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_' && c != '-') return false;
            var device = slot.ToUpperInvariant();
            if (device == "CON" || device == "PRN" || device == "AUX" || device == "NUL" || (device.Length == 4 && (device.StartsWith("COM", StringComparison.Ordinal) || device.StartsWith("LPT", StringComparison.Ordinal)) && device[3] >= '1' && device[3] <= '9')) return false;
            try
            {
                path = Path.GetFullPath(Path.Combine(root, slot + ".json"));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
                CheckAncestors(path); return true;
            }
            catch (Exception error) when (ExpectedFailure(error)) { path = null; return false; }
        }
        private static void CheckAncestors(string path)
        {
            for (var probe = path; !string.IsNullOrEmpty(probe); probe = Path.GetDirectoryName(probe))
                if ((File.Exists(probe) || Directory.Exists(probe)) && (File.GetAttributes(probe) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse save paths are not permitted.");
        }
        private static byte[] ReadBounded(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 0 || stream.Length > MaxFileBytes) throw new FormatException("Save length is invalid.");
                var bytes = new byte[(int)stream.Length]; var offset = 0;
                while (offset < bytes.Length) { var read = stream.Read(bytes, offset, bytes.Length - offset); if (read == 0) throw new IOException("Truncated save read."); offset += read; }
                if (stream.ReadByte() != -1) throw new IOException("Save grew during read."); return bytes;
            }
        }
        private static string Digest(byte[] bytes) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(bytes)); }
        private static bool ExpectedFailure(Exception error)
        { return error is IOException || error is NotSupportedException || error is UnauthorizedAccessException || error is FormatException || error is JsonException || error is CryptographicException || error is ArgumentException || error is OverflowException || error is System.Security.SecurityException; }
    }
}
