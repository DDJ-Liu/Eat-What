using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Localization;
using EatWhat.Toolkit;
using TMPro;
using UnityEngine;

public static class ShortCycleClueBoardTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("CLUE-01: " + message); checks++; }
    private static void Field(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new Exception("CLUE-01 fixture field missing: " + name);
    }
    private static CK01GeneratedDataCatalog Catalog(string root)
    {
        var catalog = new CK01GeneratedDataCatalog();
        Check(catalog.ReplaceTables(new[] {
            ShortCycleRecipeBookTests.Table<CK01RecipeData>(root,"Recipes","recipe_id"),
            ShortCycleRecipeBookTests.Table<CK01RecipeVariantData>(root,"RecipeVariants","var_id"),
            ShortCycleRecipeBookTests.Table<CK01RecipeSlotData>(root,"RecipeSlots","slot_id"),
            ShortCycleRecipeBookTests.Table<CK01ItemData>(root,"Items","item_id"),
            ShortCycleRecipeBookTests.Table<CK01TagData>(root,"Tags","tag_id"),
            ShortCycleRecipeBookTests.Table<CK01RecipeStepData>(root,"RecipeSteps","step_id"),
            ShortCycleRecipeBookTests.Table<CK01ToolData>(root,"Tools","tool_id"),
            ShortCycleRecipeBookTests.Table<CK01InitialInventoryData>(root,"InitialInventory","item_id"),
            ShortCycleRecipeBookTests.Table<CK01CookingGameConfigData>(root,"CookingGameConfig","key"),
            ShortCycleRecipeBookTests.Table<CK01FridgeCapacityLevelData>(root,"FridgeCapacityLevels","fcap_id")
        }), "actual current CSV catalog loads");
        return catalog;
    }
    private static InventoryManager Inventory(CK01GeneratedDataCatalog catalog)
    {
        var inventory = new InventoryManager();
        Field(inventory,"dataCatalog",catalog);
        string diagnostic;
        Check(inventory.TryRebuildFromDataCatalog(out diagnostic), "actual initial inventory builds: " + diagnostic);
        return inventory;
    }
    private static SlotBlock Block(out ShortCycleSubCard[] cards, out SpriteRenderer action)
    {
        var block = new SlotBlock();
        Field(block,"centerGroup",new Transform());
        action = new SpriteRenderer();
        Field(block,"actionIconRenderer",action);
        var anchors = new Transform[8];
        cards = new ShortCycleSubCard[8];
        for (var index = 0; index < 8; index++)
        {
            anchors[index] = new Transform { name = ShortCycleAnchorContract.GetSubstituteAnchorName(index) };
            cards[index] = new ShortCycleSubCard();
            Field(cards[index],"cardRoot",new GameObject());
            Field(cards[index],"node",new GameObject());
            Field(cards[index],"iconRenderer",new SpriteRenderer());
            Field(cards[index],"countLabel",new TextMeshPro());
            Field(cards[index],"link",new SpriteRenderer());
            Field(cards[index],"unlockedMaterial",new Material());
            Field(cards[index],"lockedSilhouetteMaterial",new Material());
        }
        Field(block,"subSlotAnchors",anchors);
        Field(block,"subCards",cards);
        return block;
    }
    private static InventoryCommitResult Commit(InventoryManager inventory, string itemId, int count)
    {
        return inventory.TryCommitPlaceholderStates(new List<PlaceholderState> {
            new PlaceholderState { instance_id = "clue-test", item_id = itemId, taken_count = count }
        });
    }
    private static T Read<T>(object target, string name) where T : class
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(target) as T;
        }
        throw new Exception("CLUE visibility fixture field missing: " + name);
    }

    private static bool Hidden(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer.enabled) return false;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            if (label.enabled) return false;
        return true;
    }

    private static string VisibleDetails(GameObject root)
    {
        var values = new List<string>();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer.enabled) values.Add(renderer.GetType().Name + ":" + renderer.gameObject.name);
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            if (label.enabled) values.Add(label.GetType().Name + ":" + label.gameObject.name);
        return string.Join(",",values.ToArray());
    }

    public static string RunVisibility(string root)
    {
        checks = 0;
        var catalog = Catalog(root);
        var inventory = Inventory(catalog);
        var group = new GameObject("ClueBoard_Group_VISIBILITY_TEST");
        var set = group.AddComponent<ShortCyclePresentationSet>();
        var binder = group.AddComponent<ShortCycleClueBoardBinder>();
        Field(set,"containerRoot",group);
        Field(set,"deactivateMode",DeactivateMode.RendererHidden);
        var blocks = new SlotBlock[4];
        var cards = new ShortCycleSubCard[4][];
        var actions = new SpriteRenderer[4];
        var standard = new SpriteRenderer[4];
        var names = new LocalizedText[4];
        for (var i = 0; i < blocks.Length; i++)
        {
            blocks[i] = Block(out cards[i],out actions[i]);
            blocks[i].transform.SetParent(group.transform,false);
            blocks[i].gameObject.AddComponent<SpriteRenderer>();
            standard[i] = new SpriteRenderer();
            standard[i].transform.SetParent(blocks[i].transform,false);
            Field(blocks[i],"iconRenderer",standard[i]);
            actions[i].transform.SetParent(blocks[i].transform,false);
            var name = new TextMeshPro();
            name.transform.SetParent(blocks[i].transform,false);
            names[i] = name.gameObject.AddComponent<LocalizedText>();
            Field(blocks[i],"nameLabel",name);
            var badge = new TextMeshPro();
            badge.transform.SetParent(blocks[i].transform,false);
            Field(blocks[i],"badgeLabel",badge);
            foreach (var card in cards[i])
            {
                var cardRoot = Read<GameObject>(card,"cardRoot");
                cardRoot.transform.SetParent(blocks[i].transform,false);
                card.transform.SetParent(cardRoot.transform,false);
                Read<GameObject>(card,"node").transform.SetParent(cardRoot.transform,false);
                Read<SpriteRenderer>(card,"iconRenderer").transform.SetParent(cardRoot.transform,false);
                Read<SpriteRenderer>(card,"link").transform.SetParent(cardRoot.transform,false);
                Read<TextMeshPro>(card,"countLabel").transform.SetParent(cardRoot.transform,false);
            }
        }
        var originallyDisabled = new SpriteRenderer { enabled=false };
        originallyDisabled.transform.SetParent(group.transform,false);
        Field(binder,"slotBlocks",blocks);
        Field(binder,"slotNameBindings",names);
        Field(binder,"generatedMissingSprite",new Sprite { name="CK01-MissingSprite" });
        Field(binder,"missingIconReplacement",new Sprite { name="clue-fallback" });
        Field(binder,"actionIcons",new[] { new ShortCycleActionIconBinding {
            actionId="act_boil", icon=new Sprite { name="boil" } } });
        set.Activate();
        string diagnostic;
        Check(binder.Bind(catalog,"rcp_qingtang_noodle",inventory,out diagnostic) &&
            standard[0].enabled && standard[1].enabled && standard[2].enabled &&
            !standard[3].enabled && !originallyDisabled.enabled,
            "visible RendererHidden Set shows only the three populated blocks");
        set.Deactivate();
        Check(group.activeSelf && binder.enabled && Hidden(group),
            "RendererHidden keeps the group and Binder active but hides every child Renderer/TMP: " + VisibleDetails(group));
        var before = inventory.GetQuantity("ing_noodle_dried");
        var refreshes = binder.InventoryRefreshCount;
        Check(Commit(inventory,"ing_noodle_dried",1).Succeeded &&
            binder.InventoryRefreshCount == refreshes + 1 &&
            blocks[0].Display.badge_text == (before-1) + "/1" && Hidden(group),
            "real inventory deduction refreshes hidden content without revealing it");
        set.Deactivate();
        Check(Hidden(group), "repeated Deactivate remains hidden after content refresh");
        Check(binder.Bind(catalog,"rcp_tomato_egg",inventory,out diagnostic) &&
            blocks[0].Binding.StandardItemId == "ing_tomato" &&
            blocks[1].Binding.StandardItemId == "ing_egg" &&
            blocks[2].Binding == null && blocks[3].Binding == null && Hidden(group),
            "hidden recipe rebind updates two slots and clears excess without visibility leak");
        binder.ConfigureIngredientUnlocks(new UnlockManager(catalog,null,new string[0]));
        Check(!cards[0][0].Binding.IsUnlocked && Hidden(group),
            "hidden ingredient unlock injection updates logic but cannot reveal card");
        binder.ConfigureIngredientUnlocks(null);
        set.Activate();
        Check(blocks[0].Binding.StandardItemId == "ing_tomato" &&
            standard[0].enabled && standard[1].enabled && !standard[2].enabled &&
            !standard[3].enabled && !actions[2].enabled &&
            cards[2][0].Binding == null && !Read<SpriteRenderer>(cards[2][0],"link").enabled &&
            !originallyDisabled.enabled,
            "reactivation shows latest two-slot content, not original renderer snapshot");
        set.Deactivate();
        Check(binder.Bind(catalog,"rcp_kaishui_cabbage",inventory,out diagnostic) && Hidden(group),
            "hidden locked recipe remains logically bound and invisible");
        set.Activate();
        Check(blocks[0].Binding.StandardItemId == "ing_cabbage" &&
            standard[0].enabled && !standard[1].enabled && !standard[2].enabled &&
            !standard[3].enabled && cards[0][0].Binding.RequiredCount == 3,
            "three-to-two-to-one transition shows only latest cabbage slot");
        set.Deactivate();
        Check(binder.Bind(catalog,null,inventory,out diagnostic) && Hidden(group),
            "hidden empty recipe clears content without revealing old renderers");
        set.Activate();
        Check(blocks[0].Binding == null && blocks[1].Binding == null &&
            blocks[2].Binding == null && blocks[3].Binding == null &&
            !standard[0].enabled && !standard[1].enabled && !standard[2].enabled &&
            !standard[3].enabled && !actions[0].enabled &&
            !Read<SpriteRenderer>(cards[0][0],"link").enabled &&
            !Read<TextMeshPro>(cards[0][0],"countLabel").enabled &&
            !Read<TextMeshPro>(blocks[0],"nameLabel").enabled && !originallyDisabled.enabled,
            "empty recipe stays empty after original renderer snapshot restoration");
        Check(binder.Bind(catalog,"rcp_qingtang_noodle",inventory,out diagnostic) &&
            blocks[0].Display.badge_text == inventory.GetQuantity("ing_noodle_dried") + "/1" &&
            standard[0].enabled && !standard[3].enabled,
            "visible rebind shows current stock and no fourth stale slot");
        set.Deactivate();
        binder.enabled=false;
        typeof(ShortCycleClueBoardBinder).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(binder,null);
        refreshes=binder.InventoryRefreshCount;
        Check(Commit(inventory,"ing_noodle_dried",1).Succeeded &&
            binder.InventoryRefreshCount == refreshes && Hidden(group),
            "disabled Binder unsubscribes while RendererHidden Set stays hidden");
        binder.enabled=true;
        typeof(ShortCycleClueBoardBinder).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(binder,null);
        Check(binder.InventoryRefreshCount == refreshes+1 &&
            blocks[0].Display.badge_text == inventory.GetQuantity("ing_noodle_dried") + "/1" &&
            Hidden(group),
            "Binder re-enable catches up with stock yet obeys hidden Set");
        set.Activate();
        Check(standard[0].enabled && blocks[0].Display.badge_text ==
            inventory.GetQuantity("ing_noodle_dried") + "/1" && !originallyDisabled.enabled,
            "reactivation after disable/enable shows latest stock without enabling original disabled Renderer");
        set.Deactivate();
        set.RestoreTransientState();
        Check(!originallyDisabled.enabled,
            "Set lifecycle restore preserves original disabled Renderer default");
        var otherRoot = new GameObject("non-clue-set");
        var otherSet = otherRoot.AddComponent<ShortCyclePresentationSet>();
        Field(otherSet,"containerRoot",otherRoot);
        otherSet.Activate(); otherSet.Deactivate();
        Check(!otherRoot.activeSelf, "non-clue ContainerInactive Set still deactivates its root");
        otherSet.Activate();
        Check(otherRoot.activeSelf, "non-clue ContainerInactive Set still reactivates its root");
        return "PASS: CLUE-01 visibility repair " + checks + " Set/Binder/Inventory assertions";
    }

    public static string Run(string root)
    {
        checks = 0;
        var catalog = Catalog(root);
        var inventory = Inventory(catalog);
        var blocks = new SlotBlock[3];
        var cards = new ShortCycleSubCard[3][];
        var actions = new SpriteRenderer[3];
        for (var i = 0; i < 3; i++) blocks[i] = Block(out cards[i],out actions[i]);
        var names = new[] { new LocalizedText(), new LocalizedText(), new LocalizedText() };
        var binder = new ShortCycleClueBoardBinder();
        Field(binder,"slotBlocks",blocks);
        Field(binder,"slotNameBindings",names);
        Field(binder,"generatedMissingSprite",new Sprite { name="CK01-MissingSprite" });
        var fallback = new Sprite { name="CLUE-missing-replacement" };
        Field(binder,"missingIconReplacement",fallback);
        var boil = new Sprite { name="act_boil_icon" };
        Field(binder,"actionIcons",new[] {
            new ShortCycleActionIconBinding { actionId="act_boil", icon=boil }
        });
        string diagnostic;
        Check(binder.Bind(catalog,"rcp_qingtang_noodle",inventory,out diagnostic), "noodle binds: " + diagnostic);
        Check(blocks[0].Binding.StandardItemId == "ing_noodle_dried" &&
            blocks[1].Binding.StandardItemId == "ing_clear_broth" &&
            blocks[2].Binding.StandardItemId == "ing_egg",
            "actual noodle T1 slot order remains noodle, broth, topping");
        Check(cards[0][0].Binding.ItemId == "ing_noodle_instant" &&
            cards[0][1].Binding.ItemId == "ing_noodle_rice" && cards[0][2].Binding == null &&
            cards[1][2].Binding != null && cards[2][2].Binding != null &&
            cards[1][3].Binding == null && cards[2][3].Binding == null,
            "T1 ordered three blocks and T2 2/3/3 alternatives retain fixed anchor holes");
        Check(actions[0].sprite == boil && actions[1].sprite == boil &&
            actions[2].sprite == fallback && !string.IsNullOrEmpty(diagnostic) && diagnostic.Contains("act_fry"),
            "standard_action uses explicit mapping or diagnosable replacement: " + diagnostic);
        Check(blocks[0].Display.name_key == "ing_noodle_dried.name" &&
            blocks[0].Display.badge_text == inventory.GetQuantity("ing_noodle_dried") + "/1" &&
            names[0].LocalizationKey == "ing_noodle_dried.name",
            "standard T2 item icon/name and actual inventory count");
        Check(cards[0][0].Binding.HeldCount == inventory.GetQuantity("ing_noodle_instant") &&
            cards[0][0].Binding.RequiredCount == 1,
            "sub card quantity comes from inventory, not reservations");
        var source = (IShortCycleInventoryChangeSource)inventory;
        var changes = 0;
        source.Changed += () => changes++;
        Check(!Commit(inventory,"missing_item",1).Succeeded && changes == 0,
            "failed commit does not notify");
        Check(inventory.TryCommitPlaceholderStates(new List<PlaceholderState>()).Succeeded && changes == 0,
            "successful no-op does not notify");
        Check(binder.Bind(catalog,"rcp_qingtang_noodle",inventory,out diagnostic) &&
            binder.Bind(catalog,"rcp_qingtang_noodle",inventory,out diagnostic),
            "repeated Bind remains valid without duplicate subscriptions");
        var refreshBefore = binder.InventoryRefreshCount;
        var before = cards[0][0].Binding.HeldCount;
        Check(Commit(inventory,"ing_noodle_instant",1).Succeeded && changes == 1 &&
            binder.InventoryRefreshCount == refreshBefore + 1 &&
            cards[0][0].Binding.HeldCount == before - 1,
            "successful real deduction notifies once and rebinds the actual card");
        Check(inventory.TryRebuildFromDataCatalog(out diagnostic) && changes == 2 &&
            cards[0][0].Binding.HeldCount == before,
            "changed rebuild restores actual count and rebinds");
        Check(inventory.TryRebuildFromDataCatalog(out diagnostic) && changes == 2,
            "identical rebuild is notification-free");
        var preserved = inventory.GetQuantity("ing_noodle_instant");
        Field(inventory,"dataCatalog",null);
        Check(!inventory.TryRebuildFromDataCatalog(out diagnostic) && changes == 2 &&
            inventory.GetQuantity("ing_noodle_instant") == preserved &&
            cards[0][0].Binding.HeldCount == preserved,
            "failed rebuild preserves stock and does not notify or partially refresh");
        Field(inventory,"dataCatalog",catalog);

        binder.ConfigureIngredientUnlocks(new UnlockManager(catalog,null,new[] { "ing_noodle_instant" }));
        var locked = cards[0][1];
        var lockedIcon = (SpriteRenderer)typeof(ShortCycleSubCard).GetField("iconRenderer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(locked);
        var silhouette = (Material)typeof(ShortCycleSubCard).GetField("lockedSilhouetteMaterial",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(locked);
        Check(cards[0][0].Binding.IsUnlocked && !locked.Binding.IsUnlocked &&
            locked.Binding.PrimaryCategoryTagId != null &&
            lockedIcon.sprite == locked.Binding.PrimaryCategoryTagIcon &&
            lockedIcon.sharedMaterial == silhouette,
            "explicit ingredient unlock drives first category tag silhouette in real Binder");
        binder.ConfigureIngredientUnlocks(null);
        Check(locked.Binding.IsUnlocked, "demo null ingredient profile restores all-catalog unlocked policy");
        Check(binder.Bind(catalog,"rcp_tomato_egg",inventory,out diagnostic) &&
            blocks[0].Binding.StandardItemId == "ing_tomato" &&
            blocks[1].Binding.StandardItemId == "ing_egg" &&
            cards[0][0].Binding != null && cards[0][1].Binding == null &&
            cards[1][0].Binding != null && cards[1][1].Binding != null &&
            cards[2][0].Binding == null && blocks[2].Display.icon_sprite == null &&
            names[2].LocalizationKey == string.Empty,
            "tomato egg binds 1/2 alternatives and clears extra block/name");
        Check(binder.Bind(catalog,"rcp_kaishui_cabbage",inventory,out diagnostic) &&
            blocks[0].Binding.StandardItemId == "ing_cabbage" &&
            cards[0][0].Binding.RequiredCount == 3 &&
            cards[0][0].Binding != null && cards[0][1].Binding == null &&
            blocks[1].Display.icon_sprite == null && names[1].LocalizationKey == string.Empty,
            "locked cabbage recipe remains visible with one alternative and cleared holes");
        var duplicateCatalog = Catalog(root);
        CK01RecipeData duplicateRecipe;
        Check(duplicateCatalog.TryGetRecipe("rcp_qingtang_noodle",out duplicateRecipe,out diagnostic),
            "duplicate fixture starts from actual current T1 row");
        duplicateRecipe.slots = new List<string> { "slot_noodle_1", "slot_noodle_1" };
        Check(binder.Bind(duplicateCatalog,"rcp_qingtang_noodle",inventory,out diagnostic) &&
            blocks[0].Binding.StandardItemId == "ing_noodle_dried" &&
            blocks[1].Binding.StandardItemId == "ing_noodle_dried" &&
            !ReferenceEquals(blocks[0].Binding,blocks[1].Binding) &&
            blocks[2].Binding == null,
            "duplicated T1 slot references bind twice and clear excess block");
        Check(binder.Bind(catalog,null,inventory,out diagnostic) &&
            blocks[0].Display.icon_sprite == null && cards[0][0].Binding == null,
            "clear removes all clue contents and subscription");
        var second = Inventory(catalog);
        Check(binder.Bind(catalog,"rcp_qingtang_noodle",second,out diagnostic), "rebind to second inventory");
        var rebound = cards[0][0].Binding.HeldCount;
        Check(Commit(inventory,"ing_noodle_instant",1).Succeeded && cards[0][0].Binding.HeldCount == rebound,
            "old inventory notification cannot mutate rebound binder");
        Check(Commit(second,"ing_noodle_instant",1).Succeeded && cards[0][0].Binding.HeldCount == rebound-1,
            "new inventory notification reaches rebound binder");
        var disable = typeof(ShortCycleClueBoardBinder).GetMethod("OnDisable", BindingFlags.Instance|BindingFlags.NonPublic);
        var enable = typeof(ShortCycleClueBoardBinder).GetMethod("OnEnable", BindingFlags.Instance|BindingFlags.NonPublic);
        var destroy = typeof(ShortCycleClueBoardBinder).GetMethod("OnDestroy", BindingFlags.Instance|BindingFlags.NonPublic);
        disable.Invoke(binder,null);
        var disabledCount = cards[0][0].Binding.HeldCount;
        Check(Commit(second,"ing_noodle_instant",1).Succeeded &&
            cards[0][0].Binding.HeldCount == disabledCount,
            "disabled Binder has no stale inventory subscription");
        enable.Invoke(binder,null);
        Check(cards[0][0].Binding.HeldCount == second.GetQuantity("ing_noodle_instant"),
            "re-enable attaches and catches up from real inventory");
        destroy.Invoke(binder,null);
        var destroyedCount = cards[0][0].Binding.HeldCount;
        Check(second.TryRebuildFromDataCatalog(out diagnostic) &&
            cards[0][0].Binding.HeldCount == destroyedCount,
            "destroyed Binder has no stale inventory subscription");

        var iconCard = cards[0][0];
        var iconRenderer = (SpriteRenderer)typeof(ShortCycleSubCard).GetField("iconRenderer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(iconCard);
        var missing = new Sprite { name="CK01-MissingSprite" };
        var ready = new Sprite { name="ordinary-art" };
        var aigc = new Sprite { name="future_AIGC" };
        var iconBinding = new ShortCycleSubCardBinding { ItemId="test", IsUnlocked=true,
            ItemIcon=ready, HeldCount=1, RequiredCount=1 };
        iconCard.Configure(iconBinding,missing,fallback);
        Check(iconCard.IconState == ToolkitPlaceholderState.Ready && iconRenderer.sprite == ready,
            "ordinary non-missing item is Ready with exact missing identity bound");
        iconBinding.ItemIcon = aigc;
        iconCard.Configure(iconBinding,missing,fallback);
        Check(iconCard.IconState == ToolkitPlaceholderState.Aigc && iconRenderer.sprite == aigc,
            "_AIGC item is classified by shared PlaceholderRule");
        iconBinding.ItemIcon = missing;
        iconCard.Configure(iconBinding,missing,fallback);
        Check(iconCard.IconState == ToolkitPlaceholderState.Missing && iconRenderer.sprite == fallback,
            "exact CK01-MissingSprite receives explicit replacement");
        iconCard.Configure(iconBinding,missing,null);
        Check(iconCard.IconState == ToolkitPlaceholderState.Missing && iconRenderer.sprite == null &&
            !string.IsNullOrEmpty(iconCard.IconConfigurationDiagnostic),
            "missing replacement has an explicit diagnostic");
        iconBinding.ItemIcon = ready;
        iconCard.Configure(iconBinding,null,fallback);
        Check(iconCard.IconState == null && !string.IsNullOrEmpty(iconCard.IconConfigurationDiagnostic),
            "unbound missing-source identity cannot certify ordinary art Ready");
        var noSilhouette = new ShortCycleSubCard();
        Field(noSilhouette,"iconRenderer",new SpriteRenderer());
        noSilhouette.Configure(new ShortCycleSubCardBinding { ItemId="locked",IsUnlocked=false,
            PrimaryCategoryTagIcon=ready, HeldCount=0,RequiredCount=1 },missing,fallback);
        Check(!string.IsNullOrEmpty(noSilhouette.IconConfigurationDiagnostic),
            "locked card without silhouette material reports missing reference");
        var probeBinder = new ShortCycleClueBoardBinder();
        Field(probeBinder,"slotBlocks",blocks);
        Field(probeBinder,"slotNameBindings",names);
        Field(probeBinder,"generatedMissingSprite",missing);
        Field(probeBinder,"missingIconReplacement",fallback);
        var coordinator = new ShortCycleSessionManager();
        Field(coordinator,"dataCatalog",catalog);
        Field(coordinator,"inventoryManager",second);
        var probe = new ShortCycleAutomatedProbe();
        Field(probe,"coordinator",coordinator);
        Field(probe,"clueBoardContentBinder",probeBinder);
        Field(probe,"clueBoardProbeBlocks",blocks);
        var probeFailures = new List<string>();
        typeof(ShortCycleAutomatedProbe).GetMethod("RunClueBindingChecks",
            BindingFlags.Instance|BindingFlags.NonPublic).Invoke(probe,new object[] { probeFailures });
        Check(probeFailures.Count == 0 && probe.AutomatedProbeCheckpoints.Count == 4 &&
            probe.AutomatedProbeCheckpoints[0].StartsWith("24/27 PASS") &&
            probe.AutomatedProbeCheckpoints[3].StartsWith("27/27 PASS"),
            "append-only probe 24..27 executes the actual Binder consumer: " +
            string.Join("; ",probeFailures.ToArray()));
        return "PASS: CLUE-01 " + checks + " actual CSV and Binder/Inventory assertions";
    }
}
