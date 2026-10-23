using EatWhat.Tools.Localization;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Only fills existing VerticalLayout row anchors. ModalSet/Director retain visibility and input ownership.</summary>
    public sealed class StepDetailModalPiece : MonoBehaviour
    {
        [SerializeField] private StepDetailRowPiece[] rows = new StepDetailRowPiece[10];
        [SerializeField] private TextMeshPro legacyTitle = null;
        [SerializeField] private TextMeshPro[] legacyNumbers = new TextMeshPro[0];
        public int BoundRowCount { get; private set; }
        public IReadOnlyList<StepDetailRowPiece> Rows { get { return System.Array.AsReadOnly(rows); } }
        public bool CanBind(RecipeView view, out string diagnostic)
        {
            var count = view == null ? 0 : view.steps.Count;
            if (view != null && legacyTitle == null) { diagnostic = "Connect the existing modal title so content binding can clear it."; return false; }
            if (count > 10 || count > rows.Length) { diagnostic = "Step detail requires at most ten connected ordered row anchors."; return false; }
            for (var i = 0; i < count; i++)
                if (rows[i] == null || !rows[i].CanBind(view.steps[i]))
                { diagnostic = "Step row/icon references are incomplete at " + i; return false; }
            diagnostic = null; return true;
        }
        public void Bind(RecipeView view)
        {
            BoundRowCount = view == null ? 0 : view.steps.Count;
            if (legacyTitle != null) LocalizedText.ConfigureContent(legacyTitle, string.Empty, string.Empty);
            foreach (var label in legacyNumbers) if (label != null) LocalizedText.ConfigureContent(label, string.Empty, string.Empty);
            for (var i = 0; i < rows.Length; i++) if (rows[i] != null) rows[i].Apply(view != null && i < view.steps.Count ? view.steps[i] : null);
        }
        public void Clear() { Bind(null); }
    }
}
