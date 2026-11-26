using System.Collections.Generic;
using System.Linq;
using EatWhat.Cooking.ShortCycle.Debugging;
using EatWhat.Tools.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.Rendering
{
    /// <summary>Read-only audit; measurements are independent of Builder targets.</summary>
    public static class OutlineShadowLabAudit
    {
        [MenuItem("Tools/Visual Effects Lab/Audit Outline Shadow Merge (Read Only)")]
        public static void AuditFromMenu()
        {
            var result = Audit(SceneManager.GetActiveScene());
            if (result.Success) Debug.Log(result.Summary);
            else Debug.LogError(result.Summary);
        }

        public static OutlineShadowLabAuditResult Audit(Scene scene)
        {
            var roots = scene.IsValid() ? scene.GetRootGameObjects() : new GameObject[0];
            var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(item => item.gameObject).ToArray();
            var drivers = all.SelectMany(item => item.GetComponents<OutlineShadowLabDriver>()).ToArray();
            var groups = all.SelectMany(item => item.GetComponents<SpriteOutlineMergeRenderer2D>()).ToArray();
            var members = all.SelectMany(item => item.GetComponents<SpriteOutlineMergeMember2D>()).ToArray();
            var missingScripts = all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            var wrongOwners = members.Where(member => member.gameObject.activeInHierarchy &&
                    (member.NearestOwner == null || !member.NearestOwner.Members.Contains(member)))
                .Select(member => GetPath(member.gameObject)).ToArray();
            var missingHosts = groups.Where(group => group.ShadowHost == null || group.OutlineHost == null).Select(group => group.name).ToArray();
            var hostOrderViolations = groups.Where(HasHostOrderViolation).Select(group => group.name).ToArray();
            var budgetViolations = groups.Where(group => group.OwnedRenderTextureCount > 2 || group.PeakLiveRenderTextureCount > 2)
                .Select(group => group.name).ToArray();
            var forceOffShadow = members.Count(member => member.MergeShadow == OutlineMergeOverride.ForceOff);
            var localOutline = members.Count(member => member.MergeOutline == OutlineMergeOverride.ForceOn);
            var reverseEvidence = all.Count(item => item.name.StartsWith("Reverse_"));
            var counterexamples = all.Count(item => item.name.Contains("Counterexample"));
            var rootPresent = all.Any(item => GetPath(item) == "Spaces/OutlineShadowMergeLab");
            var formalRoot = all.FirstOrDefault(item => GetPath(item) ==
                "Spaces/OutlineShadowMergeLab/FormalComponentMatrix_Group");
            var formalGroups = formalRoot == null ? new SpriteOutlineGroup2D[0] :
                formalRoot.GetComponentsInChildren<SpriteOutlineGroup2D>(true);
            var formalMembers = formalRoot == null ? new SpriteOutline2D[0] :
                formalRoot.GetComponentsInChildren<SpriteOutline2D>(true);
            var isolatedFixtures = formalRoot == null ? null : formalRoot.transform
                .Cast<Transform>().FirstOrDefault(item => item.name == "IsolatedFormalCatFixtures")?.gameObject;
            var formalAdaptersValid = formalMembers.All(member => member.MergeAdapter != null &&
                member.NearestGroup != null && member.NearestGroup.MergeBackend != null &&
                member.NearestGroup.MergeBackend.Members.Contains(member.MergeAdapter));
            var localFollowCross = formalMembers.Any(member => !member.OverrideGroup &&
                member.MergeOutline == OutlineMergeOverride.FollowGroup &&
                member.NearestGroup != null && member.NearestGroup.MergeOutlineEnabled);
            var groupForceOnCross = formalMembers.Any(member => member.OverrideGroup &&
                member.MergeOutline == OutlineMergeOverride.ForceOn &&
                member.NearestGroup != null && !member.NearestGroup.MergeOutlineEnabled);
            var formalForceOff = formalMembers.Any(member => member.MergeShadow == OutlineMergeOverride.ForceOff);
            var prefabSources = all.Count(item => PrefabUtility.GetCorrespondingObjectFromSource(item) != null &&
                item.name.StartsWith("Formal_FridgeCat"));
            var unrelatedEnabledDrivers = isolatedFixtures == null ? -1 : isolatedFixtures
                .GetComponentsInChildren<MonoBehaviour>(true)
                .Count(item => item != null && item.enabled && !(item is SpriteOutline2D) &&
                    !(item is SpriteOutlineGroup2D) && !(item is SpriteOutlineMergeMember2D) &&
                    !(item is SpriteOutlineMergeRenderer2D));
            var formalMarkers = formalRoot == null ? 0 : formalRoot.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name.StartsWith("CameraMarker_"));
            var nestedFormalGroup = formalGroups.FirstOrDefault(item => item.name == "ParametersGroup_ForceOn_Nested");
            var nestedOwnershipValid = nestedFormalGroup != null && nestedFormalGroup.GetOwnedMembers()
                .All(item => item.NearestGroup == nestedFormalGroup);
            var driverFormalValid = drivers.Length == 1 && drivers[0].FormalLocalFollowGroup != null &&
                drivers[0].FormalGroupForceOnGroup != null && drivers[0].IsolatedFormalCatFixtures == isolatedFixtures;
            GameObject forwardGroupObject = all.FirstOrDefault(item => GetPath(item) ==
                OutlineShadowLabBuilder.ForwardGroupPath);
            GameObject forward0Object = all.FirstOrDefault(item => GetPath(item) ==
                OutlineShadowLabBuilder.ForwardGroupPath + "/Forward_0");
            GameObject forward1Object = all.FirstOrDefault(item => GetPath(item) ==
                OutlineShadowLabBuilder.ForwardGroupPath + "/Forward_1");
            GameObject forward2Object = all.FirstOrDefault(item => GetPath(item) ==
                OutlineShadowLabBuilder.Forward2Path);
            SpriteOutlineGroup2D forwardFormalGroup = forwardGroupObject == null ? null :
                forwardGroupObject.GetComponent<SpriteOutlineGroup2D>();
            SpriteOutlineMergeRenderer2D forwardBackend = forwardGroupObject == null ? null :
                forwardGroupObject.GetComponent<SpriteOutlineMergeRenderer2D>();
            SpriteOutline2D forward0 = forward0Object == null ? null : forward0Object.GetComponent<SpriteOutline2D>();
            SpriteOutline2D forward1 = forward1Object == null ? null : forward1Object.GetComponent<SpriteOutline2D>();
            SpriteOutline2D forward2 = forward2Object == null ? null : forward2Object.GetComponent<SpriteOutline2D>();
            SpriteRenderer forward2Renderer = forward2Object == null ? null :
                forward2Object.GetComponent<SpriteRenderer>();
            bool forwardFollowersValid = IsFormalForwardFollower(forward0, forwardBackend) &&
                IsFormalForwardFollower(forward1, forwardBackend);
            bool forward2IndependentValid = forward2 != null && !forward2.OverrideGroup &&
                forward2.MergeOutline == OutlineMergeOverride.ForceOff &&
                forward2.MergeShadow == OutlineMergeOverride.ForceOff &&
                forward2.OutlineMaterial != null && forward2.Thickness > 0f && forward2.OutlineColor.a > 0f &&
                forward2.MergeAdapter != null && forwardBackend != null &&
                forwardBackend.Members.Contains(forward2.MergeAdapter);
            bool forwardGeometryPreserved = forwardGroupObject != null && forward0Object != null &&
                forward1Object != null && forward2Object != null &&
                forwardGroupObject.transform.localPosition == new Vector3(-5f, 0f, 0f) &&
                forward0Object.transform.localPosition == new Vector3(-0.75f, 0f, 0f) &&
                forward1Object.transform.localPosition == new Vector3(0f, 0.35f, 0f) &&
                forward2Object.transform.localPosition == new Vector3(0.75f, 0f, 0f) &&
                forward2Renderer != null && forward2Renderer.sortingOrder == 22;
            bool forwardGroupAuthorityValid = forwardFormalGroup != null && forwardBackend != null &&
                forwardFormalGroup.MergeBackend == forwardBackend && forwardBackend.Members.Count == 3 &&
                GroupMatchesBackend(forwardFormalGroup, forwardBackend);
            bool paddedFixtureValid = OutlineShadowLabBuilder.TryValidatePaddedFixture(out var paddedFixtureError) &&
                forward2Renderer != null &&
                AssetDatabase.GetAssetPath(forward2Renderer.sprite) == OutlineShadowLabBuilder.PaddedFixturePath;
            GameObject forward2MarkerObject = all.FirstOrDefault(item => GetPath(item) ==
                OutlineShadowLabBuilder.Forward2MarkerPath);
            Transform forward2Marker = forward2MarkerObject == null ? null : forward2MarkerObject.transform;
            Camera forward2Camera = drivers.Length == 1 ? drivers[0].Forward2ReviewCamera : null;
            bool driverForward2Valid = drivers.Length == 1 && forward2Camera != null &&
                drivers[0].Forward2CameraMarker == forward2Marker &&
                Mathf.Approximately(drivers[0].Forward2OrthographicSize,
                    OutlineShadowLabBuilder.Forward2ReviewOrthographicSize);
            bool forward2FrameValid = IsVisibleFromMarker(forward2Camera, forward2Marker,
                drivers.Length == 1 ? drivers[0].Forward2OrthographicSize : 0f, forward2Renderer);
            var success = scene.path == OutlineShadowLabBuilder.ScenePath && rootPresent && drivers.Length == 1 &&
                groups.Length >= 3 && members.Length >= 9 && missingScripts == 0 && wrongOwners.Length == 0 &&
                missingHosts.Length == 0 && hostOrderViolations.Length == 0 && budgetViolations.Length == 0 &&
                forceOffShadow > 0 && localOutline > 0 && reverseEvidence > 0 && counterexamples > 0 &&
                formalRoot != null && formalGroups.Length >= 2 && formalMembers.Length >= 4 &&
                formalAdaptersValid && localFollowCross && groupForceOnCross && formalForceOff &&
                isolatedFixtures != null && !isolatedFixtures.activeSelf && prefabSources >= 3 &&
                unrelatedEnabledDrivers == 0 && formalMarkers >= 4 && nestedOwnershipValid &&
                driverFormalValid && forwardFollowersValid && forward2IndependentValid &&
                forwardGeometryPreserved && forwardGroupAuthorityValid && paddedFixtureValid &&
                driverForward2Valid && forward2FrameValid && !scene.isDirty;
            var summary = "OUTLINE-LAB-AUDIT success=" + success + " scene=" + scene.path +
                " groups=" + groups.Length + " members=" + members.Length + " drivers=" + drivers.Length +
                " missingScripts=" + missingScripts + " wrongOwners=" + string.Join("|", wrongOwners) +
                " missingHosts=" + string.Join("|", missingHosts) + " hostOrder=" + string.Join("|", hostOrderViolations) +
                " budget=" + string.Join("|", budgetViolations) + " forceOffShadow=" + forceOffShadow +
                " localOutline=" + localOutline + " reverseEvidence=" + reverseEvidence +
                " counterexamples=" + counterexamples + " formalGroups=" + formalGroups.Length +
                " formalMembers=" + formalMembers.Length + " formalAdapters=" + formalAdaptersValid +
                " localFollowCross=" + localFollowCross + " groupForceOnCross=" + groupForceOnCross +
                " formalForceOff=" + formalForceOff + " catPrefabSources=" + prefabSources +
                " isolatedFixtures=" + (isolatedFixtures != null && !isolatedFixtures.activeSelf) +
                " unrelatedEnabledDrivers=" + unrelatedEnabledDrivers + " markers=" + formalMarkers +
                " nestedOwnership=" + nestedOwnershipValid + " driverFormal=" + driverFormalValid +
                " forwardFollowers=" + forwardFollowersValid +
                " forward2Independent=" + forward2IndependentValid +
                " forwardGeometryPreserved=" + forwardGeometryPreserved +
                " forwardAuthority=" + forwardGroupAuthorityValid +
                " paddedFixture=" + paddedFixtureValid +
                " paddedFixtureError=" + paddedFixtureError +
                " driverForward2=" + driverForward2Valid +
                " forward2Frame=" + forward2FrameValid +
                " sceneDirty=" + scene.isDirty;
            return new OutlineShadowLabAuditResult(success, summary);
        }

        private static string GetPath(GameObject value)
        {
            var names = new List<string>();
            for (var current = value.transform; current != null; current = current.parent) names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static bool HasHostOrderViolation(SpriteOutlineMergeRenderer2D group)
        {
            if (group.Members.Count == 0) return true;
            var sourceOrders = group.Members.Where(item => item != null && item.SourceRenderer != null)
                .Select(item => item.SourceRenderer.sortingOrder).ToArray();
            return sourceOrders.Length == 0 || group.ShadowHost == null || group.OutlineHost == null ||
                group.ShadowHost.sortingOrder != sourceOrders.Min() - 2 ||
                group.OutlineHost.sortingOrder != sourceOrders.Min() - 1;
        }

        private static bool IsFormalForwardFollower(SpriteOutline2D outline,
            SpriteOutlineMergeRenderer2D backend)
        {
            return outline != null && outline.OverrideGroup &&
                outline.MergeOutline == OutlineMergeOverride.FollowGroup &&
                outline.MergeShadow == OutlineMergeOverride.FollowGroup &&
                outline.MergeAdapter != null && backend != null && backend.Members.Contains(outline.MergeAdapter);
        }

        private static bool GroupMatchesBackend(SpriteOutlineGroup2D group,
            SpriteOutlineMergeRenderer2D backend)
        {
            var serialized = new SerializedObject(backend);
            serialized.Update();
            SerializedProperty outlineEnabled = serialized.FindProperty("groupOutlineEnabled");
            SerializedProperty outlineColor = serialized.FindProperty("groupOutlineColor");
            SerializedProperty outlineThickness = serialized.FindProperty("groupOutlineThicknessInSourcePixels");
            SerializedProperty shadowEnabled = serialized.FindProperty("groupShadowEnabled");
            SerializedProperty shadowEffectEnabled = serialized.FindProperty("groupShadowEffectEnabled");
            SerializedProperty shadowColor = serialized.FindProperty("groupShadowColor");
            SerializedProperty shadowOffset = serialized.FindProperty("groupShadowOffsetInSourcePixels");
            SerializedProperty shadowOpacity = serialized.FindProperty("groupShadowOpacity");
            SerializedProperty shadowSoftness = serialized.FindProperty("groupShadowSoftnessInSourcePixels");
            return outlineEnabled != null && outlineColor != null && outlineThickness != null &&
                shadowEnabled != null && shadowEffectEnabled != null && shadowColor != null &&
                shadowOffset != null && shadowOpacity != null && shadowSoftness != null &&
                group.MergeOutlineEnabled == outlineEnabled.boolValue &&
                group.OutlineColor == outlineColor.colorValue &&
                Mathf.Approximately(group.Thickness, outlineThickness.floatValue) &&
                group.MergeShadowEnabled == shadowEnabled.boolValue &&
                group.ShadowEnabled == shadowEffectEnabled.boolValue &&
                group.ShadowColor == shadowColor.colorValue &&
                group.ShadowOffset == shadowOffset.vector2Value &&
                Mathf.Approximately(group.ShadowOpacity, shadowOpacity.floatValue) &&
                Mathf.Approximately(group.ShadowBlur, shadowSoftness.floatValue);
        }

        private static bool IsVisibleFromMarker(Camera camera, Transform marker, float orthographicSize,
            SpriteRenderer target)
        {
            if (camera == null || marker == null || target == null || !camera.orthographic ||
                orthographicSize <= 0f || camera.aspect <= 0f)
                return false;
            Vector3 localCenter = Quaternion.Inverse(marker.rotation) * (target.bounds.center - marker.position);
            Vector3 extents = target.bounds.extents;
            return Mathf.Abs(localCenter.x) + extents.x <= orthographicSize * camera.aspect &&
                Mathf.Abs(localCenter.y) + extents.y <= orthographicSize;
        }
    }

    public readonly struct OutlineShadowLabAuditResult
    {
        public readonly bool Success;
        public readonly string Summary;
        public OutlineShadowLabAuditResult(bool success, string summary)
        {
            Success = success;
            Summary = summary;
        }
    }
}
