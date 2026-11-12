using System;
using System.Collections.Generic;

namespace EatWhat.Toolkit
{
    public enum TagSelectionMode { Any, All }

    /// <summary>Stable, read-only projection of items selected by their tag IDs.</summary>
    public static class TagFilter
    {
        public static IReadOnlyList<T> Apply<T>(
            IEnumerable<T> source,
            Func<T, IEnumerable<string>> tagsFor,
            IEnumerable<string> selectedTags,
            TagSelectionMode mode = TagSelectionMode.Any,
            StringComparer comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (tagsFor == null) throw new ArgumentNullException(nameof(tagsFor));
            if (mode != TagSelectionMode.Any && mode != TagSelectionMode.All)
                throw new ArgumentOutOfRangeException(nameof(mode));

            var selected = new HashSet<string>(comparer ?? StringComparer.Ordinal);
            if (selectedTags != null)
            {
                foreach (var tag in selectedTags)
                {
                    if (string.IsNullOrWhiteSpace(tag))
                        throw new ArgumentException("Selected tag IDs must be nonempty.", nameof(selectedTags));
                    selected.Add(tag);
                }
            }

            var result = new List<T>();
            foreach (var item in source)
            {
                if (selected.Count == 0)
                {
                    result.Add(item);
                    continue;
                }

                var tags = tagsFor(item);
                if (tags == null) continue;
                var itemTags = new HashSet<string>(comparer ?? StringComparer.Ordinal);
                foreach (var tag in tags)
                    if (!string.IsNullOrEmpty(tag)) itemTags.Add(tag);

                if (mode == TagSelectionMode.Any ? itemTags.Overlaps(selected) : itemTags.IsSupersetOf(selected))
                    result.Add(item);
            }
            return result.AsReadOnly();
        }
    }
}