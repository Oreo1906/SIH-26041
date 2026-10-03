using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurakshaXR.Editor
{
    // Phase 0 configuration only; no training, permissions or network startup.
    public static class ProjectSetup
    {
        private const string EditorVersion = "6000.3.24f1";
        // A single phone APK supports both 32-bit Android userspace and 64-bit phones.
        // The pinned Unity player and ARCore plug-in ship native libraries for both.
        public const AndroidArchitecture PhoneArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        [MenuItem("SurakshaXR/Configure Bootstrap Project")]
        public static void Configure()
        {
            if (Application.unityVersion != EditorVersion)
                throw new BuildFailedException("Open this project with Unity " + EditorVersion);

            PlayerSettings.companyName = "SurakshaXR";
            PlayerSettings.productName = "SurakshaXR";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "in.surakshaxr.app");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = PhoneArchitectures;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            const string scenePath = "Assets/SurakshaXR/Scenes/Bootstrap.unity";
            if (!File.Exists(scenePath))
            {
                Directory.CreateDirectory("Assets/SurakshaXR/Scenes");
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            // Do not replace build scenes once later phases have configured them.
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("SurakshaXR Phase 0 configuration saved. Training scenes are not implemented.");
        }
    }
}
