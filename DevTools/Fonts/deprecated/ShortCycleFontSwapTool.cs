using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EatWhat.DataTables;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.Fonts
{
    /// <summary>
    /// Produces reviewable font migration plans. The legacy API remains read-only/font-only;
    /// approved PRE-1 migrations use an exact CSV identity map and paired font/material writes.
    /// </summary>
    public static class ShortCycleFontSwapTool
    {
        public const string DefaultScenePath =
            "Assets/Scenes/Cooking/ShortCycle_P0P1.unity";
        public const string DefaultManifestPath =
            ".ai-workspace/runtime/font-preview/ShortCycleFontSwapPreview.json";

        public const string ApprovedMappingPath =
            ".ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv";
        public const string ApprovalEvidencePath =
            ".ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md";
        public const string ApprovedMappingSha256 =
            "EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B";
        public const string SupplementalMappingPath =
            ".ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv";
        public const string SupplementalApprovalEvidencePath =
            ".ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md";
        public const string SupplementalMappingSha256 =
            "E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579";
        public const string ApprovedTargetFontGuid =
            "9b07c6c50dde09d4b8719754c27ddc96";
        public const string DefaultApprovedPlanPath =
            ".ai-workspace/runtime/font-migration/approved-plan.json";
        public const string DefaultTransactionPath =
            ".ai-workspace/runtime/font-migration/transaction.json";

        private const int ApprovedRawEntryCount = 171;
        private const int SupplementalRawEntryCount = 60;
        private const int CombinedRawEntryCount = ApprovedRawEntryCount +
            SupplementalRawEntryCount;
        private const int NonTargetFingerprintVersion = 3;
        private const int ApprovedMigrationSchemaVersion = 10;
        private const int FingerprintDiagnosticSchemaVersion = 4;
        private const int BackupManifestSchemaVersion = 8;
        public const string TmpLoadMaterialNormalizationReason =
            "TMP_LOAD_FONT_ATLAS_MISMATCH_DEFAULT_MATERIAL";
        public const string ApprovedLoadObservationReason =
            "APPROVED_LOAD_OBSERVATION_TMP_DEFAULT_MATERIAL";
        public const string PersistentObjectReferenceKind = "PERSISTENT";
        public const string PrefabInstanceObjectReferenceKind =
            "UNITY_INTERNAL_PREFAB_INSTANCE";
        private static readonly Regex GlobalObjectIdPattern = new Regex(
            @"^GlobalObjectId_V1-(?<type>[0-9]+)-(?<guid>[0-9a-fA-F]{32})-" +
            @"(?<target>-?[0-9]+)-(?<prefab>-?[0-9]+)$",
            RegexOptions.CultureInvariant);
        private const string SupplementalAssetPath =
            "Assets/Scenes/ToolTests/FridgeScrollLab.unity";
        private const string SupplementalSourcePrefabPath =
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab";
        private const string SupplementalBeforeFontGuid =
            "75909fc3d0fa2c94baa5706654d37655";
        private const string SupplementalBeforeMaterialGuid =
            "53ffd732c8bc2034f93d5a714c25eda2";
        private const long SupplementalBeforeMaterialFileId = -5877652104868683785L;
        private const string AtlasMainTexture = "_MainTex";
        private const string AtlasTextureWidth = "_TextureWidth";
        private const string AtlasTextureHeight = "_TextureHeight";
        private const string AtlasGradientScale = "_GradientScale";
        private static readonly string[] AtlasPropertyNames =
        {
            AtlasMainTexture,
            AtlasTextureWidth,
            AtlasTextureHeight,
            AtlasGradientScale
        };
        private static readonly string[] ApprovedAssetPaths =
        {
            "Assets/Scenes/Cooking/ShortCycle_P0P1.unity",
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/IngredientCard.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/Sticker.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab"
        };
        // Frozen at PRE-1c-SAVED-STATE-R1 approval. These are the exact seven business
        // assets and metas that the plan is allowed to observe or later mutate. The plan
        // also carries the snapshots so Begin and rollback can prove the same baseline.
        private static readonly FileSnapshot[] ApprovedBaselineFiles =
        {
            Snapshot("Assets/Scenes/Cooking/ShortCycle_P0P1.unity",
                "233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB"),
            Snapshot("Assets/Scenes/Cooking/ShortCycle_P0P1.unity.meta",
                "F866CCF2493AAFB347EB92D498CD485F97EEFDB316FEE7E945EF423045D4C5E0"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab",
                "9A30DA8F1F7080BDDCFAE12848FD0E022FA13C226A3903789A3B5C400C502146"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab.meta",
                "5BAECDC9E43B20E55032BD8097D3170C3CA23170B66610F59F8A71638AA343B4"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab",
                "0B840E5942F96694B44BF27A9FC2216596495A64B788C7A6F885C296A4C60245"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab.meta",
                "5240E9D5F7AF48CA0AF6A7AF7D6F06DB604FE39D5F9DD0C1F48DD21CEE46B6F0"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/IngredientCard.prefab",
                "4C516362DA457838C0CC99BA811D20119131F760EDCA47A509B9A348762A1308"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/IngredientCard.prefab.meta",
                "694DB48AC87E236CAD6EE3FE78E73023BF36DD295E7F96302A522ECB5413AF4A"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/Sticker.prefab",
                "5BD1E3B284C832500F205234EA1A1C206DCE11BCC8F2B2D1E96102F6FFC473B6"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/Sticker.prefab.meta",
                "3BC65DA77514F81CE1AEB03D929DBC03BD274B81A0C730A168025750213CFA5C"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab",
                "DD7986B78C89BF07750F30FA2F5D5E91A965674E1236B42471BD0E616122D0C9"),
            Snapshot("Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab.meta",
                "7951B09364B7A773FB49032B18448EF9434D6975E5476E0787FD42AE0EC55B9B"),
            Snapshot("Assets/Scenes/ToolTests/FridgeScrollLab.unity",
                "4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D"),
            Snapshot("Assets/Scenes/ToolTests/FridgeScrollLab.unity.meta",
                "A80079CA142FB9CEBC0E76D95BC097D164C670A7D1239DCAE4DE694FA036E32D")
        };
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat",
            "Assets/Font/Heavy/Materials/ShortCycle_Static_FromLegacyShortCycle.mat",
            "Assets/Font/Heavy/Materials/ShortCycle_Static_FromHeavy.mat"
        };
        public const string ApprovedMaterialFolderPath =
            "Assets/Font/Heavy/Materials";
        public const string ModifyExistingStageMutation = "MODIFY_EXISTING";
        public const string CreateMissingStageMutation = "CREATE_MISSING";
        private const string ApprovedSourceChangeIdentity =
            "Assets/Scenes/Cooking/ShortCycle_P0P1.unity|" +
            "Spaces/Phase0_RecipeBook/Cover_Group/CoverTitle|0";

        private const string MenuRoot = "Tools/CK01/Fonts/";

        [Serializable]
        public sealed class ApprovedMapping
        {
            public string assetPath;
            public string objectPath;
            public int componentIndex;
            public string currentFontGuid;
            public string currentMaterialGlobalId;
            public string targetFontGuid;
            public string proposedMaterialPath;
            public bool sourceFontChanges;
            public string approvalStatus;
            public bool enforcePrefabShape;
            public string expectedPrefabSourceIdentity;
            public bool expectedFontOverride;
            public bool expectedMaterialOverride;

            public string Identity
            {
                get { return MakeIdentity(assetPath, objectPath, componentIndex); }
            }
        }

        [Serializable]
        public sealed class OfflineBinding
        {
            public string rowIdentity;
            public string canonicalIdentity;
            public string prefabSourceIdentity;
            public bool isPrefabInstance;
            public bool fontOverride;
            public bool materialOverride;
            public bool loadedFontOverride;
            public bool loadedMaterialOverride;
            public bool migrationFontOverrideRequired;
            public bool migrationMaterialOverrideRequired;
            public string writeAssetPath;
            public string writeObjectPath;
            public int writeComponentIndex;
            public string currentFontGuid;
            public string currentMaterialGlobalId;
            public string currentMaterialPath;
            public string approvedObservationFontGuid;
            public string approvedObservationMaterialGlobalId;
            public string effectiveMaterialGlobalId;
            public string effectiveMaterialPath;
            public string materialNormalizationReason;
            public string savedReferenceOwnerIdentity;
            public string savedReferenceAssetPath;
            public string savedReferenceAssetSha256;
            public string savedReferenceSourceAssetPath;
            public string savedReferenceSourceAssetSha256;
            public bool savedReferenceProven;
            public bool savedReferenceAssetShaMatches;
            public string nonTargetFingerprint;

            public OfflineBinding Clone()
            {
                return (OfflineBinding)MemberwiseClone();
            }
        }

        [Serializable]
        public sealed class MaterialResolutionObservation
        {
            public string rowIdentity;
            public string approvedFontGuid;
            public string approvedMaterialGlobalId;
            public string savedFontGuid;
            public string savedMaterialGlobalId;
            public string effectiveMaterialGlobalId;
            public string fontDefaultMaterialGlobalId;
            public bool savedReferenceProven;
            public bool savedAssetShaMatches;
            public bool expectedMaterialExists;
            public bool expectedMaterialAtlasMatchesFont;
            public bool effectiveEqualsFontDefault;
            public bool prefabSourceMatches;
            public bool savedMaterialOverride;
        }

        public static string ClassifyMaterialResolution(
            MaterialResolutionObservation observation)
        {
            if (observation == null) throw new ArgumentNullException("observation");
            if (string.Equals(observation.approvedMaterialGlobalId,
                    observation.savedMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(observation.savedMaterialGlobalId,
                    observation.effectiveMaterialGlobalId, StringComparison.Ordinal))
                return string.Empty;
            if (IsApprovedTmpNormalizationIdentity(observation.rowIdentity) &&
                observation.savedReferenceProven && observation.savedAssetShaMatches &&
                observation.expectedMaterialExists &&
                !observation.expectedMaterialAtlasMatchesFont &&
                observation.effectiveEqualsFontDefault && observation.prefabSourceMatches &&
                !observation.savedMaterialOverride &&
                string.Equals(observation.savedFontGuid, observation.approvedFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(observation.savedMaterialGlobalId,
                    observation.approvedMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(observation.effectiveMaterialGlobalId,
                    observation.fontDefaultMaterialGlobalId, StringComparison.Ordinal)
                    )
                return TmpLoadMaterialNormalizationReason;
            string exactSavedMaterial;
            if (TryGetApprovedLoadObservationSavedMaterial(observation.rowIdentity,
                    out exactSavedMaterial) && observation.savedReferenceProven &&
                observation.savedAssetShaMatches && observation.expectedMaterialExists &&
                !observation.expectedMaterialAtlasMatchesFont &&
                observation.effectiveEqualsFontDefault &&
                !observation.savedMaterialOverride &&
                string.Equals(observation.savedFontGuid, observation.approvedFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(observation.savedMaterialGlobalId, exactSavedMaterial,
                    StringComparison.Ordinal) &&
                string.Equals(observation.approvedMaterialGlobalId,
                    observation.fontDefaultMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(observation.effectiveMaterialGlobalId,
                    observation.fontDefaultMaterialGlobalId, StringComparison.Ordinal))
                return ApprovedLoadObservationReason;
            return string.Empty;
        }

        private static MaterialResolutionObservation BuildMaterialResolutionObservation(
            string rowIdentity,
            string approvedFontGuid,
            string approvedMaterialGlobalId,
            SavedTmpReferences saved,
            Material effectiveMaterial,
            TMP_FontAsset effectiveFont,
            string prefabSourceIdentity,
            bool referenceSnapshotTrusted)
        {
            if (saved == null) throw new ArgumentNullException("saved");
            var expectedMaterial = ResolveMaterial(saved.MaterialGlobalId);
            var defaultMaterial = effectiveFont == null ? null : effectiveFont.material;
            return new MaterialResolutionObservation
            {
                rowIdentity = rowIdentity,
                approvedFontGuid = approvedFontGuid,
                approvedMaterialGlobalId = approvedMaterialGlobalId,
                savedFontGuid = saved.FontGuid,
                savedMaterialGlobalId = saved.MaterialGlobalId,
                effectiveMaterialGlobalId = GlobalIdFor(effectiveMaterial),
                fontDefaultMaterialGlobalId = GlobalIdFor(defaultMaterial),
                savedReferenceProven = saved.Proven,
                savedAssetShaMatches = referenceSnapshotTrusted,
                expectedMaterialExists = expectedMaterial != null,
                expectedMaterialAtlasMatchesFont = MaterialAtlasMatchesFont(
                    expectedMaterial, effectiveFont),
                effectiveEqualsFontDefault = effectiveMaterial == defaultMaterial,
                prefabSourceMatches = IsExpectedTmpNormalizationSource(
                    rowIdentity, prefabSourceIdentity),
                savedMaterialOverride = saved.MaterialOverride
            };
        }

        [Serializable]
        public sealed class ObjectReferenceFingerprintDescriptor
        {
            public bool isNull;
            public bool unresolved;
            public string identityKind;
            public string persistentGlobalId;
            public string ownerGlobalId;
            public string ownerAssetGuid;
            public string assetPath;
            public string hierarchyPath;
            public string objectKind;
            public string componentType;
            public int componentIndex = -1;

            // Test-only evidence that transient runtime ids do not enter the normalized identity.
            public int transientInstanceId;
        }

        [Serializable]
        public sealed class NonTargetFingerprintProperty
        {
            public string propertyPath;
            public string propertyType;
            public string scalarValue;
            public ObjectReferenceFingerprintDescriptor objectReference;
        }

        [Serializable]
        public sealed class FingerprintDiagnosticRow
        {
            public string rowIdentity;
            public string fingerprint;
            public string[] objectReferenceIdentities;
            public string rawFontGuid;
            public string rawMaterialGlobalId;
            public string approvedObservationFontGuid;
            public string approvedObservationMaterialGlobalId;
            public string effectiveMaterialGlobalId;
            public string materialNormalizationReason;
            public bool savedFontOverride;
            public bool savedMaterialOverride;
            public bool loadedFontOverride;
            public bool loadedMaterialOverride;
            public bool migrationFontOverrideRequired;
            public bool migrationMaterialOverrideRequired;
            public string savedReferenceOwnerIdentity;
            public string savedReferenceAssetPath;
            public string savedReferenceAssetSha256;
            public string savedReferenceSourceAssetPath;
            public string savedReferenceSourceAssetSha256;
            public bool savedReferenceProven;
            public bool savedReferenceAssetShaMatches;
        }

        [Serializable]
        public sealed class FingerprintDiagnosticReport
        {
            public int schemaVersion = FingerprintDiagnosticSchemaVersion;
            public int nonTargetFingerprintVersion = NonTargetFingerprintVersion;
            public bool success;
            public string generatedAtUtc;
            public string planPath;
            public int rowCount;
            public int approvedAssetCount;
            public bool stableAcrossReloads;
            public bool referencesValid;
            public int unstableRowCount;
            public string[] unstableRowIdentities;
            public int planMismatchRowCount;
            public string[] planMismatchRowIdentities;
            public FingerprintDiagnosticRow[] firstPass;
            public FingerprintDiagnosticRow[] secondPass;
        }

        [Serializable]
        public sealed class DryRunEntry
        {
            public string canonicalIdentity;
            public string[] approvedRowIdentities;
            public string prefabSourceIdentity;
            public bool beforeFontOverride;
            public bool beforeMaterialOverride;
            public bool beforeLoadedFontOverride;
            public bool beforeLoadedMaterialOverride;
            public bool migrationFontOverrideRequired;
            public bool migrationMaterialOverrideRequired;
            public string writeAssetPath;
            public string writeObjectPath;
            public int writeComponentIndex;
            public string beforeFontGuid;
            public string approvedObservationFontGuid;
            public string approvedObservationMaterialGlobalId;
            public string beforeMaterialGlobalId;
            public string beforeMaterialPath;
            public string beforeEffectiveMaterialGlobalId;
            public string beforeEffectiveMaterialPath;
            public string beforeMaterialNormalizationReason;
            public string savedReferenceOwnerIdentity;
            public string savedReferenceAssetPath;
            public string savedReferenceAssetSha256;
            public string savedReferenceSourceAssetPath;
            public string savedReferenceSourceAssetSha256;
            public bool savedReferenceProven;
            public bool savedReferenceAssetShaMatches;
            public string afterFontGuid;
            public string afterMaterialPath;
            public string nonTargetFingerprint;
        }

        [Serializable]
        public sealed class ApprovedRowAudit
        {
            public string[] validationReasons;
            public string rowIdentity;
            public string canonicalIdentity;
            public string prefabSourceIdentity;
            public bool coveredByPrefabSource;
            public bool beforeFontOverride;
            public bool beforeMaterialOverride;
            public bool beforeLoadedFontOverride;
            public bool beforeLoadedMaterialOverride;
            public bool migrationFontOverrideRequired;
            public bool migrationMaterialOverrideRequired;
            public string assetPath;
            public string objectPath;
            public int componentIndex;
            public string beforeFontGuid;
            public string approvedObservationFontGuid;
            public string approvedObservationMaterialGlobalId;
            public string beforeMaterialGlobalId;
            public string beforeMaterialPath;
            public string beforeEffectiveMaterialGlobalId;
            public string beforeEffectiveMaterialPath;
            public string beforeMaterialNormalizationReason;
            public string savedReferenceOwnerIdentity;
            public string savedReferenceAssetPath;
            public string savedReferenceAssetSha256;
            public string savedReferenceSourceAssetPath;
            public string savedReferenceSourceAssetSha256;
            public bool savedReferenceProven;
            public bool savedReferenceAssetShaMatches;
            public string afterFontGuid;
            public string afterMaterialPath;
            public string nonTargetFingerprint;
        }

        [Serializable]
        public sealed class DryRunPlan
        {
            public int rawEntryCount;
            public int uniqueEntryCount;
            public int duplicateApprovedIdentityCount;
            public int missingIdentityCount;
            public int duplicateObservedIdentityCount;
            public int sourceDriftCount;
            public int conflictingCanonicalCount;
            public int coveredApprovedRowCount;
            public bool executeAllowed;
            public string[] reasons;
            public DryRunEntry[] entries;
            public ApprovedRowAudit[] rows;
        }

        [Serializable]
        public sealed class MaterialPlan
        {
            public string sourceMaterialGlobalId;
            public string sourceMaterialPath;
            public string targetMaterialPath;
            public string sourceStyleFingerprint;
            public string targetAtlasFingerprint;
            public string expectedMaterialFingerprint;
            public bool ready;
            public string limitation;
        }

        [Serializable]
        public sealed class ApprovedMigrationPlan
        {
            public int schemaVersion = ApprovedMigrationSchemaVersion;
            public int nonTargetFingerprintVersion = NonTargetFingerprintVersion;
            public string generatedAtUtc;
            public string mappingPath;
            public string mappingSha256;
            public string approvalEvidencePath;
            public string supplementalMappingPath;
            public string supplementalMappingSha256;
            public string supplementalApprovalEvidencePath;
            public int approvedRawEntryCount;
            public string[] approvedAssetPaths;
            public string targetFontPath;
            public string targetFontGuid;
            public int rawEntryCount;
            public int uniqueEntryCount;
            public bool executeAllowed;
            public string[] reasons;
            public MaterialPlan[] materials;
            public DryRunEntry[] entries;
            public ApprovedRowAudit[] rows;
            public string[] propagationGuardAssetPaths;
            public FileSnapshot[] approvedBaselineFiles;
        }

        [Serializable]
        public sealed class CreatedAssetRecord
        {
            public string path;
            public string fingerprint;
            public string creationBatchId;
            public string guid;
            public string assetSha256;
            public string metaSha256;
        }

        [Serializable]
        public sealed class FileSnapshot
        {
            public string path;
            public bool exists;
            public string sha256;
        }

        [Serializable]
        public sealed class BatchRecord
        {
            public string batchId;
            public string state;
            public string recordedAtUtc;
            public int writes;
            public string[] canonicalIdentities;
            public FileSnapshot[] preStageFiles;
            public bool includesMaterialCreation;
            public StageMutationRule[] mutationRules;
            public string stageContractSha256;
        }

        [Serializable]
        public sealed class StageMutationRule
        {
            public string path;
            public string mutationKind;
            public bool beforeExists;
            public string beforeSha256;
        }

        public sealed class StageSnapshotValidation
        {
            public bool success;
            public string[] changedPaths;
            public string[] reasons;
        }

        [Serializable]
        public sealed class TransactionRecord
        {
            public int schemaVersion = ApprovedMigrationSchemaVersion;
            public int nonTargetFingerprintVersion = NonTargetFingerprintVersion;
            public string taskId;
            public string createdAtUtc;
            public string state;
            public string mappingPath;
            public string mappingSha256;
            public string approvalEvidencePath;
            public string supplementalMappingPath;
            public string supplementalMappingSha256;
            public string supplementalApprovalEvidencePath;
            public int approvedRawEntryCount;
            public string[] approvedAssetPaths;
            public string planPath;
            public string targetFontPath;
            public string targetFontGuid;
            public string backupRoot;
            public MaterialPlan[] materials;
            public DryRunEntry[] entries;
            public ApprovedRowAudit[] rows;
            public string[] propagationGuardAssetPaths;
            public CreatedAssetRecord[] createdAssets;
            public string[] completedCanonicalIdentities;
            public BatchRecord[] batches;
            public FileSnapshot[] expectedFiles;
            public FileSnapshot[] approvedBaselineFiles;
            public bool materialFolderExistedAtBegin;
            public string ownedMaterialFolderPath;
            public string ownedMaterialFolderGuid;
            public string ownedMaterialFolderMetaSha256;
        }

        [Serializable]
        private sealed class BackupFileRecord
        {
            public string sourcePath;
            public string backupPath;
            public string sha256;
        }

        [Serializable]
        private sealed class BackupManifest
        {
            public int schemaVersion = BackupManifestSchemaVersion;
            public int nonTargetFingerprintVersion = NonTargetFingerprintVersion;
            public string taskId;
            public string createdAtUtc;
            public string mappingPath;
            public string mappingSha256;
            public string supplementalMappingPath;
            public string supplementalMappingSha256;
            public int approvedRawEntryCount;
            public BackupFileRecord[] files;
            public MaterialPlan[] materials;
            public string targetFontGuid;
            public string targetFontPath;
            public string targetFontFingerprint;
            public string[] newAssetPaths;
            public FileSnapshot[] approvedBaselineFiles;
            public bool materialFolderExistedAtBegin;
        }

        public sealed class OfflineTransactionResult
        {
            public int writes;
            public bool rolledBack;
        }

        public sealed class OfflineBatchResult
        {
            public int writes;
            public bool alreadyApplied;
            public int completed;
            public int total;
            public string state;
        }

        public sealed class OfflineBatchSession
        {
            public DryRunPlan plan;
            public string state;
            public string[] completedCanonicalIdentities;
            public BatchRecord[] batches;
        }

        /// <summary>
        /// Pure, Editor-independent planner used by the production preflight and the isolated
        /// DevTools fixture. No external executeAllowed flag is accepted.
        /// </summary>
        public static DryRunPlan BuildDryRunPlan(
            IEnumerable<ApprovedMapping> approvedMappings,
            IEnumerable<OfflineBinding> observedBindings)
        {
            return BuildDryRunPlan(approvedMappings, observedBindings, ApprovedRawEntryCount);
        }

        public static DryRunPlan BuildDryRunPlan(
            IEnumerable<ApprovedMapping> approvedMappings,
            IEnumerable<OfflineBinding> observedBindings,
            int expectedRawEntryCount)
        {
            if (expectedRawEntryCount <= 0)
                throw new ArgumentOutOfRangeException("expectedRawEntryCount");
            var mappings = (approvedMappings ?? Enumerable.Empty<ApprovedMapping>()).ToArray();
            var observed = (observedBindings ?? Enumerable.Empty<OfflineBinding>()).ToArray();
            var reasons = new List<string>();
            var duplicateApproved = mappings.GroupBy(item => item.Identity, StringComparer.Ordinal)
                .Where(group => group.Count() != 1).ToArray();
            var mappingByIdentity = mappings.GroupBy(item => item.Identity, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var observedByRow = observed.GroupBy(item => item.rowIdentity, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var missing = 0;
            var duplicateObserved = 0;
            var sourceDrift = 0;
            var validPairs = new List<KeyValuePair<ApprovedMapping, OfflineBinding>>();
            var auditedPairs = new List<KeyValuePair<ApprovedMapping, OfflineBinding>>();
            var auditReasons = new Dictionary<string, string[]>(StringComparer.Ordinal);

            foreach (var mapping in mappings)
            {
                OfflineBinding[] matches;
                if (!observedByRow.TryGetValue(mapping.Identity, out matches) || matches.Length == 0)
                {
                    missing++;
                    reasons.Add("MISSING_IDENTITY|" + mapping.Identity);
                    continue;
                }
                if (matches.Length != 1)
                {
                    duplicateObserved++;
                    reasons.Add("DUPLICATE_OBSERVED_IDENTITY|" + mapping.Identity);
                    continue;
                }

                var binding = matches[0];
                auditedPairs.Add(new KeyValuePair<ApprovedMapping, OfflineBinding>(
                    mapping, binding));
                var drift = new List<string>();
                if (!string.Equals(mapping.currentFontGuid, binding.currentFontGuid,
                        StringComparison.Ordinal))
                    drift.Add("FONT_GUID");
                if (!binding.savedReferenceProven)
                    drift.Add("SAVED_REFERENCE_UNPROVEN");
                if (!binding.savedReferenceAssetShaMatches)
                    drift.Add("APPROVED_BASELINE_SHA");
                var approvedLoadObservation = string.Equals(
                    binding.materialNormalizationReason,
                    ApprovedLoadObservationReason, StringComparison.Ordinal);
                if (!string.Equals(mapping.currentMaterialGlobalId,
                        binding.currentMaterialGlobalId, StringComparison.Ordinal) &&
                    !approvedLoadObservation)
                    drift.Add("MATERIAL_GLOBAL_ID");
                var materialViewsDiffer = !string.Equals(binding.currentMaterialGlobalId,
                    binding.effectiveMaterialGlobalId, StringComparison.Ordinal);
                var explainedMaterialView = string.Equals(
                        binding.materialNormalizationReason,
                        TmpLoadMaterialNormalizationReason, StringComparison.Ordinal) ||
                    approvedLoadObservation;
                if (materialViewsDiffer && !explainedMaterialView)
                    drift.Add("UNEXPLAINED_EFFECTIVE_MATERIAL");
                if (!materialViewsDiffer && !string.IsNullOrEmpty(
                        binding.materialNormalizationReason))
                    drift.Add("SPURIOUS_MATERIAL_NORMALIZATION");
                if (!string.Equals(mapping.targetFontGuid, ApprovedTargetFontGuid,
                        StringComparison.Ordinal))
                    drift.Add("TARGET_FONT_GUID");
                if (string.IsNullOrWhiteSpace(mapping.proposedMaterialPath))
                    drift.Add("TARGET_MATERIAL_PATH");
                if (mapping.enforcePrefabShape && (!binding.isPrefabInstance ||
                        !string.Equals(binding.prefabSourceIdentity,
                            mapping.expectedPrefabSourceIdentity, StringComparison.Ordinal)))
                    drift.Add("PREFAB_SOURCE");
                if (mapping.enforcePrefabShape &&
                    binding.fontOverride != mapping.expectedFontOverride)
                    drift.Add("FONT_OVERRIDE");
                if (mapping.enforcePrefabShape &&
                    binding.materialOverride != mapping.expectedMaterialOverride)
                    drift.Add("MATERIAL_OVERRIDE");
                if (drift.Count > 0)
                {
                    auditReasons[mapping.Identity] = drift.ToArray();
                    sourceDrift++;
                    reasons.Add("SOURCE_DRIFT|" + mapping.Identity + "|" +
                        string.Join(",", drift.ToArray()));
                    continue;
                }
                auditReasons[mapping.Identity] = new string[0];
                validPairs.Add(new KeyValuePair<ApprovedMapping, OfflineBinding>(mapping, binding));
            }

            foreach (var duplicate in duplicateApproved)
                reasons.Add("DUPLICATE_APPROVED_IDENTITY|" + duplicate.Key);

            var resolvedCanonical = validPairs.GroupBy(pair => pair.Key.Identity,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => ResolveCanonicalIdentity(group.First().Key, group.First().Value,
                        mappingByIdentity, observedByRow), StringComparer.Ordinal);
            var auditedCanonical = auditedPairs.GroupBy(pair => pair.Key.Identity,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => ResolveCanonicalIdentity(group.First().Key, group.First().Value,
                        mappingByIdentity, observedByRow), StringComparer.Ordinal);
            var rows = auditedPairs.Select(pair => new ApprovedRowAudit
            {
                validationReasons = auditReasons[pair.Key.Identity],
                rowIdentity = pair.Key.Identity,
                canonicalIdentity = auditedCanonical[pair.Key.Identity],
                prefabSourceIdentity = pair.Value.prefabSourceIdentity,
                coveredByPrefabSource = !string.Equals(pair.Key.Identity,
                    auditedCanonical[pair.Key.Identity], StringComparison.Ordinal),
                beforeFontOverride = pair.Value.fontOverride,
                beforeMaterialOverride = pair.Value.materialOverride,
                beforeLoadedFontOverride = pair.Value.loadedFontOverride,
                beforeLoadedMaterialOverride = pair.Value.loadedMaterialOverride,
                migrationFontOverrideRequired = pair.Value.isPrefabInstance &&
                    string.Equals(pair.Key.Identity,
                        auditedCanonical[pair.Key.Identity], StringComparison.Ordinal) &&
                    !pair.Value.fontOverride,
                migrationMaterialOverrideRequired = pair.Value.isPrefabInstance &&
                    string.Equals(pair.Key.Identity,
                        auditedCanonical[pair.Key.Identity], StringComparison.Ordinal) &&
                    !pair.Value.materialOverride,
                assetPath = pair.Key.assetPath,
                objectPath = pair.Key.objectPath,
                componentIndex = pair.Key.componentIndex,
                beforeFontGuid = pair.Value.currentFontGuid,
                approvedObservationFontGuid = pair.Key.currentFontGuid,
                approvedObservationMaterialGlobalId = pair.Key.currentMaterialGlobalId,
                beforeMaterialGlobalId = pair.Value.currentMaterialGlobalId,
                beforeMaterialPath = pair.Value.currentMaterialPath,
                beforeEffectiveMaterialGlobalId = pair.Value.effectiveMaterialGlobalId,
                beforeEffectiveMaterialPath = pair.Value.effectiveMaterialPath,
                beforeMaterialNormalizationReason = pair.Value.materialNormalizationReason,
                savedReferenceOwnerIdentity = pair.Value.savedReferenceOwnerIdentity,
                savedReferenceAssetPath = pair.Value.savedReferenceAssetPath,
                savedReferenceAssetSha256 = pair.Value.savedReferenceAssetSha256,
                savedReferenceSourceAssetPath =
                    pair.Value.savedReferenceSourceAssetPath,
                savedReferenceSourceAssetSha256 =
                    pair.Value.savedReferenceSourceAssetSha256,
                savedReferenceProven = pair.Value.savedReferenceProven,
                savedReferenceAssetShaMatches =
                    pair.Value.savedReferenceAssetShaMatches,
                afterFontGuid = pair.Key.targetFontGuid,
                afterMaterialPath = pair.Key.proposedMaterialPath,
                nonTargetFingerprint = pair.Value.nonTargetFingerprint
            }).OrderBy(row => row.assetPath, StringComparer.Ordinal)
                .ThenBy(row => row.objectPath, StringComparer.Ordinal)
                .ThenBy(row => row.componentIndex).ToArray();
            var entries = new List<DryRunEntry>();
            var canonicalConflicts = 0;
            foreach (var group in validPairs.GroupBy(pair =>
                resolvedCanonical[pair.Key.Identity],
                StringComparer.Ordinal))
            {
                var pairs = group.ToArray();
                var targets = pairs.Select(pair => pair.Key.targetFontGuid + "|" +
                        pair.Key.proposedMaterialPath)
                    .Distinct(StringComparer.Ordinal).ToArray();
                var befores = pairs.Select(pair => pair.Value.currentFontGuid + "|" +
                        pair.Value.currentMaterialGlobalId + "|" +
                        pair.Value.effectiveMaterialGlobalId + "|" +
                        pair.Value.materialNormalizationReason)
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (targets.Length != 1 || befores.Length != 1)
                {
                    canonicalConflicts++;
                    reasons.Add("CANONICAL_CONFLICT|" + group.Key);
                    continue;
                }

                var selected = pairs.FirstOrDefault(pair =>
                    string.Equals(pair.Key.Identity, group.Key, StringComparison.Ordinal));
                if (selected.Key == null) selected = pairs[0];
                entries.Add(new DryRunEntry
                {
                    canonicalIdentity = group.Key,
                    approvedRowIdentities = pairs.Select(pair => pair.Key.Identity)
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    prefabSourceIdentity = selected.Value.prefabSourceIdentity,
                    beforeFontOverride = selected.Value.fontOverride,
                    beforeMaterialOverride = selected.Value.materialOverride,
                    beforeLoadedFontOverride = selected.Value.loadedFontOverride,
                    beforeLoadedMaterialOverride = selected.Value.loadedMaterialOverride,
                    migrationFontOverrideRequired = selected.Value.isPrefabInstance &&
                        string.Equals(selected.Key.Identity, group.Key,
                            StringComparison.Ordinal) && !selected.Value.fontOverride,
                    migrationMaterialOverrideRequired = selected.Value.isPrefabInstance &&
                        string.Equals(selected.Key.Identity, group.Key,
                            StringComparison.Ordinal) && !selected.Value.materialOverride,
                    writeAssetPath = selected.Value.writeAssetPath,
                    writeObjectPath = selected.Value.writeObjectPath,
                    writeComponentIndex = selected.Value.writeComponentIndex,
                    beforeFontGuid = selected.Value.currentFontGuid,
                    approvedObservationFontGuid = selected.Key.currentFontGuid,
                    approvedObservationMaterialGlobalId =
                        selected.Key.currentMaterialGlobalId,
                    beforeMaterialGlobalId = selected.Value.currentMaterialGlobalId,
                    beforeMaterialPath = selected.Value.currentMaterialPath,
                    beforeEffectiveMaterialGlobalId = selected.Value.effectiveMaterialGlobalId,
                    beforeEffectiveMaterialPath = selected.Value.effectiveMaterialPath,
                    beforeMaterialNormalizationReason = selected.Value.materialNormalizationReason,
                    savedReferenceOwnerIdentity = selected.Value.savedReferenceOwnerIdentity,
                    savedReferenceAssetPath = selected.Value.savedReferenceAssetPath,
                    savedReferenceAssetSha256 = selected.Value.savedReferenceAssetSha256,
                    savedReferenceSourceAssetPath =
                        selected.Value.savedReferenceSourceAssetPath,
                    savedReferenceSourceAssetSha256 =
                        selected.Value.savedReferenceSourceAssetSha256,
                    savedReferenceProven = selected.Value.savedReferenceProven,
                    savedReferenceAssetShaMatches =
                        selected.Value.savedReferenceAssetShaMatches,
                    afterFontGuid = selected.Key.targetFontGuid,
                    afterMaterialPath = selected.Key.proposedMaterialPath,
                    nonTargetFingerprint = selected.Value.nonTargetFingerprint
                });
            }

            var plan = new DryRunPlan
            {
                rawEntryCount = mappings.Length,
                uniqueEntryCount = entries.Count,
                duplicateApprovedIdentityCount = duplicateApproved.Length,
                missingIdentityCount = missing,
                duplicateObservedIdentityCount = duplicateObserved,
                sourceDriftCount = sourceDrift,
                conflictingCanonicalCount = canonicalConflicts,
                coveredApprovedRowCount = rows.Select(row => row.rowIdentity)
                    .Distinct(StringComparer.Ordinal).Count(),
                reasons = reasons.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                entries = entries.OrderBy(entry => entry.writeAssetPath, StringComparer.Ordinal)
                    .ThenBy(entry => entry.writeObjectPath, StringComparer.Ordinal)
                    .ThenBy(entry => entry.writeComponentIndex).ToArray(),
                rows = rows
            };
            plan.executeAllowed = plan.rawEntryCount == expectedRawEntryCount &&
                plan.duplicateApprovedIdentityCount == 0 && plan.missingIdentityCount == 0 &&
                plan.duplicateObservedIdentityCount == 0 && plan.sourceDriftCount == 0 &&
                plan.conflictingCanonicalCount == 0 && plan.uniqueEntryCount > 0 &&
                plan.coveredApprovedRowCount == plan.rawEntryCount && plan.reasons.Length == 0;
            if (plan.rawEntryCount != expectedRawEntryCount)
            {
                plan.executeAllowed = false;
                plan.reasons = plan.reasons.Concat(new[]
                {
                    "RAW_ENTRY_COUNT|actual=" + plan.rawEntryCount +
                    "|expected=" + expectedRawEntryCount
                }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }
            if (plan.coveredApprovedRowCount != plan.rawEntryCount)
            {
                plan.executeAllowed = false;
                plan.reasons = plan.reasons.Concat(new[]
                {
                    "APPROVED_ROW_COVERAGE|actual=" + plan.coveredApprovedRowCount +
                    "|expected=" + plan.rawEntryCount
                }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }
            return plan;
        }

        private static string ResolveCanonicalIdentity(
            ApprovedMapping mapping,
            OfflineBinding binding,
            IDictionary<string, ApprovedMapping> mappings,
            IDictionary<string, OfflineBinding[]> observedByRow)
        {
            var rowIdentity = mapping.Identity;
            if (string.IsNullOrWhiteSpace(binding.prefabSourceIdentity))
                return string.IsNullOrWhiteSpace(binding.canonicalIdentity)
                    ? rowIdentity
                    : binding.canonicalIdentity;

            ApprovedMapping sourceMapping;
            OfflineBinding[] sourceBindings;
            if (binding.fontOverride || binding.materialOverride ||
                !mappings.TryGetValue(binding.prefabSourceIdentity, out sourceMapping) ||
                !observedByRow.TryGetValue(binding.prefabSourceIdentity, out sourceBindings) ||
                sourceBindings.Length != 1)
                return rowIdentity;

            var sourceBinding = sourceBindings[0];
            var beforeEquivalent = string.Equals(mapping.currentFontGuid,
                    sourceMapping.currentFontGuid, StringComparison.Ordinal) &&
                string.Equals(mapping.currentMaterialGlobalId,
                    sourceMapping.currentMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(binding.currentFontGuid, sourceBinding.currentFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(binding.currentMaterialGlobalId,
                    sourceBinding.currentMaterialGlobalId, StringComparison.Ordinal);
            var afterEquivalent = string.Equals(mapping.targetFontGuid,
                    sourceMapping.targetFontGuid, StringComparison.Ordinal) &&
                string.Equals(mapping.proposedMaterialPath,
                    sourceMapping.proposedMaterialPath, StringComparison.Ordinal);
            return beforeEquivalent && afterEquivalent
                ? binding.prefabSourceIdentity
                : rowIdentity;
        }

        /// <summary>Copies style values and replaces only the four atlas-bound values.</summary>
        public static IDictionary<string, string> MergeMaterialProperties(
            IDictionary<string, string> sourceStyle,
            IDictionary<string, string> targetAtlas)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in sourceStyle ?? new Dictionary<string, string>())
                result[pair.Key] = pair.Value;
            foreach (var propertyName in AtlasPropertyNames)
            {
                string value;
                if (targetAtlas == null || !targetAtlas.TryGetValue(propertyName, out value))
                    throw new InvalidOperationException("Missing target atlas property: " + propertyName);
                result[propertyName] = value;
            }
            return result;
        }

        /// <summary>Pure transaction simulation used by Test-FontSwap.ps1.</summary>
        public static OfflineTransactionResult ApplyOfflinePlan(
            IDictionary<string, OfflineBinding> state,
            DryRunPlan plan,
            int failAfterWrites)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (plan == null || !plan.executeAllowed)
                throw new InvalidOperationException("Offline plan is not executable.");
            var changed = new List<DryRunEntry>();
            try
            {
                ValidateOfflineBatchState(state, plan,
                    new HashSet<string>(StringComparer.Ordinal));
                foreach (var entry in plan.entries)
                {
                    ApplyOfflineEntryState(state, plan, entry, false);
                    changed.Add(entry);
                    if (failAfterWrites >= 0 && changed.Count >= failAfterWrites)
                        throw new InvalidOperationException("INJECTED_PARTIAL_FAILURE");
                }
                return new OfflineTransactionResult { writes = changed.Count, rolledBack = false };
            }
            catch
            {
                RollbackOfflineEntries(state, plan, changed);
                throw;
            }
        }

        public static int RollbackOfflinePlan(
            IDictionary<string, OfflineBinding> state,
            DryRunPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            return RollbackOfflineEntries(state, plan, plan.entries);
        }

        /// <summary>Idempotent pure rollback; entries already at before state are ignored.</summary>
        public static int RollbackOfflinePlan(
            IDictionary<string, OfflineBinding> state,
            IEnumerable<DryRunEntry> entries)
        {
            var materialized = (entries ?? Enumerable.Empty<DryRunEntry>()).ToArray();
            var compatibilityPlan = new DryRunPlan
            {
                entries = materialized,
                rows = materialized.Select(entry => new ApprovedRowAudit
                {
                    rowIdentity = entry.canonicalIdentity,
                    canonicalIdentity = entry.canonicalIdentity,
                    beforeFontGuid = entry.beforeFontGuid,
                    beforeMaterialGlobalId = entry.beforeMaterialGlobalId,
                    beforeMaterialPath = entry.beforeMaterialPath,
                    beforeEffectiveMaterialGlobalId = entry.beforeEffectiveMaterialGlobalId,
                    beforeEffectiveMaterialPath = entry.beforeEffectiveMaterialPath,
                    beforeMaterialNormalizationReason =
                        entry.beforeMaterialNormalizationReason,
                    savedReferenceOwnerIdentity = entry.savedReferenceOwnerIdentity,
                    savedReferenceAssetPath = entry.savedReferenceAssetPath,
                    savedReferenceAssetSha256 = entry.savedReferenceAssetSha256,
                    savedReferenceProven = entry.savedReferenceProven,
                    afterFontGuid = entry.afterFontGuid,
                    afterMaterialPath = entry.afterMaterialPath,
                    nonTargetFingerprint = entry.nonTargetFingerprint
                }).ToArray()
            };
            return RollbackOfflineEntries(state, compatibilityPlan, materialized);
        }

        private static int RollbackOfflineEntries(
            IDictionary<string, OfflineBinding> state,
            DryRunPlan plan,
            IEnumerable<DryRunEntry> entries)
        {
            var restored = 0;
            foreach (var entry in (entries ?? Enumerable.Empty<DryRunEntry>()).Reverse())
            {
                if (ApplyOfflineEntryState(state, plan, entry, true)) restored++;
            }
            return restored;
        }

        /// <summary>Starts a deterministic offline batch session from a full executable plan.</summary>
        public static OfflineBatchSession BeginOfflineBatchSession(DryRunPlan plan)
        {
            if (plan == null || !plan.executeAllowed || plan.entries == null ||
                plan.entries.Length == 0 || plan.rows == null ||
                plan.rows.Length != plan.rawEntryCount)
                throw new InvalidOperationException("Offline plan is not executable.");
            return new OfflineBatchSession
            {
                plan = plan,
                state = "PLANNED",
                completedCanonicalIdentities = new string[0],
                batches = new BatchRecord[0]
            };
        }

        /// <summary>
        /// Applies one explicit subset. Selection is validated against the full plan before the
        /// first write, and every completed/pending entry is revalidated between calls.
        /// </summary>
        public static OfflineBatchResult ApplyOfflineBatch(
            IDictionary<string, OfflineBinding> state,
            OfflineBatchSession session,
            string batchId,
            IEnumerable<string> selectedCanonicalIdentities,
            int failAfterWrites)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (session == null || session.plan == null || !session.plan.executeAllowed)
                throw new InvalidOperationException("Offline batch session is not executable.");
            if (session.state == "COMPLETED" || session.state == "ROLLED_BACK" ||
                session.state == "ROLLED_BACK_AFTER_FAILURE")
                throw new InvalidOperationException("Offline batch session is terminal: " + session.state);
            if (string.IsNullOrWhiteSpace(batchId))
                throw new InvalidOperationException("Batch id is required.");

            var selected = (selectedCanonicalIdentities ?? Enumerable.Empty<string>()).ToArray();
            if (selected.Length == 0)
                throw new InvalidOperationException("Batch selection is empty.");
            if (selected.Any(string.IsNullOrWhiteSpace) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new InvalidOperationException("Batch selection contains an empty or duplicate identity.");

            var all = session.plan.entries.ToDictionary(
                entry => entry.canonicalIdentity, entry => entry, StringComparer.Ordinal);
            if (selected.Any(identity => !all.ContainsKey(identity)))
                throw new InvalidOperationException("Batch selection contains an out-of-scope identity.");
            var completed = new HashSet<string>(
                session.completedCanonicalIdentities ?? new string[0], StringComparer.Ordinal);
            var prior = (session.batches ?? new BatchRecord[0]).SingleOrDefault(record =>
                string.Equals(record.batchId, batchId, StringComparison.Ordinal));
            if (prior != null)
            {
                if (!string.Equals(prior.state, "APPLIED", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "TRANSACTION_HAS_PENDING_STAGE_USE_ROLLBACK|" + batchId);
                if (!selected.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                        (prior.canonicalIdentities ?? new string[0])
                            .OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                    throw new InvalidOperationException("Batch id was already used for another selection.");
                ValidateOfflineBatchState(state, session.plan, completed);
                return new OfflineBatchResult
                {
                    writes = 0,
                    alreadyApplied = true,
                    completed = completed.Count,
                    total = all.Count,
                    state = session.state
                };
            }
            if (selected.Any(completed.Contains))
                throw new InvalidOperationException("Batch selection includes an already-completed identity.");

            ValidateSourceWriteOrder(session.plan.rows, completed, selected);
            ValidateOfflineBatchState(state, session.plan, completed);
            var changed = new List<DryRunEntry>();
            try
            {
                foreach (var identity in OrderCanonicalWrites(all, selected, false))
                {
                    var entry = all[identity];
                    ApplyOfflineEntryState(state, session.plan, entry, false);
                    changed.Add(entry);
                    if (failAfterWrites >= 0 && changed.Count >= failAfterWrites)
                        throw new InvalidOperationException("INJECTED_BATCH_FAILURE");
                }
            }
            catch
            {
                RollbackOfflinePlan(state, session.plan);
                session.completedCanonicalIdentities = new string[0];
                session.state = "ROLLED_BACK_AFTER_FAILURE";
                throw;
            }

            foreach (var identity in selected) completed.Add(identity);
            session.completedCanonicalIdentities = completed.OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            session.batches = (session.batches ?? new BatchRecord[0]).Concat(new[]
            {
                new BatchRecord
                {
                    batchId = batchId,
                    state = "APPLIED",
                    recordedAtUtc = DateTime.UtcNow.ToString("o"),
                    writes = changed.Count,
                    canonicalIdentities = selected.OrderBy(value => value,
                        StringComparer.Ordinal).ToArray()
                }
            }).ToArray();
            session.state = completed.Count == all.Count
                ? "FULLY_APPLIED_PENDING_VALIDATION"
                : "IN_PROGRESS";
            ValidateOfflineBatchState(state, session.plan, completed);
            return new OfflineBatchResult
            {
                writes = changed.Count,
                alreadyApplied = false,
                completed = completed.Count,
                total = all.Count,
                state = session.state
            };
        }

        public static OfflineBatchResult FinalizeOfflineBatch(
            IDictionary<string, OfflineBinding> state,
            OfflineBatchSession session)
        {
            if (session == null || session.plan == null)
                throw new InvalidOperationException("Offline batch session is missing.");
            var completed = new HashSet<string>(
                session.completedCanonicalIdentities ?? new string[0], StringComparer.Ordinal);
            if (completed.Count != session.plan.entries.Length)
                throw new InvalidOperationException("Offline batch session is incomplete.");
            ValidateOfflineBatchState(state, session.plan, completed);
            session.state = "COMPLETED";
            return new OfflineBatchResult
            {
                writes = 0,
                alreadyApplied = false,
                completed = completed.Count,
                total = session.plan.entries.Length,
                state = session.state
            };
        }

        public static int RollbackOfflineBatch(
            IDictionary<string, OfflineBinding> state,
            OfflineBatchSession session)
        {
            if (session == null || session.plan == null)
                throw new InvalidOperationException("Offline batch session is missing.");
            if (session.state == "ROLLED_BACK" || session.state == "ROLLED_BACK_AFTER_FAILURE")
                return 0;
            var restored = RollbackOfflinePlan(state, session.plan);
            session.completedCanonicalIdentities = new string[0];
            session.state = "ROLLED_BACK";
            return restored;
        }

        private static bool ApplyOfflineEntryState(
            IDictionary<string, OfflineBinding> state,
            DryRunPlan plan,
            DryRunEntry entry,
            bool rollback)
        {
            var affected = (plan.rows ?? new ApprovedRowAudit[0])
                .Where(row => string.Equals(row.canonicalIdentity, entry.canonicalIdentity,
                    StringComparison.Ordinal)).ToArray();
            if (affected.Length == 0)
                throw new InvalidOperationException(
                    "APPROVED_ROW_COVERAGE_MISSING|" + entry.canonicalIdentity);
            var changed = false;
            foreach (var row in affected)
            {
                OfflineBinding current;
                if (!state.TryGetValue(row.rowIdentity, out current))
                    throw new InvalidOperationException("MISSING_IDENTITY|" + row.rowIdentity);
                if (!string.Equals(current.nonTargetFingerprint, row.nonTargetFingerprint,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "NON_TARGET_FINGERPRINT_DRIFT|" + row.rowIdentity);
                var isBefore = IsOfflineRowBefore(current, row);
                var isAfter = IsOfflineRowAfter(current, row);
                if (!isBefore && !isAfter)
                    throw new InvalidOperationException((rollback
                        ? "UNKNOWN_ROLLBACK_STATE|"
                        : "UNKNOWN_STATE|") + row.rowIdentity);
                if (rollback ? isBefore : isAfter) continue;
                if (rollback)
                {
                    current.currentFontGuid = row.beforeFontGuid;
                    current.currentMaterialGlobalId = row.beforeMaterialGlobalId;
                    current.currentMaterialPath = row.beforeMaterialPath;
                    current.effectiveMaterialGlobalId = row.beforeEffectiveMaterialGlobalId;
                    current.effectiveMaterialPath = row.beforeEffectiveMaterialPath;
                    current.materialNormalizationReason =
                        row.beforeMaterialNormalizationReason;
                    current.savedReferenceOwnerIdentity = row.savedReferenceOwnerIdentity;
                    current.savedReferenceAssetPath = row.savedReferenceAssetPath;
                    current.savedReferenceAssetSha256 = row.savedReferenceAssetSha256;
                    current.savedReferenceProven = row.savedReferenceProven;
                    current.fontOverride = row.beforeFontOverride;
                    current.materialOverride = row.beforeMaterialOverride;
                }
                else
                {
                    current.currentFontGuid = row.afterFontGuid;
                    current.currentMaterialPath = row.afterMaterialPath;
                    current.currentMaterialGlobalId = "TARGET|" + row.afterMaterialPath;
                    current.effectiveMaterialPath = row.afterMaterialPath;
                    current.effectiveMaterialGlobalId =
                        "TARGET|" + row.afterMaterialPath;
                    current.materialNormalizationReason = string.Empty;
                    if (!row.coveredByPrefabSource && !string.IsNullOrWhiteSpace(
                            row.prefabSourceIdentity))
                    {
                        current.fontOverride = true;
                        current.materialOverride = true;
                    }
                }
                changed = true;
            }
            return changed;
        }

        private static bool IsOfflineRowBefore(OfflineBinding current, ApprovedRowAudit row)
        {
            return string.Equals(current.currentFontGuid, row.beforeFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(current.currentMaterialGlobalId,
                    row.beforeMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(current.effectiveMaterialGlobalId,
                    row.beforeEffectiveMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(current.materialNormalizationReason,
                    row.beforeMaterialNormalizationReason, StringComparison.Ordinal) &&
                string.Equals(current.savedReferenceOwnerIdentity,
                    row.savedReferenceOwnerIdentity, StringComparison.Ordinal) &&
                string.Equals(current.savedReferenceAssetPath,
                    row.savedReferenceAssetPath, StringComparison.Ordinal) &&
                string.Equals(current.savedReferenceAssetSha256,
                    row.savedReferenceAssetSha256, StringComparison.Ordinal) &&
                current.savedReferenceProven == row.savedReferenceProven &&
                current.fontOverride == row.beforeFontOverride &&
                current.materialOverride == row.beforeMaterialOverride;
        }

        private static bool IsOfflineRowAfter(OfflineBinding current, ApprovedRowAudit row)
        {
            var directPrefabInstance = !row.coveredByPrefabSource &&
                !string.IsNullOrWhiteSpace(row.prefabSourceIdentity);
            var expectedFontOverride = directPrefabInstance || row.beforeFontOverride;
            var expectedMaterialOverride = directPrefabInstance || row.beforeMaterialOverride;
            return string.Equals(current.currentFontGuid, row.afterFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(current.currentMaterialPath, row.afterMaterialPath,
                    StringComparison.Ordinal) &&
                string.Equals(current.effectiveMaterialPath, row.afterMaterialPath,
                    StringComparison.Ordinal) &&
                string.IsNullOrEmpty(current.materialNormalizationReason) &&
                current.fontOverride == expectedFontOverride &&
                current.materialOverride == expectedMaterialOverride;
        }

        private static void ValidateSourceWriteOrder(
            IEnumerable<ApprovedRowAudit> rows,
            ISet<string> completed,
            IEnumerable<string> selected)
        {
            var selectedSet = new HashSet<string>(selected ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            foreach (var sourceIdentity in selectedSet)
            {
                var pendingPredecessors = (rows ?? Enumerable.Empty<ApprovedRowAudit>())
                    .Where(row => string.Equals(row.prefabSourceIdentity, sourceIdentity,
                            StringComparison.Ordinal) &&
                        !string.Equals(row.canonicalIdentity, sourceIdentity,
                            StringComparison.Ordinal))
                    .Select(row => row.canonicalIdentity).Distinct(StringComparer.Ordinal)
                    .Where(identity => !completed.Contains(identity) &&
                        !selectedSet.Contains(identity))
                    .OrderBy(identity => identity, StringComparer.Ordinal).ToArray();
                if (pendingPredecessors.Length > 0)
                    throw new InvalidOperationException(
                        "SOURCE_ORDER_REQUIRES_INSTANCE_FIRST|" + sourceIdentity + "|" +
                        string.Join(",", pendingPredecessors));
            }
        }

        private static IEnumerable<string> OrderCanonicalWrites(
            IDictionary<string, DryRunEntry> all,
            IEnumerable<string> identities,
            bool rollback)
        {
            var selected = (identities ?? Enumerable.Empty<string>()).ToArray();
            return rollback
                ? selected.OrderBy(identity => all[identity].writeAssetPath.EndsWith(".unity",
                        StringComparison.OrdinalIgnoreCase))
                    .ThenBy(identity => all[identity].writeAssetPath, StringComparer.Ordinal)
                    .ThenBy(identity => identity, StringComparer.Ordinal)
                : selected.OrderByDescending(identity => all[identity].writeAssetPath.EndsWith(
                        ".unity", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(identity => all[identity].writeAssetPath, StringComparer.Ordinal)
                    .ThenBy(identity => identity, StringComparer.Ordinal);
        }

        private static void ValidateOfflineBatchState(
            IDictionary<string, OfflineBinding> state,
            DryRunPlan plan,
            ISet<string> completed)
        {
            foreach (var row in plan.rows ?? new ApprovedRowAudit[0])
            {
                OfflineBinding current;
                if (!state.TryGetValue(row.rowIdentity, out current))
                    throw new InvalidOperationException("MISSING_IDENTITY|" + row.rowIdentity);
                if (!string.Equals(current.nonTargetFingerprint, row.nonTargetFingerprint,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "NON_TARGET_FINGERPRINT_DRIFT|" + row.rowIdentity);
                var expected = completed.Contains(row.canonicalIdentity)
                    ? IsOfflineRowAfter(current, row)
                    : IsOfflineRowBefore(current, row);
                if (!expected)
                    throw new InvalidOperationException((completed.Contains(row.canonicalIdentity)
                        ? "COMPLETED_STATE_DRIFT|" : "PENDING_STATE_DRIFT|") +
                        row.rowIdentity);
            }
        }

        [MenuItem(MenuRoot + "Preview Approved Paired Font Migration (Read Only)")]
        public static void PreviewApprovedFromMenu()
        {
            var result = PreviewApprovedMigration(
                ApprovedMappingPath, ApprovalEvidencePath, DefaultApprovedPlanPath);
            Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_PREVIEW " +
                JsonUtility.ToJson(result, true));
        }

        [MenuItem(MenuRoot + "Execute Approved Paired Font Migration")]
        public static void ExecuteApprovedFromMenu()
        {
            var taskId = ReadArgument("-fontTaskId");
            if (string.IsNullOrWhiteSpace(taskId))
                throw new InvalidOperationException("-fontTaskId is required for backup ownership.");
            Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_EXECUTED " +
                ExecuteApprovedMigration(taskId, ApprovedMappingPath, ApprovalEvidencePath,
                    DefaultApprovedPlanPath, DefaultTransactionPath));
        }

        [MenuItem(MenuRoot + "Rollback Approved Paired Font Migration")]
        public static void RollbackApprovedFromMenu()
        {
            Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_ROLLED_BACK " +
                RollbackApprovedMigration(DefaultTransactionPath));
        }

        public static void PreviewApprovedBatch()
        {
            BatchGuard(() =>
            {
                var mappingPath = ReadArgument("-fontMapping") ?? ApprovedMappingPath;
                var evidencePath = ReadArgument("-fontApprovalEvidence") ?? ApprovalEvidencePath;
                var planPath = ReadArgument("-fontPlan") ?? DefaultApprovedPlanPath;
                var result = PreviewApprovedMigration(mappingPath, evidencePath,
                    ReadArgument("-fontSupplementalMapping"),
                    ReadArgument("-fontSupplementalApprovalEvidence"), planPath);
                Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_PREVIEW " + JsonUtility.ToJson(result));
            });
        }

        public static void ExecuteApprovedBatch()
        {
            BatchGuard(() =>
            {
                var taskId = ReadArgument("-fontTaskId");
                if (string.IsNullOrWhiteSpace(taskId))
                    throw new InvalidOperationException("-fontTaskId is required.");
                var result = ExecuteApprovedMigration(
                    taskId,
                    ReadArgument("-fontMapping") ?? ApprovedMappingPath,
                    ReadArgument("-fontApprovalEvidence") ?? ApprovalEvidencePath,
                    ReadArgument("-fontSupplementalMapping"),
                    ReadArgument("-fontSupplementalApprovalEvidence"),
                    ReadArgument("-fontPlan") ?? DefaultApprovedPlanPath,
                    ReadArgument("-fontTransaction") ?? DefaultTransactionPath);
                Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_EXECUTED " + result);
            });
        }

        public static void RollbackApprovedBatch()
        {
            BatchGuard(() => Debug.Log("[ShortCycleFontSwap] APPROVED_FONT_ROLLED_BACK " +
                RollbackApprovedMigration(
                    ReadArgument("-fontTransaction") ?? DefaultTransactionPath)));
        }

        public static void DiagnoseApprovedFingerprintsBatch()
        {
            BatchGuard(() => Debug.Log("[ShortCycleFontSwap] APPROVED_FINGERPRINT_DIAGNOSTIC " +
                JsonUtility.ToJson(DiagnoseApprovedNonTargetFingerprints(
                    ReadArgument("-fontPlan") ?? DefaultApprovedPlanPath,
                    ReadArgument("-fontFingerprintDiagnostic") ??
                        ".ai-workspace/runtime/font-migration/fingerprint-diagnostic.json"))));
        }

        public static ApprovedMigrationPlan PreviewApprovedMigration(
            string mappingPath,
            string approvalEvidencePath,
            string planPath)
        {
            return PreviewApprovedMigration(mappingPath, approvalEvidencePath,
                null, null, planPath);
        }

        public static ApprovedMigrationPlan PreviewApprovedMigration(
            string mappingPath,
            string approvalEvidencePath,
            string supplementalMappingPath,
            string supplementalApprovalEvidencePath,
            string planPath)
        {
            var mappingFullPath = ToFullPath(mappingPath);
            var evidenceFullPath = ToFullPath(approvalEvidencePath);
            if (!File.Exists(mappingFullPath))
                throw new FileNotFoundException("Approved font mapping is missing.", mappingFullPath);
            if (!File.Exists(evidenceFullPath))
                throw new FileNotFoundException("Font approval evidence is missing.", evidenceFullPath);

            var mappingHash = Sha256File(mappingFullPath);
            if (!string.Equals(mappingHash, ApprovedMappingSha256, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Approved mapping SHA-256 changed: " + mappingHash);
            var evidence = File.ReadAllText(evidenceFullPath, new UTF8Encoding(false, true));
            if (evidence.IndexOf(ApprovedMappingSha256, StringComparison.Ordinal) < 0 ||
                evidence.IndexOf("批准 PRE-1 字体与材质迁移草案", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Approval evidence does not bind the reviewed CSV hash and user decision.");

            var baseMappings = ParseApprovedCsvText(
                File.ReadAllText(mappingFullPath, new UTF8Encoding(false, true)));
            ValidateApprovedScope(baseMappings);
            var hasSupplement = !string.IsNullOrWhiteSpace(supplementalMappingPath) ||
                !string.IsNullOrWhiteSpace(supplementalApprovalEvidencePath);
            if (hasSupplement && (string.IsNullOrWhiteSpace(supplementalMappingPath) ||
                    string.IsNullOrWhiteSpace(supplementalApprovalEvidencePath)))
                throw new InvalidOperationException(
                    "Supplemental mapping and approval evidence must be provided together.");
            var supplementalHash = string.Empty;
            var supplementalMappings = new ApprovedMapping[0];
            if (hasSupplement)
            {
                var supplementalFullPath = ToFullPath(supplementalMappingPath);
                var supplementalEvidenceFullPath = ToFullPath(
                    supplementalApprovalEvidencePath);
                if (!File.Exists(supplementalFullPath))
                    throw new FileNotFoundException(
                        "Supplemental font mapping is missing.", supplementalFullPath);
                if (!File.Exists(supplementalEvidenceFullPath))
                    throw new FileNotFoundException(
                        "Supplemental approval evidence is missing.",
                        supplementalEvidenceFullPath);
                supplementalHash = Sha256File(supplementalFullPath);
                var supplementalEvidence = File.ReadAllText(
                    supplementalEvidenceFullPath, new UTF8Encoding(false, true));
                ValidateSupplementalAuthorization(mappingHash, supplementalHash,
                    supplementalEvidence);
                supplementalMappings = ParseSupplementalApprovedCsvText(
                    File.ReadAllText(supplementalFullPath,
                        new UTF8Encoding(false, true)));
            }
            var mappings = baseMappings.Concat(supplementalMappings).ToArray();
            var expectedRawEntryCount = hasSupplement
                ? CombinedRawEntryCount
                : ApprovedRawEntryCount;
            var approvedBaselineFiles = GetApprovedBaselineFiles(expectedRawEntryCount);
            VerifyFileSnapshots(approvedBaselineFiles);
            var approvedIdentitySet = new HashSet<string>(
                mappings.Select(item => item.Identity), StringComparer.Ordinal);
            var bindings = new List<OfflineBinding>();
            foreach (var assetGroup in mappings.GroupBy(item => item.assetPath, StringComparer.Ordinal))
                bindings.AddRange(ReadApprovedBindings(
                    assetGroup.Key, assetGroup.ToArray(), approvedIdentitySet));

            var dryRun = BuildDryRunPlan(mappings, bindings, expectedRawEntryCount);
            string[] propagationGuardAssetPaths;
            var propagationReasons = InspectUnapprovedPrefabPropagation(
                mappings, approvedIdentitySet, out propagationGuardAssetPaths);
            var targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                ShortCycleFontAtlasBuilder.StaticFontAssetPath);
            var materialPlans = BuildMaterialPlans(mappings, targetFont);
            var reasons = dryRun.reasons.Concat(propagationReasons).Concat(materialPlans
                    .Where(item => !item.ready)
                    .Select(item => "MATERIAL_NOT_READY|" + item.targetMaterialPath + "|" +
                        item.limitation))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var result = new ApprovedMigrationPlan
            {
                nonTargetFingerprintVersion = NonTargetFingerprintVersion,
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                mappingPath = mappingPath,
                mappingSha256 = mappingHash,
                approvalEvidencePath = approvalEvidencePath,
                supplementalMappingPath = hasSupplement ? supplementalMappingPath : string.Empty,
                supplementalMappingSha256 = supplementalHash,
                supplementalApprovalEvidencePath = hasSupplement
                    ? supplementalApprovalEvidencePath
                    : string.Empty,
                approvedRawEntryCount = expectedRawEntryCount,
                approvedAssetPaths = mappings.Select(item => item.assetPath)
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray(),
                targetFontPath = ShortCycleFontAtlasBuilder.StaticFontAssetPath,
                targetFontGuid = GuidFor(targetFont),
                rawEntryCount = dryRun.rawEntryCount,
                uniqueEntryCount = dryRun.uniqueEntryCount,
                executeAllowed = dryRun.executeAllowed && propagationReasons.Length == 0 &&
                    targetFont != null &&
                    string.Equals(GuidFor(targetFont), ApprovedTargetFontGuid,
                        StringComparison.Ordinal) && materialPlans.All(item => item.ready),
                reasons = reasons,
                materials = materialPlans,
                entries = dryRun.entries,
                rows = dryRun.rows,
                propagationGuardAssetPaths = propagationGuardAssetPaths,
                approvedBaselineFiles = approvedBaselineFiles
            };
            if (targetFont == null)
                result.reasons = result.reasons.Concat(new[] { "TARGET_FONT_MISSING" }).ToArray();
            else if (!string.Equals(result.targetFontGuid, ApprovedTargetFontGuid,
                    StringComparison.Ordinal))
                result.reasons = result.reasons.Concat(new[] { "TARGET_FONT_GUID_DRIFT" }).ToArray();
            WriteJsonObject(planPath, result);
            return result;
        }

        public static string ExecuteApprovedMigration(
            string taskId,
            string mappingPath,
            string approvalEvidencePath,
            string planPath,
            string transactionPath)
        {
            return ExecuteApprovedMigration(taskId, mappingPath, approvalEvidencePath,
                null, null, planPath, transactionPath);
        }

        public static string ExecuteApprovedMigration(
            string taskId,
            string mappingPath,
            string approvalEvidencePath,
            string supplementalMappingPath,
            string supplementalApprovalEvidencePath,
            string planPath,
            string transactionPath)
        {
            var transaction = BeginApprovedMigration(taskId, mappingPath,
                approvalEvidencePath, supplementalMappingPath,
                supplementalApprovalEvidencePath, planPath, transactionPath);
            var applied = ApplyApprovedBatch(transactionPath, "ALL",
                transaction.entries.Select(entry => entry.canonicalIdentity));
            var finalized = FinalizeApprovedMigration(transactionPath);
            return applied + " " + finalized;
        }

        /// <summary>
        /// Creates a fixed full-scope plan and backup without changing a business reference.
        /// ENGINE must call this once before choosing a sample or explicit batch identities.
        /// </summary>
        public static TransactionRecord BeginApprovedMigration(
            string taskId,
            string mappingPath,
            string approvalEvidencePath,
            string planPath,
            string transactionPath)
        {
            return BeginApprovedMigration(taskId, mappingPath, approvalEvidencePath,
                null, null, planPath, transactionPath);
        }

        public static TransactionRecord BeginApprovedMigration(
            string taskId,
            string mappingPath,
            string approvalEvidencePath,
            string supplementalMappingPath,
            string supplementalApprovalEvidencePath,
            string planPath,
            string transactionPath)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task id is required.", "taskId");
            if (File.Exists(ToFullPath(transactionPath)))
                throw new InvalidOperationException(
                    "Transaction path already exists; use a distinct sample/full path or resume it.");
            var plan = PreviewApprovedMigration(mappingPath, approvalEvidencePath,
                supplementalMappingPath, supplementalApprovalEvidencePath, planPath);
            if (!plan.executeAllowed)
                throw new InvalidOperationException(
                    "Approved migration preflight refused all writes: " +
                    string.Join(" | ", plan.reasons ?? new string[0]));

            ValidateApprovedBaselineSnapshotSet(plan.approvedBaselineFiles,
                plan.approvedRawEntryCount);
            VerifyFileSnapshots(plan.approvedBaselineFiles);
            EnsureApprovedScenesClosed(plan.approvedAssetPaths);
            ValidateLiveTransactionState(plan.rows, new HashSet<string>(StringComparer.Ordinal));
            var materialFolderExistedAtBegin = Directory.Exists(
                ToFullPath(ApprovedMaterialFolderPath));
            ValidateMaterialFolderPair(materialFolderExistedAtBegin,
                File.Exists(ToFullPath(ApprovedMaterialFolderPath + ".meta")));
            var backupRoot = CreateBackup(taskId, plan, materialFolderExistedAtBegin);
            var transaction = new TransactionRecord
            {
                nonTargetFingerprintVersion = plan.nonTargetFingerprintVersion,
                taskId = taskId,
                createdAtUtc = DateTime.UtcNow.ToString("o"),
                state = "PLANNED",
                mappingPath = mappingPath,
                mappingSha256 = plan.mappingSha256,
                approvalEvidencePath = approvalEvidencePath,
                supplementalMappingPath = plan.supplementalMappingPath,
                supplementalMappingSha256 = plan.supplementalMappingSha256,
                supplementalApprovalEvidencePath = plan.supplementalApprovalEvidencePath,
                approvedRawEntryCount = plan.approvedRawEntryCount,
                approvedAssetPaths = plan.approvedAssetPaths,
                planPath = planPath,
                targetFontPath = plan.targetFontPath,
                targetFontGuid = plan.targetFontGuid,
                backupRoot = backupRoot,
                materials = plan.materials,
                entries = plan.entries,
                rows = plan.rows,
                propagationGuardAssetPaths = plan.propagationGuardAssetPaths,
                approvedBaselineFiles = plan.approvedBaselineFiles,
                createdAssets = new CreatedAssetRecord[0],
                completedCanonicalIdentities = new string[0],
                batches = new BatchRecord[0],
                materialFolderExistedAtBegin = materialFolderExistedAtBegin,
                ownedMaterialFolderPath = string.Empty,
                ownedMaterialFolderGuid = string.Empty,
                ownedMaterialFolderMetaSha256 = string.Empty
            };
            transaction.expectedFiles = CaptureFileSnapshots(GetTransactionFilePaths(transaction));
            WriteJsonObject(transactionPath, transaction);
            return transaction;
        }

        /// <summary>
        /// Applies exactly one explicit subset from the fixed plan. An identical retry of a
        /// recorded batch is a verified no-op; every other completed/unknown/duplicate selection
        /// is rejected before materials or business references are written.
        /// </summary>
        public static string ApplyApprovedBatch(
            string transactionPath,
            string batchId,
            IEnumerable<string> selectedCanonicalIdentities)
        {
            var transaction = ReadExecutableTransaction(transactionPath);
            if (transaction.state == "COMPLETED" || transaction.state == "ROLLED_BACK" ||
                transaction.state == "ROLLED_BACK_AFTER_FAILURE" ||
                transaction.state == "ROLLBACK_FAILED")
                throw new InvalidOperationException("Transaction is terminal: " + transaction.state);
            if (string.IsNullOrWhiteSpace(batchId))
                throw new InvalidOperationException("Batch id is required.");
            EnsureApprovedScenesClosed(transaction.approvedAssetPaths);

            var selected = (selectedCanonicalIdentities ?? Enumerable.Empty<string>()).ToArray();
            if (selected.Length == 0)
                throw new InvalidOperationException("Batch selection is empty.");
            if (selected.Any(string.IsNullOrWhiteSpace) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new InvalidOperationException(
                    "Batch selection contains an empty or duplicate identity.");
            var all = transaction.entries.ToDictionary(entry => entry.canonicalIdentity,
                entry => entry, StringComparer.Ordinal);
            if (selected.Any(identity => !all.ContainsKey(identity)))
                throw new InvalidOperationException(
                    "Batch selection contains a forged or out-of-scope identity.");
            var completed = new HashSet<string>(
                transaction.completedCanonicalIdentities ?? new string[0], StringComparer.Ordinal);
            var prior = (transaction.batches ?? new BatchRecord[0]).SingleOrDefault(record =>
                string.Equals(record.batchId, batchId, StringComparison.Ordinal));

            VerifyFileSnapshots(transaction.expectedFiles);
            ValidateLiveTransactionState(transaction.rows, completed);
            if (prior != null)
            {
                if (!selected.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                        (prior.canonicalIdentities ?? new string[0])
                            .OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                    throw new InvalidOperationException(
                        "Batch id was already used for another selection.");
                return "batch=" + batchId + " alreadyApplied=true changed=0 completed=" +
                    completed.Count + "/" + all.Count;
            }
            if (selected.Any(completed.Contains))
                throw new InvalidOperationException(
                    "Batch selection includes an already-completed identity.");
            ValidateSourceWriteOrder(transaction.rows, completed, selected);

            var creatingMaterials =
                (transaction.createdAssets ?? new CreatedAssetRecord[0]).Length == 0;
            if (creatingMaterials)
            {
                ValidateMaterialCreationBaseline(transaction);
                transaction.ownedMaterialFolderPath = transaction.materialFolderExistedAtBegin
                    ? string.Empty
                    : ApprovedMaterialFolderPath;
                transaction.createdAssets = (transaction.materials ?? new MaterialPlan[0])
                    .OrderBy(material => material.targetMaterialPath,
                        StringComparer.Ordinal)
                    .Select(material => new CreatedAssetRecord
                    {
                        path = material.targetMaterialPath,
                        fingerprint = material.expectedMaterialFingerprint,
                        creationBatchId = batchId,
                        guid = string.Empty,
                        assetSha256 = string.Empty,
                        metaSha256 = string.Empty
                    }).ToArray();
            }
            var applyingBatch = BuildApplyingBatchRecord(transaction, batchId, selected,
                creatingMaterials);
            transaction.batches = (transaction.batches ?? new BatchRecord[0])
                .Concat(new[] { applyingBatch }).ToArray();
            transaction.state = "APPLYING";
            WriteJsonObject(transactionPath, transaction);

            var mutationStarted = true;
            try
            {
                if (creatingMaterials)
                {
                    var created = EnsureApprovedMaterials(transaction.materials);
                    var expectedOwned = transaction.createdAssets
                        .OrderBy(record => record.path, StringComparer.Ordinal).ToArray();
                    var actualOwned = created.OrderBy(record => record.path,
                        StringComparer.Ordinal).ToArray();
                    if (expectedOwned.Length != actualOwned.Length ||
                        expectedOwned.Where((record, index) =>
                            !string.Equals(record.path, actualOwned[index].path,
                                StringComparison.Ordinal) ||
                            !string.Equals(record.fingerprint,
                                actualOwned[index].fingerprint,
                                StringComparison.Ordinal)).Any())
                        throw new InvalidOperationException(
                            "CREATED_MATERIAL_OWNERSHIP_DRIFT");
                    SealOwnedCreationEvidence(transaction, batchId);
                    WriteJsonObject(transactionPath, transaction);
                }
                VerifyOwnedMaterials(transaction.createdAssets);
                VerifyOwnedMaterialFolder(transaction);
                RequireStageSnapshotTransition(applyingBatch.preStageFiles,
                    CaptureFileSnapshots(GetTransactionFilePaths(transaction)),
                    applyingBatch.mutationRules);
                ValidateLiveTransactionState(transaction.rows, completed);
                var changed = ApplyLiveEntries(selected.Select(identity => all[identity]).ToArray(),
                    false);
                AssetDatabase.SaveAssets();
                var postStageFiles = CaptureFileSnapshots(
                    GetTransactionFilePaths(transaction));
                RequireStageSnapshotTransition(applyingBatch.preStageFiles,
                    postStageFiles, applyingBatch.mutationRules);
                VerifyOwnedMaterials(transaction.createdAssets);
                VerifyOwnedMaterialFolder(transaction);
                var completedAfter = new HashSet<string>(completed,
                    StringComparer.Ordinal);
                foreach (var identity in selected) completedAfter.Add(identity);
                ValidateLiveTransactionState(transaction.rows, completedAfter);
                completed = completedAfter;
                transaction.completedCanonicalIdentities = completed.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
                applyingBatch.state = "APPLIED";
                applyingBatch.recordedAtUtc = DateTime.UtcNow.ToString("o");
                applyingBatch.writes = changed;
                transaction.state = completed.Count == all.Count
                    ? "FULLY_APPLIED_PENDING_VALIDATION"
                    : "IN_PROGRESS";
                transaction.expectedFiles = postStageFiles;
                WriteJsonObject(transactionPath, transaction);
                return "batch=" + batchId + " alreadyApplied=false changed=" + changed +
                    " completed=" + completed.Count + "/" + all.Count +
                    " state=" + transaction.state;
            }
            catch (Exception executionFailure)
            {
                if (!mutationStarted) throw;
                try
                {
                    RollbackTransactionContents(transaction);
                    transaction.completedCanonicalIdentities = new string[0];
                    transaction.state = "ROLLED_BACK_AFTER_FAILURE";
                    transaction.expectedFiles = CaptureFileSnapshots(
                        GetTransactionFilePaths(transaction));
                    WriteJsonObject(transactionPath, transaction);
                }
                catch (Exception rollbackFailure)
                {
                    transaction.state = "ROLLBACK_FAILED";
                    WriteJsonObject(transactionPath, transaction);
                    throw new AggregateException(
                        "Approved font batch failed and rollback also failed.",
                        executionFailure, rollbackFailure);
                }
                throw;
            }
        }

        public static string FinalizeApprovedMigration(string transactionPath)
        {
            var transaction = ReadExecutableTransaction(transactionPath);
            if (transaction.state == "COMPLETED") return "alreadyCompleted=true";
            var completed = new HashSet<string>(
                transaction.completedCanonicalIdentities ?? new string[0], StringComparer.Ordinal);
            if (completed.Count != transaction.entries.Length)
                throw new InvalidOperationException("Transaction does not cover the full approved plan.");
            EnsureApprovedScenesClosed(transaction.approvedAssetPaths);
            VerifyFileSnapshots(transaction.expectedFiles);
            VerifyOwnedMaterials(transaction.createdAssets);
            VerifyOwnedMaterialFolder(transaction);
            ValidateLiveTransactionState(transaction.rows, completed);
            transaction.state = "COMPLETED";
            transaction.expectedFiles = CaptureFileSnapshots(GetTransactionFilePaths(transaction));
            WriteJsonObject(transactionPath, transaction);
            return "alreadyCompleted=false completed=" + completed.Count +
                " batches=" + (transaction.batches ?? new BatchRecord[0]).Length;
        }

        public static string RollbackApprovedMigration(string transactionPath)
        {
            var transaction = ReadTransaction(transactionPath, 0);
            if (transaction.state == "ROLLED_BACK" ||
                transaction.state == "ROLLED_BACK_AFTER_FAILURE")
                return "alreadyRolledBack=true restored=0";
            if (transaction.schemaVersion != ApprovedMigrationSchemaVersion ||
                transaction.nonTargetFingerprintVersion != NonTargetFingerprintVersion)
                throw new InvalidOperationException(
                    "ROLLBACK_REQUIRES_FRESH_SCHEMA10_TRANSACTION");
            if (transaction.entries == null || transaction.entries.Length == 0)
                throw new InvalidOperationException("Font transaction has no entries.");
            EnsureApprovedScenesClosed(transaction.approvedAssetPaths);
            var restored = RollbackTransactionContents(transaction);
            transaction.completedCanonicalIdentities = new string[0];
            transaction.state = "ROLLED_BACK";
            transaction.expectedFiles = CaptureFileSnapshots(GetTransactionFilePaths(transaction));
            WriteJsonObject(transactionPath, transaction);
            return "alreadyRolledBack=false restored=" + restored;
        }

        public static ApprovedMapping[] ParseApprovedCsvText(string csvText)
        {
            var parsed = DataTableCsvParser.Parse(csvText);
            if (!parsed.IsValid)
                throw new InvalidOperationException(
                    "Approved mapping CSV parse failed: " + string.Join(" | ", parsed.Errors));
            var required = new[]
            {
                "assetPath", "objectPath", "componentIndex", "currentFontGuid",
                "currentMaterialGlobalId", "targetFontGuid", "proposedMaterialPath",
                "sourceFontChanges", "approvalStatus"
            };
            var indexes = required.ToDictionary(
                name => name, name => parsed.Table.GetHeaderIndex(name), StringComparer.Ordinal);
            var missing = indexes.Where(pair => pair.Value < 0).Select(pair => pair.Key).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException(
                    "Approved mapping CSV headers are missing: " + string.Join(",", missing));

            var rows = new List<ApprovedMapping>();
            foreach (var row in parsed.Table.Rows)
            {
                int componentIndex;
                bool sourceFontChanges;
                if (!int.TryParse(row.Values[indexes["componentIndex"]], out componentIndex) ||
                    componentIndex < 0)
                    throw new InvalidOperationException(
                        "Invalid componentIndex at CSV line " + row.LineNumber + ".");
                if (!bool.TryParse(row.Values[indexes["sourceFontChanges"]], out sourceFontChanges))
                    throw new InvalidOperationException(
                        "Invalid sourceFontChanges at CSV line " + row.LineNumber + ".");
                rows.Add(new ApprovedMapping
                {
                    assetPath = NormalizeAssetPath(row.Values[indexes["assetPath"]]),
                    objectPath = row.Values[indexes["objectPath"]],
                    componentIndex = componentIndex,
                    currentFontGuid = row.Values[indexes["currentFontGuid"]],
                    currentMaterialGlobalId = row.Values[indexes["currentMaterialGlobalId"]],
                    targetFontGuid = row.Values[indexes["targetFontGuid"]],
                    proposedMaterialPath = NormalizeAssetPath(
                        row.Values[indexes["proposedMaterialPath"]]),
                    sourceFontChanges = sourceFontChanges,
                    approvalStatus = row.Values[indexes["approvalStatus"]]
                });
            }
            return rows.ToArray();
        }

        public static ApprovedMapping[] ParseSupplementalApprovedCsvText(string csvText)
        {
            var parsed = DataTableCsvParser.Parse(csvText);
            if (!parsed.IsValid)
                throw new InvalidOperationException(
                    "Supplemental mapping CSV parse failed: " +
                    string.Join(" | ", parsed.Errors));
            var required = new[]
            {
                "assetPath", "objectPath", "componentIndex", "sourcePrefab",
                "sourceObjectPath", "sourceComponentFileId", "beforeFontGuid",
                "fontInherited", "beforeMaterialGuid", "beforeMaterialFileId",
                "materialOverridden", "targetFontGuid", "proposedMaterialPath",
                "proposedFields", "approvalStatus"
            };
            var indexes = required.ToDictionary(
                name => name, name => parsed.Table.GetHeaderIndex(name), StringComparer.Ordinal);
            var missing = indexes.Where(pair => pair.Value < 0).Select(pair => pair.Key).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException(
                    "Supplemental mapping CSV headers are missing: " +
                    string.Join(",", missing));

            var rows = new List<ApprovedMapping>();
            foreach (var row in parsed.Table.Rows)
            {
                int componentIndex;
                long materialFileId;
                bool fontInherited;
                bool materialOverridden;
                if (!int.TryParse(row.Values[indexes["componentIndex"]],
                        NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out componentIndex) || componentIndex < 0)
                    throw new InvalidOperationException(
                        "Invalid supplemental componentIndex at CSV line " +
                        row.LineNumber + ".");
                if (!long.TryParse(row.Values[indexes["beforeMaterialFileId"]],
                        NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out materialFileId))
                    throw new InvalidOperationException(
                        "Invalid supplemental material file id at CSV line " +
                        row.LineNumber + ".");
                if (!bool.TryParse(row.Values[indexes["fontInherited"]],
                        out fontInherited) ||
                    !bool.TryParse(row.Values[indexes["materialOverridden"]],
                        out materialOverridden))
                    throw new InvalidOperationException(
                        "Invalid supplemental override flags at CSV line " +
                        row.LineNumber + ".");

                var assetPath = NormalizeAssetPath(row.Values[indexes["assetPath"]]);
                var objectPath = row.Values[indexes["objectPath"]];
                var sourcePrefab = NormalizeAssetPath(row.Values[indexes["sourcePrefab"]]);
                var sourceObjectPath = row.Values[indexes["sourceObjectPath"]];
                rows.Add(new ApprovedMapping
                {
                    assetPath = assetPath,
                    objectPath = objectPath,
                    componentIndex = componentIndex,
                    currentFontGuid = row.Values[indexes["beforeFontGuid"]],
                    currentMaterialGlobalId = MakeAssetGlobalId(
                        row.Values[indexes["beforeMaterialGuid"]], materialFileId),
                    targetFontGuid = row.Values[indexes["targetFontGuid"]],
                    proposedMaterialPath = NormalizeAssetPath(
                        row.Values[indexes["proposedMaterialPath"]]),
                    sourceFontChanges = false,
                    approvalStatus = row.Values[indexes["approvalStatus"]],
                    enforcePrefabShape = true,
                    expectedPrefabSourceIdentity = MakeIdentity(sourcePrefab,
                        sourceObjectPath, componentIndex),
                    expectedFontOverride = !fontInherited,
                    expectedMaterialOverride = materialOverridden
                });

                var label = objectPath.EndsWith("/BadgeLabel", StringComparison.Ordinal)
                    ? "BadgeLabel"
                    : objectPath.EndsWith("/NameLabel", StringComparison.Ordinal)
                        ? "NameLabel"
                        : string.Empty;
                var expectedSourceFileId = label == "BadgeLabel"
                    ? "8828566160680705185"
                    : label == "NameLabel" ? "4235966125065772450" : string.Empty;
                if (!string.Equals(sourcePrefab, SupplementalSourcePrefabPath,
                        StringComparison.Ordinal) ||
                    !string.Equals(sourceObjectPath, "FridgeSlot/" + label,
                        StringComparison.Ordinal) ||
                    !string.Equals(row.Values[indexes["sourceComponentFileId"]],
                        expectedSourceFileId, StringComparison.Ordinal) ||
                    !string.Equals(row.Values[indexes["proposedFields"]],
                        "m_fontAsset + m_sharedMaterial", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Supplemental source contract drift at CSV line " +
                        row.LineNumber + ".");
            }
            var result = rows.ToArray();
            ValidateSupplementalScope(result);
            return result;
        }

        public static void ValidateSupplementalAuthorization(
            string baseMappingHash,
            string supplementalMappingHash,
            string supplementalApprovalEvidence)
        {
            if (!string.Equals(baseMappingHash, ApprovedMappingSha256,
                    StringComparison.Ordinal) ||
                !string.Equals(supplementalMappingHash, SupplementalMappingSha256,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Approved base or supplemental mapping hash changed.");
            if (string.IsNullOrWhiteSpace(supplementalApprovalEvidence) ||
                supplementalApprovalEvidence.IndexOf(ApprovedMappingSha256,
                    StringComparison.Ordinal) < 0 ||
                supplementalApprovalEvidence.IndexOf(SupplementalMappingSha256,
                    StringComparison.Ordinal) < 0 ||
                supplementalApprovalEvidence.IndexOf("Lab60 增量批准",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Supplemental approval evidence does not bind both immutable inputs.");
        }

        private static string MakeAssetGlobalId(string guid, long fileId)
        {
            var unsignedFileId = unchecked((ulong)fileId);
            return "GlobalObjectId_V1-3-" + guid + "-" +
                unsignedFileId.ToString(CultureInfo.InvariantCulture) + "-0";
        }

        private static string MakeAssetGlobalId(string guid, string fileId)
        {
            ulong unsignedFileId;
            if (!TryParseSavedNumericId(fileId, out unsignedFileId) || unsignedFileId == 0)
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|invalid_saved_file_id");
            return "GlobalObjectId_V1-3-" + NormalizeSavedGuid(guid) + "-" +
                unsignedFileId.ToString(CultureInfo.InvariantCulture) + "-0";
        }

        private static void ValidateApprovedScope(ApprovedMapping[] mappings)
        {
            if (mappings.Length != ApprovedRawEntryCount)
                throw new InvalidOperationException(
                    "Approved mapping row count changed: " + mappings.Length);
            var assets = mappings.Select(item => item.assetPath).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var expectedAssets = ApprovedAssetPaths.OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!assets.SequenceEqual(expectedAssets, StringComparer.Ordinal))
                throw new InvalidOperationException("Approved asset scope changed.");
            var materials = mappings.Select(item => item.proposedMaterialPath)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var expectedMaterials = ApprovedMaterialPaths
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!materials.SequenceEqual(expectedMaterials, StringComparer.Ordinal))
                throw new InvalidOperationException("Approved target material scope changed.");
            if (mappings.Any(item => !string.Equals(item.targetFontGuid,
                    ApprovedTargetFontGuid, StringComparison.Ordinal)))
                throw new InvalidOperationException("Approved target font GUID changed.");
            var sourceChanges = mappings.Where(item => item.sourceFontChanges)
                .Select(item => item.Identity).ToArray();
            if (sourceChanges.Length != 1 || !string.Equals(sourceChanges[0],
                    ApprovedSourceChangeIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoverTitle must remain the only approved source-font change.");
            if (mappings.Any(item => !string.Equals(item.approvalStatus,
                    "PROPOSED_ONLY", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Historical CSV approvalStatus changed; approval must remain external evidence.");
        }

        private static void ValidateSupplementalScope(ApprovedMapping[] mappings)
        {
            if (mappings.Length != SupplementalRawEntryCount)
                throw new InvalidOperationException(
                    "Supplemental mapping row count changed: " + mappings.Length);
            if (mappings.Select(item => item.Identity).Distinct(StringComparer.Ordinal).Count() !=
                SupplementalRawEntryCount)
                throw new InvalidOperationException(
                    "Supplemental mapping identities must be unique.");
            var expectedIdentities = Enumerable.Range(1, 30).SelectMany(slot =>
                    new[] { "BadgeLabel", "NameLabel" }.Select(label => MakeIdentity(
                        SupplementalAssetPath,
                        "Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_" +
                        slot.ToString("00", CultureInfo.InvariantCulture) + "/" + label, 0)))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var actualIdentities = mappings.Select(item => item.Identity)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!actualIdentities.SequenceEqual(expectedIdentities, StringComparer.Ordinal))
                throw new InvalidOperationException("Supplemental mapping identity scope changed.");
            var expectedMaterialId = MakeAssetGlobalId(SupplementalBeforeMaterialGuid,
                SupplementalBeforeMaterialFileId);
            if (mappings.Any(item => !string.Equals(item.assetPath, SupplementalAssetPath,
                        StringComparison.Ordinal) || item.componentIndex != 0 ||
                    !string.Equals(item.currentFontGuid, SupplementalBeforeFontGuid,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.currentMaterialGlobalId, expectedMaterialId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.targetFontGuid, ApprovedTargetFontGuid,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.proposedMaterialPath, ApprovedMaterialPaths[0],
                        StringComparison.Ordinal) || !item.enforcePrefabShape ||
                    item.expectedFontOverride || !item.expectedMaterialOverride ||
                    !string.Equals(item.approvalStatus, "PENDING_USER",
                        StringComparison.Ordinal)))
                throw new InvalidOperationException("Supplemental mapping semantics changed.");
        }

        private static IEnumerable<OfflineBinding> ReadApprovedBindings(
            string assetPath,
            ApprovedMapping[] mappings,
            ISet<string> approvedIdentities)
        {
            return WithAssetContents(assetPath, false, roots =>
            {
                var groups = EnumerateScopedText(assetPath, roots)
                    .GroupBy(text => MakeIdentity(assetPath, GetPath(text.gameObject),
                        Array.IndexOf(text.GetComponents<TMP_Text>(), text)), StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
                var result = new List<OfflineBinding>();
                foreach (var mapping in mappings)
                {
                    TMP_Text[] matches;
                    if (!groups.TryGetValue(mapping.Identity, out matches)) continue;
                    foreach (var text in matches)
                    {
                        var material = ReadSharedMaterial(text);
                        string prefabSourceIdentity;
                        bool fontOverride;
                        bool materialOverride;
                        var isPrefabInstance = TryReadPrefabRelationship(text,
                            out prefabSourceIdentity, out fontOverride, out materialOverride);
                        var saved = ReadSavedTmpReferences(text, assetPath, fontOverride,
                            materialOverride);
                        var effectiveMaterialId = GlobalIdFor(material);
                        var effectiveMaterialPath = AssetDatabase.GetAssetPath(material);
                        var normalization = ClassifyMaterialResolution(
                            BuildMaterialResolutionObservation(mapping.Identity,
                                mapping.currentFontGuid, mapping.currentMaterialGlobalId,
                                saved, material, text.font, prefabSourceIdentity,
                                saved.AssetShaMatches));
                        result.Add(new OfflineBinding
                        {
                            rowIdentity = mapping.Identity,
                            canonicalIdentity = mapping.Identity,
                            prefabSourceIdentity = approvedIdentities.Contains(prefabSourceIdentity)
                                ? prefabSourceIdentity
                                : string.Empty,
                            isPrefabInstance = isPrefabInstance,
                            fontOverride = saved.FontOverride,
                            materialOverride = saved.MaterialOverride,
                            loadedFontOverride = fontOverride,
                            loadedMaterialOverride = materialOverride,
                            writeAssetPath = mapping.assetPath,
                            writeObjectPath = mapping.objectPath,
                            writeComponentIndex = mapping.componentIndex,
                            currentFontGuid = saved.FontGuid,
                            currentMaterialGlobalId = saved.MaterialGlobalId,
                            currentMaterialPath = saved.MaterialPath,
                            approvedObservationFontGuid = mapping.currentFontGuid,
                            approvedObservationMaterialGlobalId =
                                mapping.currentMaterialGlobalId,
                            effectiveMaterialGlobalId = effectiveMaterialId,
                            effectiveMaterialPath = effectiveMaterialPath,
                            materialNormalizationReason = normalization,
                            savedReferenceOwnerIdentity = saved.OwnerIdentity,
                            savedReferenceAssetPath = saved.AssetPath,
                            savedReferenceAssetSha256 = saved.AssetSha256,
                            savedReferenceSourceAssetPath = saved.SourceAssetPath,
                            savedReferenceSourceAssetSha256 = saved.SourceAssetSha256,
                            savedReferenceProven = saved.Proven,
                            savedReferenceAssetShaMatches = saved.AssetShaMatches,
                            nonTargetFingerprint = ComputeNonTargetFingerprint(text, assetPath)
                        });
                    }
                }
                return result.ToArray();
            });
        }

        private sealed class SavedTmpReferences
        {
            public string FontGuid;
            public string MaterialGlobalId;
            public string MaterialPath;
            public string OwnerIdentity;
            public string AssetPath;
            public string AssetSha256;
            public string SourceAssetPath;
            public string SourceAssetSha256;
            public bool Proven;
            public bool AssetShaMatches;
            public bool FontOverride;
            public bool MaterialOverride;
        }

        [Serializable]
        public sealed class SavedPrefabOverrideEvidence
        {
            public bool relationshipProven;
            public bool fontOverride;
            public bool materialOverride;
            public string fontGlobalId;
            public string materialGlobalId;
            public string ownerIdentity;
            public string sourceIdentity;
        }

        private static SavedTmpReferences ReadSavedTmpReferences(
            TMP_Text text,
            string assetPath,
            bool loadedFontOverride,
            bool loadedMaterialOverride)
        {
            var source = PrefabUtility.IsPartOfPrefabInstance(text)
                ? PrefabUtility.GetCorrespondingObjectFromSource(text) as TMP_Text
                : null;
            if (source == null)
                return ReadDirectSavedTmpReferences(text, assetPath);

            var sourceSaved = ReadDirectSavedTmpReferences(source,
                AssetDatabase.GetAssetPath(source));
            assetPath = NormalizeAssetPath(assetPath);
            var fullPath = ToFullPath(assetPath);
            var ownerGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(text).ToString();
            var sourceGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(source).ToString();
            var evidence = ParseSavedPrefabOverrides(
                File.ReadAllText(fullPath, new UTF8Encoding(false, true)),
                AssetDatabase.AssetPathToGUID(assetPath), ownerGlobalId, sourceGlobalId);
            var assetHash = Sha256File(ToFullPath(assetPath));
            return new SavedTmpReferences
            {
                FontGuid = evidence.fontOverride
                    ? ParseSavedGlobalObjectId(evidence.fontGlobalId,
                        "saved_font_override").AssetGuid
                    : sourceSaved.FontGuid,
                MaterialGlobalId = evidence.materialOverride
                    ? evidence.materialGlobalId
                    : sourceSaved.MaterialGlobalId,
                MaterialPath = evidence.materialOverride
                    ? AssetDatabase.GUIDToAssetPath(ParseSavedGlobalObjectId(
                        evidence.materialGlobalId, "saved_material_override").AssetGuid)
                    : sourceSaved.MaterialPath,
                OwnerIdentity = evidence.ownerIdentity + "|SOURCE|" +
                    evidence.sourceIdentity,
                AssetPath = assetPath,
                AssetSha256 = assetHash,
                SourceAssetPath = sourceSaved.AssetPath,
                SourceAssetSha256 = sourceSaved.AssetSha256,
                Proven = true,
                AssetShaMatches = ApprovedBaselineMatches(assetPath, assetHash) &&
                    sourceSaved.AssetShaMatches,
                FontOverride = evidence.fontOverride,
                MaterialOverride = evidence.materialOverride
            };
        }

        /// <summary>
        /// Parses only the exact serialized PrefabInstance document. Loaded TMP state is
        /// intentionally absent from this API: it can expose derived overrides that were never
        /// saved. Zero exact entries means inheritance; duplicates and malformed references fail.
        /// </summary>
        public static SavedPrefabOverrideEvidence ParseSavedPrefabOverrides(
            string yaml,
            string ownerAssetGuid,
            string ownerGlobalId,
            string sourceGlobalId)
        {
            var owner = ParseSavedGlobalObjectId(ownerGlobalId, "saved_override_owner");
            var source = ParseSavedGlobalObjectId(sourceGlobalId, "saved_override_source");
            var normalizedOwnerGuid = NormalizeSavedGuid(ownerAssetGuid);
            if (!string.Equals(owner.AssetGuid, normalizedOwnerGuid,
                    StringComparison.Ordinal) || owner.TargetPrefabId == 0 ||
                source.TargetPrefabId != 0 ||
                string.Equals(owner.AssetGuid, source.AssetGuid, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "SAVED_PREFAB_RELATIONSHIP_UNSUPPORTED_OR_NESTED");
            var lines = (yaml ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            var start = -1;
            var end = lines.Length;
            for (var index = 0; index < lines.Length; index++)
            {
                ulong documentId;
                if (!TryParseYamlDocumentId(lines[index], out documentId)) continue;
                if (start >= 0)
                {
                    end = index;
                    break;
                }
                if (documentId == owner.TargetPrefabId) start = index + 1;
            }
            if (start < 0)
                throw new InvalidOperationException("SAVED_PREFAB_INSTANCE_DOCUMENT_MISSING|" +
                    owner.TargetPrefabId.ToString(CultureInfo.InvariantCulture));
            var sourcePrefabMarker = "m_SourcePrefab: {fileID: 100100000, guid: " +
                source.AssetGuid + ", type: 3}";
            if (!lines.Skip(start).Take(end - start).Any(line =>
                    line.Trim().Equals(sourcePrefabMarker, StringComparison.Ordinal)))
                throw new InvalidOperationException("SAVED_PREFAB_SOURCE_MISMATCH");

            var fontMatches = new List<string>();
            var materialMatches = new List<string>();
            for (var index = start; index < end; index++)
            {
                ulong targetId;
                string targetGuid;
                if (!TryParsePrefabModificationTarget(lines[index], out targetId,
                        out targetGuid) || targetId != source.TargetObjectId ||
                    !string.Equals(targetGuid, source.AssetGuid, StringComparison.Ordinal))
                    continue;
                if (index + 3 >= end)
                    throw new InvalidOperationException("SAVED_OVERRIDE_TRUNCATED");
                var propertyLine = lines[index + 1].Trim();
                var propertyPrefix = "propertyPath: ";
                if (!propertyLine.StartsWith(propertyPrefix, StringComparison.Ordinal)) continue;
                var propertyPath = propertyLine.Substring(propertyPrefix.Length).Trim();
                if (propertyPath != "m_fontAsset" && propertyPath != "m_sharedMaterial") continue;
                string fileId;
                string guid;
                if (!TryParseInlineObjectReference(lines[index + 3].Trim(),
                        "objectReference", out fileId, out guid))
                    throw new InvalidOperationException(
                        "SAVED_OVERRIDE_REFERENCE_MISSING|" + propertyPath);
                var globalId = MakeAssetGlobalId(guid, fileId);
                (propertyPath == "m_fontAsset" ? fontMatches : materialMatches).Add(globalId);
            }
            if (fontMatches.Count > 1 || materialMatches.Count > 1)
                throw new InvalidOperationException("SAVED_OVERRIDE_AMBIGUOUS|font=" +
                    fontMatches.Count + "|material=" + materialMatches.Count);
            return new SavedPrefabOverrideEvidence
            {
                relationshipProven = true,
                fontOverride = fontMatches.Count == 1,
                materialOverride = materialMatches.Count == 1,
                fontGlobalId = fontMatches.SingleOrDefault() ?? string.Empty,
                materialGlobalId = materialMatches.SingleOrDefault() ?? string.Empty,
                ownerIdentity = owner.Canonical,
                sourceIdentity = source.Canonical
            };
        }

        private static SavedTmpReferences ReadDirectSavedTmpReferences(
            TMP_Text text,
            string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            var fullPath = ToFullPath(assetPath);
            if (!File.Exists(fullPath))
                throw new InvalidOperationException("SAVED_REFERENCE_ASSET_MISSING|" + assetPath);
            var ownerIdText = GlobalObjectId.GetGlobalObjectIdSlow(text).ToString();
            var ownerId = ParseSavedGlobalObjectId(ownerIdText, "tmp_saved_owner");
            var lines = File.ReadAllLines(fullPath, new UTF8Encoding(false, true));
            var start = -1;
            var end = lines.Length;
            for (var index = 0; index < lines.Length; index++)
            {
                ulong documentId;
                if (!TryParseYamlDocumentId(lines[index], out documentId)) continue;
                if (start >= 0)
                {
                    end = index;
                    break;
                }
                if (documentId == ownerId.TargetObjectId) start = index + 1;
            }
            if (start < 0)
                throw new InvalidOperationException("SAVED_REFERENCE_DOCUMENT_MISSING|" +
                    assetPath + "|" + ownerId.Canonical);

            string fontGuid = null;
            string materialGuid = null;
            string materialFileId = null;
            for (var index = start; index < end; index++)
            {
                string guid;
                string fileId;
                if (TryParseYamlObjectReference(lines[index], "m_fontAsset", out fileId,
                        out guid))
                    fontGuid = guid;
                if (TryParseYamlObjectReference(lines[index], "m_sharedMaterial", out fileId,
                        out guid))
                {
                    materialFileId = fileId;
                    materialGuid = guid;
                }
            }
            if (string.IsNullOrWhiteSpace(fontGuid) ||
                string.IsNullOrWhiteSpace(materialGuid) ||
                string.IsNullOrWhiteSpace(materialFileId))
                throw new InvalidOperationException("SAVED_REFERENCE_FIELDS_MISSING|" +
                    assetPath + "|" + ownerId.Canonical);
            var materialId = MakeAssetGlobalId(materialGuid, materialFileId);
            var hash = Sha256File(fullPath);
            return new SavedTmpReferences
            {
                FontGuid = NormalizeSavedGuid(fontGuid),
                MaterialGlobalId = materialId,
                MaterialPath = AssetDatabase.GUIDToAssetPath(materialGuid),
                OwnerIdentity = assetPath + "|" + ownerId.Canonical,
                AssetPath = assetPath,
                AssetSha256 = hash,
                SourceAssetPath = assetPath,
                SourceAssetSha256 = hash,
                Proven = true,
                AssetShaMatches = ApprovedBaselineMatches(assetPath, hash),
                FontOverride = false,
                MaterialOverride = false
            };
        }

        private static bool TryParsePrefabModificationTarget(
            string line,
            out ulong targetId,
            out string guid)
        {
            targetId = 0;
            guid = string.Empty;
            var trimmed = (line ?? string.Empty).Trim();
            const string prefix = "- target: {fileID: ";
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var fileEnd = trimmed.IndexOf(',', prefix.Length);
            const string guidMarker = ", guid: ";
            var guidStart = trimmed.IndexOf(guidMarker, StringComparison.Ordinal);
            var guidEnd = guidStart < 0 ? -1 : trimmed.IndexOf(',',
                guidStart + guidMarker.Length);
            if (fileEnd < 0 || guidStart < 0 || guidEnd < 0 ||
                !TryParseSavedNumericId(trimmed.Substring(prefix.Length,
                    fileEnd - prefix.Length), out targetId))
                return false;
            guid = NormalizeSavedGuid(trimmed.Substring(guidStart + guidMarker.Length,
                guidEnd - guidStart - guidMarker.Length));
            return true;
        }

        private static bool TryParseInlineObjectReference(
            string line,
            string propertyName,
            out string fileId,
            out string guid)
        {
            fileId = string.Empty;
            guid = string.Empty;
            var prefix = propertyName + ": {fileID: ";
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var fileEnd = line.IndexOf(',', prefix.Length);
            const string guidMarker = ", guid: ";
            var guidStart = line.IndexOf(guidMarker, StringComparison.Ordinal);
            var guidEnd = guidStart < 0 ? -1 : line.IndexOf(',',
                guidStart + guidMarker.Length);
            ulong ignored;
            if (fileEnd < 0 || guidStart < 0 || guidEnd < 0 ||
                !TryParseSavedNumericId(line.Substring(prefix.Length,
                    fileEnd - prefix.Length), out ignored))
                return false;
            fileId = line.Substring(prefix.Length, fileEnd - prefix.Length).Trim();
            guid = NormalizeSavedGuid(line.Substring(guidStart + guidMarker.Length,
                guidEnd - guidStart - guidMarker.Length));
            return true;
        }

        private static bool TryParseYamlDocumentId(string line, out ulong documentId)
        {
            documentId = 0;
            if (string.IsNullOrEmpty(line) || !line.StartsWith("--- !u!", StringComparison.Ordinal))
                return false;
            var marker = line.IndexOf('&');
            if (marker < 0) return false;
            return TryParseSavedNumericId(line.Substring(marker + 1).Trim(), out documentId);
        }

        private static bool TryParseYamlObjectReference(
            string line,
            string propertyName,
            out string fileId,
            out string guid)
        {
            fileId = string.Empty;
            guid = string.Empty;
            var prefix = "  " + propertyName + ": {fileID: ";
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var fileEnd = line.IndexOf(',', prefix.Length);
            var guidMarker = ", guid: ";
            var guidStart = line.IndexOf(guidMarker, StringComparison.Ordinal);
            var guidEnd = guidStart < 0 ? -1 : line.IndexOf(',', guidStart + guidMarker.Length);
            if (fileEnd < 0 || guidStart < 0 || guidEnd < 0) return false;
            fileId = line.Substring(prefix.Length, fileEnd - prefix.Length).Trim();
            guid = line.Substring(guidStart + guidMarker.Length,
                guidEnd - guidStart - guidMarker.Length).Trim();
            ulong ignored;
            return TryParseSavedNumericId(fileId, out ignored) && guid.Length == 32;
        }

        private static bool MaterialAtlasMatchesFont(Material material, TMP_FontAsset font)
        {
            return material != null && font != null && material.HasProperty(AtlasMainTexture) &&
                material.GetTexture(AtlasMainTexture) == font.atlasTexture;
        }

        private static bool IsApprovedTmpNormalizationIdentity(string identity)
        {
            return Enumerable.Range(1, 20).SelectMany(slot =>
                    new[] { "BadgeLabel", "NameLabel" }.Select(label => MakeIdentity(
                        DefaultScenePath,
                        "Spaces/Phase1_Kitchen/Tray_Group/TraySlots/TraySlot_" +
                        slot.ToString("00", CultureInfo.InvariantCulture) + "_TUNE/" + label,
                        0)))
                .Contains(identity, StringComparer.Ordinal);
        }

        private static bool TryGetApprovedLoadObservationSavedMaterial(
            string identity,
            out string savedMaterialGlobalId)
        {
            savedMaterialGlobalId = string.Empty;
            string[] parts;
            try { parts = ParseIdentity(identity); }
            catch (InvalidOperationException) { return false; }
            if (parts[2] != "0" ||
                !(parts[1].EndsWith("/BadgeLabel", StringComparison.Ordinal) ||
                  parts[1].EndsWith("/NameLabel", StringComparison.Ordinal)))
                return false;
            var sourceHan = MakeAssetGlobalId(
                "53ffd732c8bc2034f93d5a714c25eda2", -5877652104868683785L);
            var heavy = MakeAssetGlobalId(
                "ce30f77e3b8c0ad45b7b7ae26b956bb6", -417943515106631090L);
            if ((string.Equals(parts[0],
                    "Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab",
                    StringComparison.Ordinal) &&
                parts[1].StartsWith("CatalogCard/", StringComparison.Ordinal)) ||
                (string.Equals(parts[0],
                    "Assets/Prefabs/Cooking/ShortCycle/GridPieces/IngredientCard.prefab",
                    StringComparison.Ordinal) &&
                parts[1].StartsWith("IngredientCard/", StringComparison.Ordinal)) ||
                (string.Equals(parts[0],
                    "Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab",
                    StringComparison.Ordinal) &&
                parts[1].StartsWith("TraySlot/", StringComparison.Ordinal)))
            {
                savedMaterialGlobalId = sourceHan;
                return true;
            }
            if (string.Equals(parts[0],
                    "Assets/Prefabs/Cooking/ShortCycle/GridPieces/Sticker.prefab",
                    StringComparison.Ordinal) &&
                parts[1].StartsWith("Sticker/", StringComparison.Ordinal))
            {
                savedMaterialGlobalId = heavy;
                return true;
            }
            return false;
        }

        private static bool IsExpectedTmpNormalizationSource(
            string identity,
            string sourceIdentity)
        {
            if (!IsApprovedTmpNormalizationIdentity(identity)) return false;
            var label = identity.EndsWith("/BadgeLabel|0", StringComparison.Ordinal)
                ? "BadgeLabel"
                : identity.EndsWith("/NameLabel|0", StringComparison.Ordinal)
                    ? "NameLabel"
                    : string.Empty;
            return label.Length > 0 && string.Equals(sourceIdentity,
                MakeIdentity("Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab",
                    "TraySlot/" + label, 0), StringComparison.Ordinal);
        }

        private static bool TryReadPrefabRelationship(
            TMP_Text text,
            out string sourceIdentity,
            out bool fontOverride,
            out bool materialOverride)
        {
            sourceIdentity = string.Empty;
            fontOverride = false;
            materialOverride = false;
            if (!PrefabUtility.IsPartOfPrefabInstance(text)) return false;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(text) as TMP_Text;
            if (source == null) return true;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            sourceIdentity = MakeIdentity(
                sourcePath,
                GetPath(source.gameObject),
                Array.IndexOf(source.GetComponents<TMP_Text>(), source));
            var serialized = new SerializedObject(text);
            var sourceSerialized = new SerializedObject(source);
            var font = serialized.FindProperty("m_fontAsset");
            var material = serialized.FindProperty("m_sharedMaterial");
            var sourceFont = sourceSerialized.FindProperty("m_fontAsset");
            var sourceMaterial = sourceSerialized.FindProperty("m_sharedMaterial");
            fontOverride = font != null && (font.prefabOverride || sourceFont == null ||
                font.objectReferenceValue != sourceFont.objectReferenceValue);
            materialOverride = material != null && (material.prefabOverride ||
                sourceMaterial == null ||
                material.objectReferenceValue != sourceMaterial.objectReferenceValue);
            return true;
        }

        public static string[] FindUnapprovedPrefabPropagation(
            IEnumerable<OfflineBinding> observedBindings,
            ISet<string> approvedIdentities)
        {
            if (approvedIdentities == null)
                throw new ArgumentNullException(nameof(approvedIdentities));
            return (observedBindings ?? Enumerable.Empty<OfflineBinding>())
                .Where(binding => binding != null && binding.isPrefabInstance &&
                    !string.IsNullOrWhiteSpace(binding.prefabSourceIdentity) &&
                    approvedIdentities.Contains(binding.prefabSourceIdentity) &&
                    !approvedIdentities.Contains(binding.rowIdentity) &&
                    (!binding.fontOverride || !binding.materialOverride))
                .Select(binding =>
                {
                    var fields = new List<string>();
                    if (!binding.fontOverride) fields.Add("m_fontAsset");
                    if (!binding.materialOverride) fields.Add("m_sharedMaterial");
                    return "UNAPPROVED_PREFAB_PROPAGATION|" +
                        binding.prefabSourceIdentity + "|" + binding.rowIdentity + "|" +
                        string.Join(",", fields.ToArray());
                })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] InspectUnapprovedPrefabPropagation(
            IEnumerable<ApprovedMapping> mappings,
            ISet<string> approvedIdentities,
            out string[] guardAssetPaths)
        {
            var sourcePaths = new HashSet<string>(mappings
                .Where(mapping => mapping.assetPath.EndsWith(".prefab",
                    StringComparison.OrdinalIgnoreCase))
                .Select(mapping => mapping.assetPath), StringComparer.Ordinal);
            var candidatePaths = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Where(path => !sourcePaths.Contains(path))
                .Where(path => AssetDatabase.GetDependencies(path, false)
                    .Any(sourcePaths.Contains))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var guarded = new List<string>();
            var reasons = new List<string>();
            foreach (var path in candidatePaths)
            {
                var linked = WithAssetContents(path, false, roots =>
                {
                    var bindings = new List<OfflineBinding>();
                    foreach (var text in roots.SelectMany(root =>
                                 root.GetComponentsInChildren<TMP_Text>(true)))
                    {
                        string sourceIdentity;
                        bool fontOverride;
                        bool materialOverride;
                        if (!TryReadPrefabRelationship(text, out sourceIdentity,
                                out fontOverride, out materialOverride) ||
                            !approvedIdentities.Contains(sourceIdentity))
                            continue;
                        var saved = ReadSavedTmpReferences(text, path, fontOverride,
                            materialOverride);
                        var rowIdentity = MakeIdentity(path, GetPath(text.gameObject),
                            Array.IndexOf(text.GetComponents<TMP_Text>(), text));
                        bindings.Add(new OfflineBinding
                        {
                            rowIdentity = rowIdentity,
                            prefabSourceIdentity = sourceIdentity,
                            isPrefabInstance = true,
                            fontOverride = saved.FontOverride,
                            materialOverride = saved.MaterialOverride,
                            loadedFontOverride = fontOverride,
                            loadedMaterialOverride = materialOverride
                        });
                    }
                    return bindings.ToArray();
                });
                if (linked.Length > 0) guarded.Add(path);
                reasons.AddRange(FindUnapprovedPrefabPropagation(linked, approvedIdentities));
            }
            guardAssetPaths = guarded.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            return reasons.OrderBy(reason => reason, StringComparer.Ordinal).ToArray();
        }

        private static MaterialPlan[] BuildMaterialPlans(
            IEnumerable<ApprovedMapping> mappings,
            TMP_FontAsset targetFont)
        {
            var result = new List<MaterialPlan>();
            var pairs = mappings.GroupBy(item => item.currentMaterialGlobalId,
                StringComparer.Ordinal);
            foreach (var pair in pairs)
            {
                var targetPaths = pair.Select(item => item.proposedMaterialPath)
                    .Distinct(StringComparer.Ordinal).ToArray();
                var plan = new MaterialPlan
                {
                    sourceMaterialGlobalId = pair.Key,
                    targetMaterialPath = targetPaths.Length == 1 ? targetPaths[0] : string.Empty
                };
                try
                {
                    if (targetPaths.Length != 1)
                        throw new InvalidOperationException("One source material maps to multiple targets.");
                    var source = ResolveMaterial(pair.Key);
                    if (source == null)
                        throw new InvalidOperationException("Source material cannot be resolved.");
                    if (targetFont == null || targetFont.material == null)
                        throw new InvalidOperationException("Target font material is missing.");
                    plan.sourceMaterialPath = AssetDatabase.GetAssetPath(source);
                    plan.sourceStyleFingerprint = ComputeMaterialFingerprint(source, false, true);
                    plan.targetAtlasFingerprint = ComputeMaterialFingerprint(
                        targetFont.material, true, false);
                    var candidate = new Material(source);
                    try
                    {
                        CopyAtlasProperties(targetFont.material, candidate);
                        plan.expectedMaterialFingerprint = ComputeMaterialFingerprint(
                            candidate, true, true);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(candidate);
                    }
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(plan.targetMaterialPath);
                    if (existing != null || File.Exists(ToFullPath(plan.targetMaterialPath)) ||
                        File.Exists(ToFullPath(plan.targetMaterialPath + ".meta")))
                        throw new InvalidOperationException(
                            "TARGET_MATERIAL_MUST_BE_ABSENT_BEFORE_TRANSACTION");
                    plan.ready = true;
                    plan.limitation = string.Empty;
                }
                catch (Exception exception)
                {
                    plan.ready = false;
                    plan.limitation = exception.Message;
                }
                result.Add(plan);
            }
            if (result.Select(item => item.targetMaterialPath)
                    .Distinct(StringComparer.Ordinal).Count() != result.Count)
            {
                foreach (var plan in result)
                {
                    plan.ready = false;
                    plan.limitation = "Multiple source materials map to one target path.";
                }
            }
            return result.OrderBy(item => item.targetMaterialPath, StringComparer.Ordinal).ToArray();
        }

        private static CreatedAssetRecord[] EnsureApprovedMaterials(MaterialPlan[] plans)
        {
            var targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                ShortCycleFontAtlasBuilder.StaticFontAssetPath);
            if (targetFont == null || targetFont.material == null)
                throw new InvalidOperationException("Target static font material is missing.");
            var created = new List<CreatedAssetRecord>();
            try
            {
                foreach (var plan in plans.OrderBy(item => item.targetMaterialPath, StringComparer.Ordinal))
                {
                    if (!plan.ready)
                        throw new InvalidOperationException(
                            "Material plan is not ready: " + plan.targetMaterialPath);
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(plan.targetMaterialPath);
                    if (existing != null)
                    {
                        if (!string.Equals(ComputeMaterialFingerprint(existing, true, true),
                                plan.expectedMaterialFingerprint, StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                "Existing material drift: " + plan.targetMaterialPath);
                        continue;
                    }

                    EnsureAssetFolder(Path.GetDirectoryName(plan.targetMaterialPath)
                        .Replace('\\', '/'));
                    var source = ResolveMaterial(plan.sourceMaterialGlobalId);
                    if (source == null)
                        throw new InvalidOperationException(
                            "Source material disappeared: " + plan.sourceMaterialGlobalId);
                    var material = new Material(source) { name = Path.GetFileNameWithoutExtension(
                        plan.targetMaterialPath) };
                    CopyAtlasProperties(targetFont.material, material);
                    AssetDatabase.CreateAsset(material, plan.targetMaterialPath);
                    var fingerprint = ComputeMaterialFingerprint(material, true, true);
                    if (!string.Equals(fingerprint, plan.expectedMaterialFingerprint,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Created material fingerprint mismatch: " + plan.targetMaterialPath);
                    created.Add(new CreatedAssetRecord
                    {
                        path = plan.targetMaterialPath,
                        fingerprint = fingerprint
                    });
                }
                AssetDatabase.SaveAssets();
                return created.ToArray();
            }
            catch
            {
                DeleteOwnedMaterials(created);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static void CopyAtlasProperties(Material source, Material destination)
        {
            foreach (var propertyName in AtlasPropertyNames)
                if (!source.HasProperty(propertyName) || !destination.HasProperty(propertyName))
                    throw new InvalidOperationException(
                        "Atlas property is unavailable: " + propertyName);
            destination.SetTexture(AtlasMainTexture, source.GetTexture(AtlasMainTexture));
            destination.SetFloat(AtlasTextureWidth, source.GetFloat(AtlasTextureWidth));
            destination.SetFloat(AtlasTextureHeight, source.GetFloat(AtlasTextureHeight));
            destination.SetFloat(AtlasGradientScale, source.GetFloat(AtlasGradientScale));
        }

        private static string ComputeMaterialFingerprint(
            Material material,
            bool includeAtlas,
            bool includeStyle)
        {
            if (material == null) return string.Empty;
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "$shader", material.shader == null ? string.Empty : material.shader.name }
            };
            var shader = material.shader;
            if (shader != null)
            {
                var count = shader.GetPropertyCount();
                for (var index = 0; index < count; index++)
                {
                    var name = shader.GetPropertyName(index);
                    var isAtlas = AtlasPropertyNames.Contains(name, StringComparer.Ordinal);
                    if ((isAtlas && !includeAtlas) || (!isAtlas && !includeStyle)) continue;
                    var type = shader.GetPropertyType(index);
                    string value;
                    if (type == ShaderPropertyType.Color)
                        value = material.GetColor(name).ToString("R");
                    else if (type == ShaderPropertyType.Vector)
                        value = material.GetVector(name).ToString("R");
                    else if (type == ShaderPropertyType.Texture)
                    {
                        var texture = material.GetTexture(name);
                        value = GlobalIdFor(texture) + "|" +
                            material.GetTextureOffset(name).ToString("R") + "|" +
                            material.GetTextureScale(name).ToString("R");
                    }
                    else
                        value = material.GetFloat(name).ToString("R",
                            System.Globalization.CultureInfo.InvariantCulture);
                    values[name] = type + "|" + value;
                }
            }
            return Sha256Text(string.Join("\n", values.Select(pair =>
                pair.Key + "=" + pair.Value).ToArray()));
        }

        private static Material ResolveMaterial(string globalIdText)
        {
            GlobalObjectId globalId;
            if (string.IsNullOrWhiteSpace(globalIdText) ||
                !GlobalObjectId.TryParse(globalIdText, out globalId))
                return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as Material;
        }

        private static Material ReadSharedMaterial(TMP_Text text)
        {
            var serialized = new SerializedObject(text);
            var property = serialized.FindProperty("m_sharedMaterial");
            return property == null ? null : property.objectReferenceValue as Material;
        }

        public static string ComputeStableNonTargetFingerprint(
            IEnumerable<NonTargetFingerprintProperty> properties)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            var values = new List<string>();
            foreach (var property in properties)
            {
                if (property == null)
                    throw new InvalidOperationException("NULL_FINGERPRINT_PROPERTY");
                var path = property.propertyPath ?? string.Empty;
                if (IsApprovedTargetProperty(path)) continue;
                var value = property.objectReference == null
                    ? "SCALAR|" + EncodeFingerprintField(property.scalarValue)
                    : "OBJECT_REFERENCE|" + EncodeFingerprintField(
                        NormalizeObjectReferenceIdentity(property.objectReference));
                values.Add(EncodeFingerprintField(path) + "|" +
                    EncodeFingerprintField(property.propertyType) + "|" + value);
            }
            return Sha256Text(string.Join("\n", values.ToArray()));
        }

        public static string NormalizeObjectReferenceIdentity(
            ObjectReferenceFingerprintDescriptor descriptor)
        {
            if (descriptor == null)
                throw new InvalidOperationException("UNRESOLVED_OBJECT_REFERENCE|null_descriptor");
            if (descriptor.unresolved)
                throw new InvalidOperationException("UNRESOLVED_OBJECT_REFERENCE|explicit");
            if (descriptor.isNull)
            {
                if (!string.IsNullOrWhiteSpace(descriptor.persistentGlobalId) ||
                    !string.IsNullOrWhiteSpace(descriptor.hierarchyPath) ||
                    !string.IsNullOrWhiteSpace(descriptor.identityKind) ||
                    !string.IsNullOrWhiteSpace(descriptor.ownerGlobalId) ||
                    !string.IsNullOrWhiteSpace(descriptor.ownerAssetGuid))
                    throw new InvalidOperationException(
                        "UNRESOLVED_OBJECT_REFERENCE|null_with_identity");
                return "NULL";
            }

            if (!string.IsNullOrWhiteSpace(descriptor.persistentGlobalId))
            {
                var reference = ParseSavedGlobalObjectId(
                    descriptor.persistentGlobalId, "reference");
                if (string.Equals(descriptor.identityKind,
                        PersistentObjectReferenceKind, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(descriptor.ownerGlobalId) ||
                        !string.IsNullOrWhiteSpace(descriptor.ownerAssetGuid))
                        throw new InvalidOperationException(
                            "UNRESOLVED_OBJECT_REFERENCE|persistent_with_owner");
                    return "PERSISTENT|" + EncodeFingerprintField(reference.Canonical);
                }

                if (string.Equals(descriptor.identityKind,
                        PrefabInstanceObjectReferenceKind, StringComparison.Ordinal))
                {
                    var owner = ParseSavedGlobalObjectId(
                        descriptor.ownerGlobalId, "owner");
                    var ownerAssetGuid = NormalizeSavedGuid(descriptor.ownerAssetGuid);
                    var ownerAssetPath = NormalizeAssetPath(descriptor.assetPath);
                    if (!(ownerAssetPath.EndsWith(".unity",
                              StringComparison.OrdinalIgnoreCase) ||
                          ownerAssetPath.EndsWith(".prefab",
                              StringComparison.OrdinalIgnoreCase)) ||
                        reference.IdentifierType != 2 || owner.IdentifierType != 2 ||
                        reference.TargetPrefabId != 0 || owner.TargetPrefabId == 0 ||
                        reference.TargetObjectId != owner.TargetPrefabId ||
                        !string.Equals(reference.AssetGuid, owner.AssetGuid,
                            StringComparison.Ordinal) ||
                        !string.Equals(reference.AssetGuid, ownerAssetGuid,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "UNRESOLVED_OBJECT_REFERENCE|prefab_instance_ownership_mismatch");
                    return "UNITY_INTERNAL_PREFAB_INSTANCE|" +
                        EncodeFingerprintField(reference.Canonical) + "|" +
                        EncodeFingerprintField(ownerAssetPath);
                }

                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|unsupported_saved_identity_kind");
            }

            if (!string.IsNullOrWhiteSpace(descriptor.identityKind) ||
                !string.IsNullOrWhiteSpace(descriptor.ownerGlobalId) ||
                !string.IsNullOrWhiteSpace(descriptor.ownerAssetGuid))
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|saved_identity_without_reference");

            var assetPath = NormalizeAssetPath(descriptor.assetPath);
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !(assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                  assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) ||
                string.IsNullOrWhiteSpace(descriptor.hierarchyPath))
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|missing_structural_owner");

            if (string.Equals(descriptor.objectKind, "GAME_OBJECT", StringComparison.Ordinal))
                return "LOCAL_GAME_OBJECT|" + EncodeFingerprintField(assetPath) + "|" +
                    EncodeFingerprintField(descriptor.hierarchyPath);
            if (string.Equals(descriptor.objectKind, "COMPONENT", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(descriptor.componentType) &&
                descriptor.componentIndex >= 0)
                return "LOCAL_COMPONENT|" + EncodeFingerprintField(assetPath) + "|" +
                    EncodeFingerprintField(descriptor.hierarchyPath) + "|" +
                    EncodeFingerprintField(descriptor.componentType) + "|" +
                    descriptor.componentIndex.ToString(CultureInfo.InvariantCulture);
            throw new InvalidOperationException(
                "UNRESOLVED_OBJECT_REFERENCE|unsupported_local_kind");
        }

        private sealed class SavedGlobalObjectId
        {
            public int IdentifierType;
            public string AssetGuid;
            public ulong TargetObjectId;
            public ulong TargetPrefabId;

            public string Canonical
            {
                get
                {
                    return "GlobalObjectId_V1-" +
                        IdentifierType.ToString(CultureInfo.InvariantCulture) + "-" +
                        AssetGuid + "-" +
                        TargetObjectId.ToString(CultureInfo.InvariantCulture) + "-" +
                        TargetPrefabId.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private static SavedGlobalObjectId ParseSavedGlobalObjectId(
            string value,
            string role)
        {
            var match = GlobalObjectIdPattern.Match((value ?? string.Empty).Trim());
            int identifierType;
            ulong targetObjectId;
            ulong targetPrefabId;
            if (!match.Success || !int.TryParse(match.Groups["type"].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture, out identifierType) ||
                identifierType < 1 || identifierType > 3 ||
                !TryParseSavedNumericId(match.Groups["target"].Value,
                    out targetObjectId) ||
                !TryParseSavedNumericId(match.Groups["prefab"].Value,
                    out targetPrefabId) ||
                targetObjectId == 0)
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|invalid_saved_global_id|" + role);
            return new SavedGlobalObjectId
            {
                IdentifierType = identifierType,
                AssetGuid = NormalizeSavedGuid(match.Groups["guid"].Value),
                TargetObjectId = targetObjectId,
                TargetPrefabId = targetPrefabId
            };
        }

        public static string CanonicalizeSavedGlobalObjectId(string value)
        {
            return ParseSavedGlobalObjectId(value, "public_diagnostic").Canonical;
        }

        private static bool TryParseSavedNumericId(string value, out ulong result)
        {
            result = 0;
            value = (value ?? string.Empty).Trim();
            if (value.StartsWith("-", StringComparison.Ordinal))
            {
                long signed;
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out signed))
                    return false;
                result = unchecked((ulong)signed);
                return true;
            }
            return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture,
                out result);
        }

        private static string NormalizeSavedGuid(string value)
        {
            var guid = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (guid.Length != 32 || guid.Any(character =>
                    !((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f'))) ||
                guid.All(character => character == '0'))
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|invalid_saved_asset_guid");
            return guid;
        }

        private static string ComputeNonTargetFingerprint(TMP_Text text, string assetPath)
        {
            string[] ignored;
            return ComputeNonTargetFingerprint(text, assetPath, out ignored);
        }

        private static string ComputeNonTargetFingerprint(
            TMP_Text text,
            string assetPath,
            out string[] objectReferenceIdentities)
        {
            var properties = new List<NonTargetFingerprintProperty>();
            var references = new List<string>();
            var iterator = new SerializedObject(text).GetIterator();
            var enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                var path = iterator.propertyPath;
                var isObjectReference = iterator.propertyType ==
                    SerializedPropertyType.ObjectReference;
                enterChildren = !isObjectReference;
                if (IsApprovedTargetProperty(path)) continue;

                var property = new NonTargetFingerprintProperty
                {
                    propertyPath = path,
                    propertyType = iterator.propertyType.ToString()
                };
                if (isObjectReference)
                {
                    property.objectReference = DescribeObjectReference(iterator, assetPath);
                    references.Add(path + "|" +
                        NormalizeObjectReferenceIdentity(property.objectReference));
                }
                else
                    property.scalarValue = StablePropertyValue(iterator);
                properties.Add(property);
            }
            objectReferenceIdentities = references.ToArray();
            return ComputeStableNonTargetFingerprint(properties);
        }

        private static ObjectReferenceFingerprintDescriptor DescribeObjectReference(
            SerializedProperty property,
            string ownerAssetPath)
        {
            var value = property.objectReferenceValue;
            if (value == null)
            {
                if (property.objectReferenceInstanceIDValue != 0)
                    return new ObjectReferenceFingerprintDescriptor { unresolved = true };
                return new ObjectReferenceFingerprintDescriptor { isNull = true };
            }

            if (EditorUtility.IsPersistent(value))
            {
                var globalIdText = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                GlobalObjectId parsed;
                if (string.IsNullOrWhiteSpace(globalIdText) ||
                    !GlobalObjectId.TryParse(globalIdText, out parsed))
                    throw new InvalidOperationException(
                        "UNRESOLVED_OBJECT_REFERENCE|invalid_persistent_global_id|" +
                        property.propertyPath);
                return new ObjectReferenceFingerprintDescriptor
                {
                    identityKind = PersistentObjectReferenceKind,
                    persistentGlobalId = globalIdText
                };
            }

            if (string.Equals(property.propertyPath, "m_PrefabInstance",
                    StringComparison.Ordinal))
            {
                var referenceGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                var owner = property.serializedObject.targetObject;
                var ownerGlobalId = owner == null
                    ? string.Empty
                    : GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
                GlobalObjectId parsedReference;
                GlobalObjectId parsedOwner;
                var normalizedOwnerPath = NormalizeAssetPath(ownerAssetPath);
                var ownerAssetGuid = AssetDatabase.AssetPathToGUID(normalizedOwnerPath);
                if (string.IsNullOrWhiteSpace(referenceGlobalId) ||
                    string.IsNullOrWhiteSpace(ownerGlobalId) ||
                    string.IsNullOrWhiteSpace(ownerAssetGuid) ||
                    !GlobalObjectId.TryParse(referenceGlobalId, out parsedReference) ||
                    !GlobalObjectId.TryParse(ownerGlobalId, out parsedOwner))
                    throw new InvalidOperationException(
                        "UNRESOLVED_OBJECT_REFERENCE|invalid_prefab_instance_identity|" +
                        property.propertyPath);
                var internalDescriptor = new ObjectReferenceFingerprintDescriptor
                {
                    identityKind = PrefabInstanceObjectReferenceKind,
                    persistentGlobalId = referenceGlobalId,
                    ownerGlobalId = ownerGlobalId,
                    ownerAssetGuid = ownerAssetGuid,
                    assetPath = normalizedOwnerPath
                };
                NormalizeObjectReferenceIdentity(internalDescriptor);
                return internalDescriptor;
            }

            GameObject gameObject;
            Component component = value as Component;
            if (component != null)
                gameObject = component.gameObject;
            else
                gameObject = value as GameObject;
            if (gameObject == null)
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|unsupported_nonpersistent_type|" +
                    property.propertyPath + "|" + value.GetType().FullName);

            var normalizedOwner = NormalizeAssetPath(ownerAssetPath);
            var scenePath = gameObject.scene.IsValid()
                ? NormalizeAssetPath(gameObject.scene.path)
                : string.Empty;
            if (!string.IsNullOrEmpty(scenePath) &&
                !string.Equals(scenePath, normalizedOwner, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "UNRESOLVED_OBJECT_REFERENCE|cross_asset_local_reference|" +
                    property.propertyPath);

            var descriptor = new ObjectReferenceFingerprintDescriptor
            {
                assetPath = normalizedOwner,
                hierarchyPath = GetStableHierarchyPath(gameObject),
                objectKind = component == null ? "GAME_OBJECT" : "COMPONENT"
            };
            if (component != null)
            {
                descriptor.componentType = component.GetType().AssemblyQualifiedName;
                var components = component.gameObject.GetComponents<Component>();
                descriptor.componentIndex = Array.FindIndex(components,
                    item => object.ReferenceEquals(item, component));
                if (descriptor.componentIndex < 0)
                    throw new InvalidOperationException(
                        "UNRESOLVED_OBJECT_REFERENCE|component_not_owned|" +
                        property.propertyPath);
            }
            return descriptor;
        }

        private static bool IsApprovedTargetProperty(string path)
        {
            return path == "m_fontAsset" || path.StartsWith("m_fontAsset.",
                       StringComparison.Ordinal) || path == "m_sharedMaterial" ||
                   path.StartsWith("m_sharedMaterial.", StringComparison.Ordinal);
        }

        private static string EncodeFingerprintField(string value)
        {
            value = value ?? string.Empty;
            return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        }

        private static string GetStableHierarchyPath(GameObject gameObject)
        {
            var segments = new Stack<string>();
            for (var current = gameObject.transform; current != null; current = current.parent)
            {
                var name = current.name ?? string.Empty;
                segments.Push(EncodeFingerprintField(name) + "#" +
                    current.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("/", segments.ToArray());
        }

        public static FingerprintDiagnosticReport DiagnoseApprovedNonTargetFingerprints(
            string planPath,
            string outputPath)
        {
            var fullPlanPath = ToFullPath(planPath);
            if (!File.Exists(fullPlanPath))
                throw new FileNotFoundException("Approved font plan is missing.", fullPlanPath);
            var plan = JsonUtility.FromJson<ApprovedMigrationPlan>(
                File.ReadAllText(fullPlanPath, new UTF8Encoding(false, true)));
            if (plan == null || plan.schemaVersion != ApprovedMigrationSchemaVersion ||
                plan.nonTargetFingerprintVersion != NonTargetFingerprintVersion)
                throw new InvalidOperationException(
                    "FINGERPRINT_DIAGNOSTIC_REQUIRES_FRESH_SCHEMA9_PLAN");
            if (!plan.executeAllowed || plan.rows == null ||
                (plan.approvedRawEntryCount != ApprovedRawEntryCount &&
                 plan.approvedRawEntryCount != CombinedRawEntryCount) ||
                plan.rows.Length != plan.approvedRawEntryCount)
                throw new InvalidOperationException(
                    "FINGERPRINT_DIAGNOSTIC_INVALID_PLAN_SCOPE");
            if (!string.Equals(plan.mappingSha256, ApprovedMappingSha256,
                    StringComparison.Ordinal) || (plan.approvedRawEntryCount ==
                    CombinedRawEntryCount && !string.Equals(
                        plan.supplementalMappingSha256, SupplementalMappingSha256,
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "FINGERPRINT_DIAGNOSTIC_APPROVAL_HASH_DRIFT");
            ValidateApprovedBaselineSnapshotSet(plan.approvedBaselineFiles,
                plan.approvedRawEntryCount);
            VerifyFileSnapshots(plan.approvedBaselineFiles);

            var assets = GetApprovedBackupAssetPaths(plan);
            foreach (var scenePath in assets.Where(path => path.EndsWith(".unity",
                         StringComparison.OrdinalIgnoreCase)))
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                    throw new InvalidOperationException(
                        "FINGERPRINT_DIAGNOSTIC_REQUIRES_SCENE_CLOSED|" + scenePath);
            }

            var first = CaptureFingerprintDiagnosticPass(plan.rows);
            var second = CaptureFingerprintDiagnosticPass(plan.rows);
            var secondByIdentity = second.ToDictionary(row => row.rowIdentity,
                StringComparer.Ordinal);
            var unstable = first.Where(row =>
                !secondByIdentity.ContainsKey(row.rowIdentity) ||
                !string.Equals(row.fingerprint,
                    secondByIdentity[row.rowIdentity].fingerprint,
                    StringComparison.Ordinal) ||
                !(row.objectReferenceIdentities ?? new string[0]).SequenceEqual(
                    secondByIdentity[row.rowIdentity].objectReferenceIdentities ??
                        new string[0], StringComparer.Ordinal) ||
                !DiagnosticMaterialEvidenceEquals(row, secondByIdentity[row.rowIdentity]))
                .Select(row => row.rowIdentity).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            var planMismatches = FindFingerprintDiagnosticPlanMismatches(
                    plan.rows, first)
                .Concat(FindFingerprintDiagnosticPlanMismatches(plan.rows, second))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var referencesValid = first.Concat(second).All(row =>
                row.savedReferenceProven && row.savedReferenceAssetShaMatches);
            var report = new FingerprintDiagnosticReport
            {
                success = unstable.Length == 0 && planMismatches.Length == 0 &&
                    referencesValid,
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                planPath = planPath,
                rowCount = first.Length,
                approvedAssetCount = assets.Length,
                stableAcrossReloads = unstable.Length == 0,
                referencesValid = referencesValid,
                unstableRowCount = unstable.Length,
                unstableRowIdentities = unstable,
                planMismatchRowCount = planMismatches.Length,
                planMismatchRowIdentities = planMismatches,
                firstPass = first,
                secondPass = second
            };
            WriteJsonObject(outputPath, report);
            if (!report.success)
                throw new InvalidOperationException(
                    "FINGERPRINT_DIAGNOSTIC_CONTRACT_DRIFT|unstable=" +
                    string.Join(",", unstable) + "|planMismatch=" +
                    string.Join(",", planMismatches) + "|referencesValid=" +
                    referencesValid);
            return report;
        }

        public static string[] FindFingerprintDiagnosticPlanMismatches(
            IEnumerable<ApprovedRowAudit> plannedRows,
            IEnumerable<FingerprintDiagnosticRow> capturedRows)
        {
            var planned = (plannedRows ?? Enumerable.Empty<ApprovedRowAudit>()).ToArray();
            var captured = (capturedRows ?? Enumerable.Empty<FingerprintDiagnosticRow>()).ToArray();
            var reasons = new HashSet<string>(StringComparer.Ordinal);
            var plannedByIdentity = planned.GroupBy(row => row.rowIdentity,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            var capturedByIdentity = captured.GroupBy(row => row.rowIdentity,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            foreach (var duplicate in plannedByIdentity.Where(pair => pair.Value.Length != 1))
                reasons.Add("DUPLICATE_PLAN|" + duplicate.Key);
            foreach (var pair in plannedByIdentity.Where(pair => pair.Value.Length == 1))
            {
                FingerprintDiagnosticRow[] observations;
                if (!capturedByIdentity.TryGetValue(pair.Key, out observations))
                {
                    reasons.Add("MISSING_CAPTURE|" + pair.Key);
                    continue;
                }
                if (observations.Length != 1)
                {
                    reasons.Add("DUPLICATE_CAPTURE|" + pair.Key);
                    continue;
                }
                var plan = pair.Value[0];
                var observed = observations[0];
                if (!string.Equals(observed.fingerprint, plan.nonTargetFingerprint,
                        StringComparison.Ordinal) ||
                    !string.Equals(observed.rawFontGuid, plan.beforeFontGuid,
                        StringComparison.Ordinal) ||
                    !string.Equals(observed.rawMaterialGlobalId,
                        plan.beforeMaterialGlobalId, StringComparison.Ordinal) ||
                    !string.Equals(observed.approvedObservationFontGuid,
                        plan.approvedObservationFontGuid, StringComparison.Ordinal) ||
                    !string.Equals(observed.approvedObservationMaterialGlobalId,
                        plan.approvedObservationMaterialGlobalId,
                        StringComparison.Ordinal) ||
                    !string.Equals(observed.effectiveMaterialGlobalId,
                        plan.beforeEffectiveMaterialGlobalId, StringComparison.Ordinal) ||
                    !string.Equals(observed.materialNormalizationReason,
                        plan.beforeMaterialNormalizationReason, StringComparison.Ordinal) ||
                    observed.savedFontOverride != plan.beforeFontOverride ||
                    observed.savedMaterialOverride != plan.beforeMaterialOverride ||
                    observed.loadedFontOverride != plan.beforeLoadedFontOverride ||
                    observed.loadedMaterialOverride != plan.beforeLoadedMaterialOverride ||
                    observed.migrationFontOverrideRequired !=
                        plan.migrationFontOverrideRequired ||
                    observed.migrationMaterialOverrideRequired !=
                        plan.migrationMaterialOverrideRequired ||
                    !string.Equals(observed.savedReferenceOwnerIdentity,
                        plan.savedReferenceOwnerIdentity, StringComparison.Ordinal) ||
                    !string.Equals(observed.savedReferenceAssetPath,
                        plan.savedReferenceAssetPath, StringComparison.Ordinal) ||
                    !string.Equals(observed.savedReferenceAssetSha256,
                        plan.savedReferenceAssetSha256, StringComparison.Ordinal) ||
                    !string.Equals(observed.savedReferenceSourceAssetPath,
                        plan.savedReferenceSourceAssetPath, StringComparison.Ordinal) ||
                    !string.Equals(observed.savedReferenceSourceAssetSha256,
                        plan.savedReferenceSourceAssetSha256, StringComparison.Ordinal) ||
                    observed.savedReferenceProven != plan.savedReferenceProven ||
                    observed.savedReferenceAssetShaMatches !=
                        plan.savedReferenceAssetShaMatches)
                    reasons.Add("PLAN_CONTRACT|" + pair.Key);
            }
            foreach (var unexpected in capturedByIdentity.Keys.Where(identity =>
                         !plannedByIdentity.ContainsKey(identity)))
                reasons.Add("UNEXPECTED_CAPTURE|" + unexpected);
            return reasons.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static FingerprintDiagnosticRow[] CaptureFingerprintDiagnosticPass(
            IEnumerable<ApprovedRowAudit> rows)
        {
            var result = new List<FingerprintDiagnosticRow>();
            foreach (var group in rows.GroupBy(row => row.assetPath,
                         StringComparer.Ordinal).OrderBy(group => group.Key,
                         StringComparer.Ordinal))
            {
                result.AddRange(WithAssetContents(group.Key, false, roots =>
                {
                    var lookup = CreateExactTextLookup(group.Key, roots);
                    var captured = new List<FingerprintDiagnosticRow>();
                    foreach (var row in group.OrderBy(item => item.rowIdentity,
                                 StringComparer.Ordinal))
                    {
                        TMP_Text text;
                        if (!lookup.TryGetValue(row.rowIdentity, out text))
                            throw new InvalidOperationException(
                                "MISSING_IDENTITY|" + row.rowIdentity);
                        string[] references;
                        var fingerprint = ComputeNonTargetFingerprint(text, group.Key,
                            out references);
                        string sourceIdentity;
                        bool fontOverride;
                        bool materialOverride;
                        TryReadPrefabRelationship(text, out sourceIdentity, out fontOverride,
                            out materialOverride);
                        var saved = ReadSavedTmpReferences(text, group.Key, fontOverride,
                            materialOverride);
                        var effective = ReadSharedMaterial(text);
                        var reason = ClassifyMaterialResolution(
                            BuildMaterialResolutionObservation(row.rowIdentity,
                                row.approvedObservationFontGuid,
                                row.approvedObservationMaterialGlobalId, saved, effective,
                                text.font, sourceIdentity, saved.AssetShaMatches));
                        captured.Add(new FingerprintDiagnosticRow
                        {
                            rowIdentity = row.rowIdentity,
                            fingerprint = fingerprint,
                            objectReferenceIdentities = references,
                            rawFontGuid = saved.FontGuid,
                            rawMaterialGlobalId = saved.MaterialGlobalId,
                            approvedObservationFontGuid =
                                row.approvedObservationFontGuid,
                            approvedObservationMaterialGlobalId =
                                row.approvedObservationMaterialGlobalId,
                            effectiveMaterialGlobalId = GlobalIdFor(effective),
                            materialNormalizationReason = reason,
                            savedFontOverride = saved.FontOverride,
                            savedMaterialOverride = saved.MaterialOverride,
                            loadedFontOverride = fontOverride,
                            loadedMaterialOverride = materialOverride,
                            migrationFontOverrideRequired =
                                row.migrationFontOverrideRequired,
                            migrationMaterialOverrideRequired =
                                row.migrationMaterialOverrideRequired,
                            savedReferenceOwnerIdentity = saved.OwnerIdentity,
                            savedReferenceAssetPath = saved.AssetPath,
                            savedReferenceAssetSha256 = saved.AssetSha256,
                            savedReferenceSourceAssetPath = saved.SourceAssetPath,
                            savedReferenceSourceAssetSha256 = saved.SourceAssetSha256,
                            savedReferenceProven = saved.Proven,
                            savedReferenceAssetShaMatches = saved.AssetShaMatches
                        });
                    }
                    return captured.ToArray();
                }));
            }
            return result.OrderBy(row => row.rowIdentity,
                StringComparer.Ordinal).ToArray();
        }

        private static bool DiagnosticMaterialEvidenceEquals(
            FingerprintDiagnosticRow left,
            FingerprintDiagnosticRow right)
        {
            return string.Equals(left.rawFontGuid, right.rawFontGuid,
                       StringComparison.Ordinal) &&
                string.Equals(left.rawMaterialGlobalId, right.rawMaterialGlobalId,
                    StringComparison.Ordinal) &&
                string.Equals(left.approvedObservationFontGuid,
                    right.approvedObservationFontGuid, StringComparison.Ordinal) &&
                string.Equals(left.approvedObservationMaterialGlobalId,
                    right.approvedObservationMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(left.effectiveMaterialGlobalId,
                    right.effectiveMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(left.materialNormalizationReason,
                    right.materialNormalizationReason, StringComparison.Ordinal) &&
                string.Equals(left.savedReferenceOwnerIdentity,
                    right.savedReferenceOwnerIdentity, StringComparison.Ordinal) &&
                string.Equals(left.savedReferenceAssetPath, right.savedReferenceAssetPath,
                    StringComparison.Ordinal) &&
                string.Equals(left.savedReferenceAssetSha256,
                    right.savedReferenceAssetSha256, StringComparison.Ordinal) &&
                string.Equals(left.savedReferenceSourceAssetPath,
                    right.savedReferenceSourceAssetPath, StringComparison.Ordinal) &&
                string.Equals(left.savedReferenceSourceAssetSha256,
                    right.savedReferenceSourceAssetSha256, StringComparison.Ordinal) &&
                left.savedFontOverride == right.savedFontOverride &&
                left.savedMaterialOverride == right.savedMaterialOverride &&
                left.loadedFontOverride == right.loadedFontOverride &&
                left.loadedMaterialOverride == right.loadedMaterialOverride &&
                left.migrationFontOverrideRequired ==
                    right.migrationFontOverrideRequired &&
                left.migrationMaterialOverrideRequired ==
                    right.migrationMaterialOverrideRequired &&
                left.savedReferenceProven == right.savedReferenceProven &&
                left.savedReferenceAssetShaMatches ==
                    right.savedReferenceAssetShaMatches;
        }

        private static string StablePropertyValue(SerializedProperty property)
        {
            try
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    return GlobalIdFor(property.objectReferenceValue);
                if (property.propertyType == SerializedPropertyType.String)
                    return property.stringValue ?? string.Empty;
                if (property.propertyType == SerializedPropertyType.Boolean)
                    return property.boolValue ? "true" : "false";
                if (property.propertyType == SerializedPropertyType.Integer ||
                    property.propertyType == SerializedPropertyType.Enum ||
                    property.propertyType == SerializedPropertyType.ArraySize ||
                    property.propertyType == SerializedPropertyType.Character)
                    return property.longValue.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                if (property.propertyType == SerializedPropertyType.Float)
                    return property.doubleValue.ToString("R",
                        System.Globalization.CultureInfo.InvariantCulture);
                if (property.propertyType == SerializedPropertyType.Color)
                    return property.colorValue.ToString("R");
                if (property.propertyType == SerializedPropertyType.Vector2)
                    return property.vector2Value.ToString("R");
                if (property.propertyType == SerializedPropertyType.Vector3)
                    return property.vector3Value.ToString("R");
                if (property.propertyType == SerializedPropertyType.Vector4)
                    return property.vector4Value.ToString("R");
                if (property.propertyType == SerializedPropertyType.Quaternion)
                    return property.quaternionValue.ToString("R");
                if (property.propertyType == SerializedPropertyType.Rect)
                    return property.rectValue.ToString("R");
                if (property.propertyType == SerializedPropertyType.Bounds)
                    return property.boundsValue.ToString("R");
                if (property.propertyType == SerializedPropertyType.Vector2Int)
                    return property.vector2IntValue.ToString();
                if (property.propertyType == SerializedPropertyType.Vector3Int)
                    return property.vector3IntValue.ToString();
                if (property.propertyType == SerializedPropertyType.RectInt)
                    return property.rectIntValue.ToString();
                if (property.propertyType == SerializedPropertyType.BoundsInt)
                    return property.boundsIntValue.ToString();
                if (property.propertyType == SerializedPropertyType.AnimationCurve)
                {
                    var curve = property.animationCurveValue;
                    return curve == null ? string.Empty : string.Join(";", curve.keys.Select(key =>
                        key.time.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        key.value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        key.inTangent.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        key.outTangent.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                        .ToArray());
                }
                var boxed = property.boxedValue;
                return boxed == null ? string.Empty : boxed.ToString();
            }
            catch
            {
                return property.ToString();
            }
        }

        private static TransactionRecord ReadTransaction(string transactionPath, int expectedSchema)
        {
            var fullPath = ToFullPath(transactionPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Font transaction is missing.", fullPath);
            var transaction = JsonUtility.FromJson<TransactionRecord>(File.ReadAllText(fullPath));
            if (transaction == null || (expectedSchema > 0 &&
                    transaction.schemaVersion != expectedSchema) ||
                (expectedSchema == 0 && transaction.schemaVersion != 1 &&
                    transaction.schemaVersion != 2 && transaction.schemaVersion != 3 &&
                    transaction.schemaVersion != 4 && transaction.schemaVersion != 5 &&
                    transaction.schemaVersion != 6 && transaction.schemaVersion != 7 &&
                    transaction.schemaVersion != 8 && transaction.schemaVersion != 9 &&
                    transaction.schemaVersion != 10))
                throw new InvalidOperationException("Unsupported font transaction schema.");
            if (transaction.entries == null || transaction.entries.Length == 0)
                throw new InvalidOperationException("Font transaction has no entries.");
            var expectedRows = transaction.schemaVersion >= 4
                ? transaction.approvedRawEntryCount
                : ApprovedRawEntryCount;
            if (transaction.schemaVersion >= 3 && (expectedRows <= 0 ||
                    transaction.rows == null || transaction.rows.Length != expectedRows))
                throw new InvalidOperationException(
                    "Font transaction does not audit all approved rows.");
            return transaction;
        }

        private static TransactionRecord ReadExecutableTransaction(string transactionPath)
        {
            var transaction = ReadTransaction(transactionPath, 0);
            if (transaction.schemaVersion != ApprovedMigrationSchemaVersion ||
                transaction.nonTargetFingerprintVersion != NonTargetFingerprintVersion)
                throw new InvalidOperationException(
                    "Legacy material-resolution transactions are not executable; create a fresh " +
                    "schema 10 preview and transaction with fingerprint version 3.");
            ValidateApprovedBaselineSnapshotSet(transaction.approvedBaselineFiles,
                transaction.approvedRawEntryCount);
            if (transaction.expectedFiles == null || transaction.expectedFiles.Length == 0)
                throw new InvalidOperationException("TRANSACTION_STAGE_SNAPSHOT_MISSING");
            ValidateOwnedMaterialRecordContract(transaction);
            return transaction;
        }

        private static string[] GetTransactionFilePaths(TransactionRecord transaction)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            Action<string> addWithMeta = path =>
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                paths.Add(NormalizeAssetPath(path));
                if (path.StartsWith("Assets/", StringComparison.Ordinal))
                    paths.Add(NormalizeAssetPath(path) + ".meta");
            };
            if (!string.IsNullOrWhiteSpace(transaction.mappingPath))
                paths.Add(NormalizeAssetPath(transaction.mappingPath));
            if (!string.IsNullOrWhiteSpace(transaction.approvalEvidencePath))
                paths.Add(NormalizeAssetPath(transaction.approvalEvidencePath));
            if (!string.IsNullOrWhiteSpace(transaction.supplementalMappingPath))
                paths.Add(NormalizeAssetPath(transaction.supplementalMappingPath));
            if (!string.IsNullOrWhiteSpace(transaction.supplementalApprovalEvidencePath))
                paths.Add(NormalizeAssetPath(
                    transaction.supplementalApprovalEvidencePath));
            if (!string.IsNullOrWhiteSpace(transaction.planPath))
                paths.Add(NormalizeAssetPath(transaction.planPath));
            addWithMeta(transaction.targetFontPath);
            foreach (var approvedPath in transaction.approvedAssetPaths ?? new string[0])
                addWithMeta(approvedPath);
            foreach (var guardPath in transaction.propagationGuardAssetPaths ?? new string[0])
                addWithMeta(guardPath);
            foreach (var entry in transaction.entries ?? new DryRunEntry[0])
                addWithMeta(entry.writeAssetPath);
            foreach (var material in transaction.materials ?? new MaterialPlan[0])
            {
                addWithMeta(material.sourceMaterialPath);
                addWithMeta(material.targetMaterialPath);
            }
            paths.Add(ApprovedMaterialFolderPath + ".meta");
            return paths.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Builds the exact production mutation policy for one batch. Existing business assets
        /// may change content but never metadata; transaction-owned materials, their metadata,
        /// and an owned Materials-folder metadata file may only appear from a proven missing
        /// pre-state during the first creation batch.
        /// </summary>
        public static StageMutationRule[] BuildBatchStageMutationPolicy(
            TransactionRecord transaction,
            IEnumerable<string> selectedCanonicalIdentities,
            bool includeMaterialCreation,
            IEnumerable<FileSnapshot> preStageFiles)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            var selected = new HashSet<string>(selectedCanonicalIdentities ??
                Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (selected.Count == 0)
                throw new InvalidOperationException("BATCH_MUTATION_POLICY_EMPTY_SELECTION");
            var snapshots = (preStageFiles ?? Enumerable.Empty<FileSnapshot>()).ToArray();
            if (snapshots.Any(snapshot => snapshot == null))
                throw new InvalidOperationException("BATCH_MUTATION_POLICY_NULL_SNAPSHOT");
            var groups = snapshots.GroupBy(snapshot => NormalizeAssetPath(snapshot.path),
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            if (groups.Any(pair => pair.Value.Length != 1))
                throw new InvalidOperationException("BATCH_MUTATION_POLICY_DUPLICATE_SNAPSHOT");
            var tracked = new HashSet<string>(GetTransactionFilePaths(transaction),
                StringComparer.Ordinal);
            var rules = new Dictionary<string, StageMutationRule>(StringComparer.Ordinal);
            Action<string, string, bool> addRule = (path, kind, mustExistBefore) =>
            {
                var normalized = NormalizeAssetPath(path);
                FileSnapshot[] group;
                if (!tracked.Contains(normalized) || !groups.TryGetValue(normalized, out group))
                    throw new InvalidOperationException(
                        "BATCH_MUTATION_POLICY_PATH_OUT_OF_SCOPE|" + normalized);
                var snapshot = group[0];
                if (snapshot.exists != mustExistBefore)
                    throw new InvalidOperationException(
                        "BATCH_MUTATION_POLICY_PRESTATE_DRIFT|" + normalized);
                if (rules.ContainsKey(normalized))
                    throw new InvalidOperationException(
                        "BATCH_MUTATION_POLICY_DUPLICATE_RULE|" + normalized);
                rules.Add(normalized, new StageMutationRule
                {
                    path = normalized,
                    mutationKind = kind,
                    beforeExists = snapshot.exists,
                    beforeSha256 = snapshot.sha256 ?? string.Empty
                });
            };
            var entries = transaction.entries ?? new DryRunEntry[0];
            if (selected.Any(identity => entries.All(entry =>
                    !string.Equals(entry.canonicalIdentity, identity,
                        StringComparison.Ordinal))))
                throw new InvalidOperationException(
                    "BATCH_MUTATION_POLICY_SELECTION_OUT_OF_SCOPE");
            foreach (var assetPath in entries.Where(entry =>
                         selected.Contains(entry.canonicalIdentity))
                     .Select(entry => NormalizeAssetPath(entry.writeAssetPath))
                     .Distinct(StringComparer.Ordinal))
            {
                addRule(assetPath, ModifyExistingStageMutation, true);
                FileSnapshot[] metaGroup;
                if (!groups.TryGetValue(assetPath + ".meta", out metaGroup) ||
                    metaGroup.Length != 1 || !metaGroup[0].exists)
                    throw new InvalidOperationException(
                        "EXISTING_ASSET_META_BASELINE_MISSING|" + assetPath + ".meta");
            }
            if (includeMaterialCreation)
            {
                foreach (var material in transaction.materials ?? new MaterialPlan[0])
                {
                    addRule(material.targetMaterialPath, CreateMissingStageMutation, false);
                    addRule(material.targetMaterialPath + ".meta",
                        CreateMissingStageMutation, false);
                }
                if (!string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderPath))
                {
                    if (!string.Equals(transaction.ownedMaterialFolderPath,
                            ApprovedMaterialFolderPath, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "OWNED_MATERIAL_FOLDER_SCOPE_DRIFT");
                    addRule(ApprovedMaterialFolderPath + ".meta",
                        CreateMissingStageMutation, false);
                }
            }
            return rules.Values.OrderBy(rule => rule.path, StringComparer.Ordinal).ToArray();
        }

        public static BatchRecord BuildApplyingBatchRecord(
            TransactionRecord transaction,
            string batchId,
            IEnumerable<string> selectedCanonicalIdentities,
            bool includeMaterialCreation)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (string.IsNullOrWhiteSpace(batchId))
                throw new InvalidOperationException("Batch id is required.");
            var selected = (selectedCanonicalIdentities ?? Enumerable.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var preStage = CloneFileSnapshots(transaction.expectedFiles);
            var batch = new BatchRecord
            {
                batchId = batchId,
                state = "APPLYING",
                recordedAtUtc = DateTime.UtcNow.ToString("o"),
                writes = 0,
                canonicalIdentities = selected,
                preStageFiles = preStage,
                includesMaterialCreation = includeMaterialCreation,
                mutationRules = BuildBatchStageMutationPolicy(transaction, selected,
                    includeMaterialCreation, preStage)
            };
            batch.stageContractSha256 = ComputeStageContractSha256(batch);
            return batch;
        }

        public static StageSnapshotValidation EvaluateApplyingStageSnapshot(
            TransactionRecord transaction,
            BatchRecord pending,
            IEnumerable<FileSnapshot> observedCurrent)
        {
            var reasons = new HashSet<string>(StringComparer.Ordinal);
            var changed = new HashSet<string>(StringComparer.Ordinal);
            if (transaction == null || pending == null)
            {
                reasons.Add("PENDING_STAGE_RECORD_MISSING");
                return new StageSnapshotValidation
                {
                    success = false,
                    changedPaths = new string[0],
                    reasons = reasons.ToArray()
                };
            }
            if (!string.Equals(transaction.state, "APPLYING", StringComparison.Ordinal) ||
                !string.Equals(pending.state, "APPLYING", StringComparison.Ordinal))
                reasons.Add("PENDING_STAGE_STATE_DRIFT");
            var expectedBefore = transaction.expectedFiles ?? new FileSnapshot[0];
            var recordedBefore = pending.preStageFiles ?? new FileSnapshot[0];
            var preState = EvaluateStageSnapshotTransition(expectedBefore, recordedBefore,
                new StageMutationRule[0]);
            foreach (var reason in preState.reasons ?? new string[0])
                reasons.Add("PENDING_PRESTATE_" + reason);
            StageMutationRule[] rebuilt = null;
            try
            {
                rebuilt = BuildBatchStageMutationPolicy(transaction,
                    pending.canonicalIdentities, pending.includesMaterialCreation,
                    recordedBefore);
            }
            catch (Exception exception)
            {
                reasons.Add("PENDING_POLICY_REBUILD_FAILED|" + exception.Message);
            }
            if (rebuilt != null && !StageMutationRulesEqual(rebuilt,
                    pending.mutationRules ?? new StageMutationRule[0]))
                reasons.Add("PENDING_MUTATION_POLICY_DRIFT");
            if (!string.Equals(pending.stageContractSha256,
                    ComputeStageContractSha256(pending), StringComparison.Ordinal))
                reasons.Add("PENDING_STAGE_CONTRACT_HASH_DRIFT");
            var transition = EvaluateStageSnapshotTransition(recordedBefore,
                observedCurrent, pending.mutationRules ?? new StageMutationRule[0]);
            foreach (var reason in transition.reasons ?? new string[0])
                reasons.Add(reason);
            foreach (var path in transition.changedPaths ?? new string[0])
                changed.Add(path);
            return new StageSnapshotValidation
            {
                success = reasons.Count == 0,
                changedPaths = changed.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray(),
                reasons = reasons.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray()
            };
        }

        private static bool StageMutationRulesEqual(
            IEnumerable<StageMutationRule> left,
            IEnumerable<StageMutationRule> right)
        {
            var first = (left ?? Enumerable.Empty<StageMutationRule>())
                .OrderBy(rule => rule == null ? string.Empty : rule.path,
                    StringComparer.Ordinal).ToArray();
            var second = (right ?? Enumerable.Empty<StageMutationRule>())
                .OrderBy(rule => rule == null ? string.Empty : rule.path,
                    StringComparer.Ordinal).ToArray();
            return first.Length == second.Length && !first.Where((rule, index) =>
                rule == null || second[index] == null ||
                !string.Equals(NormalizeAssetPath(rule.path),
                    NormalizeAssetPath(second[index].path), StringComparison.Ordinal) ||
                !string.Equals(rule.mutationKind, second[index].mutationKind,
                    StringComparison.Ordinal) ||
                rule.beforeExists != second[index].beforeExists ||
                !string.Equals(rule.beforeSha256 ?? string.Empty,
                    second[index].beforeSha256 ?? string.Empty,
                    StringComparison.Ordinal)).Any();
        }

        private static string ComputeStageContractSha256(BatchRecord batch)
        {
            if (batch == null) return string.Empty;
            var lines = new List<string>
            {
                batch.batchId ?? string.Empty,
                batch.includesMaterialCreation ? "CREATE" : "NO_CREATE"
            };
            lines.AddRange((batch.canonicalIdentities ?? new string[0])
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "IDENTITY|" + value));
            lines.AddRange((batch.preStageFiles ?? new FileSnapshot[0])
                .Where(snapshot => snapshot != null)
                .OrderBy(snapshot => NormalizeAssetPath(snapshot.path),
                    StringComparer.Ordinal)
                .Select(snapshot => "SNAPSHOT|" + NormalizeAssetPath(snapshot.path) + "|" +
                    snapshot.exists + "|" + (snapshot.sha256 ?? string.Empty)));
            lines.AddRange((batch.mutationRules ?? new StageMutationRule[0])
                .Where(rule => rule != null)
                .OrderBy(rule => NormalizeAssetPath(rule.path), StringComparer.Ordinal)
                .Select(rule => "RULE|" + NormalizeAssetPath(rule.path) + "|" +
                    rule.mutationKind + "|" + rule.beforeExists + "|" +
                    (rule.beforeSha256 ?? string.Empty)));
            return Sha256Text(string.Join("\n", lines));
        }

        private static FileSnapshot[] CloneFileSnapshots(
            IEnumerable<FileSnapshot> snapshots)
        {
            return (snapshots ?? Enumerable.Empty<FileSnapshot>()).Select(snapshot =>
                new FileSnapshot
                {
                    path = snapshot.path,
                    exists = snapshot.exists,
                    sha256 = snapshot.sha256
                }).ToArray();
        }

        private static FileSnapshot[] CaptureFileSnapshots(IEnumerable<string> paths)
        {
            return (paths ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).Select(path =>
                {
                    var fullPath = ToFullPath(path);
                    var exists = File.Exists(fullPath);
                    return new FileSnapshot
                    {
                        path = path,
                        exists = exists,
                        sha256 = exists ? Sha256File(fullPath) : string.Empty
                    };
                }).ToArray();
        }

        private static FileSnapshot Snapshot(string path, string sha256)
        {
            return new FileSnapshot
            {
                path = NormalizeAssetPath(path),
                exists = true,
                sha256 = sha256
            };
        }

        private static bool ApprovedBaselineMatches(string path, string sha256)
        {
            var normalized = NormalizeAssetPath(path);
            var snapshot = ApprovedBaselineFiles.SingleOrDefault(item =>
                string.Equals(item.path, normalized, StringComparison.Ordinal));
            return snapshot != null && snapshot.exists && string.Equals(snapshot.sha256,
                sha256, StringComparison.Ordinal);
        }

        private static FileSnapshot[] GetApprovedBaselineFiles(int approvedRawEntryCount)
        {
            var includeSupplemental = approvedRawEntryCount == CombinedRawEntryCount;
            if (!includeSupplemental && approvedRawEntryCount != ApprovedRawEntryCount)
                throw new InvalidOperationException("APPROVED_BASELINE_SCOPE_UNSUPPORTED|" +
                    approvedRawEntryCount);
            return ApprovedBaselineFiles.Where(snapshot => includeSupplemental ||
                    !snapshot.path.StartsWith(SupplementalAssetPath,
                        StringComparison.Ordinal))
                .Select(snapshot => Snapshot(snapshot.path, snapshot.sha256)).ToArray();
        }

        private static void ValidateApprovedBaselineSnapshotSet(
            IEnumerable<FileSnapshot> actual,
            int approvedRawEntryCount)
        {
            var expected = GetApprovedBaselineFiles(approvedRawEntryCount);
            var observed = (actual ?? Enumerable.Empty<FileSnapshot>())
                .OrderBy(item => item.path, StringComparer.Ordinal).ToArray();
            expected = expected.OrderBy(item => item.path, StringComparer.Ordinal).ToArray();
            if (observed.Length != expected.Length || observed.Where((item, index) =>
                    item == null || !item.exists ||
                    !string.Equals(item.path, expected[index].path,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.sha256, expected[index].sha256,
                        StringComparison.Ordinal)).Any())
                throw new InvalidOperationException("APPROVED_BASELINE_SNAPSHOT_SET_DRIFT");
        }

        private static void VerifyFileSnapshots(IEnumerable<FileSnapshot> expected)
        {
            var baseline = (expected ?? Enumerable.Empty<FileSnapshot>()).ToArray();
            RequireStageSnapshotTransition(baseline,
                CaptureFileSnapshots(baseline.Select(snapshot => snapshot.path)),
                new StageMutationRule[0]);
        }

        public static StageSnapshotValidation EvaluateStageSnapshotTransition(
            IEnumerable<FileSnapshot> expectedBefore,
            IEnumerable<FileSnapshot> observedAfter,
            IEnumerable<StageMutationRule> mutationRules)
        {
            var expected = (expectedBefore ?? Enumerable.Empty<FileSnapshot>()).ToArray();
            var observed = (observedAfter ?? Enumerable.Empty<FileSnapshot>()).ToArray();
            var rules = (mutationRules ?? Enumerable.Empty<StageMutationRule>()).ToArray();
            var reasons = new HashSet<string>(StringComparer.Ordinal);
            var changed = new HashSet<string>(StringComparer.Ordinal);
            if (expected.Length == 0)
                reasons.Add("EMPTY_EXPECTED_STAGE_SCOPE");
            if (observed.Length == 0)
                reasons.Add("EMPTY_OBSERVED_STAGE_SCOPE");
            if (expected.Any(item => item == null))
                reasons.Add("NULL_EXPECTED_STAGE_SNAPSHOT");
            if (observed.Any(item => item == null))
                reasons.Add("NULL_OBSERVED_STAGE_SNAPSHOT");
            var expectedGroups = expected.Where(item => item != null)
                .GroupBy(item => NormalizeAssetPath(item.path), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            var observedGroups = observed.Where(item => item != null)
                .GroupBy(item => NormalizeAssetPath(item.path), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            foreach (var duplicate in expectedGroups.Where(pair => pair.Value.Length != 1))
                reasons.Add("DUPLICATE_EXPECTED_STAGE_PATH|" + duplicate.Key);
            foreach (var duplicate in observedGroups.Where(pair => pair.Value.Length != 1))
                reasons.Add("DUPLICATE_OBSERVED_STAGE_PATH|" + duplicate.Key);
            if (rules.Any(rule => rule == null))
                reasons.Add("NULL_STAGE_MUTATION_RULE");
            var ruleGroups = rules.Where(rule => rule != null)
                .GroupBy(rule => NormalizeAssetPath(rule.path), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            foreach (var duplicate in ruleGroups.Where(pair => pair.Value.Length != 1))
                reasons.Add("DUPLICATE_STAGE_MUTATION_RULE|" + duplicate.Key);
            foreach (var pair in ruleGroups)
            {
                FileSnapshot[] expectedGroup;
                if (!expectedGroups.TryGetValue(pair.Key, out expectedGroup) ||
                    expectedGroup.Length != 1)
                {
                    reasons.Add("MUTATION_RULE_PATH_OUT_OF_SCOPE|" + pair.Key);
                    continue;
                }
                if (pair.Value.Length != 1) continue;
                var rule = pair.Value[0];
                var before = expectedGroup[0];
                if (before.exists != rule.beforeExists ||
                    !string.Equals(before.sha256 ?? string.Empty,
                        rule.beforeSha256 ?? string.Empty, StringComparison.Ordinal))
                    reasons.Add("MUTATION_RULE_PRESTATE_DRIFT|" + pair.Key);
                if (!string.Equals(rule.mutationKind, ModifyExistingStageMutation,
                        StringComparison.Ordinal) &&
                    !string.Equals(rule.mutationKind, CreateMissingStageMutation,
                        StringComparison.Ordinal))
                    reasons.Add("UNKNOWN_STAGE_MUTATION_KIND|" + pair.Key);
                if (string.Equals(rule.mutationKind, ModifyExistingStageMutation,
                        StringComparison.Ordinal) && !before.exists)
                    reasons.Add("MODIFY_EXISTING_REQUIRES_EXISTING_PRESTATE|" + pair.Key);
                if (string.Equals(rule.mutationKind, CreateMissingStageMutation,
                        StringComparison.Ordinal) && before.exists)
                    reasons.Add("CREATE_MISSING_REQUIRES_MISSING_PRESTATE|" + pair.Key);
            }
            foreach (var path in expectedGroups.Keys.Union(observedGroups.Keys,
                         StringComparer.Ordinal))
            {
                FileSnapshot[] beforeGroup;
                FileSnapshot[] afterGroup;
                if (!expectedGroups.TryGetValue(path, out beforeGroup) ||
                    !observedGroups.TryGetValue(path, out afterGroup))
                {
                    reasons.Add("STAGE_SCOPE_DRIFT|" + path);
                    continue;
                }
                if (beforeGroup.Length != 1 || afterGroup.Length != 1) continue;
                var before = beforeGroup[0];
                var after = afterGroup[0];
                if (before.exists == after.exists && string.Equals(before.sha256 ?? string.Empty,
                        after.sha256 ?? string.Empty, StringComparison.Ordinal))
                    continue;
                changed.Add(path);
                StageMutationRule[] ruleGroup;
                if (!ruleGroups.TryGetValue(path, out ruleGroup) || ruleGroup.Length != 1)
                {
                    reasons.Add("UNOWNED_STAGE_CHANGE|" + path);
                    continue;
                }
                var rule = ruleGroup[0];
                if (string.Equals(rule.mutationKind, ModifyExistingStageMutation,
                        StringComparison.Ordinal) && !after.exists)
                    reasons.Add("OWNED_EXISTING_FILE_REMOVED|" + path);
                else if (string.Equals(rule.mutationKind, CreateMissingStageMutation,
                        StringComparison.Ordinal) && !after.exists)
                    reasons.Add("OWNED_CREATION_INVALID_TRANSITION|" + path);
            }
            return new StageSnapshotValidation
            {
                success = reasons.Count == 0,
                changedPaths = changed.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray(),
                reasons = reasons.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray()
            };
        }

        public static StageSnapshotValidation EvaluateBackupSnapshotContract(
            IEnumerable<FileSnapshot> approvedOriginal,
            IEnumerable<FileSnapshot> manifestOriginal,
            IEnumerable<FileSnapshot> backupRecords,
            IEnumerable<FileSnapshot> backupContents)
        {
            var approved = (approvedOriginal ?? Enumerable.Empty<FileSnapshot>()).ToArray();
            var checks = new[]
            {
                new
                {
                    Prefix = "MANIFEST_BASELINE_",
                    Result = EvaluateStageSnapshotTransition(approved,
                        manifestOriginal, new StageMutationRule[0])
                },
                new
                {
                    Prefix = "BACKUP_RECORD_",
                    Result = EvaluateStageSnapshotTransition(approved,
                        backupRecords, new StageMutationRule[0])
                },
                new
                {
                    Prefix = "BACKUP_CONTENT_",
                    Result = EvaluateStageSnapshotTransition(approved,
                        backupContents, new StageMutationRule[0])
                }
            };
            var reasons = checks.SelectMany(check =>
                    (check.Result.reasons ?? new string[0]).Select(reason =>
                        check.Prefix + reason))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var changed = checks.SelectMany(check =>
                    check.Result.changedPaths ?? new string[0])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return new StageSnapshotValidation
            {
                success = reasons.Length == 0,
                changedPaths = changed,
                reasons = reasons
            };
        }

        private static void RequireStageSnapshotTransition(
            IEnumerable<FileSnapshot> expectedBefore,
            IEnumerable<FileSnapshot> observedAfter,
            IEnumerable<StageMutationRule> mutationRules)
        {
            var validation = EvaluateStageSnapshotTransition(expectedBefore,
                observedAfter, mutationRules);
            if (!validation.success)
                throw new InvalidOperationException("TRANSACTION_STAGE_DRIFT|" +
                    string.Join(",", validation.reasons ?? new string[0]));
        }

        private static void ValidateLiveTransactionState(
            ApprovedRowAudit[] rows,
            ISet<string> completed)
        {
            foreach (var group in rows.GroupBy(item => item.assetPath,
                         StringComparer.Ordinal))
            {
                WithAssetContents(group.Key, false, roots =>
                {
                    var lookup = CreateExactTextLookup(group.Key, roots);
                    foreach (var row in group)
                    {
                        TMP_Text text;
                        var identity = MakeIdentity(group.Key, row.objectPath,
                            row.componentIndex);
                        if (!lookup.TryGetValue(identity, out text))
                            throw new InvalidOperationException("MISSING_IDENTITY|" + identity);
                        if (!string.Equals(ComputeNonTargetFingerprint(text, group.Key),
                                row.nonTargetFingerprint, StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                "NON_TARGET_FINGERPRINT_DRIFT|" + identity);
                        ValidateLiveRowRelationship(text, row);
                        var shouldBeAfter = completed.Contains(row.canonicalIdentity);
                        var valid = shouldBeAfter
                            ? IsLiveRowAfter(text, row)
                            : IsLiveRowBefore(text, row, true);
                        if (!valid)
                            throw new InvalidOperationException((shouldBeAfter
                                ? "COMPLETED_STATE_DRIFT|" : "PENDING_STATE_DRIFT|") + identity);
                    }
                    return 0;
                });
            }
        }

        private static int RollbackTransactionContents(TransactionRecord transaction)
        {
            var rows = GetTransactionRows(transaction);
            ValidateRollbackOwnership(transaction, rows);
            VerifyOwnedMaterials(transaction.createdAssets);
            VerifyOwnedMaterialFolder(transaction);
            var restored = RestoreApprovedBaselineBackup(transaction);
            ValidateLiveTransactionState(rows,
                new HashSet<string>(StringComparer.Ordinal));
            DeleteOwnedMaterials(transaction.createdAssets);
            DeleteOwnedMaterialFolder(transaction);
            AssetDatabase.SaveAssets();
            VerifyFileSnapshots(transaction.approvedBaselineFiles);
            return restored;
        }

        private static void ValidateRollbackOwnership(
            TransactionRecord transaction,
            ApprovedRowAudit[] rows)
        {
            if (transaction.expectedFiles == null || transaction.expectedFiles.Length == 0)
                throw new InvalidOperationException("TRANSACTION_STAGE_SNAPSHOT_MISSING");
            ValidateApprovedBaselineSnapshotSet(transaction.approvedBaselineFiles,
                transaction.approvedRawEntryCount);
            ValidateOwnedMaterialRecordContract(transaction);
            var current = CaptureFileSnapshots(GetTransactionFilePaths(transaction));
            var applying = (transaction.batches ?? new BatchRecord[0]).Where(record =>
                    string.Equals(record.state, "APPLYING", StringComparison.Ordinal))
                .ToArray();
            if (applying.Length > 1)
                throw new InvalidOperationException("MULTIPLE_PENDING_TRANSACTION_STAGES");
            var completed = new HashSet<string>(
                transaction.completedCanonicalIdentities ?? new string[0],
                StringComparer.Ordinal);
            if (applying.Length == 0)
            {
                if ((transaction.batches ?? new BatchRecord[0]).Any(record =>
                        !string.Equals(record.state, "APPLIED", StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        "TRANSACTION_BATCH_JOURNAL_STATE_DRIFT");
                if (string.Equals(transaction.state, "APPLYING", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "PENDING_TRANSACTION_STAGE_JOURNAL_MISSING");
                RequireStageSnapshotTransition(transaction.expectedFiles, current,
                    new StageMutationRule[0]);
                ValidateLiveTransactionState(rows, completed);
                return;
            }

            var pending = applying[0];
            if (!string.Equals(transaction.state, "APPLYING", StringComparison.Ordinal) ||
                pending.preStageFiles == null || pending.preStageFiles.Length == 0 ||
                pending.mutationRules == null || pending.mutationRules.Length == 0 ||
                string.IsNullOrWhiteSpace(pending.stageContractSha256))
                throw new InvalidOperationException(
                    "PENDING_TRANSACTION_STAGE_JOURNAL_DRIFT");
            RequireStageSnapshotTransition(transaction.expectedFiles,
                pending.preStageFiles, new StageMutationRule[0]);
            var selected = pending.canonicalIdentities ?? new string[0];
            if (selected.Length == 0 || selected.Any(string.IsNullOrWhiteSpace) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length ||
                selected.Any(completed.Contains))
                throw new InvalidOperationException(
                    "PENDING_TRANSACTION_SELECTION_DRIFT");
            var all = new HashSet<string>((transaction.entries ?? new DryRunEntry[0])
                .Select(entry => entry.canonicalIdentity), StringComparer.Ordinal);
            if (selected.Any(identity => !all.Contains(identity)))
                throw new InvalidOperationException(
                    "PENDING_TRANSACTION_SELECTION_OUT_OF_SCOPE");
            var stageValidation = EvaluateApplyingStageSnapshot(transaction,
                pending, current);
            if (!stageValidation.success)
                throw new InvalidOperationException(
                    "PENDING_TRANSACTION_STAGE_DRIFT|" +
                    string.Join(",", stageValidation.reasons ?? new string[0]));
            ValidateLiveRowsForRollback(rows, completed,
                new HashSet<string>(selected,
                    StringComparer.Ordinal));
        }

        private static void ValidateOwnedMaterialRecordContract(
            TransactionRecord transaction)
        {
            var expected = (transaction.materials ?? new MaterialPlan[0])
                .OrderBy(material => material.targetMaterialPath,
                    StringComparer.Ordinal)
                .Select(material => new CreatedAssetRecord
                {
                    path = material.targetMaterialPath,
                    fingerprint = material.expectedMaterialFingerprint
                }).ToArray();
            var actual = (transaction.createdAssets ?? new CreatedAssetRecord[0])
                .OrderBy(record => record.path, StringComparer.Ordinal).ToArray();
            var ownershipRequired = (transaction.batches ?? new BatchRecord[0]).Length > 0 ||
                (transaction.expectedFiles ?? new FileSnapshot[0]).Any(snapshot =>
                    expected.Any(record => string.Equals(record.path, snapshot.path,
                        StringComparison.Ordinal)) && snapshot.exists);
            var batches = transaction.batches ?? new BatchRecord[0];
            var creationBatches = batches.Where(batch =>
                batch != null && batch.includesMaterialCreation).ToArray();
            if (creationBatches.Length > 1)
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_MULTIPLE_CREATION_BATCHES");
            foreach (var batch in batches)
            {
                if (batch == null || string.IsNullOrWhiteSpace(batch.batchId) ||
                    batch.preStageFiles == null || batch.preStageFiles.Length == 0 ||
                    batch.mutationRules == null || batch.mutationRules.Length == 0 ||
                    string.IsNullOrWhiteSpace(batch.stageContractSha256) ||
                    !string.Equals(batch.stageContractSha256,
                        ComputeStageContractSha256(batch), StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "TRANSACTION_BATCH_STAGE_CONTRACT_DRIFT");
            }
            if (!ownershipRequired && actual.Length == 0)
            {
                if (!string.IsNullOrEmpty(transaction.ownedMaterialFolderPath) ||
                    !string.IsNullOrEmpty(transaction.ownedMaterialFolderGuid) ||
                    !string.IsNullOrEmpty(transaction.ownedMaterialFolderMetaSha256))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_RECORD_PREMATURE");
                return;
            }
            if (actual.Length != expected.Length || actual.Where((record, index) =>
                    !string.Equals(record.path, expected[index].path,
                        StringComparison.Ordinal) ||
                    !string.Equals(record.fingerprint, expected[index].fingerprint,
                        StringComparison.Ordinal)).Any())
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_RECORD_CONTRACT_DRIFT");
            if (creationBatches.Length != 1 || actual.Any(record =>
                    !string.Equals(record.creationBatchId, creationBatches[0].batchId,
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_CREATION_BATCH_DRIFT");
            var creationPending = string.Equals(transaction.state, "APPLYING",
                    StringComparison.Ordinal) &&
                string.Equals(creationBatches[0].state, "APPLYING",
                    StringComparison.Ordinal);
            foreach (var record in actual)
            {
                var evidenceCount = new[]
                {
                    record.guid, record.assetSha256, record.metaSha256
                }.Count(value => !string.IsNullOrWhiteSpace(value));
                if (evidenceCount != 0 && evidenceCount != 3)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_EVIDENCE_PARTIAL|" + record.path);
                if (!creationPending && evidenceCount != 3)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_EVIDENCE_MISSING|" + record.path);
            }
            if (transaction.materialFolderExistedAtBegin)
            {
                if (!string.IsNullOrEmpty(transaction.ownedMaterialFolderPath) ||
                    !string.IsNullOrEmpty(transaction.ownedMaterialFolderGuid) ||
                    !string.IsNullOrEmpty(transaction.ownedMaterialFolderMetaSha256))
                    throw new InvalidOperationException(
                        "PREEXISTING_MATERIAL_FOLDER_CANNOT_BE_OWNED");
            }
            else
            {
                if (!string.Equals(transaction.ownedMaterialFolderPath,
                        ApprovedMaterialFolderPath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_SCOPE_DRIFT");
                var folderEvidenceCount = new[]
                {
                    transaction.ownedMaterialFolderGuid,
                    transaction.ownedMaterialFolderMetaSha256
                }.Count(value => !string.IsNullOrWhiteSpace(value));
                if (folderEvidenceCount != 0 && folderEvidenceCount != 2)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_EVIDENCE_PARTIAL");
                if (!creationPending && folderEvidenceCount != 2)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_EVIDENCE_MISSING");
            }
        }

        private static void ValidateMaterialFolderPair(
            bool folderExists,
            bool metaExists)
        {
            var parentPath = NormalizeAssetPath(
                Path.GetDirectoryName(ApprovedMaterialFolderPath));
            if (!Directory.Exists(ToFullPath(parentPath)) ||
                !File.Exists(ToFullPath(parentPath + ".meta")) ||
                !AssetDatabase.IsValidFolder(parentPath))
                throw new InvalidOperationException(
                    "MATERIAL_FOLDER_PARENT_BASELINE_MISSING|" + parentPath);
            if (folderExists != metaExists)
                throw new InvalidOperationException(
                    "MATERIAL_FOLDER_META_PAIR_DRIFT|" + ApprovedMaterialFolderPath);
            if (folderExists && !AssetDatabase.IsValidFolder(ApprovedMaterialFolderPath))
                throw new InvalidOperationException(
                    "MATERIAL_FOLDER_ASSET_DATABASE_DRIFT|" +
                    ApprovedMaterialFolderPath);
        }

        private static string[] GetExpectedNewAssetPaths(
            IEnumerable<MaterialPlan> materials,
            bool materialFolderExistedAtBegin)
        {
            var paths = (materials ?? Enumerable.Empty<MaterialPlan>())
                .SelectMany(material => new[]
                {
                    NormalizeAssetPath(material.targetMaterialPath),
                    NormalizeAssetPath(material.targetMaterialPath) + ".meta"
                }).ToList();
            if (!materialFolderExistedAtBegin)
            {
                paths.Add(ApprovedMaterialFolderPath);
                paths.Add(ApprovedMaterialFolderPath + ".meta");
            }
            return paths.Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static void ValidateMaterialCreationBaseline(
            TransactionRecord transaction)
        {
            if ((transaction.createdAssets ?? new CreatedAssetRecord[0]).Length != 0)
                throw new InvalidOperationException(
                    "MATERIAL_CREATION_BASELINE_ALREADY_OWNED");
            var folderExists = Directory.Exists(ToFullPath(ApprovedMaterialFolderPath));
            var folderMetaExists = File.Exists(
                ToFullPath(ApprovedMaterialFolderPath + ".meta"));
            ValidateMaterialFolderPair(folderExists, folderMetaExists);
            if (folderExists != transaction.materialFolderExistedAtBegin)
                throw new InvalidOperationException(
                    "MATERIAL_FOLDER_BASELINE_DRIFT");
            var expected = (transaction.expectedFiles ?? new FileSnapshot[0])
                .Where(snapshot => snapshot != null)
                .GroupBy(snapshot => NormalizeAssetPath(snapshot.path),
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(),
                    StringComparer.Ordinal);
            Action<string, bool> requireState = (path, exists) =>
            {
                FileSnapshot[] group;
                var normalized = NormalizeAssetPath(path);
                if (!expected.TryGetValue(normalized, out group) || group.Length != 1 ||
                    group[0].exists != exists)
                    throw new InvalidOperationException(
                        "MATERIAL_CREATION_EXPECTED_PRESTATE_DRIFT|" + normalized);
            };
            requireState(ApprovedMaterialFolderPath + ".meta", folderExists);
            foreach (var material in transaction.materials ?? new MaterialPlan[0])
            {
                requireState(material.targetMaterialPath, false);
                requireState(material.targetMaterialPath + ".meta", false);
                if (File.Exists(ToFullPath(material.targetMaterialPath)) ||
                    File.Exists(ToFullPath(material.targetMaterialPath + ".meta")) ||
                    AssetDatabase.LoadMainAssetAtPath(material.targetMaterialPath) != null)
                    throw new InvalidOperationException(
                        "TARGET_MATERIAL_MUST_BE_ABSENT_BEFORE_TRANSACTION|" +
                        material.targetMaterialPath);
            }
        }

        private static void SealOwnedCreationEvidence(
            TransactionRecord transaction,
            string batchId)
        {
            foreach (var record in transaction.createdAssets ??
                         new CreatedAssetRecord[0])
            {
                if (!string.Equals(record.creationBatchId, batchId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_CREATION_BATCH_DRIFT|" + record.path);
                var material = AssetDatabase.LoadAssetAtPath<Material>(record.path);
                if (material == null || !File.Exists(ToFullPath(record.path)) ||
                    !File.Exists(ToFullPath(record.path + ".meta")))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_CREATION_INCOMPLETE|" + record.path);
                if (!string.Equals(ComputeMaterialFingerprint(material, true, true),
                        record.fingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_DRIFT|" + record.path);
                record.guid = AssetDatabase.AssetPathToGUID(record.path);
                record.assetSha256 = Sha256File(ToFullPath(record.path));
                record.metaSha256 = Sha256File(ToFullPath(record.path + ".meta"));
                if (string.IsNullOrWhiteSpace(record.guid))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_GUID_MISSING|" + record.path);
            }
            VerifyOwnedMaterialFolder(transaction);
            if (!string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderPath))
            {
                transaction.ownedMaterialFolderGuid =
                    AssetDatabase.AssetPathToGUID(ApprovedMaterialFolderPath);
                transaction.ownedMaterialFolderMetaSha256 = Sha256File(
                    ToFullPath(ApprovedMaterialFolderPath + ".meta"));
                if (string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderGuid))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_GUID_MISSING");
            }
        }

        private static void VerifyOwnedMaterialFolder(TransactionRecord transaction)
        {
            var folderExists = Directory.Exists(ToFullPath(ApprovedMaterialFolderPath));
            var metaExists = File.Exists(ToFullPath(ApprovedMaterialFolderPath + ".meta"));
            ValidateMaterialFolderPair(folderExists, metaExists);
            if (transaction.materialFolderExistedAtBegin)
            {
                if (!folderExists || !string.IsNullOrEmpty(
                        transaction.ownedMaterialFolderPath))
                    throw new InvalidOperationException(
                        "PREEXISTING_MATERIAL_FOLDER_DRIFT");
                return;
            }
            if (!string.Equals(transaction.ownedMaterialFolderPath,
                    ApprovedMaterialFolderPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_FOLDER_SCOPE_DRIFT");
            if (!folderExists)
            {
                if (!string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderGuid) ||
                    !string.IsNullOrWhiteSpace(
                        transaction.ownedMaterialFolderMetaSha256))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FOLDER_EVIDENCE_WITHOUT_FOLDER");
                return;
            }
            var allowedFiles = new HashSet<string>((transaction.materials ??
                    new MaterialPlan[0]).SelectMany(material => new[]
                {
                    Path.GetFullPath(ToFullPath(material.targetMaterialPath)),
                    Path.GetFullPath(ToFullPath(material.targetMaterialPath + ".meta"))
                }), StringComparer.OrdinalIgnoreCase);
            var unknown = Directory.EnumerateFileSystemEntries(
                    ToFullPath(ApprovedMaterialFolderPath), "*",
                    SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath)
                .Where(path => !allowedFiles.Contains(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            if (unknown.Length > 0)
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_FOLDER_UNKNOWN_CONTENT|" +
                    string.Join(",", unknown));
            if (!string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderGuid) &&
                (!string.Equals(AssetDatabase.AssetPathToGUID(
                         ApprovedMaterialFolderPath),
                     transaction.ownedMaterialFolderGuid, StringComparison.Ordinal) ||
                 !string.Equals(Sha256File(ToFullPath(
                         ApprovedMaterialFolderPath + ".meta")),
                     transaction.ownedMaterialFolderMetaSha256,
                     StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_FOLDER_EVIDENCE_DRIFT");
        }

        private static void DeleteOwnedMaterialFolder(TransactionRecord transaction)
        {
            if (string.IsNullOrWhiteSpace(transaction.ownedMaterialFolderPath)) return;
            VerifyOwnedMaterialFolder(transaction);
            var fullPath = ToFullPath(ApprovedMaterialFolderPath);
            if (!Directory.Exists(fullPath)) return;
            if (Directory.EnumerateFileSystemEntries(fullPath).Any())
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_FOLDER_NOT_EMPTY");
            if (!AssetDatabase.DeleteAsset(ApprovedMaterialFolderPath))
                throw new InvalidOperationException(
                    "COULD_NOT_DELETE_OWNED_MATERIAL_FOLDER");
            if (Directory.Exists(fullPath) || File.Exists(ToFullPath(
                    ApprovedMaterialFolderPath + ".meta")))
                throw new InvalidOperationException(
                    "OWNED_MATERIAL_FOLDER_DELETE_INCOMPLETE");
        }

        private static int RestoreApprovedBaselineBackup(TransactionRecord transaction)
        {
            var manifestPath = NormalizeAssetPath(transaction.backupRoot) +
                "/backup-manifest.json";
            var fullManifestPath = ToFullPath(manifestPath);
            if (!File.Exists(fullManifestPath))
                throw new InvalidOperationException("ROLLBACK_BACKUP_MANIFEST_MISSING|" +
                    manifestPath);
            var manifest = JsonUtility.FromJson<BackupManifest>(
                File.ReadAllText(fullManifestPath, new UTF8Encoding(false, true)));
            if (manifest == null ||
                manifest.schemaVersion != BackupManifestSchemaVersion ||
                !string.Equals(manifest.taskId, transaction.taskId,
                    StringComparison.Ordinal) ||
                !string.Equals(manifest.mappingSha256, transaction.mappingSha256,
                    StringComparison.Ordinal) ||
                manifest.materialFolderExistedAtBegin !=
                    transaction.materialFolderExistedAtBegin)
                throw new InvalidOperationException("ROLLBACK_BACKUP_MANIFEST_DRIFT");
            var expectedNewPaths = GetExpectedNewAssetPaths(transaction.materials,
                transaction.materialFolderExistedAtBegin);
            if (!(manifest.newAssetPaths ?? new string[0])
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(expectedNewPaths, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "ROLLBACK_BACKUP_NEW_ASSET_SCOPE_DRIFT");
            var expected = transaction.approvedBaselineFiles ?? new FileSnapshot[0];
            var records = manifest.files ?? new BackupFileRecord[0];
            var recordSnapshots = records.Select(record => new FileSnapshot
            {
                path = NormalizeAssetPath(record.sourcePath),
                exists = true,
                sha256 = record.sha256
            }).ToArray();
            var contentSnapshots = records.Select(record =>
            {
                var backupPath = ToFullPath(record.backupPath);
                var exists = File.Exists(backupPath);
                return new FileSnapshot
                {
                    path = NormalizeAssetPath(record.sourcePath),
                    exists = exists,
                    sha256 = exists ? Sha256File(backupPath) : string.Empty
                };
            }).ToArray();
            var backupValidation = EvaluateBackupSnapshotContract(expected,
                manifest.approvedBaselineFiles, recordSnapshots, contentSnapshots);
            if (!backupValidation.success)
                throw new InvalidOperationException("ROLLBACK_BACKUP_CONTRACT_DRIFT|" +
                    string.Join(",", backupValidation.reasons));
            var bySource = records.ToDictionary(record => record.sourcePath,
                StringComparer.Ordinal);
            foreach (var snapshot in expected)
                File.Copy(ToFullPath(bySource[snapshot.path].backupPath),
                    ToFullPath(snapshot.path), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            VerifyFileSnapshots(expected);
            return transaction.entries.Length;
        }

        private static void EnsureApprovedScenesClosed(IEnumerable<string> assetPaths)
        {
            foreach (var path in (assetPaths ?? Enumerable.Empty<string>()).Where(path =>
                         path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)))
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded)
                    throw new InvalidOperationException(
                        "APPROVED_TRANSACTION_REQUIRES_SCENE_CLOSED|" + path);
            }
        }

        private static ApprovedRowAudit[] GetTransactionRows(TransactionRecord transaction)
        {
            if (transaction.rows != null && transaction.rows.Length > 0)
                return transaction.rows;
            return (transaction.entries ?? new DryRunEntry[0]).Select(entry =>
                new ApprovedRowAudit
                {
                    validationReasons = new string[0],
                    rowIdentity = entry.canonicalIdentity,
                    canonicalIdentity = entry.canonicalIdentity,
                    prefabSourceIdentity = entry.prefabSourceIdentity,
                    beforeFontOverride = entry.beforeFontOverride,
                    beforeMaterialOverride = entry.beforeMaterialOverride,
                    beforeLoadedFontOverride = entry.beforeLoadedFontOverride,
                    beforeLoadedMaterialOverride = entry.beforeLoadedMaterialOverride,
                    migrationFontOverrideRequired = entry.migrationFontOverrideRequired,
                    migrationMaterialOverrideRequired =
                        entry.migrationMaterialOverrideRequired,
                    assetPath = entry.writeAssetPath,
                    objectPath = entry.writeObjectPath,
                    componentIndex = entry.writeComponentIndex,
                    beforeFontGuid = entry.beforeFontGuid,
                    approvedObservationFontGuid = entry.approvedObservationFontGuid,
                    approvedObservationMaterialGlobalId =
                        entry.approvedObservationMaterialGlobalId,
                    beforeMaterialGlobalId = entry.beforeMaterialGlobalId,
                    beforeMaterialPath = entry.beforeMaterialPath,
                    beforeEffectiveMaterialGlobalId = entry.beforeEffectiveMaterialGlobalId,
                    beforeEffectiveMaterialPath = entry.beforeEffectiveMaterialPath,
                    beforeMaterialNormalizationReason =
                        entry.beforeMaterialNormalizationReason,
                    savedReferenceOwnerIdentity = entry.savedReferenceOwnerIdentity,
                    savedReferenceAssetPath = entry.savedReferenceAssetPath,
                    savedReferenceAssetSha256 = entry.savedReferenceAssetSha256,
                    savedReferenceSourceAssetPath = entry.savedReferenceSourceAssetPath,
                    savedReferenceSourceAssetSha256 =
                        entry.savedReferenceSourceAssetSha256,
                    savedReferenceProven = entry.savedReferenceProven,
                    savedReferenceAssetShaMatches =
                        entry.savedReferenceAssetShaMatches,
                    afterFontGuid = entry.afterFontGuid,
                    afterMaterialPath = entry.afterMaterialPath,
                    nonTargetFingerprint = entry.nonTargetFingerprint
                }).ToArray();
        }

        private static void ValidateLiveRowsForRollback(
            ApprovedRowAudit[] rows,
            ISet<string> completed,
            ISet<string> pending)
        {
            foreach (var group in rows.GroupBy(item => item.assetPath,
                         StringComparer.Ordinal))
            {
                WithAssetContents(group.Key, false, roots =>
                {
                    var lookup = CreateExactTextLookup(group.Key, roots);
                    foreach (var row in group)
                    {
                        TMP_Text text;
                        var identity = MakeIdentity(group.Key, row.objectPath,
                            row.componentIndex);
                        if (!lookup.TryGetValue(identity, out text))
                            throw new InvalidOperationException("MISSING_IDENTITY|" + identity);
                        if (!string.Equals(ComputeNonTargetFingerprint(text, group.Key),
                                row.nonTargetFingerprint,
                                StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                "NON_TARGET_FINGERPRINT_DRIFT|" + identity);
                        ValidateLiveRowRelationship(text, row);
                        var isBefore = IsLiveRowBefore(text, row, true);
                        var isAfter = IsLiveRowAfter(text, row);
                        if (completed.Contains(row.canonicalIdentity))
                        {
                            if (!isAfter)
                                throw new InvalidOperationException(
                                    "COMPLETED_ROLLBACK_STATE_DRIFT|" + identity);
                        }
                        else if (pending.Contains(row.canonicalIdentity))
                        {
                            if (!isBefore && !isAfter)
                                throw new InvalidOperationException(
                                    "UNKNOWN_PENDING_ROLLBACK_STATE|" + identity);
                        }
                        else if (!isBefore)
                            throw new InvalidOperationException(
                                "UNOWNED_ROLLBACK_STATE|" + identity);
                    }
                    return 0;
                });
            }
        }

        private static void ValidateLiveRowRelationship(TMP_Text text, ApprovedRowAudit row)
        {
            string sourceIdentity;
            bool fontOverride;
            bool materialOverride;
            TryReadPrefabRelationship(text, out sourceIdentity, out fontOverride,
                out materialOverride);
            if (!string.Equals(sourceIdentity ?? string.Empty,
                    row.prefabSourceIdentity ?? string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "PREFAB_RELATIONSHIP_DRIFT|" + row.rowIdentity);
        }

        private static bool IsLiveRowBefore(
            TMP_Text text,
            ApprovedRowAudit row,
            bool referenceSnapshotTrusted)
        {
            string sourceIdentity;
            bool fontOverride;
            bool materialOverride;
            TryReadPrefabRelationship(text, out sourceIdentity, out fontOverride,
                out materialOverride);
            var saved = ReadSavedTmpReferences(text, row.assetPath, fontOverride,
                materialOverride);
            var effective = ReadSharedMaterial(text);
            var reason = ClassifyMaterialResolution(
                BuildMaterialResolutionObservation(row.rowIdentity,
                    row.approvedObservationFontGuid,
                    row.approvedObservationMaterialGlobalId, saved, effective,
                    text.font, sourceIdentity, referenceSnapshotTrusted));
            return string.Equals(saved.FontGuid, row.beforeFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(saved.MaterialGlobalId,
                    row.beforeMaterialGlobalId, StringComparison.Ordinal) &&
                string.Equals(GlobalIdFor(effective), row.beforeEffectiveMaterialGlobalId,
                    StringComparison.Ordinal) &&
                string.Equals(reason, row.beforeMaterialNormalizationReason,
                    StringComparison.Ordinal) &&
                string.Equals(saved.OwnerIdentity, row.savedReferenceOwnerIdentity,
                    StringComparison.Ordinal) &&
                string.Equals(saved.AssetPath, row.savedReferenceAssetPath,
                    StringComparison.Ordinal) &&
                string.Equals(saved.SourceAssetPath,
                    row.savedReferenceSourceAssetPath, StringComparison.Ordinal) &&
                saved.Proven == row.savedReferenceProven &&
                saved.FontOverride == row.beforeFontOverride &&
                saved.MaterialOverride == row.beforeMaterialOverride &&
                fontOverride == row.beforeLoadedFontOverride &&
                materialOverride == row.beforeLoadedMaterialOverride;
        }

        private static bool IsLiveRowAfter(TMP_Text text, ApprovedRowAudit row)
        {
            string sourceIdentity;
            bool fontOverride;
            bool materialOverride;
            TryReadPrefabRelationship(text, out sourceIdentity, out fontOverride,
                out materialOverride);
            var saved = ReadSavedTmpReferences(text, row.assetPath, fontOverride,
                materialOverride);
            return string.Equals(saved.FontGuid, row.afterFontGuid,
                    StringComparison.Ordinal) &&
                string.Equals(saved.MaterialPath, row.afterMaterialPath,
                    StringComparison.Ordinal) &&
                string.Equals(AssetDatabase.GetAssetPath(ReadSharedMaterial(text)),
                    row.afterMaterialPath, StringComparison.Ordinal) &&
                saved.FontOverride == (row.migrationFontOverrideRequired ||
                    row.beforeFontOverride) &&
                saved.MaterialOverride == (row.migrationMaterialOverrideRequired ||
                    row.beforeMaterialOverride);
        }

        private static int ApplyLiveEntries(DryRunEntry[] entries, bool rollback)
        {
            IEnumerable<IGrouping<string, DryRunEntry>> groups =
                entries.GroupBy(item => item.writeAssetPath, StringComparer.Ordinal);
            groups = rollback
                ? groups.OrderBy(group => group.Key.EndsWith(".unity",
                    StringComparison.OrdinalIgnoreCase)).ThenBy(group => group.Key)
                : groups.OrderByDescending(group => group.Key.EndsWith(".unity",
                    StringComparison.OrdinalIgnoreCase)).ThenBy(group => group.Key);
            var changed = 0;
            foreach (var group in groups)
            {
                changed += WithAssetContentsConditional(group.Key, roots =>
                {
                    var lookup = CreateExactTextLookup(group.Key, roots);
                    var localChanged = 0;
                    foreach (var entry in group)
                    {
                        TMP_Text text;
                        var identity = MakeIdentity(group.Key, entry.writeObjectPath,
                            entry.writeComponentIndex);
                        if (!lookup.TryGetValue(identity, out text))
                            throw new InvalidOperationException("MISSING_IDENTITY|" + identity);
                        var targetFontPath = rollback
                            ? AssetDatabase.GUIDToAssetPath(entry.beforeFontGuid)
                            : AssetDatabase.GUIDToAssetPath(entry.afterFontGuid);
                        var targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetFontPath);
                        var targetMaterial = rollback
                            ? ResolveMaterial(entry.beforeMaterialGlobalId)
                            : AssetDatabase.LoadAssetAtPath<Material>(entry.afterMaterialPath);
                        if (targetFont == null || targetMaterial == null)
                            throw new InvalidOperationException(
                                "TARGET_REFERENCE_MISSING|" + identity);
                        var serialized = new SerializedObject(text);
                        var fontProperty = serialized.FindProperty("m_fontAsset");
                        var materialProperty = serialized.FindProperty("m_sharedMaterial");
                        if (fontProperty == null || materialProperty == null)
                            throw new InvalidOperationException(
                                "TMP_REFERENCE_PROPERTY_MISSING|" + identity);
                        var referenceChange = fontProperty.objectReferenceValue != targetFont ||
                            materialProperty.objectReferenceValue != targetMaterial;
                        var overrideRestore = rollback && PrefabUtility.IsPartOfPrefabInstance(text) &&
                            ((!entry.beforeFontOverride && fontProperty.prefabOverride) ||
                             (!entry.beforeMaterialOverride && materialProperty.prefabOverride));
                        if (!referenceChange && !overrideRestore) continue;
                        if (referenceChange)
                        {
                            fontProperty.objectReferenceValue = targetFont;
                            materialProperty.objectReferenceValue = targetMaterial;
                            if (!serialized.ApplyModifiedPropertiesWithoutUndo())
                                throw new InvalidOperationException("PAIRED_WRITE_FAILED|" + identity);
                        }
                        if (rollback && PrefabUtility.IsPartOfPrefabInstance(text))
                        {
                            serialized.UpdateIfRequiredOrScript();
                            fontProperty = serialized.FindProperty("m_fontAsset");
                            materialProperty = serialized.FindProperty("m_sharedMaterial");
                            if (!entry.beforeFontOverride && fontProperty.prefabOverride)
                                PrefabUtility.RevertPropertyOverride(fontProperty,
                                    InteractionMode.AutomatedAction);
                            if (!entry.beforeMaterialOverride && materialProperty.prefabOverride)
                                PrefabUtility.RevertPropertyOverride(materialProperty,
                                    InteractionMode.AutomatedAction);
                        }
                        localChanged++;
                    }
                    return localChanged;
                });
            }
            return changed;
        }

        private static Dictionary<string, TMP_Text> CreateExactTextLookup(
            string assetPath,
            IEnumerable<GameObject> roots)
        {
            var result = new Dictionary<string, TMP_Text>(StringComparer.Ordinal);
            foreach (var text in EnumerateScopedText(assetPath, roots))
            {
                var identity = MakeIdentity(assetPath, GetPath(text.gameObject),
                    Array.IndexOf(text.GetComponents<TMP_Text>(), text));
                if (result.ContainsKey(identity))
                    throw new InvalidOperationException("DUPLICATE_OBSERVED_IDENTITY|" + identity);
                result.Add(identity, text);
            }
            return result;
        }

        public static string[] GetApprovedBackupAssetPaths(ApprovedMigrationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var expected = ApprovedAssetPaths.Concat(plan.approvedRawEntryCount ==
                    CombinedRawEntryCount ? new[] { SupplementalAssetPath } : new string[0])
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (plan.approvedRawEntryCount != ApprovedRawEntryCount &&
                plan.approvedRawEntryCount != CombinedRawEntryCount)
                throw new InvalidOperationException(
                    "Approved backup row scope is unsupported: " +
                    plan.approvedRawEntryCount);
            var fromRows = (plan.rows ?? new ApprovedRowAudit[0])
                .Select(row => row.assetPath).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var declared = (plan.approvedAssetPaths ?? new string[0])
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!fromRows.SequenceEqual(expected, StringComparer.Ordinal) ||
                !declared.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Approved backup asset scope changed.");
            return expected;
        }

        private static string CreateBackup(
            string taskId,
            ApprovedMigrationPlan plan,
            bool materialFolderExistedAtBegin)
        {
            if (taskId.Any(character => !(char.IsLetterOrDigit(character) ||
                    character == '-' || character == '_')))
                throw new InvalidOperationException("Unsafe font backup task id: " + taskId);
            var backupRoot = "DevTools/Fonts/backup/" + taskId;
            var records = new List<BackupFileRecord>();
            var assets = GetApprovedBackupAssetPaths(plan);
            foreach (var sourcePath in assets.SelectMany(path => new[] { path, path + ".meta" }))
            {
                var sourceFullPath = ToFullPath(sourcePath);
                if (!File.Exists(sourceFullPath))
                    throw new FileNotFoundException("Backup source is missing.", sourceFullPath);
                var backupPath = backupRoot + "/" + sourcePath;
                var backupFullPath = ToFullPath(backupPath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupFullPath));
                var sourceHash = Sha256File(sourceFullPath);
                var approvedSnapshot = plan.approvedBaselineFiles.SingleOrDefault(item =>
                    string.Equals(item.path, sourcePath, StringComparison.Ordinal));
                if (approvedSnapshot == null || !approvedSnapshot.exists ||
                    !string.Equals(approvedSnapshot.sha256, sourceHash,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "BACKUP_APPROVED_BASELINE_DRIFT|" + sourcePath);
                if (File.Exists(backupFullPath))
                {
                    if (!string.Equals(Sha256File(backupFullPath), sourceHash,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Existing backup differs: " + backupPath);
                }
                else
                    File.Copy(sourceFullPath, backupFullPath, false);
                records.Add(new BackupFileRecord
                {
                    sourcePath = sourcePath,
                    backupPath = backupPath,
                    sha256 = sourceHash
                });
            }
            var targetFontFullPath = ToFullPath(plan.targetFontPath);
            var backupManifest = new BackupManifest
            {
                taskId = taskId,
                createdAtUtc = DateTime.UtcNow.ToString("o"),
                mappingPath = plan.mappingPath,
                mappingSha256 = plan.mappingSha256,
                supplementalMappingPath = plan.supplementalMappingPath,
                supplementalMappingSha256 = plan.supplementalMappingSha256,
                approvedRawEntryCount = plan.approvedRawEntryCount,
                files = records.ToArray(),
                materials = plan.materials,
                targetFontGuid = plan.targetFontGuid,
                targetFontPath = plan.targetFontPath,
                targetFontFingerprint = Sha256File(targetFontFullPath),
                newAssetPaths = GetExpectedNewAssetPaths(plan.materials,
                    materialFolderExistedAtBegin),
                approvedBaselineFiles = plan.approvedBaselineFiles,
                materialFolderExistedAtBegin = materialFolderExistedAtBegin
            };
            WriteJsonObject(backupRoot + "/backup-manifest.json", backupManifest);
            return backupRoot;
        }

        private static void VerifyOwnedMaterials(IEnumerable<CreatedAssetRecord> records)
        {
            foreach (var record in records ?? Enumerable.Empty<CreatedAssetRecord>())
            {
                if (record == null || string.IsNullOrWhiteSpace(record.path))
                    throw new InvalidOperationException("OWNED_MATERIAL_RECORD_INVALID");
                var assetExists = File.Exists(ToFullPath(record.path));
                var metaExists = File.Exists(ToFullPath(record.path + ".meta"));
                if (assetExists != metaExists)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_FILE_META_PAIR_DRIFT|" + record.path);
                var material = AssetDatabase.LoadAssetAtPath<Material>(record.path);
                if (!assetExists)
                {
                    if (material != null || !string.IsNullOrWhiteSpace(record.guid) ||
                        !string.IsNullOrWhiteSpace(record.assetSha256) ||
                        !string.IsNullOrWhiteSpace(record.metaSha256))
                        throw new InvalidOperationException(
                            "OWNED_MATERIAL_EVIDENCE_WITHOUT_ASSET|" + record.path);
                    continue;
                }
                if (material == null)
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_TYPE_DRIFT|" + record.path);
                var current = ComputeMaterialFingerprint(material, true, true);
                if (!string.Equals(current, record.fingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_DRIFT|" + record.path);
                if (!string.IsNullOrWhiteSpace(record.guid) &&
                    (!string.Equals(AssetDatabase.AssetPathToGUID(record.path),
                         record.guid, StringComparison.Ordinal) ||
                     !string.Equals(Sha256File(ToFullPath(record.path)),
                         record.assetSha256, StringComparison.Ordinal) ||
                     !string.Equals(Sha256File(ToFullPath(record.path + ".meta")),
                         record.metaSha256, StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_CREATION_EVIDENCE_DRIFT|" + record.path);
            }
        }

        private static void DeleteOwnedMaterials(IEnumerable<CreatedAssetRecord> records)
        {
            VerifyOwnedMaterials(records);
            foreach (var record in records ?? Enumerable.Empty<CreatedAssetRecord>())
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(record.path) == null) continue;
                if (!AssetDatabase.DeleteAsset(record.path))
                    throw new InvalidOperationException(
                        "Could not delete transaction-owned material: " + record.path);
                if (File.Exists(ToFullPath(record.path)) ||
                    File.Exists(ToFullPath(record.path + ".meta")))
                    throw new InvalidOperationException(
                        "OWNED_MATERIAL_DELETE_INCOMPLETE|" + record.path);
            }
        }

        private static int WithAssetContentsConditional(
            string assetPath,
            Func<GameObject[], int> action)
        {
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var activeBefore = SceneManager.GetActiveScene();
                var existing = SceneManager.GetSceneByPath(assetPath);
                var openedHere = !existing.IsValid() || !existing.isLoaded;
                var scene = openedHere
                    ? EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive)
                    : existing;
                try
                {
                    if (!openedHere && scene.isDirty)
                        throw new InvalidOperationException(
                            "Refusing to write an already-loaded dirty scene: " + assetPath);
                    var changed = action(scene.GetRootGameObjects());
                    if (changed > 0 && !EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save scene: " + assetPath);
                    return changed;
                }
                finally
                {
                    if (openedHere) EditorSceneManager.CloseScene(scene, true);
                    if (activeBefore.IsValid() && activeBefore.isLoaded &&
                        SceneManager.GetActiveScene() != activeBefore)
                        SceneManager.SetActiveScene(activeBefore);
                }
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                var changed = action(new[] { root });
                if (changed > 0 && PrefabUtility.SaveAsPrefabAsset(root, assetPath) == null)
                    throw new InvalidOperationException("Could not save prefab: " + assetPath);
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            var normalized = NormalizeAssetPath(assetFolder);
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) &&
                normalized != "Assets")
                throw new InvalidOperationException("Unsafe asset folder: " + assetFolder);
            var current = "Assets";
            foreach (var segment in normalized.Split('/').Skip(1))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, segment);
                    if (string.IsNullOrWhiteSpace(guid))
                        throw new InvalidOperationException("Could not create folder: " + next);
                }
                current = next;
            }
        }

        private static string MakeIdentity(string assetPath, string objectPath, int componentIndex)
        {
            return NormalizeAssetPath(assetPath) + "|" + objectPath + "|" + componentIndex;
        }

        private static string[] ParseIdentity(string identity)
        {
            var last = identity.LastIndexOf('|');
            var middle = last < 0 ? -1 : identity.LastIndexOf('|', last - 1);
            if (middle < 0 || last < 0)
                throw new InvalidOperationException("Invalid font identity: " + identity);
            return new[]
            {
                identity.Substring(0, middle),
                identity.Substring(middle + 1, last - middle - 1),
                identity.Substring(last + 1)
            };
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        private static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(
                    new UTF8Encoding(false).GetBytes(value ?? string.Empty)))
                    .Replace("-", string.Empty);
        }

        private static void BatchGuard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Serializable]
        public sealed class SwapEntry
        {
            public string assetPath;
            public string objectPath;
            public int componentIndex;
            public string componentType;
            public string currentFontPath;
            public string currentFontGuid;
            public string currentSourceFontPath;
            public string currentSourceFontGuid;
            public string sharedMaterialPath;
            public string sharedMaterialGuid;
            public string sharedMaterialGlobalId;
            public string targetFontPath;
            public string targetFontGuid;
            public string targetSourceFontPath;
            public string targetSourceFontGuid;
            public bool sourceFontCompatible;
            public bool materialCompatible;
            public bool safeToSwitchOnlyFontAsset;
            public string limitation;
        }

        [Serializable]
        public sealed class SwapManifest
        {
            public int schemaVersion = 1;
            public string generatedAtUtc;
            public string targetFontPath;
            public string targetFontGuid;
            public string[] allowlistedAssetPaths;
            public int entryCount;
            public int safeEntryCount;
            public int unsafeEntryCount;
            public bool executeAllowed;
            public SwapEntry[] entries;
        }

        [MenuItem(MenuRoot + "Preview ShortCycle Font Swap (Read Only)")]
        public static void PreviewFromMenu()
        {
            var manifest = Preview(
                new[] { DefaultScenePath },
                ShortCycleFontAtlasBuilder.StaticFontAssetPath,
                DefaultManifestPath);
            Debug.Log("[ShortCycleFontSwap] FONT_SWAP_PREVIEW " +
                JsonUtility.ToJson(manifest, true));
        }

        [MenuItem(MenuRoot + "Execute Reviewed ShortCycle Font Swap Manifest")]
        public static void ExecuteFromMenu()
        {
            var result = ExecuteManifest(DefaultManifestPath);
            Debug.Log("[ShortCycleFontSwap] FONT_SWAP_EXECUTED " + result);
        }

        /// <summary>Unity -executeMethod entry point. Accepts -fontAllowlist and -fontManifest.</summary>
        public static void PreviewBatch()
        {
            try
            {
                var allowlist = ReadAllowlistFromCommandLine();
                var manifestPath = ReadArgument("-fontManifest") ?? DefaultManifestPath;
                var manifest = Preview(
                    allowlist,
                    ShortCycleFontAtlasBuilder.StaticFontAssetPath,
                    manifestPath);
                Debug.Log("[ShortCycleFontSwap] FONT_SWAP_PREVIEW " + JsonUtility.ToJson(manifest));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>Unity -executeMethod entry point. Accepts -fontManifest.</summary>
        public static void ExecuteBatch()
        {
            try
            {
                var manifestPath = ReadArgument("-fontManifest") ?? DefaultManifestPath;
                Debug.Log("[ShortCycleFontSwap] FONT_SWAP_EXECUTED " +
                    ExecuteManifest(manifestPath));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static SwapManifest Preview(
            IEnumerable<string> allowlistedAssetPaths,
            string targetFontPath,
            string manifestPath)
        {
            var allowlist = ValidateAllowlist(allowlistedAssetPaths);
            var target = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetFontPath);
            var entries = new List<SwapEntry>();
            foreach (var assetPath in allowlist)
                entries.AddRange(ReadEntries(assetPath, targetFontPath, target));

            var result = new SwapManifest
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                targetFontPath = targetFontPath,
                targetFontGuid = GuidFor(target),
                allowlistedAssetPaths = allowlist,
                entryCount = entries.Count,
                safeEntryCount = entries.Count(entry => entry.safeToSwitchOnlyFontAsset),
                unsafeEntryCount = entries.Count(entry => !entry.safeToSwitchOnlyFontAsset),
                executeAllowed = target != null && entries.Count > 0 &&
                    entries.All(entry => entry.safeToSwitchOnlyFontAsset),
                entries = entries.OrderBy(entry => entry.assetPath, StringComparer.Ordinal)
                    .ThenBy(entry => entry.objectPath, StringComparer.Ordinal)
                    .ThenBy(entry => entry.componentIndex)
                    .ToArray()
            };
            WriteJson(manifestPath, result);
            return result;
        }

        public static string ExecuteManifest(string manifestPath)
        {
            var fullManifestPath = ToFullPath(manifestPath);
            if (!File.Exists(fullManifestPath))
                throw new FileNotFoundException("Font swap manifest is missing.", fullManifestPath);

            var manifest = JsonUtility.FromJson<SwapManifest>(File.ReadAllText(fullManifestPath));
            if (manifest == null || manifest.schemaVersion != 1)
                throw new InvalidOperationException("Unsupported font swap manifest schema.");
            if (!manifest.executeAllowed || manifest.entries == null || manifest.entries.Length == 0)
                throw new InvalidOperationException(
                    "Manifest is not executable. Review every reported material/source limitation first.");

            var allowlist = ValidateAllowlist(manifest.allowlistedAssetPaths);
            var target = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(manifest.targetFontPath);
            if (target == null || !string.Equals(GuidFor(target), manifest.targetFontGuid,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Target font is missing or changed since preview.");

            // Complete a read-only preflight over the whole allowlist before the first write.
            foreach (var assetPath in allowlist)
                VerifyManifestEntries(assetPath, target, manifest.entries);

            var changedComponents = 0;
            foreach (var assetPath in allowlist)
                changedComponents += ApplyAsset(assetPath, target, manifest.entries);

            AssetDatabase.SaveAssets();
            return "assets=" + allowlist.Length + " components=" + changedComponents;
        }

        private static IEnumerable<SwapEntry> ReadEntries(
            string assetPath,
            string targetFontPath,
            TMP_FontAsset target)
        {
            return WithAssetContents(assetPath, false, roots =>
                EnumerateScopedText(assetPath, roots)
                    .Select(text => CreateEntry(assetPath, text, targetFontPath, target))
                    .ToArray());
        }

        private static SwapEntry CreateEntry(
            string assetPath,
            TMP_Text text,
            string targetFontPath,
            TMP_FontAsset target)
        {
            var serialized = new SerializedObject(text);
            var materialProperty = serialized.FindProperty("m_sharedMaterial");
            var sharedMaterial = materialProperty == null
                ? null
                : materialProperty.objectReferenceValue as Material;
            var current = text.font;
            var currentSource = current == null ? null : current.sourceFontFile;
            var targetSource = target == null ? null : target.sourceFontFile;
            var sourceCompatible = currentSource != null && targetSource != null &&
                currentSource == targetSource;
            var materialCompatible = target != null && IsMaterialCompatible(sharedMaterial, target);
            var limitations = new List<string>();
            if (target == null) limitations.Add("TARGET_FONT_MISSING");
            if (current == null) limitations.Add("CURRENT_FONT_MISSING");
            if (!sourceCompatible) limitations.Add("SOURCE_FONT_DIFFERS_OR_UNKNOWN");
            if (!materialCompatible) limitations.Add("MATERIAL_ATLAS_INCOMPATIBLE");

            return new SwapEntry
            {
                assetPath = assetPath,
                objectPath = GetPath(text.gameObject),
                componentIndex = Array.IndexOf(text.GetComponents<TMP_Text>(), text),
                componentType = text.GetType().FullName,
                currentFontPath = AssetDatabase.GetAssetPath(current),
                currentFontGuid = GuidFor(current),
                currentSourceFontPath = AssetDatabase.GetAssetPath(currentSource),
                currentSourceFontGuid = GuidFor(currentSource),
                sharedMaterialPath = AssetDatabase.GetAssetPath(sharedMaterial),
                sharedMaterialGuid = GuidFor(sharedMaterial),
                sharedMaterialGlobalId = GlobalIdFor(sharedMaterial),
                targetFontPath = targetFontPath,
                targetFontGuid = GuidFor(target),
                targetSourceFontPath = AssetDatabase.GetAssetPath(targetSource),
                targetSourceFontGuid = GuidFor(targetSource),
                sourceFontCompatible = sourceCompatible,
                materialCompatible = materialCompatible,
                safeToSwitchOnlyFontAsset = target != null && current != null &&
                    sourceCompatible && materialCompatible,
                limitation = limitations.Count == 0 ? string.Empty : string.Join("|", limitations)
            };
        }

        private static bool IsMaterialCompatible(Material material, TMP_FontAsset target)
        {
            if (material == null) return true;
            var mainTexture = material.GetTexture(ShaderUtilities.ID_MainTex);
            return target.atlasTextures != null &&
                target.atlasTextures.Any(texture => texture != null && texture == mainTexture);
        }

        private static void VerifyManifestEntries(
            string assetPath,
            TMP_FontAsset target,
            IEnumerable<SwapEntry> allEntries)
        {
            var expected = allEntries.Where(entry => entry.assetPath == assetPath)
                .OrderBy(entry => entry.objectPath, StringComparer.Ordinal)
                .ThenBy(entry => entry.componentIndex)
                .ToArray();
            var current = ReadEntries(assetPath, AssetDatabase.GetAssetPath(target), target)
                .OrderBy(entry => entry.objectPath, StringComparer.Ordinal)
                .ThenBy(entry => entry.componentIndex)
                .ToArray();
            if (current.Length != expected.Length)
                throw new InvalidOperationException(assetPath + " TMP count changed since preview.");

            for (var index = 0; index < expected.Length; index++)
            {
                var before = expected[index];
                var now = current[index];
                if (before.objectPath != now.objectPath ||
                    before.componentIndex != now.componentIndex ||
                    before.currentFontGuid != now.currentFontGuid ||
                    before.sharedMaterialGuid != now.sharedMaterialGuid ||
                    before.sharedMaterialGlobalId != now.sharedMaterialGlobalId ||
                    !now.safeToSwitchOnlyFontAsset)
                {
                    throw new InvalidOperationException(
                        assetPath + " changed or became incompatible at " + now.objectPath + ".");
                }
            }
        }

        private static int ApplyAsset(
            string assetPath,
            TMP_FontAsset target,
            IEnumerable<SwapEntry> allEntries)
        {
            var entries = allEntries.Where(entry => entry.assetPath == assetPath).ToArray();
            var changed = 0;
            WithAssetContents(assetPath, true, roots =>
            {
                var lookup = EnumerateScopedText(assetPath, roots)
                    .ToDictionary(
                        text => GetPath(text.gameObject) + "#" +
                            Array.IndexOf(text.GetComponents<TMP_Text>(), text),
                        text => text,
                        StringComparer.Ordinal);
                foreach (var entry in entries)
                {
                    TMP_Text text;
                    if (!lookup.TryGetValue(entry.objectPath + "#" + entry.componentIndex, out text))
                        throw new InvalidOperationException("TMP entry disappeared: " + entry.objectPath);
                    var serialized = new SerializedObject(text);
                    var fontProperty = serialized.FindProperty("m_fontAsset");
                    if (fontProperty == null)
                        throw new InvalidOperationException("TMP m_fontAsset is unavailable: " + entry.objectPath);
                    if (fontProperty.objectReferenceValue == target) continue;
                    fontProperty.objectReferenceValue = target;
                    if (!serialized.ApplyModifiedPropertiesWithoutUndo())
                        throw new InvalidOperationException("TMP font write failed: " + entry.objectPath);
                    changed++;
                }
                return 0;
            });
            return changed;
        }

        private static T WithAssetContents<T>(
            string assetPath,
            bool save,
            Func<GameObject[], T> action)
        {
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var activeBefore = SceneManager.GetActiveScene();
                var existing = SceneManager.GetSceneByPath(assetPath);
                var openedHere = !existing.IsValid() || !existing.isLoaded;
                var scene = openedHere
                    ? EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive)
                    : existing;
                try
                {
                    if (save && !openedHere && scene.isDirty)
                        throw new InvalidOperationException(
                            "Refusing to save an already-loaded dirty scene: " + assetPath);
                    var result = action(scene.GetRootGameObjects());
                    if (save && !EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save scene: " + assetPath);
                    return result;
                }
                finally
                {
                    if (openedHere) EditorSceneManager.CloseScene(scene, true);
                    if (activeBefore.IsValid() && activeBefore.isLoaded &&
                        SceneManager.GetActiveScene() != activeBefore)
                        SceneManager.SetActiveScene(activeBefore);
                }
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                var result = action(new[] { root });
                if (save && PrefabUtility.SaveAsPrefabAsset(root, assetPath) == null)
                    throw new InvalidOperationException("Could not save prefab: " + assetPath);
                return result;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static IEnumerable<TMP_Text> EnumerateScopedText(
            string assetPath,
            IEnumerable<GameObject> roots)
        {
            GameObject[] scope;
            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var spaces = roots.SingleOrDefault(root => root.name == "Spaces");
                if (spaces == null)
                    throw new InvalidOperationException(assetPath + " has no unique Spaces root.");
                scope = new[] { spaces };
            }
            else
            {
                scope = roots.ToArray();
            }

            return scope.SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                .Where(text => text != null)
                .OrderBy(text => GetPath(text.gameObject), StringComparer.Ordinal)
                .ThenBy(text => Array.IndexOf(text.GetComponents<TMP_Text>(), text))
                .ToArray();
        }

        private static string[] ValidateAllowlist(IEnumerable<string> paths)
        {
            var result = (paths ?? Enumerable.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/').Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (result.Length == 0) throw new InvalidOperationException("Font swap allowlist is empty.");
            foreach (var path in result)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    !(path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                      path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Allowlist path must be a scene or prefab: " + path);
                if (!File.Exists(ToFullPath(path)))
                    throw new FileNotFoundException("Allowlisted asset is missing.", path);
            }
            return result;
        }

        private static string[] ReadAllowlistFromCommandLine()
        {
            var value = ReadArgument("-fontAllowlist");
            return string.IsNullOrWhiteSpace(value)
                ? new[] { DefaultScenePath }
                : value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < args.Length; index++)
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                    return args[index + 1];
            return null;
        }

        private static string GuidFor(UnityEngine.Object value)
        {
            var path = AssetDatabase.GetAssetPath(value);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static string GlobalIdFor(UnityEngine.Object value)
        {
            return value == null || !EditorUtility.IsPersistent(value)
                ? string.Empty
                : GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
        }

        private static string GetPath(GameObject gameObject)
        {
            var names = new Stack<string>();
            for (var current = gameObject.transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names.ToArray());
        }

        private static void WriteJson(string path, SwapManifest manifest)
        {
            var fullPath = ToFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(
                fullPath,
                JsonUtility.ToJson(manifest, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void WriteJsonObject(string path, object value)
        {
            var fullPath = ToFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(
                fullPath,
                JsonUtility.ToJson(value, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static string ToFullPath(string projectRelativePath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(root, projectRelativePath));
        }
    }
}
