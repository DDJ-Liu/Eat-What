using System;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Version-specific Unity adapter, before the unchanged Core FSM can take its Seamless bypass.</summary>
    public sealed class NarrativeDialogueContinuationPolicy
    {
        readonly Action<string> diagnostic;
        public NarrativeDialogueContinuationPolicy(Action<string> diagnostic = null)
        { this.diagnostic = diagnostic ?? (message => UnityEngine.Debug.LogWarning(message)); }

        public DialoguePresentationResolution Resolve(DialoguePresentationData config, NarrativeDialogue dialogue, string triggerTemplateName = null)
        {
            var original = DialoguePresentationResolver.Resolve(config, dialogue, triggerTemplateName);
            if (!original.Success || original.Snapshot.Get<DialogueContinuationMode>("exit.continuationMode") != DialogueContinuationMode.Seamless)
                return original;
            // Copy only the authoring group being normalized. Other groups remain read-only Resolver inputs.
            var exits = new DialogueExitConfig[config.exit.Length];
            for (int i = 0; i < exits.Length; i++)
            {
                var value = config.exit[i];
                exits[i] = new DialogueExitConfig { name = value.name, order = value.order,
                    letterboxOutTime = value.letterboxOutTime, boxTime = value.boxTime,
                    portraitTime = value.portraitTime, portraitSlide = value.portraitSlide,
                    undimTime = value.undimTime, cameraReturnTime = value.cameraReturnTime,
                    controlResume = value.controlResume, continuationMode = DialogueContinuationMode.FullFlow };
            }
            var runtime = new DialoguePresentationData { tier = config.tier, trigger = config.trigger,
                entrance = config.entrance, exit = exits, layout = config.layout, speaker = config.speaker,
                expression = config.expression, text = config.text, reaction = config.reaction, cg = config.cg };
            string source = dialogue.Overrides;
            foreach (var item in DialogueOverrideParser.ParseItems(source).Items)
            {
                if (!item.Success || item.Key != "exit.continuationMode" || (DialogueContinuationMode)item.Value.Value != DialogueContinuationMode.Seamless) continue;
                // The existing parser has already validated this exact enum atom. Equal-length replacement
                // preserves offsets, whitespace and diagnostics of every other item, including duplicates.
                int atom = item.SourceText.IndexOf('=') + 1;
                while (char.IsWhiteSpace(item.SourceText[atom])) atom++;
                int position = item.Position + atom;
                source = source.Substring(0, position) + "FullFlow" + source.Substring(position + "Seamless".Length);
            }
            var resolved = DialoguePresentationResolver.ResolveWithOverrides(runtime, dialogue, DialogueOverrideParser.ParseItems(source), triggerTemplateName);
            if (resolved.Success) diagnostic("Dialogue " + dialogue.DialogueId + ": Seamless uses FullFlow in 0.1.4.");
            return resolved;
        }
    }
}
