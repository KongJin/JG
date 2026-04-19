using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Features.Account.Infrastructure
{
    internal static class FirestoreFieldSerializer
    {
        public static string BuildFieldsJson(object data)
        {
            var members = data.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            bool useProperties = members.Length == 0;

            var builder = new StringBuilder();
            builder.Append("{\"fields\":{");
            bool first = true;

            if (useProperties)
            {
                var properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if (!property.CanRead || property.GetIndexParameters().Length > 0)
                        continue;

                    AppendField(builder, property.Name, property.GetValue(data), ref first);
                }
            }
            else
            {
                for (int i = 0; i < members.Length; i++)
                    AppendField(builder, members[i].Name, members[i].GetValue(data), ref first);
            }

            builder.Append("}}");
            return builder.ToString();
        }

        public static string BuildRawJsonDocument(string rawJson)
        {
            return $"{{\"fields\":{{\"json\":{{\"stringValue\":\"{EscapeJsonString(rawJson)}\"}}}}}}";
        }

        private static void AppendField(StringBuilder builder, string name, object value, ref bool first)
        {
            if (!first)
                builder.Append(',');

            first = false;
            builder.Append('"');
            builder.Append(name);
            builder.Append("\":{");
            builder.Append(GetFirestoreType(value));
            builder.Append(':');
            builder.Append(FormatFirestoreValue(value));
            builder.Append('}');
        }

        private static string GetFirestoreType(object value)
        {
            if (value is string) return "\"stringValue\"";
            if (value is bool) return "\"booleanValue\"";
            if (value is int) return "\"integerValue\"";
            if (value is long) return "\"integerValue\"";
            if (value is float) return "\"doubleValue\"";
            if (value is double) return "\"doubleValue\"";
            return "\"stringValue\"";
        }

        private static string FormatFirestoreValue(object value)
        {
            switch (value)
            {
                case null:
                    return "\"\"";
                case string s:
                    return $"\"{EscapeJsonString(s)}\"";
                case bool b:
                    return b ? "true" : "false";
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString("G9", CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString("G17", CultureInfo.InvariantCulture);
                default:
                    return $"\"{EscapeJsonString(value.ToString())}\"";
            }
        }

        internal static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    internal static class FirestoreFieldReader
    {
        public static string GetString(string json, string fieldName)
        {
            return FindFieldValue(json, fieldName, "stringValue", unescapeString: true);
        }

        public static int GetInt(string json, string fieldName)
        {
            string value = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
        }

        public static long GetLong(string json, string fieldName)
        {
            string value = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : 0L;
        }

        public static float GetFloat(string json, string fieldName)
        {
            string doubleValue = FindFieldValue(json, fieldName, "doubleValue", unescapeString: false);
            if (float.TryParse(doubleValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedDouble))
                return parsedDouble;

            string integerValue = FindFieldValue(json, fieldName, "integerValue", unescapeString: false);
            return float.TryParse(integerValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedInt)
                ? parsedInt
                : 0f;
        }

        public static string FindFieldValue(string json, string fieldName, string firestoreValueType, bool unescapeString)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            string fieldToken = $"\"{fieldName}\"";
            int fieldIndex = json.IndexOf(fieldToken, StringComparison.Ordinal);
            if (fieldIndex < 0)
                return null;

            string typeToken = $"\"{firestoreValueType}\"";
            int typeIndex = json.IndexOf(typeToken, fieldIndex, StringComparison.Ordinal);
            if (typeIndex < 0)
                return null;

            int colonIndex = json.IndexOf(':', typeIndex + typeToken.Length);
            if (colonIndex < 0)
                return null;

            int valueStart = SkipWhitespace(json, colonIndex + 1);
            if (valueStart >= json.Length)
                return null;

            if (json[valueStart] == '"')
            {
                string rawString = ReadQuotedValue(json, valueStart, out _);
                return unescapeString ? UnescapeJsonString(rawString) : rawString;
            }

            int valueEnd = valueStart;
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}' && !char.IsWhiteSpace(json[valueEnd]))
                valueEnd++;

            return json.Substring(valueStart, valueEnd - valueStart);
        }

        private static int SkipWhitespace(string text, int startIndex)
        {
            int index = startIndex;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
            return index;
        }

        private static string ReadQuotedValue(string text, int quoteIndex, out int nextIndex)
        {
            var builder = new StringBuilder();
            bool escaping = false;

            for (int i = quoteIndex + 1; i < text.Length; i++)
            {
                char c = text[i];
                if (escaping)
                {
                    builder.Append('\\');
                    builder.Append(c);
                    escaping = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (c == '"')
                {
                    nextIndex = i + 1;
                    return builder.ToString();
                }

                builder.Append(c);
            }

            nextIndex = text.Length;
            return builder.ToString();
        }

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\\", "\\");
        }
    }
}
