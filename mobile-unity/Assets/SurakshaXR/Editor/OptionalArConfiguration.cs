using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SurakshaXR.Editor
{
    public static class OptionalArConfiguration
    {
        public static void Configure()
        {
            const string folder = "Assets/SurakshaXR/XR";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/SurakshaXR", "XR");
            var core = AssetDatabase.LoadAssetAtPath<ARCoreSettings>(folder + "/ARCore.asset");
            if (core == null) { core = ScriptableObject.CreateInstance<ARCoreSettings>(); AssetDatabase.CreateAsset(core, folder + "/ARCore.asset"); }
            core.requirement = ARCoreSettings.Requirement.Optional; core.depth = ARCoreSettings.Requirement.Optional; ARCoreSettings.currentSettings = core;
            var targets = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(folder + "/XRSettings.asset");
            if (targets == null) { targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>(); AssetDatabase.CreateAsset(targets, folder + "/XRSettings.asset"); }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targets, true);
            if (!targets.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) targets.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var settings = targets.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart = false; settings.Manager.automaticLoading = false; settings.Manager.automaticRunning = false;
            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android))
                throw new BuildFailedException("Could not configure the pinned ARCore loader.");
            EditorUtility.SetDirty(targets); EditorUtility.SetDirty(settings); EditorUtility.SetDirty(settings.Manager); EditorUtility.SetDirty(core);
            AssetDatabase.SaveAssets();
            if (core.requirement != ARCoreSettings.Requirement.Optional || core.depth != ARCoreSettings.Requirement.Optional || settings.InitManagerOnStart)
                throw new BuildFailedException("AR must be optional and must not start at launch.");
        }
    }
}
