using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EatWhat.DevTools.Workflow
{
    /// <summary>Evidence-only scalar JSON for Unity value types; no Editor or scene dependencies.</summary>
    public static class UnityEvidenceJson
    {
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.None, Settings());
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings());
        }

        public static object Deserialize(string json, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            return JsonConvert.DeserializeObject(json, type, Settings());
        }

        private static JsonSerializerSettings Settings()
        {
            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                FloatParseHandling = FloatParseHandling.Double,
                FloatFormatHandling = FloatFormatHandling.String,
                TypeNameHandling = TypeNameHandling.None
            };
            settings.Converters.Add(new UnityScalars());
            return settings;
        }

        private sealed class UnityScalars : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
                return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                    type == typeof(Quaternion) || type == typeof(Rect) || type == typeof(Color) || type == typeof(Bounds);
            }

            private static void Scalar(JsonWriter writer, string name, float value)
            {
                writer.WritePropertyName(name);
                writer.WriteValue(value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null) { writer.WriteNull(); return; }
                writer.WriteStartObject();
                if (value is Vector2)
                {
                    var v = (Vector2)value;
                    Scalar(writer, "x", v.x); Scalar(writer, "y", v.y);
                }
                else if (value is Vector3)
                {
                    var v = (Vector3)value;
                    Scalar(writer, "x", v.x); Scalar(writer, "y", v.y); Scalar(writer, "z", v.z);
                }
                else if (value is Vector4)
                {
                    var v = (Vector4)value;
                    Scalar(writer, "x", v.x); Scalar(writer, "y", v.y); Scalar(writer, "z", v.z); Scalar(writer, "w", v.w);
                }
                else if (value is Quaternion)
                {
                    var v = (Quaternion)value;
                    Scalar(writer, "x", v.x); Scalar(writer, "y", v.y); Scalar(writer, "z", v.z); Scalar(writer, "w", v.w);
                }
                else if (value is Rect)
                {
                    var v = (Rect)value;
                    Scalar(writer, "x", v.x); Scalar(writer, "y", v.y); Scalar(writer, "width", v.width); Scalar(writer, "height", v.height);
                }
                else if (value is Color)
                {
                    var v = (Color)value;
                    Scalar(writer, "r", v.r); Scalar(writer, "g", v.g); Scalar(writer, "b", v.b); Scalar(writer, "a", v.a);
                }
                else if (value is Bounds)
                {
                    var v = (Bounds)value;
                    // Preserve stored extents, avoiding size*0.5 overflow/subnormal rounding.
                    writer.WritePropertyName("center"); serializer.Serialize(writer, v.center);
                    writer.WritePropertyName("extents"); serializer.Serialize(writer, v.extents);
                }
                else throw new JsonSerializationException("Unsupported Unity evidence value.");
                writer.WriteEndObject();
            }

            private static void Fields(JObject node, params string[] names)
            {
                if (node.Properties().Count() != names.Length || names.Any(name => node.Property(name, StringComparison.Ordinal) == null))
                    throw new JsonSerializationException("Unity evidence fields must be exactly: " + string.Join(",", names));
            }

            private static float Number(JToken token)
            {
                if (token == null) throw new JsonSerializationException("Missing Unity evidence number.");
                if (token.Type == JTokenType.String)
                {
                    switch ((string)token)
                    {
                        case "NaN": return float.NaN;
                        case "Infinity": return float.PositiveInfinity;
                        case "-Infinity": return float.NegativeInfinity;
                        default: throw new JsonSerializationException("Unexpected Unity evidence numeric string.");
                    }
                }
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    throw new JsonSerializationException("Unity evidence field must be a number.");
                var number = token.Value<double>();
                var single = (float)number;
                if (!double.IsNaN(number) && !double.IsInfinity(number) && float.IsInfinity(single))
                    throw new JsonSerializationException("Unity evidence number exceeds Single range.");
                return single;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var nullable = Nullable.GetUnderlyingType(objectType);
                var type = nullable ?? objectType;
                if (reader.TokenType == JsonToken.Null)
                {
                    if (nullable != null) return null;
                    throw new JsonSerializationException("Null cannot represent " + type.Name);
                }
                var node = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (type == typeof(Vector2))
                {
                    Fields(node, "x", "y");
                    return new Vector2(Number(node["x"]), Number(node["y"]));
                }
                if (type == typeof(Vector3))
                {
                    Fields(node, "x", "y", "z");
                    return new Vector3(Number(node["x"]), Number(node["y"]), Number(node["z"]));
                }
                if (type == typeof(Vector4))
                {
                    Fields(node, "x", "y", "z", "w");
                    return new Vector4(Number(node["x"]), Number(node["y"]), Number(node["z"]), Number(node["w"]));
                }
                if (type == typeof(Quaternion))
                {
                    Fields(node, "x", "y", "z", "w");
                    return new Quaternion(Number(node["x"]), Number(node["y"]), Number(node["z"]), Number(node["w"]));
                }
                if (type == typeof(Rect))
                {
                    Fields(node, "x", "y", "width", "height");
                    return new Rect(Number(node["x"]), Number(node["y"]), Number(node["width"]), Number(node["height"]));
                }
                if (type == typeof(Color))
                {
                    Fields(node, "r", "g", "b", "a");
                    return new Color(Number(node["r"]), Number(node["g"]), Number(node["b"]), Number(node["a"]));
                }
                if (type == typeof(Bounds))
                {
                    Fields(node, "center", "extents");
                    var bounds = new Bounds();
                    bounds.center = node["center"].ToObject<Vector3>(serializer);
                    bounds.extents = node["extents"].ToObject<Vector3>(serializer);
                    return bounds;
                }
                throw new JsonSerializationException("Unsupported Unity evidence type: " + type.FullName);
            }
        }
    }
}
