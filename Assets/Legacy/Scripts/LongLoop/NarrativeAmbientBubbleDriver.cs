using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    // Only scene capabilities cross this boundary; directory/resolution/cooldown/line selection are production implementations.
    public interface INarrativeBubbleWorld
    {
        bool TryGetGroundDistance(out double distance, out string diagnostic);
        bool TryGetSpeakerHead(string speakerId, out Transform head, out string diagnostic);
    }

    /// <summary>The trigger's coordinator; delegates every pool/progress/cooldown decision to its one Core model.</summary>
    public sealed class NarrativeAmbientBubbleDriver
    {
        private readonly string npcId;
        private readonly NarrativeRuntimeEntry runtime;
        private readonly INarrativeBubbleWorld world;
        private readonly INarrativeBubbleSink sink;
        private readonly string triggerTemplate;
        private readonly List<string> diagnostics = new List<string>();
        private INarrativeDataDirectory modelDirectory;
        private NarrativeAmbientBubbleModel model;
        private int consumedModelDiagnostics;
        private string poolId;
        public IReadOnlyList<string> Diagnostics => diagnostics.AsReadOnly();
        public double? CooldownUntilSeconds => model == null ? null : model.CooldownUntilSeconds;
        public NarrativeAmbientBubbleDriver(string npcId, NarrativeRuntimeEntry runtime,
            INarrativeBubbleWorld world, INarrativeBubbleSink sink, string poolId, string triggerTemplate = null)
        { this.npcId = npcId; this.runtime = runtime; this.world = world; this.sink = sink;
            this.poolId = poolId; this.triggerTemplate = triggerTemplate; }

        public void ChangePool(string value)
        { poolId = value; if (model != null) model.ChangePool(value); }

        public bool Tick(double nowSeconds, Func<int, int> chooseIndex)
        { return TryTick(nowSeconds, chooseIndex, false); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugForce(double nowSeconds, Func<int, int> chooseIndex)
        { return TryTick(nowSeconds, chooseIndex, true); }
#endif
        private bool TryTick(double nowSeconds, Func<int, int> chooseIndex, bool force)
        {
            if (string.IsNullOrWhiteSpace(poolId)) return false;
            if (string.IsNullOrWhiteSpace(npcId) || runtime == null || world == null || sink == null)
                return Diagnose("MISSING_BINDING: NPC id, Runtime Entry, scene world and view are required.");
            INarrativeDataDirectory directory;
            if (!runtime.TryGetDirectory(out directory)) return Diagnose("RUNTIME_UNAVAILABLE: " + string.Join("; ", runtime.Diagnostics));
            if (model == null)
            {
                modelDirectory = directory; model = new NarrativeAmbientBubbleModel(npcId, directory); model.ChangePool(poolId);
            }
            // A directory is an immutable lifetime dependency. Never recreate the model in Update and erase its deadline.
            if (!ReferenceEquals(modelDirectory, directory))
                return Diagnose("DIRECTORY_CHANGED: Existing NPC model is retained and suspended; recreate the NPC/scene at an explicit lifetime boundary.");
            string issue; DialogueTriggerResolution trigger;
            if (!runtime.TryResolveTrigger(triggerTemplate, out trigger, out issue)) return Diagnose("TRIGGER_UNAVAILABLE: " + issue);
            NarrativeAmbientBubbleEmission emission;
            bool emitted;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (force) emitted = model.DebugForce(nowSeconds, chooseIndex, out emission);
            else
#endif
            {
                double distance;
                if (!world.TryGetGroundDistance(out distance, out issue)) return Diagnose("GROUND_UNAVAILABLE: " + issue);
                emitted = model.TryTrigger(nowSeconds, distance, trigger.Snapshot, chooseIndex, out emission);
            }
            while (consumedModelDiagnostics < model.Diagnostics.Count)
            {
                var item = model.Diagnostics[consumedModelDiagnostics++];
                Diagnose("MODEL: " + item.Code + ": " + item.PoolId + "/" + item.Context + ": " + item.Reason);
            }
            if (!emitted) return false;
            Transform head;
            if (!world.TryGetSpeakerHead(emission.SpeakerId, out head, out issue) || head == null)
                return Diagnose("SPEAKER_UNAVAILABLE: " + emission.SpeakerId + ": " + issue);
            DialoguePresentationResolution presentation;
            if (!runtime.TryResolveDialogue(emission.DialogueId, out presentation, out issue, triggerTemplate))
                return Diagnose("PRESENTATION_UNAVAILABLE: " + issue);
            // A skipped display still consumed this successful Core emission. Do not roll back or retry the same line.
            if (!sink.Show(emission.NpcId, head, emission.TextKey, presentation.Snapshot.GetGroup("tier"), nowSeconds))
                return Diagnose("DISPLAY_SKIPPED: " + emission.TextKey);
            return true;
        }
        private bool Diagnose(string issue) { if (!diagnostics.Contains(issue)) diagnostics.Add(issue); return false; }
    }
}
