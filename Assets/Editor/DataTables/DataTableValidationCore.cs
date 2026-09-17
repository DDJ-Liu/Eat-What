using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EatWhat.DataTables
{
    /// <summary>
    /// Unity-independent data-table model, RFC-style CSV parser, and validator.
    /// Keep this file free of Unity references so DevTools can compile it with Add-Type.
    /// </summary>
    [Serializable]
    public sealed class DataTableSchema
    {
        public int schemaVersion = 1;
        public string tableName;
        public string csvPath;
        public string targetType;
        public string outputDirectory;
        public string assetNameField;
        public string primaryKeyField;
        public DataTableContractExemptions contractExemptions = new DataTableContractExemptions();
        public List<DataTableColumnSchema> columns = new List<DataTableColumnSchema>();
        public List<DataTableReferenceSchema> references = new List<DataTableReferenceSchema>();
        public List<DataTableRuleSchema> rules = new List<DataTableRuleSchema>();
    }

    [Serializable]
    public sealed class DataTableContractExemptions
    {
        public string primaryKeyPattern;
        public string listSeparator;
    }

    [Serializable]
    public sealed class DataTableColumnSchema
    {
        public string name;
        public string csvColumn;
        public string type;
        public bool required;
        public string defaultValue;
        public string enumType;
        public string separator;
        public bool writeToAsset = true;
        public string pattern;
        public string minimum;
        public int minItems;
        public int maxItems;
        public string requiredWhenField;
        public List<string> requiredWhenValues = new List<string>();
        public bool requiredWhenNonEmpty;
        public bool unique;
        public List<string> enumValues = new List<string>();
    }

    [Serializable]
    public sealed class DataTableReferenceSchema
    {
        public string field;
        public string targetSchema;
        public string targetField;
        public bool isList;
        public string separator;
    }

    /// <summary>
    /// Declarative cross-row contract marker. The editor-external validator owns
    /// the CK01 rule handlers so schemas remain auditable without Unity.
    /// </summary>
    [Serializable]
    public sealed class DataTableRuleSchema
    {
        public string kind;
        public string field;
        public string otherField;
        public string message;
    }

    public sealed class DataTableCsvTable
    {
        public readonly List<string> Headers = new List<string>();
        public readonly List<DataTableCsvRow> Rows = new List<DataTableCsvRow>();

        public int GetHeaderIndex(string header)
        {
            for (int index = 0; index < Headers.Count; index++)
            {
                if (string.Equals(Headers[index], header, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    public sealed class DataTableCsvRow
    {
        public int LineNumber;
        public readonly List<string> Values = new List<string>();
    }

    public sealed class DataTableCsvParseResult
    {
        public readonly DataTableCsvTable Table = new DataTableCsvTable();
        public readonly List<string> Errors = new List<string>();
        public bool IsValid { get { return Errors.Count == 0; } }
    }

    public sealed class DataTableValidationContext
    {
        public readonly Dictionary<string, HashSet<string>> EnumValues =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        public readonly Dictionary<string, HashSet<string>> ReferenceValues =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    }

    public sealed class DataTableValidationResult
    {
        public DataTableCsvTable Table;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public bool IsValid { get { return Errors.Count == 0; } }
    }

    public static class DataTableCsvParser
    {
        public static DataTableCsvParseResult Parse(string text)
        {
            var result = new DataTableCsvParseResult();
            if (text == null)
            {
                result.Errors.Add("CSV text is null.");
                return result;
            }

            var rows = new List<DataTableCsvRow>();
            var currentRow = new DataTableCsvRow { LineNumber = 1 };
            var field = new StringBuilder();
            var inQuotes = false;
            var quoteClosed = false;
            var line = 1;

            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character == '"')
                {
                    if (inQuotes)
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                            quoteClosed = true;
                        }
                    }
                    else if (field.Length == 0 && !quoteClosed)
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        result.Errors.Add("CSV line " + line + " contains an unexpected quote.");
                    }

                    continue;
                }

                if (inQuotes)
                {
                    if (character == '\r')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '\n')
                        {
                            index++;
                        }

                        field.Append('\n');
                        line++;
                    }
                    else
                    {
                        field.Append(character);
                        if (character == '\n')
                        {
                            line++;
                        }
                    }

                    continue;
                }

                if (quoteClosed && character != ',' && character != '\r' && character != '\n')
                {
                    result.Errors.Add("CSV line " + line + " contains text after a closing quote.");
                    quoteClosed = false;
                }

                if (character == ',')
                {
                    currentRow.Values.Add(field.ToString());
                    field.Length = 0;
                    quoteClosed = false;
                    continue;
                }

                if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    currentRow.Values.Add(field.ToString());
                    field.Length = 0;
                    quoteClosed = false;
                    rows.Add(currentRow);
                    line++;
                    currentRow = new DataTableCsvRow { LineNumber = line };
                    continue;
                }

                field.Append(character);
            }

            if (inQuotes)
            {
                result.Errors.Add("CSV line " + line + " has an unterminated quoted field.");
            }

            if (field.Length > 0 || currentRow.Values.Count > 0 || quoteClosed)
            {
                currentRow.Values.Add(field.ToString());
                rows.Add(currentRow);
            }

            if (rows.Count == 0)
            {
                result.Errors.Add("CSV does not contain a header row.");
                return result;
            }

            result.Table.Headers.AddRange(rows[0].Values);
            for (var headerIndex = 0; headerIndex < result.Table.Headers.Count; headerIndex++)
            {
                var header = result.Table.Headers[headerIndex];
                if (string.IsNullOrWhiteSpace(header))
                {
                    result.Errors.Add("CSV header " + (headerIndex + 1) + " is empty.");
                    continue;
                }

                for (var previousIndex = 0; previousIndex < headerIndex; previousIndex++)
                {
                    if (string.Equals(header, result.Table.Headers[previousIndex], StringComparison.Ordinal))
                    {
                        result.Errors.Add("CSV header '" + header + "' is duplicated.");
                        break;
                    }
                }
            }

            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.Values.Count == 1 && row.Values[0].Length == 0)
                {
                    continue;
                }

                if (row.Values.Count != result.Table.Headers.Count)
                {
                    result.Errors.Add(
                        "CSV line " + row.LineNumber + " has " + row.Values.Count +
                        " columns; expected " + result.Table.Headers.Count + ".");
                }

                result.Table.Rows.Add(row);
            }

            return result;
        }
    }

    public static class DataTablePathRules
    {
        public static bool IsSafeRelativePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
            {
                return false;
            }

            var segments = value.Replace('\\', '/').Split('/');
            foreach (var segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSafeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "." || value == "..")
            {
                return false;
            }

            if (value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0)
            {
                return false;
            }

            return value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }

    public static class DataTableValidator
    {
        public static DataTableValidationResult Validate(
            DataTableSchema schema,
            DataTableCsvParseResult parseResult,
            DataTableValidationContext context)
        {
            var result = new DataTableValidationResult();
            if (parseResult != null)
            {
                result.Table = parseResult.Table;
                result.Errors.AddRange(parseResult.Errors);
            }

            if (schema == null)
            {
                result.Errors.Add("Schema is null.");
                return result;
            }

            ValidateSchema(schema, result);
            if (parseResult == null || parseResult.Table == null)
            {
                result.Errors.Add("CSV parse result is null.");
                return result;
            }

            var table = parseResult.Table;
            foreach (var column in schema.columns ?? new List<DataTableColumnSchema>())
            {
                if (column == null || string.IsNullOrWhiteSpace(column.csvColumn))
                {
                    continue;
                }

                if (table.GetHeaderIndex(column.csvColumn) < 0)
                {
                    result.Errors.Add("Schema column '" + column.name + "' requires missing CSV header '" + column.csvColumn + "'.");
                }
            }

            var primaryKeys = new HashSet<string>(StringComparer.Ordinal);
            var assetNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in table.Rows)
            {
                ValidateRow(schema, row, table, context, result);
                ValidateUniqueValue(schema, row, table, schema.primaryKeyField, "primary key", primaryKeys, false, result);
                ValidateUniqueValue(schema, row, table, schema.assetNameField, "asset name", assetNames, true, result);
            }

            foreach (var column in schema.columns ?? new List<DataTableColumnSchema>())
            {
                if (column == null || !column.unique ||
                    string.Equals(column.name, schema.primaryKeyField, StringComparison.Ordinal) ||
                    string.Equals(column.name, schema.assetNameField, StringComparison.Ordinal))
                {
                    continue;
                }

                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in table.Rows)
                {
                    ValidateUniqueValue(schema, row, table, column.name, "unique field", seen, false, result);
                }
            }

            ValidateReferences(schema, table, context, result);
            return result;
        }

        public static string GetEffectiveValue(DataTableColumnSchema column, DataTableCsvRow row, DataTableCsvTable table)
        {
            if (column == null || row == null || table == null)
            {
                return string.Empty;
            }

            var index = table.GetHeaderIndex(column.csvColumn);
            var rawValue = index >= 0 && index < row.Values.Count ? row.Values[index] : string.Empty;
            return string.IsNullOrEmpty(rawValue) ? (column.defaultValue ?? string.Empty) : rawValue;
        }

        public static DataTableColumnSchema FindColumn(DataTableSchema schema, string name)
        {
            if (schema == null || schema.columns == null)
            {
                return null;
            }

            foreach (var column in schema.columns)
            {
                if (column != null && string.Equals(column.name, name, StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return null;
        }

        private static void ValidateSchema(DataTableSchema schema, DataTableValidationResult result)
        {
            RequireSchemaValue(schema.tableName, "tableName", result);
            RequireSchemaValue(schema.csvPath, "csvPath", result);
            RequireSchemaValue(schema.targetType, "targetType", result);
            RequireSchemaValue(schema.outputDirectory, "outputDirectory", result);
            RequireSchemaValue(schema.assetNameField, "assetNameField", result);
            RequireSchemaValue(schema.primaryKeyField, "primaryKeyField", result);

            if (!DataTablePathRules.IsSafeRelativePath(schema.csvPath))
            {
                result.Errors.Add("Schema csvPath must be a safe path relative to DataTables: '" + schema.csvPath + "'.");
            }

            if (string.IsNullOrEmpty(schema.outputDirectory) ||
                !schema.outputDirectory.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal) ||
                !DataTablePathRules.IsSafeRelativePath(schema.outputDirectory))
            {
                result.Errors.Add("Schema outputDirectory must be a safe path below Assets/: '" + schema.outputDirectory + "'.");
            }

            if (schema.columns == null || schema.columns.Count == 0)
            {
                result.Errors.Add("Schema has no columns.");
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in schema.columns)
            {
                if (column == null)
                {
                    result.Errors.Add("Schema contains a null column.");
                    continue;
                }

                RequireSchemaValue(column.name, "column.name", result);
                RequireSchemaValue(column.csvColumn, "column.csvColumn", result);
                RequireSchemaValue(column.type, "column.type", result);
                if (!names.Add(column.name ?? string.Empty))
                {
                    result.Errors.Add("Schema column name '" + column.name + "' is duplicated.");
                }

                if (!IsSupportedType(column.type))
                {
                    result.Errors.Add("Schema column '" + column.name + "' has unsupported type '" + column.type + "'.");
                }

                if (IsEnumType(column.type) && string.IsNullOrWhiteSpace(column.enumType))
                {
                    if (column.enumValues == null || column.enumValues.Count == 0)
                    {
                        result.Errors.Add("Schema enum column '" + column.name + "' is missing enumType or enumValues.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(column.pattern))
                {
                    try { new System.Text.RegularExpressions.Regex(column.pattern); }
                    catch (ArgumentException exception)
                    {
                        result.Errors.Add("Schema column '" + column.name + "' has invalid pattern: " + exception.Message);
                    }
                }
            }

            if (FindColumn(schema, schema.assetNameField) == null)
            {
                result.Errors.Add("Schema assetNameField '" + schema.assetNameField + "' is not a column.");
            }

            if (FindColumn(schema, schema.primaryKeyField) == null)
            {
                result.Errors.Add("Schema primaryKeyField '" + schema.primaryKeyField + "' is not a column.");
            }
        }

        private static void ValidateRow(
            DataTableSchema schema,
            DataTableCsvRow row,
            DataTableCsvTable table,
            DataTableValidationContext context,
            DataTableValidationResult result)
        {
            foreach (var column in schema.columns ?? new List<DataTableColumnSchema>())
            {
                if (column == null)
                {
                    continue;
                }

                var value = GetEffectiveValue(column, row, table);
                var conditionRequiresValue = IsConditionMatched(column, row, table);
                if ((column.required || conditionRequiresValue) && string.IsNullOrWhiteSpace(value))
                {
                    result.Errors.Add(RowPrefix(row) + "required field '" + column.name + "' is empty.");
                    continue;
                }

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                ValidateTypedValue(column, value, row, context, result);
                ValidatePattern(column, value, row, result);
                ValidateListCardinality(column, value, row, result);
            }
        }

        private static void ValidateUniqueValue(
            DataTableSchema schema,
            DataTableCsvRow row,
            DataTableCsvTable table,
            string field,
            string displayName,
            HashSet<string> seen,
            bool requireAssetNameSafety,
            DataTableValidationResult result)
        {
            var column = FindColumn(schema, field);
            if (column == null)
            {
                return;
            }

            var value = GetEffectiveValue(column, row, table);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (requireAssetNameSafety && !DataTablePathRules.IsSafeAssetName(value))
            {
                result.Errors.Add(RowPrefix(row) + "asset name '" + value + "' is not a safe file name.");
            }

            if (!seen.Add(value))
            {
                result.Errors.Add(RowPrefix(row) + "duplicates " + displayName + " '" + value + "'.");
            }
        }

        private static void ValidateReferences(
            DataTableSchema schema,
            DataTableCsvTable table,
            DataTableValidationContext context,
            DataTableValidationResult result)
        {
            foreach (var reference in schema.references ?? new List<DataTableReferenceSchema>())
            {
                if (reference == null)
                {
                    result.Errors.Add("Schema contains a null reference.");
                    continue;
                }

                var field = FindColumn(schema, reference.field);
                if (field == null)
                {
                    result.Errors.Add("Schema reference field '" + reference.field + "' is not a column.");
                    continue;
                }

                if (!DataTablePathRules.IsSafeRelativePath(reference.targetSchema) || string.IsNullOrWhiteSpace(reference.targetField))
                {
                    result.Errors.Add("Schema reference for '" + reference.field + "' has an unsafe target.");
                    continue;
                }

                var key = ReferenceKey(reference.targetSchema, reference.targetField);
                HashSet<string> values;
                if (context == null || !context.ReferenceValues.TryGetValue(key, out values))
                {
                    result.Errors.Add("Schema reference for '" + reference.field + "' targets unavailable field '" + key + "'.");
                    continue;
                }

                foreach (var row in table.Rows)
                {
                    var value = GetEffectiveValue(field, row, table);
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    var separator = !string.IsNullOrEmpty(reference.separator)
                        ? reference.separator
                        : (string.IsNullOrEmpty(field.separator) ? "|" : field.separator);
                    var valuesToCheck = reference.isList || IsListType(field.type)
                        ? value.Split(new[] { separator }, StringSplitOptions.None)
                        : new[] { value };
                    foreach (var rawReference in valuesToCheck)
                    {
                        var referenceValue = rawReference.Trim();
                        if (referenceValue.Length == 0 || values.Contains(referenceValue))
                        {
                            continue;
                        }

                        result.Errors.Add(RowPrefix(row) + "field '" + reference.field + "' reference '" + referenceValue + "' does not exist in '" + key + "'.");
                    }
                }
            }
        }

        private static void ValidateTypedValue(
            DataTableColumnSchema column,
            string value,
            DataTableCsvRow row,
            DataTableValidationContext context,
            DataTableValidationResult result)
        {
            if (string.Equals(column.type, "int", StringComparison.Ordinal))
            {
                int parsed;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' is not an int.");
                }
                else if (!string.IsNullOrWhiteSpace(column.minimum))
                {
                    int minimum;
                    if (!int.TryParse(column.minimum, NumberStyles.Integer, CultureInfo.InvariantCulture, out minimum))
                    {
                        result.Errors.Add("Schema column '" + column.name + "' has invalid int minimum '" + column.minimum + "'.");
                    }
                    else if (parsed < minimum)
                    {
                        result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' must be >= " + minimum + ".");
                    }
                }

                return;
            }

            if (string.Equals(column.type, "float", StringComparison.Ordinal))
            {
                float parsed;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' is not a float.");
                }
                else if (!string.IsNullOrWhiteSpace(column.minimum))
                {
                    float minimum;
                    if (!float.TryParse(column.minimum, NumberStyles.Float, CultureInfo.InvariantCulture, out minimum))
                    {
                        result.Errors.Add("Schema column '" + column.name + "' has invalid float minimum '" + column.minimum + "'.");
                    }
                    else if (parsed < minimum)
                    {
                        result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' must be >= " + minimum.ToString(CultureInfo.InvariantCulture) + ".");
                    }
                }

                return;
            }

            if (string.Equals(column.type, "bool", StringComparison.Ordinal))
            {
                bool ignored;
                if (!TryParseBool(value, out ignored))
                {
                    result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' is not a bool.");
                }

                return;
            }

            if (string.Equals(column.type, "enum", StringComparison.Ordinal))
            {
                ValidateEnumValue(column, value, row, context, result);
                return;
            }

            if (string.Equals(column.type, "list<enum>", StringComparison.Ordinal))
            {
                var separator = string.IsNullOrEmpty(column.separator) ? "|" : column.separator;
                var values = value.Split(new[] { separator }, StringSplitOptions.None);
                foreach (var listValue in values)
                {
                    var trimmed = listValue.Trim();
                    if (trimmed.Length == 0)
                    {
                        result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' contains an empty enum list item.");
                    }
                    else
                    {
                        ValidateEnumValue(column, trimmed, row, context, result);
                    }
                }

                return;
            }

            if (IsListType(column.type))
            {
                var separator = string.IsNullOrEmpty(column.separator) ? "|" : column.separator;
                foreach (var listValue in value.Split(new[] { separator }, StringSplitOptions.None))
                {
                    if (listValue.Trim().Length == 0)
                    {
                        result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' contains an empty list item.");
                    }
                }
            }
        }

        private static void ValidateEnumValue(
            DataTableColumnSchema column,
            string value,
            DataTableCsvRow row,
            DataTableValidationContext context,
            DataTableValidationResult result)
        {
            HashSet<string> enumValues;
            if (column.enumValues != null && column.enumValues.Count > 0)
            {
                enumValues = new HashSet<string>(column.enumValues, StringComparer.Ordinal);
            }
            else if (context == null || string.IsNullOrWhiteSpace(column.enumType) ||
                !context.EnumValues.TryGetValue(column.enumType, out enumValues))
            {
                result.Errors.Add("Enum type '" + column.enumType + "' used by field '" + column.name + "' is unavailable.");
                return;
            }

            if (!enumValues.Contains(value))
            {
                result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' is not a " + column.enumType + " value.");
            }
        }

        private static void RequireSchemaValue(string value, string name, DataTableValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Errors.Add("Schema " + name + " is required.");
            }
        }

        private static bool IsSupportedType(string value)
        {
            return string.Equals(value, "string", StringComparison.Ordinal) ||
                   string.Equals(value, "id", StringComparison.Ordinal) ||
                   string.Equals(value, "loc_key", StringComparison.Ordinal) ||
                   string.Equals(value, "ref", StringComparison.Ordinal) ||
                   string.Equals(value, "list<ref>", StringComparison.Ordinal) ||
                   string.Equals(value, "sprite", StringComparison.Ordinal) ||
                   string.Equals(value, "key", StringComparison.Ordinal) ||
                   string.Equals(value, "int", StringComparison.Ordinal) ||
                   string.Equals(value, "float", StringComparison.Ordinal) ||
                   string.Equals(value, "bool", StringComparison.Ordinal) ||
                   string.Equals(value, "enum", StringComparison.Ordinal) ||
                   string.Equals(value, "list<enum>", StringComparison.Ordinal);
        }

        private static bool IsEnumType(string value)
        {
            return string.Equals(value, "enum", StringComparison.Ordinal) ||
                   string.Equals(value, "list<enum>", StringComparison.Ordinal);
        }

        private static bool IsListType(string value)
        {
            return string.Equals(value, "list<enum>", StringComparison.Ordinal) ||
                   string.Equals(value, "list<ref>", StringComparison.Ordinal);
        }

        private static bool IsConditionMatched(DataTableColumnSchema column, DataTableCsvRow row, DataTableCsvTable table)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.requiredWhenField))
            {
                return false;
            }

            var conditionalColumn = FindColumnByCsvHeader(table, column.requiredWhenField);
            if (conditionalColumn == null)
            {
                return false;
            }

            var index = table.GetHeaderIndex(conditionalColumn);
            var value = index >= 0 && index < row.Values.Count ? row.Values[index] : string.Empty;
            if (column.requiredWhenNonEmpty)
            {
                return !string.IsNullOrWhiteSpace(value);
            }

            return column.requiredWhenValues != null && column.requiredWhenValues.Contains(value);
        }

        private static string FindColumnByCsvHeader(DataTableCsvTable table, string header)
        {
            return table != null && table.GetHeaderIndex(header) >= 0 ? header : null;
        }

        private static void ValidatePattern(DataTableColumnSchema column, string value, DataTableCsvRow row, DataTableValidationResult result)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.pattern) || string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(value, column.pattern))
            {
                result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' value '" + value + "' does not match required pattern.");
            }
        }

        private static void ValidateListCardinality(DataTableColumnSchema column, string value, DataTableCsvRow row, DataTableValidationResult result)
        {
            if (column == null || !IsListType(column.type) || string.IsNullOrEmpty(value))
            {
                return;
            }

            var separator = string.IsNullOrEmpty(column.separator) ? "|" : column.separator;
            var count = value.Split(new[] { separator }, StringSplitOptions.None).Length;
            if (column.minItems > 0 && count < column.minItems)
            {
                result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' requires at least " + column.minItems + " items.");
            }

            if (column.maxItems > 0 && count > column.maxItems)
            {
                result.Errors.Add(RowPrefix(row) + "field '" + column.name + "' allows at most " + column.maxItems + " items.");
            }
        }

        private static bool TryParseBool(string value, out bool parsed)
        {
            if (bool.TryParse(value, out parsed))
            {
                return true;
            }

            if (value == "1")
            {
                parsed = true;
                return true;
            }

            if (value == "0")
            {
                parsed = false;
                return true;
            }

            return false;
        }

        private static string RowPrefix(DataTableCsvRow row)
        {
            return "CSV line " + (row == null ? 0 : row.LineNumber) + ": ";
        }

        public static string ReferenceKey(string schemaPath, string field)
        {
            return (schemaPath ?? string.Empty).Replace('\\', '/') + "|" + (field ?? string.Empty);
        }
    }
}
