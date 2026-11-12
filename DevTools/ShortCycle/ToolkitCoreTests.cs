using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using EatWhat.DataTables;
using EatWhat.Toolkit;
using UnityEngine;

public static class ToolkitCoreTests
{
    private sealed class Item
    {
        public readonly string Id;
        public readonly string[] Tags;
        public Item(string id, params string[] tags) { Id = id; Tags = tags; }
    }

    private static readonly List<string> cases = new List<string>();
    public static IReadOnlyList<string> Cases { get { return cases.AsReadOnly(); } }
    private static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("TK-01: " + label);
        cases.Add(label);
    }
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
    private static void Field(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Missing fixture field " + name);
        field.SetValue(target, value);
    }

    public static string Run(string root)
    {
        cases.Clear();
        var a = new Item("a", "veg", "fruit");
        var b = new Item("b", "egg");
        var c = new Item("c", "veg");
        var source = new[] { a, b, c, a };
        var any = TagFilter.Apply(source, item => item.Tags, new[] { "egg", "veg", "egg" });
        Check(any.Count == 4 && ReferenceEquals(any[0], a) && ReferenceEquals(any[1], b) &&
            ReferenceEquals(any[2], c) && ReferenceEquals(any[3], a), "tag any preserves source order and duplicate source rows");
        var all = TagFilter.Apply(source, item => item.Tags, new[] { "veg", "fruit", "veg" }, TagSelectionMode.All);
        Check(all.Count == 2 && ReferenceEquals(all[0], a) && ReferenceEquals(all[1], a),
            "tag all requires every distinct selected ID without duplicating output");
        Check(TagFilter.Apply(source, item => item.Tags, new string[0]).Count == source.Length &&
            TagFilter.Apply(source, item => item.Tags, new[] { "absent" }).Count == 0,
            "empty selection keeps all rows; no match is empty");
        Check(TagFilter.Apply(new[] { new Item("upper", "VEG") }, item => item.Tags,
            new[] { "veg" }, comparer: StringComparer.OrdinalIgnoreCase).Count == 1 &&
            TagFilter.Apply(new[] { new Item("upper", "VEG") }, item => item.Tags,
            new[] { "veg" }).Count == 0, "caller controls tag ID comparison");
        Check(Throws<ArgumentException>(() => TagFilter.Apply(source, item => item.Tags, new[] { " " })) &&
            source.Length == 4, "invalid selected tag fails without changing source");

        var points = new[] { "existing-layout-0", "existing-layout-1" };
        var planned = TidyLayout.Plan(source, item => item.Id == "b" || item.Id == "c", points);
        Check(planned.Count == 2 && planned[0].SourceIndex == 1 && planned[0].PointIndex == 0 &&
            planned[0].Point == points[0] && planned[1].SourceIndex == 2 &&
            planned[1].Point == points[1], "visible items compact onto existing point snapshot in order");
        Check(points[0] == "existing-layout-0" && ReferenceEquals(source[0], a) &&
            TidyLayout.Plan(source, item => false, points).Count == 0,
            "planning leaves authored points and source untouched, including empty view");
        Check(Throws<ArgumentException>(() => TidyLayout.Plan(source, item => true, points)),
            "insufficient existing point capacity fails instead of moving layout");

        var parsed = DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(root, "DataTables/Cooking/Items.csv")));
        Check(parsed.IsValid, "actual Items CSV parses with production parser");
        var stackColumn = parsed.Table.GetHeaderIndex("stack_type");
        Check(stackColumn >= 0, "actual Items field stack_type exists");
        var observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in parsed.Table.Rows)
        {
            var value = row.Values[stackColumn];
            if (string.IsNullOrEmpty(value)) continue;
            StackedIconShape shape;
            Check(StackedIcon.TryParseShape(value, out shape), "actual Items stack_type has a supported shape: " + value);
            observed.Add(value);
        }
        Check(observed.SetEquals(new[] { "single", "stack", "pack", "container" }),
            "four shapes match actual Items stack_type values");
        var capacity = new Dictionary<StackedIconShape, int> {
            { StackedIconShape.Single, 1 }, { StackedIconShape.Stack, 3 },
            { StackedIconShape.Pack, 2 }, { StackedIconShape.Container, 1 }
        };
        foreach (var shape in observed)
        {
            var plan = StackedIcon.Plan(shape, 5, capacity, true);
            Check(plan.Quantity == 5 && plan.QuantityLabel == "5" &&
                plan.VisibleIconCount == capacity[plan.Shape], "explicit fixture capacity and quantity for " + shape);
        }
        var zero = StackedIcon.Plan("stack", 0, capacity, false);
        Check(zero.VisibleIconCount == 0 && zero.QuantityLabel == null &&
            StackedIcon.Plan("stack", 1, capacity, true).QuantityLabel == "1",
            "zero and one quantity obey explicit display choice");
        Check(Throws<ArgumentException>(() => StackedIcon.Plan("other", 1, capacity, true)) &&
            Throws<ArgumentOutOfRangeException>(() => StackedIcon.Plan("single", -1, capacity, true)) &&
            Throws<ArgumentException>(() => StackedIcon.Plan("single", 1,
                new Dictionary<StackedIconShape, int> { { StackedIconShape.Single, 1 } }, true)),
            "unknown shape, negative quantity and incomplete visual configuration fail");

        Check(PlaceholderRule.Classify(false, "x_AIGC") == ToolkitPlaceholderState.Missing &&
            PlaceholderRule.Classify(true, null) == ToolkitPlaceholderState.Ready &&
            PlaceholderRule.Classify(true, "x_aIgC") == ToolkitPlaceholderState.Aigc &&
            PlaceholderRule.Classify(true, "x_AIGC_extra") == ToolkitPlaceholderState.Ready,
            "missing precedes suffix; AIGC suffix is case insensitive and exact");
        Check(StickerNotesPiece.GetArtState(null) == StickerNotesPiece.ArtState.Missing &&
            StickerNotesPiece.GetArtState(new Sprite { name = "x_aigc" }) == StickerNotesPiece.ArtState.Aigc &&
            StickerNotesPiece.GetArtState(new Sprite { name = "x" }) == StickerNotesPiece.ArtState.Ready,
            "StickerNotes public three-state API stays compatible");
        var sticker = new Sticker();
        var notes = new StickerNotesPiece();
        var fallback = new Sprite { name = "fallback" };
        Field(notes, "notes", new[] { sticker });
        Field(notes, "genericNoteTemplate", fallback);
        notes.Apply(new RecipeView("r", "r", null, 0, true,
            new[] { new RecipeTagView("t", "tag.key", null, null) }, new RecipeSlotView[0]));
        Check(ReferenceEquals(sticker.Display.icon_sprite, fallback) &&
            sticker.Display.name_key == "tag.key" && !sticker.Display.is_placeholder,
            "missing sticker image retains legacy generic template and localization");
        var aigc = new Sprite { name = "paper_AIGC" };
        notes.Apply(new RecipeView("r", "r", null, 0, true,
            new[] { new RecipeTagView("t", "tag.key", null, aigc) }, new RecipeSlotView[0]));
        Check(ReferenceEquals(sticker.Display.icon_sprite, aigc) && sticker.Display.is_placeholder,
            "AIGC sticker retains original sprite and placeholder flag");
        notes.Apply(null);
        Check(sticker.Display.icon_sprite == null && sticker.Display.name_key == null &&
            !sticker.Display.is_placeholder, "empty recipe clears sticker content without inventing fallback");
        return "PASS: TK-01 " + cases.Count + " assertions";
    }
}