using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using EatWhat.DataTables;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Toolkit;
using UnityEngine;
using TMPro;

public static class ShortCycleRecipeBookTests
{
    public static IDictionary<string, string[]> VerifiedStepChains { get; private set; }
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("D-T1a: " + message); checks++; }
    private static void Field(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
          if (field != null) { field.SetValue(target, value); return; } }
        throw new Exception("Missing fixture field " + name);
    }
    private static List<Dictionary<string,string>> Csv(string root, string table)
    {
        var parsed = DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(root, "DataTables/Cooking/" + table + ".csv")));
        Check(parsed.IsValid, table + " parses with the actual production CSV parser");
        var result = new List<Dictionary<string,string>>();
        foreach (var row in parsed.Table.Rows)
        { var values = new Dictionary<string,string>();
          for (var i = 0; i < parsed.Table.Headers.Count; i++) values[parsed.Table.Headers[i]] = row.Values[i];
          result.Add(values); }
        return result;
    }
    public static CK01GeneratedDataTable Table<T>(string root, string name, string primaryKey) where T : ScriptableObject, new()
    {
        var result = new List<CK01GeneratedDataRowReference>();
        foreach (var row in Csv(root, name))
        {
            var asset = new T(); asset.name = row[primaryKey];
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                string value; if (!row.TryGetValue(field.Name, out value)) continue;
                if (field.FieldType == typeof(string)) field.SetValue(asset, value);
                else if (field.FieldType == typeof(int)) field.SetValue(asset, string.IsNullOrEmpty(value) ? 0 : int.Parse(value, CultureInfo.InvariantCulture));
                else if (field.FieldType == typeof(float)) field.SetValue(asset, string.IsNullOrEmpty(value) ? 0f : float.Parse(value, CultureInfo.InvariantCulture));
                else if (field.FieldType == typeof(bool)) field.SetValue(asset, value == "true");
                else if (field.FieldType == typeof(List<string>)) field.SetValue(asset, new List<string>(value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)));
                else if (field.FieldType == typeof(Sprite)) field.SetValue(asset, string.IsNullOrEmpty(value) ? null : new Sprite { name = value });
            }
            result.Add(new CK01GeneratedDataRowReference(row[primaryKey], asset));
        }
        return new CK01GeneratedDataTable(name, null, result);
    }
    private static readonly List<string> catalogFeedbackCases = new List<string>();
    public static IReadOnlyList<string> CatalogFeedbackCases { get { return catalogFeedbackCases.AsReadOnly(); } }
    private static void Feedback(bool value, string message)
    {
        Check(value, message);
        catalogFeedbackCases.Add(message);
    }
    public static string RunCatalogFeedback()
    {
        checks = 0;
        catalogFeedbackCases.Clear();
        var card = new CatalogCard();
        var surface = card.gameObject.AddComponent<SpriteRenderer>();
        var icon = new SpriteRenderer();
        var question = new SpriteRenderer { sprite = new Sprite { name = "question" }, enabled = false };
        var oldLocked = new Sprite { name = "legacy-locked" };
        var dish = new Sprite { name = "dish" };
        var tag = new Sprite { name = "tag" };
        var tagIcon = new SpriteRenderer();
        var unusedTagIcon = new SpriteRenderer();
        var nameLabel = new TextMeshPro();
        var badgeLabel = new TextMeshPro();
        var favoriteStar = new SpriteRenderer { sprite = new Sprite { name = "star" }, enabled = false };
        var nameBackground = new SpriteRenderer { enabled = true };
        var tape01 = new SpriteRenderer { enabled = false };
        var tape02 = new SpriteRenderer { enabled = true };
        Field(card, "iconRenderer", icon);
        Field(card, "placeholderRenderer", question);
        Field(card, "lockedRecipeSprite", oldLocked);
        Field(card, "nameLabel", nameLabel);
        Field(card, "badgeLabel", badgeLabel);
        Field(card, "favoriteStar", favoriteStar);
        Field(card, "tagIcons", new[] { tagIcon, unusedTagIcon });
        Field(card, "contentDecorations", new[] { nameBackground, tape01, tape02 });

        var locked = new RecipeView("locked", "recipe.locked", dish, 0, false,
            new[] { new RecipeTagView("tag", "tag.key", tag, null) },
            new RecipeSlotView[0]);
        var unlocked = locked.WithUnlocked(true);
        card.Apply(locked);
        card.SetInteractionAllowed(true);
        Feedback(question.enabled && icon.sprite == null && icon.enabled &&
            card.Display.is_placeholder, "locked uses only dedicated placeholder, not main icon");
        Feedback(ReferenceEquals(card.View, locked) && !card.InteractionAllowed &&
            !card.Select().Succeeded, "locked recipe remains unselectable");
        Feedback(surface.enabled && nameBackground.enabled && !tape01.enabled &&
            tape02.enabled, "populated card preserves each authored decoration state");

        card.Apply(unlocked);
        card.SetInteractionAllowed(true);
        Feedback(ReferenceEquals(icon.sprite, dish) && !question.enabled &&
            !card.Display.is_placeholder && card.InteractionAllowed,
            "unlocked recipe restores dish and interaction without placeholder");
        card.SetFavorite(true);
        Feedback(favoriteStar.enabled && card.FavoriteVisible, "populated favorite remains visible");
        card.Apply(null);
        Feedback(!surface.enabled && !icon.enabled && !question.enabled &&
            card.gameObject.activeSelf && card.gameObject.SetActiveCallCount == 0,
            "empty card hides its surfaces without disabling reusable owner");
        Feedback(!nameBackground.enabled && !tape01.enabled && !tape02.enabled,
            "empty card hides only explicit background and tape renderers");
        Feedback(!tagIcon.enabled && !unusedTagIcon.enabled && !favoriteStar.enabled &&
            !card.FavoriteVisible, "empty card does not retain tag or favorite");
        card.Apply(null);
        card.Apply(unlocked);
        Feedback(nameBackground.enabled && !tape01.enabled && tape02.enabled,
            "rebind restores original mixed decoration states after repeated clear");
        Feedback(ReferenceEquals(icon.sprite, dish) && !question.enabled &&
            tagIcon.enabled && !unusedTagIcon.enabled && !favoriteStar.enabled,
            "rebind restores content tags but never invents favorite or empty tag");
        tape01.enabled = true;
        card.Apply(null);
        card.Apply(unlocked);
        Feedback(nameBackground.enabled && tape01.enabled && tape02.enabled,
            "next clear cycle remembers latest populated decoration state");

        var fallback = new CatalogCard();
        var fallbackIcon = new SpriteRenderer();
        Field(fallback, "iconRenderer", fallbackIcon);
        Field(fallback, "lockedRecipeSprite", oldLocked);
        fallback.Apply(locked);
        Feedback(ReferenceEquals(fallbackIcon.sprite, oldLocked) && !fallback.Display.is_placeholder,
            "legacy missing-placeholder binding keeps exactly one locked icon");

        var recipe = new CK01RecipeData { name_key = "legacy.key", name = "legacy" };
        var variant = new CK01RecipeVariantData { display_sprite = dish };
        card.ConfigureRecipe(recipe, variant, false, false);
        Feedback(icon.sprite == null && question.enabled && card.Display.is_placeholder,
            "legacy ConfigureRecipe locked state uses single placeholder");
        card.ConfigureRecipe(recipe, variant, true, true);
        Feedback(ReferenceEquals(icon.sprite, dish) && !question.enabled &&
            badgeLabel.text == "★", "legacy ConfigureRecipe unlocked dish and badge remain");
        card.ConfigureRecipe(null, null, false, true);
        Feedback(!surface.enabled && !question.enabled && !nameBackground.enabled &&
            !tape01.enabled && !tape02.enabled && !tagIcon.enabled && !favoriteStar.enabled,
            "legacy empty call clears content and decoration without stale tag/star");
        return "PASS: Catalog feedback " + checks + " assertions";
    }
    private static readonly List<string> stepIconCases = new List<string>();
    public static IReadOnlyList<string> StepIconCases { get { return stepIconCases.AsReadOnly(); } }
    private static void StepIconCheck(bool value, string message)
    {
        Check(value, message);
        stepIconCases.Add(message);
    }
    public static string RunStepIconFeedback()
    {
        checks = 0;
        stepIconCases.Clear();
        var row = new StepDetailRowPiece();
        var surface = row.gameObject.AddComponent<SpriteRenderer>();
        var description = new TextMeshPro();
        var ingredients = new[] { new SpriteRenderer(), new SpriteRenderer(), new SpriteRenderer(), new SpriteRenderer() };
        var tools = new[] { new SpriteRenderer(), new SpriteRenderer(), new SpriteRenderer() };
        var generatedMissing = new Sprite { name = "CK01-MissingSprite", rect = new Rect(0f, 0f, 1f, 1f), pixelsPerUnit = 100f };
        var replacement = new Sprite { name = "existing-question-mark", rect = new Rect(0f, 0f, 86f, 100f), pixelsPerUnit = 100f };
        var real = new Sprite { name = "real-art", rect = new Rect(0f, 0f, 512f, 512f), pixelsPerUnit = 100f };
        var aigc = new Sprite { name = "concept_AIGC", rect = new Rect(0f, 0f, 200f, 100f), pixelsPerUnit = 50f };
        tools[0].transform.localScale = new Vector3(60f, 60f, 3f);
        tools[0].transform.localPosition = new Vector3(7f, 8f, 9f);
        tools[0].transform.localEulerAngles = new Vector3(0f, 0f, 23f);
        Field(row, "description", description);
        Field(row, "ingredientIcons", ingredients);
        Field(row, "toolIcons", tools);
        Field(row, "generatedMissingSprite", generatedMissing);
        Field(row, "missingIconReplacement", replacement);
        var mixed = new RecipeStepView("step", "step.key", new[] {
            new RecipeIconView("real", "real.key", real),
            new RecipeIconView("generated", "generated.key", generatedMissing),
            new RecipeIconView("aigc", "aigc.key", aigc)
        }, new[] {
            new RecipeIconView("null", "null.key", null),
            new RecipeIconView("generated-tool", "generated.key", generatedMissing)
        });
        StepIconCheck(row.CanBind(mixed) && row.HasMissingSpriteBindings,
            "existing CanBind accepts both icon groups with exact source and distinct replacement");
        row.Apply(mixed);
        StepIconCheck(surface.enabled && ReferenceEquals(row.View, mixed) &&
            ReferenceEquals(ingredients[0].sprite, real) && ingredients[0].enabled &&
            row.IngredientIconStates[0] == ToolkitPlaceholderState.Ready,
            "ready ingredient keeps real source art and explicit Ready state");
        StepIconCheck(ReferenceEquals(ingredients[1].sprite, replacement) && ingredients[1].enabled &&
            row.IngredientIconStates[1] == ToolkitPlaceholderState.Missing,
            "exact generated MissingSprite ingredient uses distinct nonmagenta replacement");
        StepIconCheck(ReferenceEquals(ingredients[2].sprite, aigc) && ingredients[2].enabled &&
            row.IngredientIconStates[2] == ToolkitPlaceholderState.Aigc,
            "AIGC ingredient preserves source art and explicit Aigc state");
        StepIconCheck(ReferenceEquals(tools[0].sprite, replacement) && tools[0].enabled &&
            row.ToolIconStates[0] == ToolkitPlaceholderState.Missing &&
            ReferenceEquals(tools[1].sprite, replacement) && tools[1].enabled &&
            row.ToolIconStates[1] == ToolkitPlaceholderState.Missing,
            "null and generated MissingSprite tools both use explicit Missing replacement");
        StepIconCheck(!ingredients[3].enabled && ingredients[3].sprite == null &&
            !row.IngredientIconStates[3].HasValue && !tools[2].enabled && tools[2].sprite == null &&
            !row.ToolIconStates[2].HasValue && row.IconConfigurationDiagnostic == null,
            "unused icon slots stay empty and fully configured row has no diagnostic");

        var ready = new RecipeStepView("ready", "ready.key", new[] {
            new RecipeIconView("ready", "ready.key", real)
        }, new[] { new RecipeIconView("aigc", "aigc.key", aigc) });
        row.Apply(ready);
        StepIconCheck(ReferenceEquals(ingredients[0].sprite, real) &&
            row.IngredientIconStates[0] == ToolkitPlaceholderState.Ready &&
            !ingredients[1].enabled && ingredients[1].sprite == null &&
            ReferenceEquals(tools[0].sprite, aigc) &&
            row.ToolIconStates[0] == ToolkitPlaceholderState.Aigc &&
            !tools[1].enabled && tools[1].sprite == null,
            "Missing to Ready or Aigc rebind clears former replacement and surplus slots");
        row.Apply(null);
        StepIconCheck(row.View == null && !surface.enabled &&
            !ingredients[0].enabled && ingredients[0].sprite == null &&
            !tools[0].enabled && tools[0].sprite == null &&
            !row.IngredientIconStates[0].HasValue && !row.ToolIconStates[0].HasValue &&
            row.IconConfigurationDiagnostic == null,
            "empty view clears both groups, state and diagnostic without retaining art");
        row.Apply(mixed);
        StepIconCheck(ReferenceEquals(ingredients[1].sprite, replacement) &&
            row.IngredientIconStates[1] == ToolkitPlaceholderState.Missing &&
            ReferenceEquals(tools[1].sprite, replacement) &&
            row.ToolIconStates[1] == ToolkitPlaceholderState.Missing,
            "rebind after empty restores only current Missing icons in both groups");

        Field(row, "missingIconReplacement", null);
        row.Apply(mixed);
        StepIconCheck(!row.HasMissingSpriteBindings &&
            !ingredients[1].enabled && ingredients[1].sprite == null &&
            !tools[0].enabled && tools[0].sprite == null &&
            row.IngredientIconStates[1] == ToolkitPlaceholderState.Missing &&
            row.ToolIconStates[0] == ToolkitPlaceholderState.Missing &&
            row.IconConfigurationDiagnostic != null,
            "missing replacement hides missing icons and exposes binding diagnostic");
        Field(row, "missingIconReplacement", generatedMissing);
        row.Apply(mixed);
        StepIconCheck(!row.HasMissingSpriteBindings && !ingredients[1].enabled &&
            ingredients[1].sprite == null && row.IconConfigurationDiagnostic != null,
            "generated MissingSprite cannot be reused as its own replacement");
        Field(row, "generatedMissingSprite", null);
        Field(row, "missingIconReplacement", replacement);
        row.Apply(mixed);
        StepIconCheck(ReferenceEquals(ingredients[0].sprite, real) &&
            !row.IngredientIconStates[0].HasValue &&
            row.IconConfigurationDiagnostic != null &&
            ReferenceEquals(ingredients[1].sprite, generatedMissing) &&
            !row.IngredientIconStates[1].HasValue,
            "unbound source identity preserves legacy visible art but does not certify Ready");
        StepIconCheck(!row.CanBind(new RecipeStepView("overflow", "key",
            new RecipeIconView[5], new RecipeIconView[0])),
            "existing CanBind capacity guard still rejects oversized ingredient group");

        Field(row, "generatedMissingSprite", generatedMissing);
        row.Apply(mixed);
        var questionScale = tools[0].transform.localScale;
        StepIconCheck(ReferenceEquals(tools[0].sprite, replacement) && tools[0].enabled &&
            Math.Abs(questionScale.x - 0.6f) < 0.00001f && Math.Abs(questionScale.y - 0.6f) < 0.00001f && questionScale.z == 3f &&
            Math.Abs(replacement.rect.width / replacement.pixelsPerUnit * questionScale.x - 0.516f) < 0.00001f &&
            Math.Abs(replacement.rect.height / replacement.pixelsPerUnit * questionScale.y - 0.6f) < 0.00001f &&
            tools[0].transform.localPosition.Equals(new Vector3(7f, 8f, 9f)) && tools[0].transform.localEulerAngles.z == 23f,
            "old scale 60 becomes 0.516x0.6 question rectangle while preserving position, rotation and Z scale");
        StepIconCheck(Math.Abs(ingredients[0].transform.localScale.x - 0.1171875f) < 0.00001f &&
            Math.Abs(ingredients[0].transform.localScale.y - 0.1171875f) < 0.00001f &&
            Math.Abs(ingredients[2].transform.localScale.x - 0.15f) < 0.00001f,
            "Ready 512 PPU100 and rectangular AIGC 200x100 PPU50 use their own sprite dimensions");
        row.Apply(mixed);
        StepIconCheck(Math.Abs(tools[0].transform.localScale.x - 0.6f) < 0.00001f &&
            Math.Abs(ingredients[0].transform.localScale.x - 0.1171875f) < 0.00001f,
            "repeated binding recomputes absolute scale without accumulating scale");
        var invalid = new Sprite { name = "invalid-art", rect = new Rect(0f, 0f, float.NaN, 512f), pixelsPerUnit = 100f };
        row.Apply(new RecipeStepView("invalid", "key", new[] { new RecipeIconView("invalid", "key", invalid) },
            new RecipeIconView[0]));
        StepIconCheck(!ingredients[0].enabled && ingredients[0].sprite == null && row.IconConfigurationDiagnostic != null &&
            !tools[0].enabled && tools[0].sprite == null,
            "invalid source size hides art with a diagnostic and empty slots remain hidden");
        var zeroPpu = new Sprite { name = "zero-ppu", rect = new Rect(0f, 0f, 512f, 512f), pixelsPerUnit = 0f };
        Field(row, "missingIconReplacement", zeroPpu);
        row.Apply(mixed);
        StepIconCheck(!tools[0].enabled && tools[0].sprite == null && row.IconConfigurationDiagnostic != null,
            "zero PPU replacement cannot leave an oversized icon visible");
        var infinitePpu = new Sprite { name = "infinite-ppu", rect = new Rect(0f, 0f, 512f, 512f), pixelsPerUnit = float.PositiveInfinity };
        Field(row, "missingIconReplacement", infinitePpu);
        row.Apply(mixed);
        StepIconCheck(!tools[0].enabled && tools[0].sprite == null && row.IconConfigurationDiagnostic != null,
            "nonfinite PPU cannot produce an infinite icon");
        Field(row, "missingIconReplacement", replacement);
        row.Apply(mixed);
        StepIconCheck(tools[0].enabled && Math.Abs(tools[0].transform.localScale.x - 0.6f) < 0.00001f &&
            row.IconConfigurationDiagnostic == null,
            "valid rebind after invalid art restores bounded Missing icon and clears diagnostics");
        return "PASS: step icon feedback " + checks + " assertions";
    }
    public static string Run(string root)
    {
        checks = 0;
        VerifiedStepChains = new Dictionary<string, string[]>();
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[] {
            Table<CK01RecipeData>(root, "Recipes", "recipe_id"),
            Table<CK01RecipeVariantData>(root, "RecipeVariants", "var_id"),
            Table<CK01RecipeSlotData>(root, "RecipeSlots", "slot_id"),
            Table<CK01ItemData>(root, "Items", "item_id"),
            Table<CK01TagData>(root, "Tags", "tag_id"),
            Table<CK01RecipeStepData>(root, "RecipeSteps", "step_id"),
            Table<CK01ToolData>(root, "Tools", "tool_id") });
        var views = catalog.EnumerateRecipesOrdered();
        var sourceRecipes = Csv(root, "Recipes");
        Check(views.Count == sourceRecipes.Count, "catalog card total equals actual T1 rows including locked rows");
        for (var i = 1; i < views.Count; i++)
            Check(views[i-1].display_order_in_menu <= views[i].display_order_in_menu, "display_order sort");
        Check(views.Count == 3 && views[0].recipe_id == "rcp_qingtang_noodle" && views[1].recipe_id == "rcp_tomato_egg" && views[2].recipe_id == "rcp_kaishui_cabbage", "current three-row order");
        string diagnostic;
        foreach (var view in views)
        {
            CK01RecipeData row; CK01RecipeVariantData variant;
            Check(catalog.TryGetRecipe(view.recipe_id, out row, out diagnostic), diagnostic);
            Check(view.name_key == row.name_key && view.note_tags.Count == row.note_tags.Count && view.slots.Count == row.slots.Count, "T1 fields are not guessed or merged");
            Check(catalog.TryGetRow("RecipeVariants", row.standard_variant_id, out variant, out diagnostic) && ReferenceEquals(view.display_sprite, variant.display_sprite), "T1 -> T13 standard display sprite");
            for (var n = 0; n < view.note_tags.Count; n++)
            { CK01TagData tag;
              Check(catalog.TryGetTag(row.note_tags[n], out tag, out diagnostic) && view.note_tags[n].name_key == tag.name_key && ReferenceEquals(view.note_tags[n].icon_sprite,tag.icon_sprite) && ReferenceEquals(view.note_tags[n].sticker_sprite,tag.sticker_sprite), "ordered T6 tags/keys/sprites"); }
            for (var n = 0; n < view.slots.Count; n++)
            { CK01RecipeSlotData slot; CK01ItemData item;
              Check(catalog.TryGetRecipeSlot(row.slots[n], out slot, out diagnostic), diagnostic);
              Check(catalog.TryGetItem(slot.standard_ingredient, out item, out diagnostic) && view.slots[n].slot_id == row.slots[n] && view.slots[n].standard_ingredient.required_count == slot.required_count && view.slots[n].standard_ingredient.name_key == item.name_key && ReferenceEquals(view.slots[n].standard_ingredient.icon_sprite, item.icon_sprite), "ordered T2 -> T4 icon/count mapping");
              ShortCycleSlotBlockBinding binding;
              Check(ShortCycleRecipeSlotBinding.TryResolve(view.slots[n], id => { CK01ItemData x; string d; return catalog.TryGetItem(id,out x,out d) ? x : null; }, id => { CK01TagData x; string d; return catalog.TryGetTag(id,out x,out d) ? x : null; }, id => 7, id => true, null, out binding, out diagnostic), diagnostic);
              Check(binding.StandardRequiredCount == slot.required_count && binding.Substitutes.Count == view.slots[n].substitutes.Count, "clue-board consumes the same immutable slot requirement"); }
        }
        foreach (var view in views)
        {
            IList<CK01RecipeStepData> chain;
            Check(catalog.TryGetRecipeStepChain(view.recipe_id, out chain, out diagnostic), diagnostic);
            Check(view.steps.Count == chain.Count && view.steps.Count <= 10, "step DTO follows the existing catalog chain");
            var ids = new List<string>();
            CK01RecipeData recipe; catalog.TryGetRecipe(view.recipe_id, out recipe, out diagnostic);
            var next = recipe.first_step_id;
            for (var index = 0; index < chain.Count; index++)
            {
                var step = view.steps[index]; ids.Add(step.step_id);
                Check(step.step_id == next && step.display_text_key == chain[index].display_text_key, "stable chain ID and display_text_key");
                Check(step.standard_ingredients.Count == chain[index].standard_ingredients.Count && step.standard_tools.Count == chain[index].standard_tools.Count, "T3 icon list cardinality");
                for (var k = 0; k < step.standard_ingredients.Count; k++)
                { CK01ItemData item; var id = chain[index].standard_ingredients[k];
                  Check(catalog.TryGetItem(id, out item, out diagnostic) && step.standard_ingredients[k].id == id && ReferenceEquals(step.standard_ingredients[k].icon_sprite,item.icon_sprite), "step ingredient icon uses T4 unique catalog"); }
                for (var k = 0; k < step.standard_tools.Count; k++)
                { CK01ToolData tool; var id = chain[index].standard_tools[k];
                  Check(catalog.TryGetRow("Tools",id,out tool,out diagnostic) && step.standard_tools[k].id == id && ReferenceEquals(step.standard_tools[k].icon_sprite,tool.icon_sprite), "step tool icon uses T7 unique catalog"); }
                next = chain[index].next_step_id;
            }
            VerifiedStepChains[view.recipe_id] = ids.ToArray();
            Check(view.WithUnlocked(!view.is_unlocked).steps.Count == view.steps.Count, "unlock overlays preserve step snapshots");
        }
        var readonlyRejected = false;
        try { ((IList<RecipeView>)views).Add(views[0]); } catch (NotSupportedException) { readonlyRejected = true; }
        Check(readonlyRejected, "recipe enumeration is read-only");
        CK01RecipeData first; catalog.TryGetRecipe(views[0].recipe_id, out first, out diagnostic);
        var oldKey = first.name_key; first.name_key = "fixture.changed";
        RecipeView fresh; Check(catalog.TryGetRecipeView(views[0].recipe_id, out fresh, out diagnostic) && fresh.name_key == "fixture.changed" && views[0].name_key == oldKey, "immutable snapshot; later reads still derive from the unique catalog");
        first.name_key = oldKey;
        var unlock = new UnlockManager(catalog, new string[0]);
        Check(unlock.IsUnlocked(views[0].recipe_id) && unlock.IsUnlocked(views[1].recipe_id) && !unlock.IsUnlocked(views[2].recipe_id) && !unlock.IsUnlocked("missing"), "actual defaults and empty runtime profile");
        Check(new UnlockManager(catalog, new[] { views[2].recipe_id }).IsUnlocked(views[2].recipe_id), "future explicit profile union remains read-only");
        var player = new PlayerUnlockManager();player.Configure(catalog);
        Check(!player.IsRecipeUnlocked(views[2].recipe_id), "existing PlayerUnlockManager no longer unlocks every recipe");
        var cards = new CatalogCard[8]; var surfaces = new SpriteRenderer[8]; for (var i=0;i<cards.Length;i++) { cards[i] = new CatalogCard(); surfaces[i] = cards[i].gameObject.AddComponent<SpriteRenderer>(); }
        var ingredients = new IngredientListPiece();var entries = new IngredientCard[6];for(var i=0;i<entries.Length;i++)entries[i]=new IngredientCard();Field(ingredients,"entries",entries);
        var notes = new StickerNotesPiece();var stickers=new Sticker[5];for(var i=0;i<5;i++)stickers[i]=new Sticker();Field(notes,"notes",stickers);
        var title = new TextMeshPro();var tabTitle=new TextMeshPro();var tab=new CurrentRecipeTabPiece();Field(tab,"title",tabTitle);Field(tab,"favoriteStar",new SpriteRenderer{sprite=new Sprite{name="ui_caipu_favorite_star"}});
        var dish = new SpriteRenderer();var book=new RecipeBookBinder();Field(book,"catalogCards",cards);Field(book,"browseTitle",title);Field(book,"browseDish",dish);Field(book,"ingredientList",ingredients);Field(book,"stickerNotes",notes);Field(book,"currentRecipeTab",tab);
        var detail = new StepDetailModalPiece(); var detailRows = new StepDetailRowPiece[10];
        for (var index=0; index<detailRows.Length; index++)
        {
            detailRows[index] = new StepDetailRowPiece(); Field(detailRows[index],"description",new TextMeshPro());
            var ingredientsIcons = new SpriteRenderer[8];var toolsIcons=new SpriteRenderer[8];
            for(var k=0;k<8;k++){ingredientsIcons[k]=new SpriteRenderer();toolsIcons[k]=new SpriteRenderer();}
            Field(detailRows[index],"ingredientIcons",ingredientsIcons);Field(detailRows[index],"toolIcons",toolsIcons);
        }
        var oldTitle=new TextMeshPro{text="old title"};var oldNumber=new TextMeshPro{text="1"};
        Field(detail,"rows",detailRows);Field(detail,"legacyTitle",oldTitle);Field(detail,"legacyNumbers",new[]{oldNumber});Field(book,"stepDetail",detail);
        Check(book.ValidateConfiguration(views.Count,out diagnostic),diagnostic);
        var active = new bool[8];var positions=new Vector3[8];for(var i=0;i<8;i++){active[i]=cards[i].gameObject.activeSelf;positions[i]=cards[i].transform.position;}
        book.BindCatalog(views);
        Check(book.CatalogRecipeCount==3 && book.VisibleCatalogCardCount==3 && cards[3].View==null, "unused authored anchors have no recipe content"); Check(surfaces[0].enabled && surfaces[2].enabled && !surfaces[3].enabled && !surfaces[7].enabled, "unused grid cards hide root paper surfaces without disabling their containers");
        cards[2].SetInteractionAllowed(true);Check(!cards[2].InteractionAllowed && !cards[2].Select().Succeeded,"locked click rejects and framework interaction cannot re-enable it");
        var loc=new LocTableAsset();foreach(var view in views)loc.entries.Add(new LocTableEntry{key=view.name_key,zh_cn="fixture:"+view.recipe_id});foreach(var view in views)foreach(var step in view.steps)loc.entries.Add(new LocTableEntry{key=step.display_text_key,zh_cn="fixture:"+step.step_id});LocService.Configure(loc);
        foreach(var view in views)
        {
            Check(book.CanBindBrowse(view,out diagnostic),diagnostic);book.BindBrowse(view);
            Check(title.text==LocService.Get(view.name_key) && tabTitle.text==title.text && ReferenceEquals(dish.sprite,view.display_sprite),"three switches refresh title/dish/current tab");
            Check(detail.BoundRowCount==view.steps.Count && oldTitle.text==string.Empty && oldNumber.text==string.Empty,"step modal has chain rows with no generated title or numbering");
            for(var k=0;k<view.steps.Count;k++)Check(detailRows[k].View.step_id==view.steps[k].step_id && detailRows[k].DisplayedText==LocService.Get(view.steps[k].display_text_key),"three recipe switches bind ordered localized step rows");
            for(var n=0;n<view.slots.Count;n++)Check(entries[n].Display.name_key==view.slots[n].standard_ingredient.name_key && entries[n].Display.badge_text=="×"+view.slots[n].standard_ingredient.required_count,"ingredient list icon/key/count");
            for(var n=0;n<view.note_tags.Count;n++)Check(stickers[n].Display.name_key==view.note_tags[n].name_key,"note template preserves tag localization key");
        }
        Check(StickerNotesPiece.GetArtState(null)==StickerNotesPiece.ArtState.Missing && StickerNotesPiece.GetArtState(new Sprite{name="note_AIGC"})==StickerNotesPiece.ArtState.Aigc && StickerNotesPiece.GetArtState(new Sprite{name="note"})==StickerNotesPiece.ArtState.Ready,"local three-state placeholder classification");
        var repeats=new List<RecipeSlotView>{views[0].slots[0],views[0].slots[0],views[0].slots[0],views[0].slots[0],views[0].slots[0]};
        var five=new RecipeView("fixture-five","fixture",null,1,true,new RecipeTagView[0],repeats);book.BindBrowse(five);Check(entries[4].Display.badge_text=="×"+repeats[4].standard_ingredient.required_count,"ordered requirements beyond four use existing additional anchors");
        var many=new List<RecipeView>();for(var i=0;i<9;i++)many.Add(views[0]);book.BindCatalog(many);Check(book.PaginationRequired && book.CatalogRecipeCount==9 && book.VisibleCatalogCardCount==8,"more than eight records reports deferred pagination without inventing pages");
        book.Clear();Check(!surfaces[0].enabled && !surfaces[7].enabled,"clear hides every paper surface");Check(book.CatalogRecipeCount==0 && book.CurrentRecipe==null && title.text==string.Empty && dish.sprite==null && entries[0].Display.name_key==null,"clear removes content");book.BindCatalog(views);book.BindBrowse(views[0]);Check(book.CatalogRecipeCount==3 && book.CurrentRecipe==views[0],"clear/rebind is repeatable");
        for(var i=0;i<8;i++)Check(cards[i].gameObject.activeSelf==active[i] && cards[i].transform.position.Equals(positions[i]),"binder does not activate containers or move authored anchors");
        var session=new ShortCycleSessionManager();Field(session,"dataCatalog",catalog);Field(session,"recipeBookBinder",book);
        var save=new ObservableNoOpSaveBoundary();var machine=new ShortCycleStateMachine(new ShortCycleContext(),save,new ShortCycleDataProcessor(new UnavailableInventoryConsumptionGateway("fixture"),save,new UnavailablePhase2HandoffSink("fixture")),null);
        Field(session,"stateMachine",machine);
        Check(!session.TrySelectRecipe(views[2].recipe_id,false).Succeeded && string.IsNullOrEmpty(session.Context.current_recipe_id),"locked recipe is rejected at actual Session boundary before context mutation");
        RecipeView missing;Check(!catalog.TryGetRecipeView("missing",out missing,out diagnostic) && missing==null && !string.IsNullOrEmpty(diagnostic),"missing recipe gives bool/out/diagnostic");
        var profile=new PlayerProfile();var manager=new PlayerProfileManager(profile);
        var externalFavorites=new List<string>{views[1].recipe_id};var externalUnlocks=new List<string>{views[2].recipe_id};
        var snapshot=new PlayerProfileSnapshot(externalFavorites,externalUnlocks);externalFavorites.Clear();externalUnlocks.Clear();
        manager.ApplySnapshot(snapshot);
        Check(profile.IsFavorite(views[1].recipe_id) && profile.UnlockedRecipeIds.Count==1,"snapshot detaches caller lists and restores favorites/unlocks");
        var isolated=manager.GetSnapshot();manager.ToggleFavorite(views[0].recipe_id);
        Check(isolated.FavoriteRecipeIds.Count==1 && isolated.FavoriteRecipeIds[0]==views[1].recipe_id,"snapshot is detached from subsequent live toggles");
        var mutationRejected=false;try{((IList<string>)isolated.FavoriteRecipeIds).Add("bad");}catch(NotSupportedException){mutationRejected=true;}
        Check(mutationRejected,"snapshot collections are read-only");
        manager.ApplySnapshot(new PlayerProfileSnapshot(new[]{views[0].recipe_id,views[0].recipe_id,""},new[]{views[2].recipe_id}));
        Check(profile.FavoriteRecipeIds.Count==1,"apply normalizes duplicate/empty IDs without inventing values");
        manager.ApplySnapshot(null);Check(profile.FavoriteRecipeIds.Count==0 && profile.UnlockedRecipeIds.Count==0,"explicit empty apply clears memory only");
        Field(session,"playerProfile",profile);
        var context=session.Context;context.Enter(null,ShortCycleTriggerContext.FreeTry);context.SelectRecipe(views[0].recipe_id,false);context.phase0_view=ShortCyclePhase0View.RecipeBrowse;
        var router=new ShortCycleActionRouter();router.Configure(session);
        book.BindCatalog(views);book.BindBrowse(views[0]);
        for(var k=0;k<cards.Length;k++)Field(cards[k],"favoriteStar",new SpriteRenderer{sprite=new Sprite{name="ui_caipu_favorite_star"}});
        session.RefreshFavoriteContent();
        for(var count=0;count<3;count++)
        {
            var result=router.DispatchAction(ShortCycleActionNames.ToggleFavorite);
            var favorite=profile.IsFavorite(views[0].recipe_id);
            Check(result.Succeeded && favorite==(count%2==0) && cards[0].FavoriteVisible==favorite && book.CurrentFavoriteVisible==favorite,"three canonical favorite actions keep model/catalog/current stars synchronized");
        }
        Check(router.RegisteredActionNames.Count==23,"favorite integration does not add a 24th action");
        var retained=manager.GetSnapshot();book.Clear();book.BindCatalog(views);book.BindBrowse(views[0]);session.RefreshFavoriteContent();
        Check(cards[0].FavoriteVisible && book.CurrentFavoriteVisible && profile.IsFavorite(views[0].recipe_id),"content clear/rebind does not erase in-memory favorites");
        Check(!session.ApplyProfileSnapshot(retained).Succeeded,"save adapter must restore snapshots before an active session");
        context.ExitShortCycle();Check(session.ApplyProfileSnapshot(new PlayerProfileSnapshot(retained.FavoriteRecipeIds,new[]{views[2].recipe_id})).Succeeded && session.IsRecipeUnlocked(views[2].recipe_id) && session.IsRecipeUnlocked(views[0].recipe_id),"idle snapshot apply preserves default unlock union");
        manager.ApplySnapshot(null);Check(!session.IsRecipeUnlocked(views[2].recipe_id) && session.IsRecipeUnlocked(views[0].recipe_id),"clear removes profile unlock but preserves T1 defaults");
        Check(save.ReachedSavePoints.Count==0,"profile actions/snapshots do not perform persistence or trigger save points");
        var eleven=new List<RecipeStepView>();for(var k=0;k<11;k++)eleven.Add(views[0].steps[0]);
        var tooMany=new RecipeView("fixture-eleven","key",null,0,true,new RecipeTagView[0],new RecipeSlotView[0],eleven);
        Check(!detail.CanBind(tooMany,out diagnostic),"step modal rejects more than ten rows");
        detail.Clear();Check(detail.BoundRowCount==0 && detailRows[0].View==null && detailRows[0].DisplayedText==string.Empty,"step clear removes old rows");
        return "PASS: D-T1a/D-T1b "+checks+" assertions, production CSV mappings, immutable DTO, content-only binder, unlock/selection and lifecycle.";
    }
}
