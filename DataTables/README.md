# DataTables CSV contract

> 当前状态（2026-09-17阶段复核）：CK01业务契约为v2.4，JSON schemaVersion仍为2；14表及生成资产已导入并完成本轮验收。下述v1.2/v2.2段落保留为兼容契约历史，不表示当前等待重导。测试历史CSV已迁至DevTools/DataTables/Fixtures/；生产权威源仍为本目录。


`DataTables/` is the human-editable, UTF-8 CSV source of truth for generated
ScriptableObject data. It deliberately lives outside `Assets/`: editing a CSV
does not create, move, or serialize Unity assets.

Each table consists of a CSV under its system directory and a schema under
`DataTables/Schemas/`. A schema declares the source CSV, ScriptableObject type,
generated output directory, primary key, asset-name field, and every CSV to
field mapping. Schema v2.2 additionally supports `id`, `key`, `loc_key`, `ref`,
`list<ref>`, and `sprite`, plus ID regexes, conditional required fields, list
cardinality, numeric minimums, declarative enum values, uniqueness, list
references, non-empty conditional requirements, and explicit table-level
contract exemptions. CSV follows RFC-style quote escaping: commas, quotes (`""`), and
newlines inside quoted values are supported. Formal CK01 list fields use `;` as
their separator; `KitchenAreas.containerTags` and `preset_tools` retain the
legacy `|` separator required by the existing serialized contract.

`assetName` and the primary key are import metadata. They must each be unique;
they do not need to be serialized into the target object. `writeToAsset: false`
is used for metadata-only columns.

Run the editor-external guard before handing a table to Unity:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/DataTables/Validate-DataTables.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/DataTables/Test-Validate-DataTables.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/DataTables/Test-CK01BSchemas.ps1
```

CK01-B currently defines fourteen source-only schemas: Recipes, RecipeSlots,
RecipeSteps, Items, Tags, Tools, CookingGameConfig, Localization,
FridgeCapacityLevels, InitialInventory, ProcessActions, ProcessingRecords,
RecipeVariants, and KitchenAreas. The checked-in rows are the formal CK01 demo
content for clear-broth noodles, tomato-and-egg, and boiled cabbage, plus five
kitchen areas and ten raw-item initial inventory rows.
`Validate-DataTables.ps1 -ReportDirectory <path>` also writes
auditable slot-reference, orphan-localization, and recipe-step-chain JSON
reports. RecipeSlots v2.2 uses `standard_action`, `sub_01` through `sub_08`, and
matching conditionally required `sub_*_count` columns; substitutes must be
contiguous and each populated count must be at least one. B-T2 supplies generated target types, an aggregate `LocTableAsset`,
and runtime `LocService`. The Editor importer searches only the registered
IngredientIcons, DishIcons, TagIcons, ToolIcons, RecipeBook, and
Characters/FridgeCat directories; missing or ambiguous Sprite names
warn and resolve to a stable placeholder created through AssetDatabase only
during B-T3 import. Do not hand-author generated `.asset` files.

The Unity-side entry point is `Tools/Data Tables/Import All`, implemented by
`DataTableImportService.ImportAll()`. It validates every schema and CSV before
creating or changing any asset. Output paths must stay under `Assets/`, source
paths must stay under `DataTables/`, and existing output assets are never
deleted by an import. Re-running semantically identical data leaves existing
assets untouched.

The 2026-09-01 content v1.2 / Schema v2.2 update changes source/schema/code
contracts only. Generated Unity `.asset` files still represent the previously
verified Editor import until the next dedicated Editor task imports and
verifies this authority source.

The first table, `Cooking/KitchenAreas.csv`, generates only to
`Assets/Generated/DataTables/Cooking/KitchenAreas`. It must never overwrite the
hand-authored samples in `Assets/Scripts/DataBase/CookingData/Example`.
