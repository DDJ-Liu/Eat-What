using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EatWhat.EditorTools.Fonts;

internal static class FontSwapDryRunHarness
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static ShortCycleFontSwapTool.NonTargetFingerprintProperty Scalar(
        string path, string type, string value)
    {
        return new ShortCycleFontSwapTool.NonTargetFingerprintProperty
        {
            propertyPath = path,
            propertyType = type,
            scalarValue = value
        };
    }

    private static ShortCycleFontSwapTool.NonTargetFingerprintProperty Reference(
        string path,
        ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor descriptor)
    {
        return new ShortCycleFontSwapTool.NonTargetFingerprintProperty
        {
            propertyPath = path,
            propertyType = "ObjectReference",
            objectReference = descriptor
        };
    }

    private static ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor LocalComponent(
        string hierarchyPath, string componentType, int componentIndex, int transientId)
    {
        return new ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor
        {
            assetPath = "Assets/Scenes/ToolTests/FridgeScrollLab.unity",
            hierarchyPath = hierarchyPath,
            objectKind = "COMPONENT",
            componentType = componentType,
            componentIndex = componentIndex,
            transientInstanceId = transientId
        };
    }

    private static ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor PrefabInstance(
        string assetGuid, long prefabInstanceId, long ownerObjectId, int transientId)
    {
        return new ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor
        {
            identityKind = ShortCycleFontSwapTool.PrefabInstanceObjectReferenceKind,
            persistentGlobalId = "GlobalObjectId_V1-2-" + assetGuid + "-" +
                prefabInstanceId + "-0",
            ownerGlobalId = "GlobalObjectId_V1-2-" + assetGuid + "-" + ownerObjectId +
                "-" + prefabInstanceId,
            ownerAssetGuid = assetGuid,
            assetPath = "Assets/Scenes/Cooking/ShortCycle_P0P1.unity",
            transientInstanceId = transientId
        };
    }

    private static ShortCycleFontSwapTool.NonTargetFingerprintProperty[] FingerprintFixture(
        ShortCycleFontSwapTool.ObjectReferenceFingerprintDescriptor renderer,
        string fileId = "17", string text = "Hello", string color = "RGBA(1,1,1,1)",
        string fontSize = "36", string arrayValue = "alpha")
    {
        return new[]
        {
            Scalar("m_FileID", "Integer", fileId),
            Scalar("m_text", "String", text),
            Scalar("m_fontSize", "Float", fontSize),
            Scalar("m_color", "Color", color),
            Scalar("m_nested.Array.size", "ArraySize", "1"),
            Scalar("m_nested.Array.data[0].value", "String", arrayValue),
            Reference("m_renderer", renderer),
            Reference("m_fontAsset", new ShortCycleFontSwapTool.
                ObjectReferenceFingerprintDescriptor { unresolved = true }),
            Reference("m_sharedMaterial", new ShortCycleFontSwapTool.
                ObjectReferenceFingerprintDescriptor { unresolved = true })
        };
    }

    private static ShortCycleFontSwapTool.OfflineBinding Observe(
        ShortCycleFontSwapTool.ApprovedMapping mapping)
    {
        return new ShortCycleFontSwapTool.OfflineBinding
        {
            rowIdentity = mapping.Identity,
            canonicalIdentity = mapping.Identity,
            prefabSourceIdentity = mapping.expectedPrefabSourceIdentity,
            isPrefabInstance = mapping.enforcePrefabShape,
            fontOverride = mapping.expectedFontOverride,
            materialOverride = mapping.expectedMaterialOverride,
            loadedFontOverride = mapping.expectedFontOverride,
            loadedMaterialOverride = mapping.expectedMaterialOverride,
            writeAssetPath = mapping.assetPath,
            writeObjectPath = mapping.objectPath,
            writeComponentIndex = mapping.componentIndex,
            currentFontGuid = mapping.currentFontGuid,
            currentMaterialGlobalId = mapping.currentMaterialGlobalId,
            currentMaterialPath = "source|" + mapping.currentMaterialGlobalId,
            approvedObservationFontGuid = mapping.currentFontGuid,
            approvedObservationMaterialGlobalId = mapping.currentMaterialGlobalId,
            effectiveMaterialGlobalId = mapping.currentMaterialGlobalId,
            effectiveMaterialPath = "source|" + mapping.currentMaterialGlobalId,
            materialNormalizationReason = string.Empty,
            savedReferenceOwnerIdentity = "fixture-owner|" + mapping.Identity,
            savedReferenceAssetPath = mapping.assetPath,
            savedReferenceAssetSha256 = "fixture-sha|" + mapping.assetPath,
            savedReferenceSourceAssetPath = mapping.assetPath,
            savedReferenceSourceAssetSha256 = "fixture-sha|" + mapping.assetPath,
            savedReferenceProven = true,
            savedReferenceAssetShaMatches = true,
            nonTargetFingerprint = "NON_TARGET|" + mapping.Identity
        };
    }

    private static Dictionary<string, ShortCycleFontSwapTool.OfflineBinding> StateFor(
        ShortCycleFontSwapTool.DryRunPlan plan)
    {
        return plan.rows.ToDictionary(
            row => row.rowIdentity,
            row => new ShortCycleFontSwapTool.OfflineBinding
            {
                rowIdentity = row.rowIdentity,
                canonicalIdentity = row.canonicalIdentity,
                prefabSourceIdentity = row.prefabSourceIdentity,
                isPrefabInstance = !string.IsNullOrWhiteSpace(row.prefabSourceIdentity),
                fontOverride = row.beforeFontOverride,
                materialOverride = row.beforeMaterialOverride,
                loadedFontOverride = row.beforeLoadedFontOverride,
                loadedMaterialOverride = row.beforeLoadedMaterialOverride,
                writeAssetPath = row.assetPath,
                writeObjectPath = row.objectPath,
                writeComponentIndex = row.componentIndex,
                currentFontGuid = row.beforeFontGuid,
                currentMaterialGlobalId = row.beforeMaterialGlobalId,
                currentMaterialPath = row.beforeMaterialPath,
                approvedObservationFontGuid = row.approvedObservationFontGuid,
                approvedObservationMaterialGlobalId =
                    row.approvedObservationMaterialGlobalId,
                effectiveMaterialGlobalId = row.beforeEffectiveMaterialGlobalId,
                effectiveMaterialPath = row.beforeEffectiveMaterialPath,
                materialNormalizationReason = row.beforeMaterialNormalizationReason,
                savedReferenceOwnerIdentity = row.savedReferenceOwnerIdentity,
                savedReferenceAssetPath = row.savedReferenceAssetPath,
                savedReferenceAssetSha256 = row.savedReferenceAssetSha256,
                savedReferenceSourceAssetPath = row.savedReferenceSourceAssetPath,
                savedReferenceSourceAssetSha256 =
                    row.savedReferenceSourceAssetSha256,
                savedReferenceProven = row.savedReferenceProven,
                savedReferenceAssetShaMatches = row.savedReferenceAssetShaMatches,
                nonTargetFingerprint = row.nonTargetFingerprint
            },
            StringComparer.Ordinal);
    }

    private static ShortCycleFontSwapTool.FingerprintDiagnosticRow DiagnosticFor(
        ShortCycleFontSwapTool.ApprovedRowAudit row)
    {
        return new ShortCycleFontSwapTool.FingerprintDiagnosticRow
        {
            rowIdentity = row.rowIdentity,
            fingerprint = row.nonTargetFingerprint,
            objectReferenceIdentities = new string[0],
            rawFontGuid = row.beforeFontGuid,
            rawMaterialGlobalId = row.beforeMaterialGlobalId,
            approvedObservationFontGuid = row.approvedObservationFontGuid,
            approvedObservationMaterialGlobalId =
                row.approvedObservationMaterialGlobalId,
            effectiveMaterialGlobalId = row.beforeEffectiveMaterialGlobalId,
            materialNormalizationReason = row.beforeMaterialNormalizationReason,
            savedFontOverride = row.beforeFontOverride,
            savedMaterialOverride = row.beforeMaterialOverride,
            loadedFontOverride = row.beforeLoadedFontOverride,
            loadedMaterialOverride = row.beforeLoadedMaterialOverride,
            migrationFontOverrideRequired = row.migrationFontOverrideRequired,
            migrationMaterialOverrideRequired = row.migrationMaterialOverrideRequired,
            savedReferenceOwnerIdentity = row.savedReferenceOwnerIdentity,
            savedReferenceAssetPath = row.savedReferenceAssetPath,
            savedReferenceAssetSha256 = row.savedReferenceAssetSha256,
            savedReferenceSourceAssetPath = row.savedReferenceSourceAssetPath,
            savedReferenceSourceAssetSha256 = row.savedReferenceSourceAssetSha256,
            savedReferenceProven = row.savedReferenceProven,
            savedReferenceAssetShaMatches = row.savedReferenceAssetShaMatches
        };
    }

    private static bool IsBefore(
        IDictionary<string, ShortCycleFontSwapTool.OfflineBinding> state,
        ShortCycleFontSwapTool.DryRunPlan plan)
    {
        return plan.rows.All(row =>
        {
            var current = state[row.rowIdentity];
            return current.currentFontGuid == row.beforeFontGuid &&
                current.currentMaterialGlobalId == row.beforeMaterialGlobalId &&
                current.effectiveMaterialGlobalId == row.beforeEffectiveMaterialGlobalId &&
                current.materialNormalizationReason ==
                    row.beforeMaterialNormalizationReason &&
                current.nonTargetFingerprint == row.nonTargetFingerprint &&
                current.fontOverride == row.beforeFontOverride &&
                current.materialOverride == row.beforeMaterialOverride;
        });
    }

    private static bool IsAfter(
        IDictionary<string, ShortCycleFontSwapTool.OfflineBinding> state,
        ShortCycleFontSwapTool.DryRunPlan plan)
    {
        return plan.rows.All(row =>
        {
            var current = state[row.rowIdentity];
            var directInstance = !row.coveredByPrefabSource &&
                !string.IsNullOrWhiteSpace(row.prefabSourceIdentity);
            return current.currentFontGuid == row.afterFontGuid &&
                current.currentMaterialPath == row.afterMaterialPath &&
                current.effectiveMaterialPath == row.afterMaterialPath &&
                string.IsNullOrEmpty(current.materialNormalizationReason) &&
                current.nonTargetFingerprint == row.nonTargetFingerprint &&
                current.fontOverride == (directInstance || row.beforeFontOverride) &&
                current.materialOverride == (directInstance || row.beforeMaterialOverride);
        });
    }

    public static int Main(string[] args)
    {
        try
        {
            Require(args.Length == 3,
                "Expected base CSV, supplemental CSV, and supplemental approval evidence paths.");
            var mappings = ShortCycleFontSwapTool.ParseApprovedCsvText(
                File.ReadAllText(args[0]));
            Require(mappings.Length == 171, "Approved raw mapping count must remain 171.");
            Require(mappings.Select(item => item.Identity).Distinct(StringComparer.Ordinal).Count() ==
                mappings.Length, "Approved row identities must be unique.");
            var supplementalText = File.ReadAllText(args[1]);
            var supplemental = ShortCycleFontSwapTool.ParseSupplementalApprovedCsvText(
                supplementalText);
            Require(supplemental.Length == 60,
                "Supplemental raw mapping count must remain 60.");
            ShortCycleFontSwapTool.ValidateSupplementalAuthorization(
                ShortCycleFontSwapTool.ApprovedMappingSha256,
                ShortCycleFontSwapTool.SupplementalMappingSha256,
                File.ReadAllText(args[2]));
            var authorizationBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ValidateSupplementalAuthorization(
                    ShortCycleFontSwapTool.ApprovedMappingSha256, "DRIFT",
                    File.ReadAllText(args[2]));
            }
            catch (InvalidOperationException) { authorizationBlocked = true; }
            Require(authorizationBlocked,
                "Supplemental authorization must reject a changed hash.");
            var pathDriftBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ParseSupplementalApprovedCsvText(
                    supplementalText.Replace(
                        "Assets/Scenes/ToolTests/FridgeScrollLab.unity",
                        "Assets/Scenes/ToolTests/Unauthorized.unity"));
            }
            catch (InvalidOperationException) { pathDriftBlocked = true; }
            Require(pathDriftBlocked,
                "Supplemental scope must reject unknown asset paths.");
            var missingRowBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ParseSupplementalApprovedCsvText(string.Join("\n",
                    supplementalText.Split(new[] { "\r\n", "\n" },
                        StringSplitOptions.None).Where(line => line.Length > 0).Take(60)));
            }
            catch (InvalidOperationException) { missingRowBlocked = true; }
            Require(missingRowBlocked,
                "Supplemental scope must reject a missing row.");

            const string hierarchy = "6:Spaces#0/5:Panel#1/4:Text#2";
            var rendererA = LocalComponent(hierarchy,
                "UnityEngine.CanvasRenderer, UnityEngine.UIModule", 1, 101);
            var rendererB = LocalComponent(hierarchy,
                "UnityEngine.CanvasRenderer, UnityEngine.UIModule", 1, 202);
            var stableReferenceA = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(
                FingerprintFixture(rendererA));
            var stableReferenceB = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(
                FingerprintFixture(rendererB));
            Require(stableReferenceA == stableReferenceB,
                "Transient object-reference instance ids must not affect the fingerprint.");

            var differentSceneInstance = ShortCycleFontSwapTool.
                ComputeStableNonTargetFingerprint(FingerprintFixture(LocalComponent(
                    "6:Spaces#0/5:Panel#2/4:Text#2",
                    "UnityEngine.CanvasRenderer, UnityEngine.UIModule", 1, 303)));
            var differentComponentIndex = ShortCycleFontSwapTool.
                ComputeStableNonTargetFingerprint(FingerprintFixture(LocalComponent(
                    hierarchy, "UnityEngine.CanvasRenderer, UnityEngine.UIModule", 2, 404)));
            var differentComponentType = ShortCycleFontSwapTool.
                ComputeStableNonTargetFingerprint(FingerprintFixture(LocalComponent(
                    hierarchy, "UnityEngine.Transform, UnityEngine.CoreModule", 1, 505)));
            Require(stableReferenceA != differentSceneInstance &&
                    stableReferenceA != differentComponentIndex &&
                    stableReferenceA != differentComponentType,
                "Different local instances, component slots, and component types must drift.");

            var persistentA = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(new[]
            {
                Reference("m_renderer", new ShortCycleFontSwapTool.
                    ObjectReferenceFingerprintDescriptor
                    {
                        identityKind = ShortCycleFontSwapTool.PersistentObjectReferenceKind,
                        persistentGlobalId =
                            "GlobalObjectId_V1-1-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-11-0"
                    })
            });
            var persistentB = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(new[]
            {
                Reference("m_renderer", new ShortCycleFontSwapTool.
                    ObjectReferenceFingerprintDescriptor
                    {
                        identityKind = ShortCycleFontSwapTool.PersistentObjectReferenceKind,
                        persistentGlobalId =
                            "GlobalObjectId_V1-1-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-11-0"
                    })
            });
            Require(persistentA != persistentB,
                "Different persistent resource identities must drift.");

            const string sceneGuid = "7a0c3d3c3003f5449819f0e57a50a528";
            var prefabReferenceA = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(
                new[] { Reference("m_PrefabInstance", PrefabInstance(
                    sceneGuid, 1661337093, 6578026039991287067, 101)) });
            var prefabReferenceB = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(
                new[] { Reference("m_PrefabInstance", PrefabInstance(
                    sceneGuid, 1661337093, 6578026039991287067, 202)) });
            Require(prefabReferenceA == prefabReferenceB,
                "Transient ids must not affect a valid internal PrefabInstance identity.");
            var differentPrefabInstance = ShortCycleFontSwapTool.
                ComputeStableNonTargetFingerprint(new[]
                {
                    Reference("m_PrefabInstance", PrefabInstance(
                        sceneGuid, 1935786674, 6578026039991287067, 303))
                });
            var differentPrefabAsset = ShortCycleFontSwapTool.
                ComputeStableNonTargetFingerprint(new[]
                {
                    Reference("m_PrefabInstance", PrefabInstance(
                        "8b1d4e4d4114a655a920a1f68b61b639", 1661337093,
                        6578026039991287067, 404))
                });
            Require(prefabReferenceA != differentPrefabInstance &&
                    prefabReferenceA != differentPrefabAsset,
                "Different Prefab instances and saved assets must drift.");

            var invalidInternalReferencesBlocked = new[]
            {
                PrefabInstance("00000000000000000000000000000000", 1661337093,
                    6578026039991287067, 1),
                PrefabInstance(sceneGuid, 0, 6578026039991287067, 2),
                PrefabInstance(sceneGuid, 1661337093, 6578026039991287067, 3),
                PrefabInstance(sceneGuid, 1661337093, 6578026039991287067, 4),
                PrefabInstance(sceneGuid, 1661337093, 6578026039991287067, 5)
            };
            invalidInternalReferencesBlocked[2].ownerGlobalId =
                "GlobalObjectId_V1-2-" + sceneGuid + "-6578026039991287067-1935786674";
            invalidInternalReferencesBlocked[3].ownerAssetGuid =
                "8b1d4e4d4114a655a920a1f68b61b639";
            invalidInternalReferencesBlocked[4].persistentGlobalId =
                "GlobalObjectId_V1-2-not-a-guid-1661337093-0";
            Require(invalidInternalReferencesBlocked.All(descriptor =>
            {
                try
                {
                    ShortCycleFontSwapTool.NormalizeObjectReferenceIdentity(descriptor);
                    return false;
                }
                catch (InvalidOperationException exception)
                {
                    return exception.Message.StartsWith(
                        "UNRESOLVED_OBJECT_REFERENCE|", StringComparison.Ordinal);
                }
            }), "Zero ids and mismatched Prefab ownership must fail closed.");
            var unknownInternalTypeBlocked = false;
            try
            {
                var unknown = PrefabInstance(sceneGuid, 1661337093,
                    6578026039991287067, 6);
                unknown.identityKind = "UNITY_INTERNAL_UNKNOWN";
                ShortCycleFontSwapTool.NormalizeObjectReferenceIdentity(unknown);
            }
            catch (InvalidOperationException exception)
            {
                unknownInternalTypeBlocked = exception.Message.StartsWith(
                    "UNRESOLVED_OBJECT_REFERENCE|", StringComparison.Ordinal);
            }
            Require(unknownInternalTypeBlocked,
                "Unknown Unity internal reference types must fail closed.");

            var nullReference = ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(new[]
            {
                Reference("m_renderer", new ShortCycleFontSwapTool.
                    ObjectReferenceFingerprintDescriptor { isNull = true })
            });
            Require(nullReference != persistentA,
                "Null and valid object references must remain distinct.");
            var unresolvedBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(new[]
                {
                    Reference("m_renderer", new ShortCycleFontSwapTool.
                        ObjectReferenceFingerprintDescriptor { unresolved = true })
                });
            }
            catch (InvalidOperationException exception)
            {
                unresolvedBlocked = exception.Message.StartsWith(
                    "UNRESOLVED_OBJECT_REFERENCE|", StringComparison.Ordinal);
            }
            Require(unresolvedBlocked,
                "Unresolved object references must fail closed.");

            var scalarVariants = new[]
            {
                FingerprintFixture(rendererA, fileId: "18"),
                FingerprintFixture(rendererA, text: "Changed"),
                FingerprintFixture(rendererA, color: "RGBA(0,1,1,1)"),
                FingerprintFixture(rendererA, fontSize: "37"),
                FingerprintFixture(rendererA, arrayValue: "beta")
            };
            Require(scalarVariants.All(properties =>
                    ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(properties) !=
                    stableReferenceA),
                "Normal m_FileID, text, color, font size, array, and nested data must drift.");
            var targetFixtureB = FingerprintFixture(rendererA);
            targetFixtureB[7] = Reference("m_fontAsset", new ShortCycleFontSwapTool.
                ObjectReferenceFingerprintDescriptor
                {
                    identityKind = ShortCycleFontSwapTool.PersistentObjectReferenceKind,
                    persistentGlobalId =
                        "GlobalObjectId_V1-1-cccccccccccccccccccccccccccccccc-2-0"
                });
            targetFixtureB[8] = Reference("m_sharedMaterial", new ShortCycleFontSwapTool.
                ObjectReferenceFingerprintDescriptor
                {
                    identityKind = ShortCycleFontSwapTool.PersistentObjectReferenceKind,
                    persistentGlobalId =
                        "GlobalObjectId_V1-1-dddddddddddddddddddddddddddddddd-3-0"
                });
            Require(ShortCycleFontSwapTool.ComputeStableNonTargetFingerprint(targetFixtureB) ==
                    stableReferenceA,
                "Only m_fontAsset and m_sharedMaterial references must be excluded.");

            var observed = mappings.Select(Observe).ToArray();
            var baseline = ShortCycleFontSwapTool.BuildDryRunPlan(mappings, observed);
            Require(baseline.executeAllowed, "Exact baseline should be executable.");
            Require(baseline.rawEntryCount == 171 && baseline.uniqueEntryCount == 171,
                "Baseline raw/unique counts are wrong.");
            Require(baseline.coveredApprovedRowCount == 171 && baseline.rows.Length == 171 &&
                    baseline.rows.Select(row => row.rowIdentity)
                        .Distinct(StringComparer.Ordinal).Count() == 171,
                "Every approved row must have one stable audit record.");

            var combinedMappings = mappings.Concat(supplemental).ToArray();
            var combinedObserved = combinedMappings.Select(Observe).ToArray();
            var combinedPlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                combinedMappings, combinedObserved, 231);
            Require(combinedPlan.executeAllowed && combinedPlan.rawEntryCount == 231 &&
                    combinedPlan.coveredApprovedRowCount == 231 &&
                    combinedPlan.rows.Length == 231 && combinedPlan.uniqueEntryCount == 231,
                "Combined plan must preserve all 231 approved rows in the synthetic baseline.");
            const string normalizedDefaultMaterial =
                "GlobalObjectId_V1-3-75909fc3d0fa2c94baa5706654d37655-" +
                "9052723641695353799-0";
            var normalizedMapping = mappings.Single(item => item.objectPath ==
                "Spaces/Phase1_Kitchen/Tray_Group/TraySlots/" +
                "TraySlot_01_TUNE/BadgeLabel");
            var normalizedObservation = new ShortCycleFontSwapTool.
                MaterialResolutionObservation
                {
                    rowIdentity = normalizedMapping.Identity,
                    approvedFontGuid = normalizedMapping.currentFontGuid,
                    approvedMaterialGlobalId = normalizedMapping.currentMaterialGlobalId,
                    savedFontGuid = normalizedMapping.currentFontGuid,
                    savedMaterialGlobalId = normalizedMapping.currentMaterialGlobalId,
                    effectiveMaterialGlobalId = normalizedDefaultMaterial,
                    fontDefaultMaterialGlobalId = normalizedDefaultMaterial,
                    savedReferenceProven = true,
                    savedAssetShaMatches = true,
                    expectedMaterialExists = true,
                    expectedMaterialAtlasMatchesFont = false,
                    effectiveEqualsFontDefault = true,
                    prefabSourceMatches = true,
                    savedMaterialOverride = false
                };
            var normalizedReason = ShortCycleFontSwapTool.ClassifyMaterialResolution(
                normalizedObservation);
            Require(normalizedReason == ShortCycleFontSwapTool.
                    TmpLoadMaterialNormalizationReason,
                "The exact proven TMP atlas mismatch must be classified narrowly.");
            var normalizedObserved = mappings.Select(Observe).ToArray();
            var normalizedBinding = normalizedObserved.Single(item =>
                item.rowIdentity == normalizedMapping.Identity);
            normalizedBinding.effectiveMaterialGlobalId = normalizedDefaultMaterial;
            normalizedBinding.effectiveMaterialPath = "font-default";
            normalizedBinding.materialNormalizationReason = normalizedReason;
            var normalizedPlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                mappings, normalizedObserved);
            Require(normalizedPlan.executeAllowed && normalizedPlan.rows.Length == 171 &&
                    normalizedPlan.rows.Single(row => row.rowIdentity ==
                        normalizedMapping.Identity).beforeMaterialGlobalId ==
                    normalizedMapping.currentMaterialGlobalId &&
                    normalizedPlan.rows.Single(row => row.rowIdentity ==
                        normalizedMapping.Identity).beforeEffectiveMaterialGlobalId ==
                    normalizedDefaultMaterial,
                "A proven normalization must retain distinct raw/effective evidence.");
            var normalizedState = StateFor(normalizedPlan);
            var normalizedApply = ShortCycleFontSwapTool.ApplyOfflinePlan(
                normalizedState, normalizedPlan, -1);
            Require(normalizedApply.writes == normalizedPlan.uniqueEntryCount &&
                    ShortCycleFontSwapTool.RollbackOfflinePlan(
                        normalizedState, normalizedPlan) == normalizedPlan.uniqueEntryCount &&
                    IsBefore(normalizedState, normalizedPlan) &&
                    ShortCycleFontSwapTool.RollbackOfflinePlan(
                        normalizedState, normalizedPlan) == 0,
                "Normalized before-state must survive full and idempotent rollback.");
            normalizedBinding.materialNormalizationReason = string.Empty;
            var unexplainedPlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                mappings, normalizedObserved);
            Require(!unexplainedPlan.executeAllowed && unexplainedPlan.reasons.Any(reason =>
                    reason.EndsWith("|UNEXPLAINED_EFFECTIVE_MATERIAL",
                        StringComparison.Ordinal)),
                "An unclassified effective mismatch must fail closed.");
            normalizedObservation.savedAssetShaMatches = false;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "Saved asset SHA drift must not be accepted as TMP normalization.");
            normalizedObservation.savedAssetShaMatches = true;
            normalizedObservation.savedMaterialOverride = true;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "An explicit material override must not be accepted as inherited normalization.");
            normalizedObservation.savedMaterialOverride = false;
            normalizedObservation.savedReferenceProven = false;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "An unproven saved reference must fail closed.");
            normalizedObservation.savedReferenceProven = true;
            normalizedObservation.expectedMaterialExists = false;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "A missing approved material must fail closed.");
            normalizedObservation.expectedMaterialExists = true;
            normalizedObservation.expectedMaterialAtlasMatchesFont = true;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "A matching approved atlas cannot explain TMP normalization.");
            normalizedObservation.expectedMaterialAtlasMatchesFont = false;
            normalizedObservation.prefabSourceMatches = false;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "An unexpected source relationship must fail closed.");
            normalizedObservation.prefabSourceMatches = true;
            normalizedObservation.savedFontGuid = "wrong-font";
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "A wrong saved font must fail closed.");
            normalizedObservation.savedFontGuid = normalizedMapping.currentFontGuid;
            normalizedObservation.savedMaterialGlobalId =
                "GlobalObjectId_V1-3-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-1-0";
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "A wrong saved material must fail closed.");
            normalizedObservation.savedMaterialGlobalId =
                normalizedMapping.currentMaterialGlobalId;
            normalizedObservation.effectiveMaterialGlobalId =
                "GlobalObjectId_V1-3-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-1-0";
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    normalizedObservation) == string.Empty,
                "A third-party effective material must fail closed.");
            Require(ShortCycleFontSwapTool.CanonicalizeSavedGlobalObjectId(
                    normalizedMapping.currentMaterialGlobalId) ==
                    normalizedMapping.currentMaterialGlobalId &&
                ShortCycleFontSwapTool.CanonicalizeSavedGlobalObjectId(
                    "GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-" +
                    "-5877652104868683785-0") ==
                    normalizedMapping.currentMaterialGlobalId,
                "Signed YAML fileID and unsigned GlobalObjectId forms must canonicalize losslessly.");

            var approvedLoadMapping = mappings.Single(item => item.assetPath.EndsWith(
                    "/CatalogCard.prefab", StringComparison.Ordinal) &&
                item.objectPath == "CatalogCard/BadgeLabel");
            const string sourceHanRawMaterial =
                "GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-" +
                "12569091968840867831-0";
            var approvedLoadEvidence = new ShortCycleFontSwapTool.
                MaterialResolutionObservation
                {
                    rowIdentity = approvedLoadMapping.Identity,
                    approvedFontGuid = approvedLoadMapping.currentFontGuid,
                    approvedMaterialGlobalId = approvedLoadMapping.currentMaterialGlobalId,
                    savedFontGuid = approvedLoadMapping.currentFontGuid,
                    savedMaterialGlobalId = sourceHanRawMaterial,
                    effectiveMaterialGlobalId = normalizedDefaultMaterial,
                    fontDefaultMaterialGlobalId = normalizedDefaultMaterial,
                    savedReferenceProven = true,
                    savedAssetShaMatches = true,
                    expectedMaterialExists = true,
                    expectedMaterialAtlasMatchesFont = false,
                    effectiveEqualsFontDefault = true,
                    prefabSourceMatches = false,
                    savedMaterialOverride = false
                };
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    approvedLoadEvidence) == ShortCycleFontSwapTool.
                    ApprovedLoadObservationReason,
                "The exact eight-row approved load observation must be distinguished from raw.");
            var approvedLoadBindings = mappings.Select(Observe).ToArray();
            var approvedLoadBinding = approvedLoadBindings.Single(item =>
                item.rowIdentity == approvedLoadMapping.Identity);
            approvedLoadBinding.currentMaterialGlobalId = sourceHanRawMaterial;
            approvedLoadBinding.currentMaterialPath = "saved-source-han";
            approvedLoadBinding.effectiveMaterialGlobalId = normalizedDefaultMaterial;
            approvedLoadBinding.effectiveMaterialPath = "loaded-font-default";
            approvedLoadBinding.materialNormalizationReason =
                ShortCycleFontSwapTool.ApprovedLoadObservationReason;
            approvedLoadBinding.loadedMaterialOverride = true;
            var approvedLoadPlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                mappings, approvedLoadBindings);
            var approvedLoadRow = approvedLoadPlan.rows.Single(row =>
                row.rowIdentity == approvedLoadMapping.Identity);
            Require(approvedLoadPlan.executeAllowed &&
                    approvedLoadRow.approvedObservationMaterialGlobalId ==
                        normalizedDefaultMaterial &&
                    approvedLoadRow.beforeMaterialGlobalId == sourceHanRawMaterial &&
                    approvedLoadRow.beforeEffectiveMaterialGlobalId ==
                        normalizedDefaultMaterial &&
                    approvedLoadRow.beforeLoadedMaterialOverride &&
                    approvedLoadRow.afterMaterialPath.EndsWith(
                        "ShortCycle_Static_FromLegacyShortCycle.mat",
                        StringComparison.Ordinal),
                "Approved observation, saved raw, loaded effective, and legacy target must remain independent.");
            approvedLoadEvidence.savedMaterialGlobalId =
                "GlobalObjectId_V1-3-ce30f77e3b8c0ad45b7b7ae26b956bb6-" +
                "18028800558602920526-0";
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    approvedLoadEvidence) == string.Empty,
                "The exact source-row raw material identity must fail closed on drift.");
            approvedLoadEvidence.savedMaterialGlobalId = sourceHanRawMaterial;
            approvedLoadEvidence.savedMaterialOverride = true;
            Require(ShortCycleFontSwapTool.ClassifyMaterialResolution(
                    approvedLoadEvidence) == string.Empty,
                "A newly saved source-row override must fail closed.");

            const string heavyRawMaterial =
                "GlobalObjectId_V1-3-ce30f77e3b8c0ad45b7b7ae26b956bb6-" +
                "18028800558602920526-0";
            var triStateObserved = combinedMappings.Select(Observe).ToArray();
            var normalizedSceneRows = triStateObserved.Where(item =>
                    item.writeAssetPath ==
                        "Assets/Scenes/Cooking/ShortCycle_P0P1.unity" &&
                    item.writeObjectPath.StartsWith(
                        "Spaces/Phase1_Kitchen/Tray_Group/TraySlots/TraySlot_",
                        StringComparison.Ordinal) &&
                    (item.writeObjectPath.EndsWith("/BadgeLabel",
                         StringComparison.Ordinal) ||
                     item.writeObjectPath.EndsWith("/NameLabel",
                         StringComparison.Ordinal)))
                .ToArray();
            Require(normalizedSceneRows.Length == 40,
                "The tri-state fixture must identify exactly 40 approved scene rows.");
            foreach (var item in normalizedSceneRows)
            {
                item.effectiveMaterialGlobalId = normalizedDefaultMaterial;
                item.effectiveMaterialPath = "loaded-font-default";
                item.materialNormalizationReason = ShortCycleFontSwapTool.
                    TmpLoadMaterialNormalizationReason;
            }
            var approvedSourceRows = triStateObserved.Where(item =>
                    item.writeAssetPath.StartsWith(
                        "Assets/Prefabs/Cooking/ShortCycle/GridPieces/",
                        StringComparison.Ordinal) &&
                    (item.writeObjectPath.EndsWith("/BadgeLabel",
                         StringComparison.Ordinal) ||
                     item.writeObjectPath.EndsWith("/NameLabel",
                         StringComparison.Ordinal)) &&
                    (item.writeAssetPath.EndsWith("/CatalogCard.prefab",
                         StringComparison.Ordinal) ||
                     item.writeAssetPath.EndsWith("/IngredientCard.prefab",
                         StringComparison.Ordinal) ||
                     item.writeAssetPath.EndsWith("/TraySlot.prefab",
                         StringComparison.Ordinal) ||
                     item.writeAssetPath.EndsWith("/Sticker.prefab",
                         StringComparison.Ordinal)))
                .ToArray();
            Require(approvedSourceRows.Length == 8,
                "The tri-state fixture must identify exactly eight approved source rows.");
            foreach (var item in approvedSourceRows)
            {
                item.currentMaterialGlobalId = item.writeAssetPath.EndsWith(
                        "/Sticker.prefab", StringComparison.Ordinal)
                    ? heavyRawMaterial
                    : sourceHanRawMaterial;
                item.currentMaterialPath = "saved-raw-material";
                item.effectiveMaterialGlobalId = normalizedDefaultMaterial;
                item.effectiveMaterialPath = "loaded-font-default";
                item.materialNormalizationReason = ShortCycleFontSwapTool.
                    ApprovedLoadObservationReason;
                item.loadedMaterialOverride = true;
            }
            var triStatePlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                combinedMappings, triStateObserved, 231);
            Require(triStatePlan.executeAllowed &&
                    triStatePlan.rows.Count(row => row.beforeMaterialNormalizationReason ==
                        ShortCycleFontSwapTool.TmpLoadMaterialNormalizationReason) == 40 &&
                    triStatePlan.rows.Count(row => row.beforeMaterialNormalizationReason ==
                        ShortCycleFontSwapTool.ApprovedLoadObservationReason) == 8 &&
                    triStatePlan.rows.Count(row => string.IsNullOrEmpty(
                        row.beforeMaterialNormalizationReason)) == 183,
                "The shared tri-state plan must retain 40 scene, eight source, and 183 normal rows.");
            var matchingDiagnostic = triStatePlan.rows.Select(DiagnosticFor).ToArray();
            Require(ShortCycleFontSwapTool.FindFingerprintDiagnosticPlanMismatches(
                    triStatePlan.rows, matchingDiagnostic).Length == 0,
                "A diagnostic pass that exactly matches the plan must be accepted.");
            var identicallyWrongDiagnostic = triStatePlan.rows.Select(DiagnosticFor).ToArray();
            foreach (var item in identicallyWrongDiagnostic.Where(item =>
                         item.materialNormalizationReason == ShortCycleFontSwapTool.
                             ApprovedLoadObservationReason))
            {
                item.approvedObservationMaterialGlobalId = item.rawMaterialGlobalId;
                item.materialNormalizationReason = string.Empty;
            }
            var wrongFirst = ShortCycleFontSwapTool.
                FindFingerprintDiagnosticPlanMismatches(
                    triStatePlan.rows, identicallyWrongDiagnostic);
            var wrongSecond = ShortCycleFontSwapTool.
                FindFingerprintDiagnosticPlanMismatches(
                    triStatePlan.rows, identicallyWrongDiagnostic);
            Require(wrongFirst.Length == 8 && wrongSecond.Length == 8 &&
                    wrongFirst.SequenceEqual(wrongSecond, StringComparer.Ordinal),
                "Two identical but plan-inconsistent passes must reject all eight source rows.");

            const string ownerGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            const string sourceGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            const string materialGuid = "cccccccccccccccccccccccccccccccc";
            const string ownerGlobal =
                "GlobalObjectId_V1-2-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-123-900";
            const string sourceGlobal =
                "GlobalObjectId_V1-1-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-123-0";
            var inheritedYaml = "--- !u!1001 &900\nPrefabInstance:\n" +
                "  m_Modification:\n    m_Modifications: []\n" +
                "  m_SourcePrefab: {fileID: 100100000, guid: " + sourceGuid +
                ", type: 3}\n";
            var inheritedSaved = ShortCycleFontSwapTool.ParseSavedPrefabOverrides(
                inheritedYaml, ownerGuid, ownerGlobal, sourceGlobal);
            Require(inheritedSaved.relationshipProven && !inheritedSaved.fontOverride &&
                    !inheritedSaved.materialOverride,
                "Disk absence must prove inheritance even when a loaded API reports a derived override.");
            var overrideBlock =
                "    - target: {fileID: 123, guid: " + sourceGuid + ", type: 3}\n" +
                "      propertyPath: m_sharedMaterial\n      value: \n" +
                "      objectReference: {fileID: -7, guid: " + materialGuid +
                ", type: 2}\n";
            var explicitYaml = "--- !u!1001 &900\nPrefabInstance:\n" +
                "  m_Modification:\n    m_Modifications:\n" + overrideBlock +
                "  m_SourcePrefab: {fileID: 100100000, guid: " + sourceGuid +
                ", type: 3}\n";
            var explicitSaved = ShortCycleFontSwapTool.ParseSavedPrefabOverrides(
                explicitYaml, ownerGuid, ownerGlobal, sourceGlobal);
            Require(explicitSaved.materialOverride &&
                    explicitSaved.materialGlobalId.EndsWith(
                        "-18446744073709551609-0", StringComparison.Ordinal),
                "The disk parser must retain signed YAML fileID bits losslessly.");
            var ambiguousSavedBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ParseSavedPrefabOverrides(
                    explicitYaml.Replace(overrideBlock, overrideBlock + overrideBlock),
                    ownerGuid, ownerGlobal, sourceGlobal);
            }
            catch (InvalidOperationException) { ambiguousSavedBlocked = true; }
            Require(ambiguousSavedBlocked,
                "Duplicate exact saved overrides must be rejected as ambiguous.");
            var baselineDriftBindings = mappings.Select(Observe).ToArray();
            baselineDriftBindings[0].savedReferenceAssetShaMatches = false;
            Require(!ShortCycleFontSwapTool.BuildDryRunPlan(mappings,
                    baselineDriftBindings).executeAllowed,
                "A saved asset that no longer matches its approved baseline must be rejected.");
            var combinedAssets = combinedMappings.Select(item => item.assetPath)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            var backupPaths = ShortCycleFontSwapTool.GetApprovedBackupAssetPaths(
                new ShortCycleFontSwapTool.ApprovedMigrationPlan
                {
                    approvedRawEntryCount = 231,
                    approvedAssetPaths = combinedAssets,
                    rows = combinedPlan.rows
                });
            Require(backupPaths.Length == 7 &&
                    backupPaths.Contains(
                        "Assets/Scenes/ToolTests/FridgeScrollLab.unity",
                        StringComparer.Ordinal),
                "Combined transaction must back up seven approved resources.");

            var sharedAsset =
                "Assets/Scenes/ToolTests/FridgeScrollLab.unity";
            var sharedEntries = triStatePlan.entries.Where(entry =>
                    entry.writeAssetPath == sharedAsset)
                .Take(2).ToArray();
            Require(sharedEntries.Length == 2 &&
                    sharedEntries[0].canonicalIdentity !=
                        sharedEntries[1].canonicalIdentity,
                "The stage fixture requires two real approved identities in one file.");
            var selectedPrefabEntry = triStatePlan.entries.First(entry =>
                entry.writeAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));
            var unselectedEntry = triStatePlan.entries.First(entry =>
                !string.Equals(entry.writeAssetPath, sharedAsset, StringComparison.Ordinal) &&
                !string.Equals(entry.writeAssetPath, selectedPrefabEntry.writeAssetPath,
                    StringComparison.Ordinal));
            var policyEntries = sharedEntries.Concat(new[]
                { selectedPrefabEntry, unselectedEntry }).ToArray();
            var materialPaths = new[]
            {
                "Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat",
                "Assets/Font/Heavy/Materials/ShortCycle_Static_FromLegacyShortCycle.mat",
                "Assets/Font/Heavy/Materials/ShortCycle_Static_FromHeavy.mat"
            };
            var policyMaterials = materialPaths.Select((path, index) =>
                new ShortCycleFontSwapTool.MaterialPlan
                {
                    targetMaterialPath = path,
                    expectedMaterialFingerprint = "MATERIAL-FINGERPRINT-" + index
                }).ToArray();
            var preStageList = new List<ShortCycleFontSwapTool.FileSnapshot>
            {
                new ShortCycleFontSwapTool.FileSnapshot
                    { path = "approved-input.csv", exists = true, sha256 = "INPUT" },
                new ShortCycleFontSwapTool.FileSnapshot
                {
                    path = ShortCycleFontSwapTool.ApprovedMaterialFolderPath + ".meta",
                    exists = false, sha256 = string.Empty
                }
            };
            foreach (var assetPath in policyEntries.Select(entry => entry.writeAssetPath)
                         .Distinct(StringComparer.Ordinal))
            {
                preStageList.Add(new ShortCycleFontSwapTool.FileSnapshot
                    { path = assetPath, exists = true, sha256 = "ASSET|" + assetPath });
                preStageList.Add(new ShortCycleFontSwapTool.FileSnapshot
                    { path = assetPath + ".meta", exists = true, sha256 = "META|" + assetPath });
            }
            foreach (var materialPath in materialPaths)
            {
                preStageList.Add(new ShortCycleFontSwapTool.FileSnapshot
                    { path = materialPath, exists = false, sha256 = string.Empty });
                preStageList.Add(new ShortCycleFontSwapTool.FileSnapshot
                    { path = materialPath + ".meta", exists = false, sha256 = string.Empty });
            }
            var preStage = preStageList.OrderBy(snapshot => snapshot.path,
                StringComparer.Ordinal).ToArray();
            Func<ShortCycleFontSwapTool.FileSnapshot[],
                ShortCycleFontSwapTool.FileSnapshot[]> cloneStage = snapshots =>
                    snapshots.Select(snapshot => new ShortCycleFontSwapTool.FileSnapshot
                    {
                        path = snapshot.path,
                        exists = snapshot.exists,
                        sha256 = snapshot.sha256
                    }).ToArray();
            var policyTransaction = new ShortCycleFontSwapTool.TransactionRecord
            {
                state = "PLANNED",
                mappingPath = "approved-input.csv",
                approvedAssetPaths = policyEntries.Select(entry => entry.writeAssetPath)
                    .Distinct(StringComparer.Ordinal).ToArray(),
                entries = policyEntries,
                materials = policyMaterials,
                expectedFiles = cloneStage(preStage),
                materialFolderExistedAtBegin = false,
                ownedMaterialFolderPath = ShortCycleFontSwapTool.ApprovedMaterialFolderPath
            };
            var selectedPolicyIdentities = new[]
            {
                sharedEntries[0].canonicalIdentity,
                selectedPrefabEntry.canonicalIdentity
            };
            var existingBatch = ShortCycleFontSwapTool.BuildApplyingBatchRecord(
                policyTransaction, "PRODUCTION-POLICY", selectedPolicyIdentities, false);
            policyTransaction.state = "APPLYING";
            var expectedWriteStage = cloneStage(preStage);
            foreach (var snapshot in expectedWriteStage.Where(snapshot =>
                         snapshot.path == sharedAsset ||
                         snapshot.path == selectedPrefabEntry.writeAssetPath))
                snapshot.sha256 = "EXPECTED-WRITE|" + snapshot.path;
            var stageAccepted = ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                policyTransaction, existingBatch, expectedWriteStage);
            Require(stageAccepted.success && stageAccepted.changedPaths.OrderBy(value => value,
                        StringComparer.Ordinal).SequenceEqual(new[]
                    {
                        selectedPrefabEntry.writeAssetPath, sharedAsset
                    }.OrderBy(value => value, StringComparer.Ordinal),
                    StringComparer.Ordinal),
                "The production policy must allow only selected existing asset content writes.");
            var sameFileState = StateFor(triStatePlan);
            var sameFileSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(
                triStatePlan);
            ShortCycleFontSwapTool.ApplyOfflineBatch(sameFileState, sameFileSession,
                "SAME-FILE-ONE", new[] { sharedEntries[0].canonicalIdentity }, -1);
            var pendingSameFileRows = triStatePlan.rows.Where(row =>
                    row.canonicalIdentity == sharedEntries[1].canonicalIdentity)
                .ToArray();
            Require(pendingSameFileRows.Length > 0 && pendingSameFileRows.All(row =>
                    sameFileState[row.rowIdentity].currentFontGuid == row.beforeFontGuid &&
                    sameFileState[row.rowIdentity].currentMaterialGlobalId ==
                        row.beforeMaterialGlobalId),
                "The second identity in the changed file must remain a valid pending row.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(
                        sameFileState, sameFileSession) > 0 &&
                    IsBefore(sameFileState, triStatePlan) &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(
                        sameFileState, sameFileSession) == 0,
                "The owned same-file sample must restore completely and idempotently.");

            var sceneMetaTamper = cloneStage(expectedWriteStage);
            sceneMetaTamper.Single(snapshot => snapshot.path == sharedAsset + ".meta")
                .sha256 = "SCENE-META-TAMPER";
            var prefabMetaTamper = cloneStage(expectedWriteStage);
            prefabMetaTamper.Single(snapshot => snapshot.path ==
                selectedPrefabEntry.writeAssetPath + ".meta").sha256 =
                    "PREFAB-META-TAMPER";
            var deletedMeta = cloneStage(expectedWriteStage);
            var deletedMetaSnapshot = deletedMeta.Single(snapshot =>
                snapshot.path == sharedAsset + ".meta");
            deletedMetaSnapshot.exists = false;
            deletedMetaSnapshot.sha256 = string.Empty;
            var externalTamper = expectedWriteStage.Select(snapshot =>
                    new ShortCycleFontSwapTool.FileSnapshot
                    {
                        path = snapshot.path,
                        exists = snapshot.exists,
                        sha256 = snapshot.path == "approved-input.csv"
                            ? "INPUT-TAMPER"
                            : snapshot.sha256
                    }).ToArray();
            var unselectedTamper = cloneStage(expectedWriteStage);
            unselectedTamper.Single(snapshot => snapshot.path ==
                unselectedEntry.writeAssetPath).sha256 = "UNSELECTED-TAMPER";
            Require(!ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                        policyTransaction, existingBatch, sceneMetaTamper).success &&
                    !ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                        policyTransaction, existingBatch, prefabMetaTamper).success &&
                    !ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                        policyTransaction, existingBatch, deletedMeta).success &&
                    !ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                        policyTransaction, existingBatch, externalTamper).success &&
                    !ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                        policyTransaction, existingBatch, unselectedTamper).success,
                "Production policy must reject scene/prefab meta, input, and unselected asset drift.");

            policyTransaction.state = "PLANNED";
            policyTransaction.expectedFiles = cloneStage(preStage);
            var creationBatch = ShortCycleFontSwapTool.BuildApplyingBatchRecord(
                policyTransaction, "CREATE-MATERIALS", selectedPolicyIdentities, true);
            policyTransaction.state = "APPLYING";
            var createdStage = cloneStage(expectedWriteStage);
            foreach (var snapshot in createdStage.Where(snapshot =>
                         materialPaths.Contains(snapshot.path, StringComparer.Ordinal) ||
                         materialPaths.Select(path => path + ".meta").Contains(
                             snapshot.path, StringComparer.Ordinal) ||
                         snapshot.path == ShortCycleFontSwapTool.ApprovedMaterialFolderPath +
                            ".meta"))
            {
                snapshot.exists = true;
                snapshot.sha256 = "CREATED|" + snapshot.path;
            }
            Require(ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                    policyTransaction, creationBatch, createdStage).success,
                "Exact first-batch material, meta, and owned-folder creation must be allowed.");
            var partialCreation = cloneStage(expectedWriteStage);
            foreach (var path in new[]
                     {
                         materialPaths[0], materialPaths[0] + ".meta",
                         ShortCycleFontSwapTool.ApprovedMaterialFolderPath + ".meta"
                     })
            {
                var snapshot = partialCreation.Single(item => item.path == path);
                snapshot.exists = true;
                snapshot.sha256 = "PARTIAL|" + path;
            }
            Require(ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                    policyTransaction, creationBatch, partialCreation).success,
                "An APPLYING rollback audit must recognize only a partial owned creation.");
            var originalStageContract = creationBatch.stageContractSha256;
            creationBatch.stageContractSha256 = "TAMPERED-CONTRACT";
            Require(!ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                    policyTransaction, creationBatch, partialCreation).success,
                "APPLYING journal tampering must fail before rollback restoration.");
            creationBatch.stageContractSha256 = originalStageContract;

            var sameNamePreStage = cloneStage(preStage);
            foreach (var path in new[] { materialPaths[0], materialPaths[0] + ".meta" })
            {
                var snapshot = sameNamePreStage.Single(item => item.path == path);
                snapshot.exists = true;
                snapshot.sha256 = "PREEXISTING|" + path;
            }
            policyTransaction.state = "PLANNED";
            policyTransaction.expectedFiles = sameNamePreStage;
            var sameNameRejected = false;
            try
            {
                ShortCycleFontSwapTool.BuildApplyingBatchRecord(policyTransaction,
                    "SAME-NAME", selectedPolicyIdentities, true);
            }
            catch (InvalidOperationException) { sameNameRejected = true; }
            Require(sameNameRejected,
                "A same-name existing material/meta pair must block first creation.");

            policyTransaction.state = "PLANNED";
            policyTransaction.expectedFiles = cloneStage(createdStage);
            var subsequentBatch = ShortCycleFontSwapTool.BuildApplyingBatchRecord(
                policyTransaction, "SUBSEQUENT", selectedPolicyIdentities, false);
            policyTransaction.state = "APPLYING";
            var subsequentMetaTamper = cloneStage(createdStage);
            subsequentMetaTamper.Single(snapshot => snapshot.path ==
                materialPaths[0] + ".meta").sha256 = "REPLACED-MATERIAL-META";
            Require(!ShortCycleFontSwapTool.EvaluateApplyingStageSnapshot(
                    policyTransaction, subsequentBatch, subsequentMetaTamper).success,
                "A later batch must not reuse the first-creation allowance for material metadata.");

            var approvedBackup = Enumerable.Range(0, 14).Select(index =>
                new ShortCycleFontSwapTool.FileSnapshot
                {
                    path = "Assets/Approved/asset-" + index,
                    exists = true,
                    sha256 = "ORIGINAL-" + index
                }).ToArray();
            Func<ShortCycleFontSwapTool.FileSnapshot[]> cloneBackup = () =>
                approvedBackup.Select(snapshot => new ShortCycleFontSwapTool.FileSnapshot
                {
                    path = snapshot.path,
                    exists = snapshot.exists,
                    sha256 = snapshot.sha256
                }).ToArray();
            Require(ShortCycleFontSwapTool.EvaluateBackupSnapshotContract(
                    approvedBackup, cloneBackup(), cloneBackup(), cloneBackup()).success,
                "The fourteen-file backup contract must accept an exact original copy.");
            var tamperedBackup = cloneBackup();
            tamperedBackup[3].sha256 = "TAMPERED";
            var outOfScopeBackup = cloneBackup().Concat(new[]
            {
                new ShortCycleFontSwapTool.FileSnapshot
                    { path = "Assets/Outside.asset", exists = true, sha256 = "OUTSIDE" }
            }).ToArray();
            Require(!ShortCycleFontSwapTool.EvaluateBackupSnapshotContract(
                        approvedBackup, cloneBackup(), cloneBackup(), tamperedBackup).success &&
                    !ShortCycleFontSwapTool.EvaluateBackupSnapshotContract(
                        approvedBackup, cloneBackup(), outOfScopeBackup,
                        cloneBackup()).success,
                "Tampered and out-of-scope backup evidence must fail before restoration.");

            var catalogPrefab = mappings.Single(item =>
                item.assetPath.EndsWith("/CatalogCard.prefab", StringComparison.Ordinal) &&
                item.objectPath.EndsWith("/BadgeLabel", StringComparison.Ordinal));
            var catalogScene = mappings.First(item =>
                item.assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                item.objectPath.EndsWith("CatalogCard_01/BadgeLabel", StringComparison.Ordinal));
            Require(catalogScene.currentMaterialGlobalId != catalogPrefab.currentMaterialGlobalId &&
                    catalogScene.proposedMaterialPath != catalogPrefab.proposedMaterialPath,
                "Catalog source/instance fixture must preserve distinct style semantics.");
            var distinctObserved = mappings.Select(Observe).ToArray();
            var distinctInstance = distinctObserved.Single(item =>
                item.rowIdentity == catalogScene.Identity);
            distinctInstance.prefabSourceIdentity = catalogPrefab.Identity;
            distinctInstance.isPrefabInstance = true;
            var distinctPlan = ShortCycleFontSwapTool.BuildDryRunPlan(mappings, distinctObserved);
            Require(distinctPlan.executeAllowed && distinctPlan.uniqueEntryCount == 171 &&
                    distinctPlan.entries.Any(entry =>
                        entry.canonicalIdentity == catalogScene.Identity) &&
                    distinctPlan.rows.Single(row => row.rowIdentity == catalogScene.Identity)
                        .canonicalIdentity == catalogScene.Identity,
                "Different source/instance before+after semantics must remain independent writes.");

            var fridgePrefab = mappings.Single(item =>
                item.assetPath.EndsWith("/FridgeSlot.prefab", StringComparison.Ordinal) &&
                item.objectPath.EndsWith("/BadgeLabel", StringComparison.Ordinal));
            var fridgeScene = mappings.First(item =>
                item.assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                item.objectPath.EndsWith("FridgeSlot_R01_C01_TUNE/BadgeLabel",
                    StringComparison.Ordinal));
            Require(fridgeScene.currentFontGuid == fridgePrefab.currentFontGuid &&
                    fridgeScene.currentMaterialGlobalId == fridgePrefab.currentMaterialGlobalId &&
                    fridgeScene.proposedMaterialPath == fridgePrefab.proposedMaterialPath,
                "Fridge source/instance fixture must be semantically equivalent.");
            var dedupObserved = mappings.Select(Observe).ToArray();
            var inheritedInstance = dedupObserved.Single(item =>
                item.rowIdentity == fridgeScene.Identity);
            inheritedInstance.prefabSourceIdentity = fridgePrefab.Identity;
            inheritedInstance.isPrefabInstance = true;
            var dedup = ShortCycleFontSwapTool.BuildDryRunPlan(mappings, dedupObserved);
            Require(dedup.executeAllowed && dedup.uniqueEntryCount == 170 &&
                    dedup.coveredApprovedRowCount == 171,
                "Equivalent inherited source/instance rows must deduplicate without losing audit coverage.");
            Require(dedup.entries.Single(entry => entry.canonicalIdentity == fridgePrefab.Identity)
                    .approvedRowIdentities.Length == 2,
                "Deduplicated entry must retain both approved row identities.");
            Require(dedup.rows.Single(row => row.rowIdentity == fridgeScene.Identity)
                    .coveredByPrefabSource,
                "Inherited row must explicitly record source propagation coverage.");

            var overrideObserved = mappings.Select(Observe).ToArray();
            var overriddenInstance = overrideObserved.Single(item =>
                item.rowIdentity == fridgeScene.Identity);
            overriddenInstance.prefabSourceIdentity = fridgePrefab.Identity;
            overriddenInstance.isPrefabInstance = true;
            overriddenInstance.materialOverride = true;
            var overridePlan = ShortCycleFontSwapTool.BuildDryRunPlan(
                mappings, overrideObserved);
            Require(overridePlan.executeAllowed && overridePlan.uniqueEntryCount == 171 &&
                    !overridePlan.rows.Single(row => row.rowIdentity == fridgeScene.Identity)
                        .coveredByPrefabSource,
                "A real font/material override must preserve the instance write identity.");

            var approvedIdentitySet = new HashSet<string>(
                mappings.Select(mapping => mapping.Identity), StringComparer.Ordinal);
            var unapprovedRow = Observe(fridgeScene);
            unapprovedRow.rowIdentity =
                "Assets/Scenes/Cooking/Unapproved.unity|Unapproved/BadgeLabel|0";
            unapprovedRow.prefabSourceIdentity = fridgePrefab.Identity;
            unapprovedRow.isPrefabInstance = true;
            unapprovedRow.fontOverride = false;
            unapprovedRow.materialOverride = true;
            var propagationReasons = ShortCycleFontSwapTool.FindUnapprovedPrefabPropagation(
                new[] { inheritedInstance, unapprovedRow }, approvedIdentitySet);
            Require(propagationReasons.Length == 1 && propagationReasons[0] ==
                    "UNAPPROVED_PREFAB_PROPAGATION|" + fridgePrefab.Identity + "|" +
                    unapprovedRow.rowIdentity + "|m_fontAsset",
                "Unapproved source propagation must stop with the exact source, row, and fields.");

            var labApproved = combinedObserved.First(item =>
                item.rowIdentity.StartsWith(
                    "Assets/Scenes/ToolTests/FridgeScrollLab.unity|",
                    StringComparison.Ordinal));
            var labOutside = labApproved.Clone();
            labOutside.rowIdentity =
                "Assets/Scenes/ToolTests/FridgeScrollLab.unity|Outside/BadgeLabel|0";
            labOutside.canonicalIdentity = labOutside.rowIdentity;
            var combinedApprovedIdentitySet = new HashSet<string>(
                combinedMappings.Select(mapping => mapping.Identity), StringComparer.Ordinal);
            var combinedPropagationReasons =
                ShortCycleFontSwapTool.FindUnapprovedPrefabPropagation(
                    new[] { labApproved, labOutside }, combinedApprovedIdentitySet);
            Require(combinedPropagationReasons.Length == 1 &&
                    combinedPropagationReasons[0] ==
                        "UNAPPROVED_PREFAB_PROPAGATION|" +
                        labOutside.prefabSourceIdentity + "|" + labOutside.rowIdentity +
                        "|m_fontAsset",
                "The approved Lab row must pass while one extra inherited row remains blocked.");

            var supplementalOverrideObserved = combinedMappings.Select(Observe).ToArray();
            supplementalOverrideObserved.Single(item => item.rowIdentity == labApproved.rowIdentity)
                .materialOverride = false;
            var supplementalOverrideDrift = ShortCycleFontSwapTool.BuildDryRunPlan(
                combinedMappings, supplementalOverrideObserved, 231);
            Require(!supplementalOverrideDrift.executeAllowed &&
                    supplementalOverrideDrift.reasons.Any(reason =>
                        reason == "SOURCE_DRIFT|" + labApproved.rowIdentity +
                            "|MATERIAL_OVERRIDE"),
                "The approved Lab material override shape must be an exact preflight contract.");

            var conflictObserved = mappings.Select(Observe).ToArray();
            conflictObserved.Single(item => item.rowIdentity == catalogScene.Identity)
                .canonicalIdentity = catalogPrefab.Identity;
            var physicalConflict = ShortCycleFontSwapTool.BuildDryRunPlan(
                mappings, conflictObserved);
            Require(!physicalConflict.executeAllowed &&
                    physicalConflict.conflictingCanonicalCount == 1 &&
                    physicalConflict.reasons.Any(reason => reason ==
                        "CANONICAL_CONFLICT|" + catalogPrefab.Identity),
                "Mutually exclusive targets for one real write location must remain rejected.");

            var driftObserved = mappings.Select(Observe).ToArray();
            driftObserved[0].currentFontGuid = "DRIFT";
            var drift = ShortCycleFontSwapTool.BuildDryRunPlan(mappings, driftObserved);
            Require(!drift.executeAllowed && drift.sourceDriftCount == 1 &&
                    drift.reasons.Any(reason => reason.StartsWith("SOURCE_DRIFT|",
                        StringComparison.Ordinal)),
                "Source drift must be itemized and refuse execution.");

            var sourceStyle = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "_FaceColor", "red" },
                { "_OutlineWidth", "0.25" },
                { "_MainTex", "old-atlas" },
                { "_TextureWidth", "512" },
                { "_TextureHeight", "512" },
                { "_GradientScale", "9" }
            };
            var targetAtlas = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "_MainTex", "static-atlas" },
                { "_TextureWidth", "2048" },
                { "_TextureHeight", "2048" },
                { "_GradientScale", "12" }
            };
            var merged = ShortCycleFontSwapTool.MergeMaterialProperties(sourceStyle, targetAtlas);
            Require(merged["_FaceColor"] == "red" && merged["_OutlineWidth"] == "0.25",
                "Material style properties must be preserved.");
            foreach (var pair in targetAtlas)
                Require(merged[pair.Key] == pair.Value,
                    "Atlas property was not replaced: " + pair.Key);

            var refusedState = StateFor(baseline);
            var refusedSnapshot = refusedState.ToDictionary(pair => pair.Key,
                pair => pair.Value.Clone(), StringComparer.Ordinal);
            var refusalObserved = mappings.Select(Observe).ToArray();
            refusalObserved[0].currentMaterialGlobalId = "DRIFT";
            var refusedPlan = ShortCycleFontSwapTool.BuildDryRunPlan(mappings, refusalObserved);
            var refused = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflinePlan(refusedState, refusedPlan, -1);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && refusedState.All(pair =>
                    pair.Value.currentFontGuid == refusedSnapshot[pair.Key].currentFontGuid &&
                    pair.Value.currentMaterialGlobalId ==
                        refusedSnapshot[pair.Key].currentMaterialGlobalId),
                "Refused preflight must perform zero writes.");

            var partialState = StateFor(baseline);
            var partialFailed = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflinePlan(partialState, baseline, 5);
            }
            catch (InvalidOperationException exception)
            {
                partialFailed = exception.Message == "INJECTED_PARTIAL_FAILURE";
            }
            Require(partialFailed && IsBefore(partialState, baseline),
                "Injected partial failure must roll back all writes.");
            Require(ShortCycleFontSwapTool.RollbackOfflinePlan(partialState, baseline.entries) == 0,
                "Repeated rollback must be a no-op.");

            var successState = StateFor(baseline);
            var applied = ShortCycleFontSwapTool.ApplyOfflinePlan(successState, baseline, -1);
            Require(applied.writes == baseline.uniqueEntryCount && !applied.rolledBack,
                "Successful dry-run write count is wrong.");
            Require(ShortCycleFontSwapTool.RollbackOfflinePlan(successState, baseline.entries) ==
                    baseline.uniqueEntryCount && IsBefore(successState, baseline),
                "First rollback must restore every unique entry.");
            Require(ShortCycleFontSwapTool.RollbackOfflinePlan(successState, baseline.entries) == 0,
                "Second rollback must remain idempotent.");

            var fingerprintState = StateFor(baseline);
            fingerprintState[baseline.entries[0].canonicalIdentity].nonTargetFingerprint = "DRIFT";
            var fingerprintBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflinePlan(fingerprintState, baseline, -1);
            }
            catch (InvalidOperationException exception)
            {
                fingerprintBlocked = exception.Message.StartsWith(
                    "NON_TARGET_FINGERPRINT_DRIFT|", StringComparison.Ordinal);
            }
            Require(fingerprintBlocked,
                "Non-target fingerprint drift must stop the transaction.");

            var sampleEntry = baseline.entries.First(entry =>
                entry.approvedRowIdentities.Length == 1 &&
                entry.afterMaterialPath.IndexOf("FromSourceHan", StringComparison.Ordinal) >= 0);
            var sampleState = StateFor(baseline);
            var sampleSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            var sampleResult = ShortCycleFontSwapTool.ApplyOfflineBatch(sampleState,
                sampleSession, "SAMPLE-1", new[] { sampleEntry.canonicalIdentity }, -1);
            Require(sampleResult.writes == 1 && sampleResult.completed == 1 &&
                    sampleState[sampleEntry.canonicalIdentity].currentFontGuid ==
                        sampleEntry.afterFontGuid &&
                    baseline.entries.Where(entry => entry != sampleEntry).All(entry =>
                        sampleState[entry.canonicalIdentity].currentFontGuid ==
                            entry.beforeFontGuid),
                "One-business-reference SourceHan sample must remain isolated.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(sampleState, sampleSession) == 1 &&
                    IsBefore(sampleState, baseline),
                "Sample rollback must restore the original full baseline.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(sampleState, sampleSession) == 0,
                "Repeated sample rollback must be safe.");

            var inheritedState = StateFor(dedup);
            var inheritedSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(dedup);
            var inheritedSourceResult = ShortCycleFontSwapTool.ApplyOfflineBatch(inheritedState,
                inheritedSession, "INHERITED-SOURCE", new[] { fridgePrefab.Identity }, -1);
            Require(inheritedSourceResult.writes == 1 &&
                    inheritedState[fridgePrefab.Identity].currentMaterialPath ==
                        fridgePrefab.proposedMaterialPath &&
                    inheritedState[fridgeScene.Identity].currentMaterialPath ==
                        fridgeScene.proposedMaterialPath &&
                    !inheritedState[fridgeScene.Identity].materialOverride,
                "One source write must propagate only to its equivalent inherited approved rows.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(
                        inheritedState, inheritedSession) == 1 && IsBefore(inheritedState, dedup) &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(
                        inheritedState, inheritedSession) == 0,
                "Inherited propagation rollback must restore values and override structure idempotently.");

            var sourceOrderState = StateFor(distinctPlan);
            var sourceOrderSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(distinctPlan);
            var sourceFirstBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(sourceOrderState,
                    sourceOrderSession, "SOURCE-FIRST", new[] { catalogPrefab.Identity }, -1);
            }
            catch (InvalidOperationException exception)
            {
                sourceFirstBlocked = exception.Message.StartsWith(
                    "SOURCE_ORDER_REQUIRES_INSTANCE_FIRST|", StringComparison.Ordinal);
            }
            Require(sourceFirstBlocked && IsBefore(sourceOrderState, distinctPlan),
                "Prefab source writes must wait for independently approved instances.");
            var instanceResult = ShortCycleFontSwapTool.ApplyOfflineBatch(sourceOrderState,
                sourceOrderSession, "INSTANCE-FIRST", new[] { catalogScene.Identity }, -1);
            Require(instanceResult.writes == 1 &&
                    sourceOrderState[catalogScene.Identity].materialOverride &&
                    sourceOrderState[catalogPrefab.Identity].currentMaterialGlobalId ==
                        catalogPrefab.currentMaterialGlobalId,
                "Instance-first batch must not spread the sample to the Prefab source.");
            var sourceResult = ShortCycleFontSwapTool.ApplyOfflineBatch(sourceOrderState,
                sourceOrderSession, "SOURCE-SECOND", new[] { catalogPrefab.Identity }, -1);
            var sourceRetry = ShortCycleFontSwapTool.ApplyOfflineBatch(sourceOrderState,
                sourceOrderSession, "SOURCE-SECOND", new[] { catalogPrefab.Identity }, -1);
            Require(sourceResult.writes == 1 && sourceRetry.alreadyApplied &&
                    sourceRetry.writes == 0 &&
                    sourceOrderState[catalogScene.Identity].currentMaterialPath ==
                        catalogScene.proposedMaterialPath &&
                    sourceOrderState[catalogPrefab.Identity].currentMaterialPath ==
                        catalogPrefab.proposedMaterialPath,
                "Instance/source batches must preserve distinct targets across recovery.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(
                        sourceOrderState, sourceOrderSession) == 2 &&
                    IsBefore(sourceOrderState, distinctPlan) &&
                    !sourceOrderState[catalogScene.Identity].materialOverride &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(
                        sourceOrderState, sourceOrderSession) == 0,
                "Instance/source rollback must restore the original inheritance shape exactly.");

            var invalidState = StateFor(baseline);
            var invalidSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            var duplicateBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(invalidState, invalidSession,
                    "INVALID-DUP", new[] { sampleEntry.canonicalIdentity,
                        sampleEntry.canonicalIdentity }, -1);
            }
            catch (InvalidOperationException) { duplicateBlocked = true; }
            Require(duplicateBlocked && IsBefore(invalidState, baseline),
                "Duplicate selection must perform zero writes.");
            var forgedBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(invalidState, invalidSession,
                    "INVALID-FORGED", new[] { "FORGED" }, -1);
            }
            catch (InvalidOperationException) { forgedBlocked = true; }
            Require(forgedBlocked && IsBefore(invalidState, baseline),
                "Forged selection must perform zero writes.");

            var batchState = StateFor(baseline);
            var batchSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            var midpoint = baseline.entries.Length / 2;
            var batchOne = baseline.entries.Take(midpoint)
                .Select(entry => entry.canonicalIdentity).ToArray();
            var batchTwo = baseline.entries.Skip(midpoint)
                .Select(entry => entry.canonicalIdentity).ToArray();
            var batchOneResult = ShortCycleFontSwapTool.ApplyOfflineBatch(batchState,
                batchSession, "BATCH-1", batchOne, -1);
            var batchOneRetry = ShortCycleFontSwapTool.ApplyOfflineBatch(batchState,
                batchSession, "BATCH-1", batchOne, -1);
            Require(batchOneResult.writes == batchOne.Length && batchOneRetry.alreadyApplied &&
                    batchOneRetry.writes == 0,
                "Identical completed-batch re-entry must be a verified no-op.");
            var batchTwoResult = ShortCycleFontSwapTool.ApplyOfflineBatch(batchState,
                batchSession, "BATCH-2", batchTwo, -1);
            var finalized = ShortCycleFontSwapTool.FinalizeOfflineBatch(batchState, batchSession);
            Require(batchTwoResult.writes == batchTwo.Length && finalized.state == "COMPLETED" &&
                    IsAfter(batchState, baseline),
                "Two explicit batches must cover and finalize the full plan.");

            var pendingDriftState = StateFor(baseline);
            var pendingDriftSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            ShortCycleFontSwapTool.ApplyOfflineBatch(pendingDriftState, pendingDriftSession,
                "PENDING-1", new[] { baseline.entries[0].canonicalIdentity }, -1);
            pendingDriftState[baseline.entries[1].canonicalIdentity].currentFontGuid = "DRIFT";
            var pendingDriftBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(pendingDriftState, pendingDriftSession,
                    "PENDING-2", new[] { baseline.entries[2].canonicalIdentity }, -1);
            }
            catch (InvalidOperationException exception)
            {
                pendingDriftBlocked = exception.Message.StartsWith(
                    "PENDING_STATE_DRIFT|", StringComparison.Ordinal);
            }
            Require(pendingDriftBlocked,
                "Pending drift between batches must block before new writes.");

            var completedDriftState = StateFor(baseline);
            var completedDriftSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            ShortCycleFontSwapTool.ApplyOfflineBatch(completedDriftState, completedDriftSession,
                "COMPLETED-1", new[] { baseline.entries[0].canonicalIdentity }, -1);
            completedDriftState[baseline.entries[0].canonicalIdentity].currentFontGuid = "DRIFT";
            var completedDriftBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(completedDriftState,
                    completedDriftSession, "COMPLETED-2",
                    new[] { baseline.entries[1].canonicalIdentity }, -1);
            }
            catch (InvalidOperationException exception)
            {
                completedDriftBlocked = exception.Message.StartsWith(
                    "COMPLETED_STATE_DRIFT|", StringComparison.Ordinal);
            }
            Require(completedDriftBlocked,
                "Completed drift between batches must block before new writes.");

            var batchFailureState = StateFor(baseline);
            var batchFailureSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(baseline);
            ShortCycleFontSwapTool.ApplyOfflineBatch(batchFailureState, batchFailureSession,
                "FAILURE-1", batchOne, -1);
            var batchFailureRolledBack = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(batchFailureState, batchFailureSession,
                    "FAILURE-2", batchTwo, 3);
            }
            catch (InvalidOperationException exception)
            {
                batchFailureRolledBack = exception.Message == "INJECTED_BATCH_FAILURE";
            }
            Require(batchFailureRolledBack && IsBefore(batchFailureState, baseline) &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(batchFailureState,
                        batchFailureSession) == 0,
                "Batch failure must roll back the full session and remain idempotent.");

            var labSourceIdentity = supplemental[0].expectedPrefabSourceIdentity;
            Require(!string.IsNullOrWhiteSpace(labSourceIdentity) &&
                    combinedPlan.entries.Any(entry =>
                        entry.canonicalIdentity == labSourceIdentity),
                "The approved Lab source Prefab identity must be part of the combined plan.");
            var combinedState = StateFor(combinedPlan);
            var combinedSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(combinedPlan);
            var combinedSourceFirstBlocked = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(combinedState, combinedSession,
                    "COMBINED-SOURCE-FIRST", new[] { labSourceIdentity }, -1);
            }
            catch (InvalidOperationException exception)
            {
                combinedSourceFirstBlocked = exception.Message.StartsWith(
                    "SOURCE_ORDER_REQUIRES_INSTANCE_FIRST|", StringComparison.Ordinal);
            }
            Require(combinedSourceFirstBlocked && IsBefore(combinedState, combinedPlan),
                "The combined transaction must write approved scene instances before sources.");
            var combinedSceneEntries = combinedPlan.entries.Where(entry =>
                    entry.writeAssetPath.EndsWith(".unity",
                        StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.canonicalIdentity).ToArray();
            var combinedPrefabEntries = combinedPlan.entries.Where(entry =>
                    entry.writeAssetPath.EndsWith(".prefab",
                        StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.canonicalIdentity).ToArray();
            Require(combinedSceneEntries.Length > 60 && combinedPrefabEntries.Length > 0,
                "The combined fixture must contain both scenes and all source Prefabs.");
            var combinedScenesResult = ShortCycleFontSwapTool.ApplyOfflineBatch(
                combinedState, combinedSession, "COMBINED-SCENES", combinedSceneEntries, -1);
            var combinedScenesRetry = ShortCycleFontSwapTool.ApplyOfflineBatch(
                combinedState, combinedSession, "COMBINED-SCENES", combinedSceneEntries, -1);
            var combinedPrefabsResult = ShortCycleFontSwapTool.ApplyOfflineBatch(
                combinedState, combinedSession, "COMBINED-PREFABS", combinedPrefabEntries, -1);
            var combinedFinalized = ShortCycleFontSwapTool.FinalizeOfflineBatch(
                combinedState, combinedSession);
            Require(combinedScenesResult.writes == combinedSceneEntries.Length &&
                    combinedScenesRetry.alreadyApplied && combinedScenesRetry.writes == 0 &&
                    combinedPrefabsResult.writes == combinedPrefabEntries.Length &&
                    combinedFinalized.state == "COMPLETED" &&
                    IsAfter(combinedState, combinedPlan),
                "The combined 231-row transaction must resume and finalize without repeat writes.");
            Require(ShortCycleFontSwapTool.RollbackOfflineBatch(
                        combinedState, combinedSession) == combinedPlan.entries.Length &&
                    IsBefore(combinedState, combinedPlan) &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(
                        combinedState, combinedSession) == 0,
                "The combined transaction rollback must be complete and idempotent.");

            var secondSceneState = StateFor(combinedPlan);
            var secondSceneSession = ShortCycleFontSwapTool.BeginOfflineBatchSession(combinedPlan);
            var firstScenePath = combinedPlan.entries.Select(entry => entry.writeAssetPath)
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(path,
                        "Assets/Scenes/ToolTests/FridgeScrollLab.unity",
                        StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal).Single();
            var firstSceneEntries = combinedPlan.entries.Where(entry =>
                    string.Equals(entry.writeAssetPath, firstScenePath,
                        StringComparison.Ordinal))
                .Select(entry => entry.canonicalIdentity).ToArray();
            var labSceneEntries = combinedPlan.entries.Where(entry =>
                    string.Equals(entry.writeAssetPath,
                        "Assets/Scenes/ToolTests/FridgeScrollLab.unity",
                        StringComparison.Ordinal))
                .Select(entry => entry.canonicalIdentity).ToArray();
            Require(labSceneEntries.Length == 60,
                "The second-scene failure fixture must contain all 60 Lab rows.");
            ShortCycleFontSwapTool.ApplyOfflineBatch(secondSceneState, secondSceneSession,
                "FIRST-SCENE", firstSceneEntries, -1);
            var secondSceneFailureRolledBack = false;
            try
            {
                ShortCycleFontSwapTool.ApplyOfflineBatch(secondSceneState, secondSceneSession,
                    "SECOND-SCENE", labSceneEntries, 3);
            }
            catch (InvalidOperationException exception)
            {
                secondSceneFailureRolledBack = exception.Message == "INJECTED_BATCH_FAILURE";
            }
            Require(secondSceneFailureRolledBack && IsBefore(secondSceneState, combinedPlan) &&
                    ShortCycleFontSwapTool.RollbackOfflineBatch(
                        secondSceneState, secondSceneSession) == 0,
                "A failure in the second scene must roll back both scenes and remain idempotent.");

            Console.WriteLine("{\"success\":true,\"rawEntries\":" +
                baseline.rawEntryCount + ",\"baselineUnique\":" + baseline.uniqueEntryCount +
                ",\"dedupUnique\":" + dedup.uniqueEntryCount +
                ",\"approvedRowCoverage\":" + baseline.coveredApprovedRowCount +
                ",\"semanticDistinctUnique\":" + distinctPlan.uniqueEntryCount +
                ",\"sourceDriftReasons\":" + drift.reasons.Length +
                ",\"partialRollback\":true,\"repeatRollback\":true," +
                "\"nonTargetFingerprintGuard\":true," +
                "\"stableObjectReferenceFingerprint\":true," +
                "\"persistentReferenceGuard\":true," +
                "\"internalPrefabReferenceGuard\":true," +
                "\"internalPrefabOwnershipGuard\":true," +
                "\"invalidGlobalIdGuard\":true," +
                "\"unknownInternalTypeGuard\":true," +
                "\"sceneInstanceReferenceGuard\":true," +
                "\"componentReferenceGuard\":true," +
                "\"unresolvedReferenceGuard\":true," +
                "\"scalarAndNestedFingerprintGuard\":true," +
                "\"targetReferenceExclusion\":true," +
                "\"sampleRollback\":true,\"explicitBatches\":2," +
                "\"physicalConflictGuard\":true,\"inheritedPropagation\":true," +
                "\"overrideClassification\":true,\"unapprovedPropagationGuard\":true," +
                "\"sourceOrderGuard\":true," +
                "\"overrideStructureRollback\":true," +
                "\"batchReentryNoWrites\":true,\"invalidSelectionZeroWrites\":true," +
                "\"pendingDriftGuard\":true,\"completedDriftGuard\":true," +
                "\"batchFailureRollback\":true," +
                "\"supplementalRows\":60,\"combinedRows\":231," +
                "\"combinedUnique\":" + combinedPlan.uniqueEntryCount + "," +
                "\"supplementalAuthorizationGuard\":true," +
                "\"supplementalScopeGuard\":true,\"combinedBackupAssets\":7," +
                 "\"combinedPropagationGuard\":true," +
                 "\"combinedBatchRecovery\":true," +
                 "\"secondSceneFailureRollback\":true," +
                 "\"supplementalOverrideGuard\":true," +
                 "\"materialDualViewGuard\":true," +
                 "\"materialNormalizationFailClosed\":true," +
                 "\"normalizedRollback\":true," +
                 "\"approvedLoadObservationGuard\":true," +
                 "\"savedDiskOverrideParserGuard\":true," +
                 "\"approvedBaselineShaGuard\":true," +
                 "\"unsignedGlobalObjectIdGuard\":true," +
                 "\"diagnosticPlanContractGuard\":true," +
                 "\"identicalWrongDiagnosticGuard\":true," +
                 "\"transactionStageSnapshotGuard\":true," +
                 "\"productionMetaPolicyGuard\":true," +
                 "\"ownedMaterialCreationGuard\":true," +
                 "\"sameNameMaterialGuard\":true," +
                 "\"subsequentMaterialMetaGuard\":true," +
                 "\"applyingPolicyJournalGuard\":true," +
                 "\"sameFilePendingRowGuard\":true," +
                 "\"backupSnapshotContractGuard\":true}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
