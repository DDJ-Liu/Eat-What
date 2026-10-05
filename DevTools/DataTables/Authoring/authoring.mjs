import fs from "node:fs/promises";
import path from "node:path";
import crypto from "node:crypto";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";
import { headerDescriptions } from "./header-descriptions.mjs";

const args = process.argv.slice(2);
const command = args.shift();
const options = {};
for (let i = 0; i < args.length; i += 2) {
  const key = args[i];
  if (!key?.startsWith("--") || i + 1 >= args.length) {
    throw new Error(`Invalid argument near '${key ?? "<end>"}'.`);
  }
  options[key.slice(2)] = args[i + 1];
}

function requireOption(name) {
  const value = options[name];
  if (!value) throw new Error(`Missing --${name}.`);
  return path.resolve(value);
}

async function listFiles(root, predicate) {
  const result = [];
  async function visit(current) {
    const entries = await fs.readdir(current, { withFileTypes: true });
    for (const entry of entries) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) await visit(full);
      else if (predicate(full)) result.push(full);
    }
  }
  await visit(root);
  return result.sort((a, b) => a.localeCompare(b, "en"));
}

function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = "";
  let quoted = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quoted) {
      if (ch === '"') {
        if (text[i + 1] === '"') {
          cell += '"';
          i++;
        } else {
          quoted = false;
        }
      } else {
        cell += ch;
      }
      continue;
    }
    if (ch === '"' && cell.length === 0) quoted = true;
    else if (ch === ',') {
      row.push(cell);
      cell = "";
    } else if (ch === '\r' || ch === '\n') {
      if (ch === '\r' && text[i + 1] === '\n') i++;
      row.push(cell);
      rows.push(row);
      row = [];
      cell = "";
    } else {
      cell += ch;
    }
  }
  if (cell.length > 0 || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }
  return rows;
}

function csvCell(value) {
  const source = value == null ? "" : value instanceof Date ? value.toISOString() : String(value);
  if (!/[",\r\n]/.test(source)) return source;
  return `"${source.replaceAll('"', '""')}"`;
}

function serializeCsv(rows) {
  return rows.map((row) => row.map(csvCell).join(",")).join("\r\n") + "\r\n";
}

function columnName(index) {
  let name = "";
  for (let value = index + 1; value > 0; value = Math.floor((value - 1) / 26)) {
    name = String.fromCharCode(65 + ((value - 1) % 26)) + name;
  }
  return name;
}

function quoteSheet(name) {
  return `'${name.replaceAll("'", "''")}'`;
}

function toArrayBuffer(buffer) {
  return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
}

async function loadSchemas(dataRoot) {
  const schemaRoot = path.join(dataRoot, "Schemas");
  const files = await listFiles(schemaRoot, (file) => file.endsWith(".schema.json"));
  const schemas = [];
  for (const file of files) {
    const schema = JSON.parse(await fs.readFile(file, "utf8"));
    schema.__path = file;
    schema.__relativePath = path.relative(schemaRoot, file).replaceAll(path.sep, "/");
    schemas.push(schema);
  }
  if (schemas.length !== 14) throw new Error(`Expected 14 schemas, found ${schemas.length}.`);
  return schemas;
}

function effectiveReferences(schema, schemas) {
  const references = [...(schema.references ?? [])];
  const localization = schemas.find((candidate) => candidate.tableName === "Localization");
  if (localization && schema.tableName !== "Localization") {
    for (const column of schema.columns) {
      if (column.type === "loc_key" && !references.some((entry) => entry.field === column.name)) {
        references.push({
          field: column.name,
          targetSchema: localization.__relativePath,
          targetField: localization.primaryKeyField,
          isList: false,
        });
      }
    }
  }
  return references;
}

function resolveReference(reference, schemas) {
  const normalized = reference.targetSchema.replace(/^Schemas\//i, "").replaceAll("\\", "/");
  const target = schemas.find((schema) => schema.__relativePath.toLowerCase() === normalized.toLowerCase());
  if (!target) throw new Error(`Unknown reference schema '${reference.targetSchema}'.`);
  const index = target.columns.findIndex((column) => column.name === reference.targetField || column.csvColumn === reference.targetField);
  if (index < 0) throw new Error(`Unknown reference field '${reference.targetField}' in '${target.tableName}'.`);
  return { target, index };
}

function headerComment(schema, column, schemas, sample) {
  const table = headerDescriptions[schema.tableName];
  const purpose = table?.fields?.[column.name];
  if (!purpose) throw new Error("Missing header description: " + schema.tableName + "." + column.name);
  const reference = effectiveReferences(schema, schemas).find((entry) => entry.field === column.name);
  const lines = [
    "[AI189_HEADER_DOC]",
    "用途/填法：" + purpose,
    "消费点：" + table.consumer + "；schema 校验与导表器。",
    "类型：" + column.type + "；" + (column.required ? "必填" : "可留空") +
      (column.defaultValue === undefined ? "；schema 未声明默认值。" : "；schema 默认值：" + column.defaultValue + "（不自动改现有单元格）。"),
    "单位：" + (/单位/.test(purpose) ? "见用途/填法；不要自行换算。" : ["int", "float"].includes(column.type) ? "schema 未明确，按业务确认。" : "不适用（标识、文本或枚举）。"),
    "示例：" + (sample ? String(sample).replaceAll(/\r?\n/g, " ↵ ").slice(0, 140) : "当前表无非空样例；按字段说明及 schema 填写。"),
  ];
  if (column.separator) lines.push("多值分隔符：'" + column.separator + "'；逐项填写 ID/枚举，不把逗号当分隔符。");
  if (reference) {
    const { target } = resolveReference(reference, schemas);
    lines.push("关联：" + target.tableName + "." + reference.targetField + "；须引用已存在的目标 ID。");
    lines.push("提示：无效引用会标红；以共享校验器结果为准。");
  } else if (column.type === "loc_key" && schema.tableName !== "Localization") {
    lines.push("关联：Localization.loc_key；须有对应本地化键。");
  }
  const rules = [];
  if (column.pattern) rules.push("格式 " + column.pattern);
  if (column.enumValues?.length) rules.push("允许值 " + column.enumValues.join(" / "));
  if (column.minItems !== undefined) rules.push("至少 " + column.minItems + " 项");
  if (column.maxItems !== undefined) rules.push("至多 " + column.maxItems + " 项");
  if (column.unique) rules.push("本表唯一");
  if (column.requiredWhenField) rules.push(column.requiredWhenField + "=" + (column.requiredWhenValues ?? []).join("/") + " 时必填");
  if (rules.length) lines.push("约束：" + rules.join("；") + "。");
  if (column.type === "float" && !/单位/.test(purpose)) lines.push("单位：当前 schema 未定义，填写前需业务确认。");
  return lines.join("\n");
}

// This is the exact pre-AI189 tool template, not a prefix heuristic. The
// updater may replace only a whole comment matching one of these strings.
function legacyToolComments(schema, column, schemas) {
  const typeLines = ["Type: " + column.type, column.required ? "Required" : "Optional"];
  if (column.enumValues?.length) typeLines.push("Allowed: " + column.enumValues.join(", "));
  if (column.separator) typeLines.push("List separator: " + column.separator);
  const comments = [typeLines.join("\n")];
  const reference = effectiveReferences(schema, schemas).find((entry) => entry.field === column.name);
  if (reference) {
    const { target } = resolveReference(reference, schemas);
    const isList = Boolean(reference.isList || column.type === "list<ref>");
    const separator = reference.separator || column.separator || ";";
    comments.push("Reference: " + target.tableName + "." + reference.targetField + "\n" +
      (isList ? "Separate multiple IDs with '" + separator + "'." : "Choose an ID from the drop-down.") +
      " Invalid IDs are highlighted red.");
  }
  return comments;
}

async function headerCommentPlan() {
  const dataRoot = requireOption("data-root");
  const schemas = await loadSchemas(dataRoot);
  const tables = [];
  for (const schema of schemas) {
    const csvPath = path.join(dataRoot, ...schema.csvPath.split("/"));
    const rows = parseCsv(await fs.readFile(csvPath, "utf8"));
    const headers = schema.columns.map((column) => column.csvColumn ?? column.name);
    if (JSON.stringify(rows[0] ?? []) !== JSON.stringify(headers)) throw new Error("Schema/CSV header drift: " + schema.csvPath);
    const comments = schema.columns.map((column, index) => {
      const sample = rows.slice(1).map((row) => row[index]).find((value) => value !== "");
      return {
        field: column.name, header: headers[index], address: columnName(index) + "1",
        text: headerComment(schema, column, schemas, sample),
        legacyToolTexts: legacyToolComments(schema, column, schemas),
      };
    });
    const documented = headerDescriptions[schema.tableName]?.fields ?? {};
    const extras = Object.keys(documented).filter((field) => !schema.columns.some((column) => column.name === field));
    if (extras.length) throw new Error("Description contains unknown fields: " + schema.tableName + "." + extras.join(","));
    tables.push({ tableName: schema.tableName, csvPath: schema.csvPath, comments });
  }
  console.log(JSON.stringify({ action: "header-comment-plan", tableCount: tables.length, fieldCount: tables.reduce((sum, table) => sum + table.comments.length, 0), tables }));
}

async function buildWorkbook() {
  const dataRoot = requireOption("data-root");
  const output = requireOption("output");
  const schemas = await loadSchemas(dataRoot);
  const workbook = Workbook.create();
  workbook.comments.setSelf({ displayName: "Eat-What Data Authoring" });

  for (const schema of schemas) workbook.worksheets.add(schema.tableName);

  for (const schema of schemas) {
    const sheet = workbook.worksheets.getItem(schema.tableName);
    const csvPath = path.join(dataRoot, ...schema.csvPath.split("/"));
    const rows = parseCsv(await fs.readFile(csvPath, "utf8"));
    const headers = schema.columns.map((column) => column.csvColumn ?? column.name);
    if (JSON.stringify(rows[0] ?? []) !== JSON.stringify(headers)) {
      throw new Error(`${schema.csvPath} headers do not match its schema.`);
    }
    const matrix = [headers, ...rows.slice(1)];
    sheet.getRangeByIndexes(0, 0, matrix.length, headers.length).values = matrix;
    sheet.freezePanes.freezeRows(1);
    sheet.tabColor = schema.tableName === "Localization" ? "#7E57C2" : "#1F4E78";

    const used = sheet.getRangeByIndexes(0, 0, Math.max(matrix.length, 2), headers.length);
    used.format.font = { name: "Aptos", size: 10, color: "#1F2937" };
    used.format.verticalAlignment = "center";
    used.format.borders = { preset: "all", style: "thin", color: "#D9E1F2" };
    sheet.getRangeByIndexes(0, 0, 1, headers.length).format = {
      fill: "#1F4E78",
      font: { name: "Aptos", size: 10, bold: true, color: "#FFFFFF" },
      wrapText: true,
      verticalAlignment: "center",
    };
    sheet.getRangeByIndexes(0, 0, 1, headers.length).format.rowHeight = 30;

    for (let columnIndex = 0; columnIndex < schema.columns.length; columnIndex++) {
      const column = schema.columns[columnIndex];
      const headerCell = sheet.getCell(0, columnIndex);
      if (column.required) {
        headerCell.format = {
          fill: "#F4B183",
          font: { name: "Aptos", size: 10, bold: true, color: "#442000" },
          wrapText: true,
          verticalAlignment: "center",
        };
      }

      const maxLength = Math.max(
        headers[columnIndex].length,
        ...matrix.slice(1, 101).map((row) => String(row[columnIndex] ?? "").split(/\r?\n/).reduce((max, part) => Math.max(max, part.length), 0)),
      );
      sheet.getRangeByIndexes(0, columnIndex, Math.max(matrix.length, 2), 1).format.columnWidth = Math.min(42, Math.max(12, maxLength + 2));


      const validationRange = sheet.getRangeByIndexes(1, columnIndex, 4999, 1);
      if (column.type === "bool") {
        validationRange.dataValidation = { rule: { type: "list", values: ["true", "false"] } };
      } else if (Array.isArray(column.enumValues) && column.enumValues.length > 0) {
        validationRange.dataValidation = { rule: { type: "list", values: column.enumValues } };
      }
    }

    for (const reference of effectiveReferences(schema, schemas)) {
      const sourceIndex = schema.columns.findIndex((column) => column.name === reference.field || column.csvColumn === reference.field);
      if (sourceIndex < 0) continue;
      const sourceColumn = schema.columns[sourceIndex];
      const { target, index: targetIndex } = resolveReference(reference, schemas);
      const sourceLetter = columnName(sourceIndex);
      const targetLetter = columnName(targetIndex);
      const targetRange = `${quoteSheet(target.tableName)}!$${targetLetter}$2:$${targetLetter}$5000`;
      const validationRange = sheet.getRangeByIndexes(1, sourceIndex, 4999, 1);
      const separator = reference.separator || sourceColumn.separator || ";";
      const isList = Boolean(reference.isList || sourceColumn.type === "list<ref>");

      if (!isList) {
        validationRange.dataValidation = { rule: { type: "list", formula1: targetRange } };
        validationRange.conditionalFormats.addCustom(
          `=AND(${sourceLetter}2<>"",COUNTIF(${targetRange},${sourceLetter}2)=0)`,
          { fill: "#FFC7CE", font: { bold: true, color: "#9C0006" } },
        );
      } else {
        const escapedSeparator = separator.replaceAll('"', '""');
        const invalidFormat = { fill: "#FFC7CE", font: { bold: true, color: "#9C0006" } };
        const targetCsv = parseCsv(await fs.readFile(path.join(dataRoot, ...target.csvPath.split("/")), "utf8"));
        const targetValues = new Set(targetCsv.slice(1).map((row) => String(row[targetIndex] ?? "")).filter(Boolean));
        const invalidExpression = (cellReference) =>
          `IFERROR(SUMPRODUCT(--(COUNTIF(${targetRange},TRIM(TEXTSPLIT(${cellReference},"${escapedSeparator}")))=0))>0,FALSE)`;

        for (let rowIndex = 1; rowIndex < matrix.length; rowIndex++) {
          const cellReference = `${sourceLetter}${rowIndex + 1}`;
          const original = String(matrix[rowIndex][sourceIndex] ?? "");
          const escapedOriginal = original.replaceAll('"', '""');
          const cellRange = sheet.getRangeByIndexes(rowIndex, sourceIndex, 1, 1);
          cellRange.conditionalFormats.addCustom(
            `=AND(${cellReference}<>"${escapedOriginal}",${cellReference}<>"",${invalidExpression(cellReference)})`,
            invalidFormat,
          );
          const tokens = original ? original.split(separator).map((token) => token.trim()).filter(Boolean) : [];
          if (tokens.some((token) => !targetValues.has(token))) cellRange.format = invalidFormat;
        }

        const futureStartIndex = Math.max(1, matrix.length);
        if (futureStartIndex < 5000) {
          const futureReference = `${sourceLetter}${futureStartIndex + 1}`;
          sheet.getRangeByIndexes(futureStartIndex, sourceIndex, 5000 - futureStartIndex, 1).conditionalFormats.addCustom(
            `=AND(${futureReference}<>"",${invalidExpression(futureReference)})`,
            invalidFormat,
          );
        }
      }

    }
    for (let columnIndex = 0; columnIndex < schema.columns.length; columnIndex++) {
      const column = schema.columns[columnIndex];
      const sample = rows.slice(1).map((row) => row[columnIndex]).find((value) => value !== "");
      workbook.comments.addThread(
        { cell: sheet.getCell(0, columnIndex) },
        headerComment(schema, column, schemas, sample),
      );
    }
  }

  await fs.mkdir(path.dirname(output), { recursive: true });
  const blob = await SpreadsheetFile.exportXlsx(workbook);
  await blob.save(output);
  await fs.rm(`${output}.inspect.ndjson`, { force: true });
  console.log(JSON.stringify({ action: "build", output, sheetCount: schemas.length }));
}

function valueText(value) {
  if (value == null) return "";
  if (typeof value === "boolean") return value ? "true" : "false";
  if (value instanceof Date) return value.toISOString();
  return String(value);
}

async function exportWorkbook() {
  const dataRoot = requireOption("data-root");
  const input = requireOption("input");
  const outputRoot = requireOption("output-root");
  const baselineRoot = options["baseline-root"] ? path.resolve(options["baseline-root"]) : dataRoot;
  const schemas = await loadSchemas(dataRoot);
  const inputBytes = await fs.readFile(input);
  const inputSha256 = crypto.createHash("sha256").update(inputBytes).digest("hex").toUpperCase();
  const expectedInputSha256 = options["expected-input-sha"]?.toUpperCase();
  if (expectedInputSha256 && expectedInputSha256 !== inputSha256) {
    throw new Error(`[AUTHORING_EXTERNAL_DRIFT] Input workbook hash mismatch; expected ${expectedInputSha256}, observed ${inputSha256}.`);
  }
  const workbook = await SpreadsheetFile.importXlsx(toArrayBuffer(inputBytes));
  const written = [];
  const skipped = [];
  const pendingWrites = [];
  const provenanceCells = [];

  for (const schema of schemas) {
    const sheet = workbook.worksheets.getItem(schema.tableName);
    const used = sheet.getUsedRange(true);
    const values = used ? used.values : [];
    const formulas = used ? (used.formulas ?? []) : [];
    const headers = schema.columns.map((column) => column.csvColumn ?? column.name);
    const actualHeaders = (values[0] ?? []).slice(0, headers.length).map(valueText);
    if (JSON.stringify(actualHeaders) !== JSON.stringify(headers)) {
      throw new Error(`Sheet '${schema.tableName}' headers do not match its schema.`);
    }

    const dataRows = values.slice(1).map((row) => headers.map((_, index) => valueText(row[index])));
    while (dataRows.length > 0 && dataRows[dataRows.length - 1].every((cell) => cell === "")) dataRows.pop();
    if (dataRows.length === 0 || dataRows.every((row) => row.every((cell) => cell === ""))) {
      skipped.push(schema.csvPath);
      continue;
    }

    const target = path.join(outputRoot, ...schema.csvPath.split("/"));
    pendingWrites.push({ target, contents: serializeCsv([headers, ...dataRows]) });
    written.push(schema.csvPath);

    const primaryKeyIndex = Math.max(0, schema.columns.findIndex((column) => column.name === schema.primaryKeyField || column.csvColumn === schema.primaryKeyField));
    for (let rowIndex = 0; rowIndex < dataRows.length; rowIndex++) {
      for (let columnIndex = 0; columnIndex < headers.length; columnIndex++) {
        const sourceValue = dataRows[rowIndex][columnIndex];
        const rawValue = values[rowIndex + 1]?.[columnIndex];
        const sourceFormula = valueText(formulas[rowIndex + 1]?.[columnIndex]);
        provenanceCells.push({
          sheet: schema.tableName,
          cell: `${columnName(columnIndex)}${rowIndex + 2}`,
          primaryKey: dataRows[rowIndex][primaryKeyIndex] ?? "",
          field: schema.columns[columnIndex].name,
          csvColumn: headers[columnIndex],
          csvPath: schema.csvPath,
          csvRow: rowIndex + 2,
          csvColumnIndex: columnIndex + 1,
          sourceValue,
          sourceValueType: rawValue == null ? "null" : rawValue instanceof Date ? "date" : typeof rawValue,
          sourceFormula: sourceFormula || null,
          csvValue: sourceValue,
          exactTextEqual: true,
        });
      }
    }
  }

  const preWriteSha256 = crypto.createHash("sha256").update(await fs.readFile(input)).digest("hex").toUpperCase();
  if (preWriteSha256 !== inputSha256) {
    throw new Error(`[AUTHORING_EXTERNAL_DRIFT] Input workbook changed before derived CSV write; expected ${inputSha256}, observed ${preWriteSha256}.`);
  }
  for (const pending of pendingWrites) {
    await fs.mkdir(path.dirname(pending.target), { recursive: true });
    await fs.writeFile(pending.target, pending.contents, { encoding: "utf8" });
  }

  const glyphReport = options["glyph-report"]
    ? path.resolve(options["glyph-report"])
    : path.join(outputRoot, `新增字形清单_${new Date().toISOString().slice(0, 10).replaceAll("-", "")}.txt`);
  await writeGlyphReport(baselineRoot, outputRoot, glyphReport);
  const provenanceOutput = options["provenance-output"] ? path.resolve(options["provenance-output"]) : null;
  if (provenanceOutput) {
    await fs.mkdir(path.dirname(provenanceOutput), { recursive: true });
    await fs.writeFile(provenanceOutput, JSON.stringify({
      schemaVersion: 1,
      sourceWorkbook: options["source-workbook"] ? path.resolve(options["source-workbook"]) : input,
      sourceWorkbookSha256: options["source-workbook-sha"]?.toUpperCase() ?? inputSha256,
      formattedCandidateSha256: inputSha256,
      outputRoot,
      cellCount: provenanceCells.length,
      cells: provenanceCells,
    }, null, 2), "utf8");
  }
  console.log(JSON.stringify({ action: "export", input, inputSha256, outputRoot, written, skipped, glyphReport, provenanceOutput, provenanceCells: provenanceCells.length }));
}

function localizationMap(rows) {
  const header = rows[0] ?? [];
  const keyIndex = header.indexOf("loc_key");
  const valueIndex = header.indexOf("zh_cn");
  if (keyIndex < 0 || valueIndex < 0) throw new Error("Localization CSV must expose loc_key and zh_cn.");
  const map = new Map();
  for (const row of rows.slice(1)) {
    const key = row[keyIndex] ?? "";
    if (key) map.set(key, row[valueIndex] ?? "");
  }
  return map;
}

function glyphs(text) {
  return [...text].filter((character) => !/\s/u.test(character));
}

async function writeGlyphReport(baselineRoot, comparisonRoot, output) {
  const baselinePath = path.join(baselineRoot, "Localization", "Localization.csv");
  const requestedComparisonPath = path.join(comparisonRoot, "Localization", "Localization.csv");
  const comparisonPath = await fs.access(requestedComparisonPath).then(() => requestedComparisonPath, () => baselinePath);
  const baseline = localizationMap(parseCsv(await fs.readFile(baselinePath, "utf8")));
  const comparison = localizationMap(parseCsv(await fs.readFile(comparisonPath, "utf8")));
  const baselineGlyphs = new Set([...baseline.values()].flatMap(glyphs));
  const sourceKeys = new Map();
  const changedKeys = [];
  for (const [key, value] of comparison) {
    if ((baseline.get(key) ?? null) !== value) changedKeys.push(key);
    for (const character of glyphs(value)) {
      if (baselineGlyphs.has(character)) continue;
      if (!sourceKeys.has(character)) sourceKeys.set(character, new Set());
      sourceKeys.get(character).add(key);
    }
  }
  const characters = [...sourceKeys.keys()].sort((a, b) => a.codePointAt(0) - b.codePointAt(0));
  const lines = [
    "G53 NEW GLYPH REPORT",
    `Baseline: ${baselinePath}`,
    `Comparison: ${comparisonPath}`,
    `Changed localization keys: ${changedKeys.length}`,
    `New glyph count: ${characters.length}`,
    `New glyphs: ${characters.join("")}`,
    "",
    "Glyph sources:",
  ];
  if (characters.length === 0) lines.push("(none)");
  for (const character of characters) {
    const codePoint = character.codePointAt(0).toString(16).toUpperCase().padStart(4, "0");
    lines.push(`${character}\tU+${codePoint}\t${[...sourceKeys.get(character)].sort().join(",")}`);
  }
  lines.push("", "Changed keys:", ...(changedKeys.length ? changedKeys.sort() : ["(none)"]));
  await fs.mkdir(path.dirname(output), { recursive: true });
  await fs.writeFile(output, lines.join("\r\n") + "\r\n", "utf8");
  return { output, characters, changedKeys };
}

async function glyphReportOnly() {
  const baselineRoot = requireOption("baseline-root");
  const comparisonRoot = requireOption("comparison-root");
  const output = requireOption("output");
  const result = await writeGlyphReport(baselineRoot, comparisonRoot, output);
  console.log(JSON.stringify({ action: "glyph", ...result }));
}

async function inspectWorkbook() {
  const input = requireOption("input");
  const outputDir = requireOption("output-dir");
  const workbook = await SpreadsheetFile.importXlsx(toArrayBuffer(await fs.readFile(input)));
  const sheetSummary = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 10000 });
  const errorScan = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 100 },
    maxChars: 10000,
  });
  await fs.mkdir(outputDir, { recursive: true });
  const sheetNames = [];
  for (let index = 0; index < 14; index++) {
    const sheet = workbook.worksheets.getItemAt(index);
    const name = sheet.name;
    sheetNames.push(name);
    const preview = await workbook.render({ sheetName: name, autoCrop: "all", scale: 1, format: "png" });
    await fs.writeFile(path.join(outputDir, `${String(index + 1).padStart(2, "0")}_${name}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
  await fs.writeFile(path.join(outputDir, "inspection.json"), JSON.stringify({ sheetNames, sheetSummary, errorScan }, null, 2), "utf8");
  console.log(JSON.stringify({ action: "inspect", input, outputDir, sheetCount: sheetNames.length }));
}

async function renderSheet() {
  const input = requireOption("input");
  const output = requireOption("output");
  const sheetName = options.sheet;
  if (!sheetName) throw new Error("Missing --sheet.");
  const workbook = await SpreadsheetFile.importXlsx(toArrayBuffer(await fs.readFile(input)));
  workbook.worksheets.getItem(sheetName);
  const preview = await workbook.render({ sheetName, autoCrop: "all", scale: 0.75, format: "png" });
  await fs.mkdir(path.dirname(output), { recursive: true });
  await fs.writeFile(output, new Uint8Array(await preview.arrayBuffer()));
  console.log(JSON.stringify({ action: "render-sheet", input, output, sheet: sheetName }));
}

async function verifyWorkbook() {
  const input = requireOption("input");
  const workbook = await SpreadsheetFile.importXlsx(toArrayBuffer(await fs.readFile(input)));
  const sheets = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 12000 });
  const formulaErrors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 100 },
    maxChars: 12000,
  });
  const formulaErrorRecords = String(formulaErrors.ndjson ?? "")
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line))
    .filter((record) => record.kind !== "notice");
  console.log(JSON.stringify({
    action: "verify",
    input,
    sheetCount: sheets.recordCount,
    sheets: sheets.ndjson,
    formulaErrorCount: formulaErrorRecords.length,
    formulaErrors: formulaErrorRecords,
  }));
}

async function digestDataTables() {
  const dataRoot = requireOption("data-root");
  const schemas = await loadSchemas(dataRoot);
  const files = {};
  const aggregate = crypto.createHash("sha256");
  for (const schema of schemas) {
    const csvPath = path.join(dataRoot, ...schema.csvPath.split("/"));
    const canonical = serializeCsv(parseCsv(await fs.readFile(csvPath, "utf8")));
    const digest = crypto.createHash("sha256").update(canonical, "utf8").digest("hex").toUpperCase();
    files[schema.csvPath] = digest;
    aggregate.update(schema.csvPath, "utf8");
    aggregate.update(canonical, "utf8");
  }
  console.log(JSON.stringify({
    action: "digest",
    dataRoot,
    aggregate: aggregate.digest("hex").toUpperCase(),
    files,
  }));
}

async function clearSheet() {
  const input = requireOption("input");
  const output = requireOption("output");
  const sheetName = options.sheet;
  if (!sheetName) throw new Error("Missing --sheet.");
  const workbook = await SpreadsheetFile.importXlsx(toArrayBuffer(await fs.readFile(input)));
  const sheet = workbook.worksheets.getItem(sheetName);
  const used = sheet.getUsedRange(true);
  if (used) {
    const values = used.values ?? [];
    const width = Math.max(1, values[0]?.length ?? 1);
    const height = Math.max(0, values.length - 1);
    if (height > 0) sheet.getRangeByIndexes(1, 0, height, width).clear({ applyTo: "contents" });
  }
  await fs.mkdir(path.dirname(output), { recursive: true });
  const blob = await SpreadsheetFile.exportXlsx(workbook);
  await blob.save(output);
  await fs.rm(`${output}.inspect.ndjson`, { force: true });
  console.log(JSON.stringify({ action: "clear-sheet", input, output, sheet: sheetName }));
}

if (command === "build") await buildWorkbook();
else if (command === "header-comment-plan") await headerCommentPlan();
else if (command === "export") await exportWorkbook();
else if (command === "glyph") await glyphReportOnly();
else if (command === "inspect") await inspectWorkbook();
else if (command === "render-sheet") await renderSheet();
else if (command === "verify") await verifyWorkbook();
else if (command === "digest") await digestDataTables();
else if (command === "clear-sheet") await clearSheet();
else throw new Error(`Unknown command '${command ?? ""}'.`);
