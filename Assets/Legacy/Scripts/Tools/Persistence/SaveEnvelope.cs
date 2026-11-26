using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace EatWhat.Tools.Persistence
{
    public sealed class SaveEnvelope
    {
        public const int CurrentFormatVersion = 1;
        public const string CurrentSchemaVersion = "2.4.1";
        public int FormatVersion { get; }
        public string SchemaVersion { get; }
        public string CreatedUtc { get; }
        public string Hmac { get; }
        private readonly JObject payload;
        public JObject Payload { get { return (JObject)payload.DeepClone(); } }
        public SaveEnvelope(int formatVersion, string schemaVersion, string createdUtc, JObject payload, string hmac)
        {
            DateTimeOffset stamp;
            if (string.IsNullOrEmpty(schemaVersion) || payload == null || string.IsNullOrEmpty(createdUtc) ||
                !createdUtc.EndsWith("Z", StringComparison.Ordinal) || !DateTimeOffset.TryParse(createdUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out stamp) || stamp.Offset != TimeSpan.Zero)
                throw new FormatException("Invalid save envelope.");
            FormatVersion = formatVersion; SchemaVersion = schemaVersion; CreatedUtc = createdUtc;
            this.payload = (JObject)Canonicalize(payload, 0); Hmac = hmac ?? string.Empty;
        }
        public static SaveEnvelope Create(JObject sections, DateTime createdUtc)
        {
            if (createdUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC creation time is required.");
            return new SaveEnvelope(CurrentFormatVersion, CurrentSchemaVersion, createdUtc.ToString("o", CultureInfo.InvariantCulture), sections, string.Empty);
        }
        public SaveEnvelope WithHmac(string hmac) { return new SaveEnvelope(FormatVersion, SchemaVersion, CreatedUtc, payload, hmac); }
        public string ToJson() { return Serialize(Root(true)); }
        internal string AuthenticationJson() { return Serialize(Root(false)); }
        private JObject Root(bool includeHmac)
        {
            var root = new JObject { ["format_version"] = FormatVersion, ["schema_version"] = SchemaVersion, ["created_utc"] = CreatedUtc, ["payload"] = payload.DeepClone() };
            if (includeHmac) root["hmac"] = Hmac; return root;
        }
        public static SaveEnvelope Parse(string json)
        {
            using (var input = new StringReader(json))
            using (var reader = new ExactNumberReader(input, json))
            {
                var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                while (reader.Read()) if (reader.TokenType != JsonToken.Comment) throw new FormatException("Trailing save content.");
                if (root.Count != 5 || root["format_version"] == null || root["format_version"].Type != JTokenType.Integer ||
                    root["schema_version"] == null || root["schema_version"].Type != JTokenType.String || root["created_utc"] == null || root["created_utc"].Type != JTokenType.String ||
                    !(root["payload"] is JObject) || root["hmac"] == null || root["hmac"].Type != JTokenType.String)
                    throw new FormatException("Invalid save envelope fields.");
                return new SaveEnvelope((int)root["format_version"], (string)root["schema_version"], (string)root["created_utc"], (JObject)root["payload"], (string)root["hmac"]);
            }
        }
        /// <summary>Shared exact-number canonical JSON; does not create or change a save envelope.</summary>
        public static string SerializeCanonicalObject(JObject value) { if (value == null) throw new ArgumentNullException("value"); return Serialize(value); }
        public static JObject ParseCanonicalObject(string json)
        {
            using (var input = new StringReader(json)) using (var reader = new ExactNumberReader(input, json))
            {
                var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                while (reader.Read()) if (reader.TokenType != JsonToken.Comment) throw new FormatException("Trailing canonical JSON content.");
                return (JObject)Canonicalize(root, 0);
            }
        }
        private static string Serialize(JObject value)
        {
            using (var text = new StringWriter(CultureInfo.InvariantCulture))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.None, Culture = CultureInfo.InvariantCulture, StringEscapeHandling = StringEscapeHandling.EscapeNonAscii })
            { Canonicalize(value, 0).WriteTo(writer); writer.Flush(); return text.ToString(); }
        }
        // JSON has integer/float syntax, not CLR decimal/double type tags. Prefer decimal
        // only when its serialized value is exact; otherwise require an exact double
        // roundtrip. Never silently round a high precision decimal through Double.
        private static string NumberText(object value)
        {
            if (value is decimal) return JsonConvert.ToString((decimal)value);
            if (value is double) return JsonConvert.ToString((double)value);
            if (value is float) return JsonConvert.ToString((float)value);
            throw new FormatException("Unsupported save number type.");
        }
        private static object ExactFloat(string text)
        {
            var identity = NumberIdentity(text);
            decimal exactDecimal;
            if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out exactDecimal) &&
                NumberIdentity(JsonConvert.ToString(exactDecimal)) == identity) return exactDecimal;
            double exactDouble;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out exactDouble) &&
                !double.IsNaN(exactDouble) && !double.IsInfinity(exactDouble) &&
                NumberIdentity(JsonConvert.ToString(exactDouble)) == identity) return exactDouble;
            throw new FormatException("Save number cannot be represented without precision loss.");
        }
        // Exact base-ten identity of a finite JSON number, without floating arithmetic.
        private static string NumberIdentity(string text)
        {
            var exponentAt = text.IndexOfAny(new[] { 'e', 'E' });
            var mantissa = exponentAt < 0 ? text : text.Substring(0, exponentAt);
            var exponent = exponentAt < 0 ? 0 : int.Parse(text.Substring(exponentAt + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var negative = mantissa.StartsWith("-", StringComparison.Ordinal);
            if (negative) mantissa = mantissa.Substring(1);
            var dot = mantissa.IndexOf('.');
            if (dot >= 0) { exponent = checked(exponent - (mantissa.Length - dot - 1)); mantissa = mantissa.Remove(dot, 1); }
            if (mantissa.Length == 0) throw new FormatException("Non-finite or invalid save number.");
            foreach (var digit in mantissa) if (digit < '0' || digit > '9') throw new FormatException("Non-finite or invalid save number.");
            mantissa = mantissa.TrimStart('0');
            if (mantissa.Length == 0) return "0";
            var trimmed = mantissa.TrimEnd('0'); exponent = checked(exponent + mantissa.Length - trimmed.Length);
            return (negative ? "-" : "") + trimmed + "e" + exponent.ToString(CultureInfo.InvariantCulture);
        }
        // JsonTextReader still owns JSON grammar, duplicate keys, strings and depth.
        // Its line position lets us recover each original float lexeme before a
        // lossy Double token can enter the tree. Integers retain Json.NET BigInteger.
        private sealed class ExactNumberReader : JsonTextReader
        {
            private readonly string source;
            private readonly List<int> lineStarts = new List<int> { 0 };
            internal ExactNumberReader(TextReader input, string source) : base(input)
            {
                this.source = source; DateParseHandling = DateParseHandling.None;
                FloatParseHandling = FloatParseHandling.Double; MaxDepth = 64;
                for (var i = 0; i < source.Length; i++)
                    if (source[i] == '\r' || source[i] == '\n')
                    { if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n') i++; lineStarts.Add(i + 1); }
            }
            public override bool Read()
            {
                var more = base.Read();
                if (more && TokenType == JsonToken.Float)
                {
                    var end = lineStarts[LineNumber - 1] + LinePosition; var start = end;
                    while (start > 0 && IsNumberCharacter(source[start - 1])) start--;
                    SetToken(JsonToken.Float, ExactFloat(source.Substring(start, end - start)), false);
                }
                return more;
            }
            private static bool IsNumberCharacter(char c)
            { return c >= '0' && c <= '9' || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E'; }
        }
        private static JToken Canonicalize(JToken token, int depth)
        {
            if (depth > 64) throw new FormatException("Save payload is too deep.");
            var obj = token as JObject;
            if (obj != null)
            {
                var properties = new List<JProperty>(obj.Properties()); properties.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
                var result = new JObject(); foreach (var property in properties) result.Add(property.Name, Canonicalize(property.Value, depth + 1)); return result;
            }
            var array = token as JArray;
            if (array != null) { var result = new JArray(); foreach (var value in array) result.Add(Canonicalize(value, depth + 1)); return result; }
            if (token.Type != JTokenType.Null && token.Type != JTokenType.String && token.Type != JTokenType.Integer && token.Type != JTokenType.Float && token.Type != JTokenType.Boolean)
                throw new FormatException("Only JSON values are allowed in a save payload.");
            if (token.Type == JTokenType.Float) return new JValue(ExactFloat(NumberText(((JValue)token).Value)));
            return token.DeepClone();
        }
    }
}
