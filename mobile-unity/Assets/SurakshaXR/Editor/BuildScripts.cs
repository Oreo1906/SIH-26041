using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace SurakshaXR.Editor
{
    public static class BuildScripts
    {
        public static void PreparePreview()
        {
            ProjectSetup.Configure();
            OptionalArConfiguration.Configure();
            // Unity 6000.3 exposes this project switch only in its internal editor
            // settings class. Pinning the editor makes the reflected bridge explicit.
            var textProjectSettings = Type.GetType("UnityEditor.UIElements.UIToolkitProjectSettings, UnityEditor.UIElementsModule");
            var advancedText = textProjectSettings?.GetProperty("enableAdvancedText", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (advancedText == null) throw new BuildFailedException("Pinned editor's advanced text setting is unavailable.");
            advancedText.SetValue(null, true);
            const string resources = "Assets/SurakshaXR/Resources";
            Directory.CreateDirectory(resources + "/Localization");
            var sourceRoot = Path.GetFullPath("Assets/StreamingAssets/Content");
            var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*.json", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(sourceFile) == "content_manifest.json") continue;
                using (var sha = SHA256.Create())
                    hashes[sourceFile.Substring(sourceRoot.Length + 1).Replace('\\', '/')] =
                        BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(sourceFile))).Replace("-", "").ToLowerInvariant();
            }
            File.WriteAllText(Path.Combine(sourceRoot, "content_manifest.json"), JsonConvert.SerializeObject(new {
                contentVersion = "preview-0.1.0", validationStatus = "demo-unvalidated", reviewer = (string)null,
                builtAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"), files = hashes
            }, Formatting.Indented));
            foreach (var source in Directory.GetFiles("Assets/StreamingAssets/Content", "*.json", SearchOption.AllDirectories))
            {
                var relative = source.Substring("Assets/StreamingAssets/Content/".Length);
                var destination = resources + "/Content/" + relative;
                Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(source, destination, true);
            }
            AssetDatabase.Refresh();
            var sharedPath = resources + "/Localization/Shared.asset";
            var shared = AssetDatabase.LoadAssetAtPath<SharedTableData>(sharedPath);
            if (shared == null)
            {
                shared = ScriptableObject.CreateInstance<SharedTableData>(); shared.TableCollectionName = "Preview";
                AssetDatabase.CreateAsset(shared, sharedPath);
            }
            foreach (var locale in new[] { "en", "hi", "sat" })
            {
                var path = resources + "/Localization/" + locale + ".asset";
                var table = AssetDatabase.LoadAssetAtPath<StringTable>(path);
                if (table == null) { table = ScriptableObject.CreateInstance<StringTable>(); AssetDatabase.CreateAsset(table, path); }
                table.SharedData = shared; table.LocaleIdentifier = new LocaleIdentifier(locale);
                var strings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("../demo/localization/" + locale + ".json"));
                table.RemoveEntry("ui.offline_badge");
                foreach (var pair in strings.OrderBy(x => x.Key, StringComparer.Ordinal)) table.AddEntry(pair.Key, pair.Value);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(shared);
            var fontCharacters = string.Concat(new[] { "en", "hi", "sat" }.Select(locale =>
                string.Concat(JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("../demo/localization/" + locale + ".json")).Values)));
            fontCharacters += "English हिन्दी ᱥᱟᱱᱛᱟᱲᱤ 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz <>^v/:.-_";
            var fonts = new List<FontAsset>();
            foreach (var name in new[] { "NotoSans", "NotoSansDevanagari", "NotoSansOlChiki" })
            {
                var fontPath = resources + "/Fonts/" + name + " SDF.asset";
                var font = AssetDatabase.LoadAssetAtPath<FontAsset>(fontPath);
                if (font == null)
                {
                    font = FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(resources + "/Fonts/" + name + "-Regular.ttf"));
                    font.name = name + " SDF";
                    AssetDatabase.CreateAsset(font, fontPath);
                    AssetDatabase.AddObjectToAsset(font.material, font);
                    foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
                }
                font.TryAddCharacters(fontCharacters, out string missing);
                var serializedFont = new SerializedObject(font);
                serializedFont.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
                serializedFont.ApplyModifiedPropertiesWithoutUndo();
                foreach (var texture in font.atlasTextures)
                {
                    if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
                    EditorUtility.SetDirty(texture);
                }
                EditorUtility.SetDirty(font); fonts.Add(font);
            }
            var textSettingsPath = resources + "/PreviewTextSettings.asset";
            var textSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(textSettingsPath);
            if (textSettings == null) { textSettings = ScriptableObject.CreateInstance<PanelTextSettings>(); AssetDatabase.CreateAsset(textSettings, textSettingsPath); }
            textSettings.defaultFontAsset = fonts[0]; textSettings.fallbackFontAssets = fonts;
            var serializedTextSettings = new SerializedObject(textSettings);
            serializedTextSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serializedTextSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(textSettings);
            var materialPath = resources + "/PreviewMaterial.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), materialPath);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        public static void BuildAndroid()
        { Build(ProjectSetup.PhoneArchitectures, "SurakshaXR-preview.apk", false); }
        public static void BuildEmulator()
        { Build(AndroidArchitecture.X86_64, "emulator-only/SurakshaXR-emulator-x86_64.apk", true); }
        private static void Build(AndroidArchitecture architecture, string fileName, bool development)
        {
            PreparePreview();
            PlayerSettings.Android.targetArchitectures = architecture;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            EditorUserBuildSettings.buildAppBundle = false;
            var output = Path.GetFullPath("../artifacts/android/" + fileName); Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
                locationPathName = output, target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.CompressWithLz4HC
            });
            File.WriteAllText("../artifacts/unity/" + Path.GetFileName(fileName) + "-summary.txt", report.summary.result + "\nBytes: " + report.summary.totalSize + "\nErrors: " + report.summary.totalErrors + "\nDuration: " + report.summary.totalTime);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Android build failed. Inspect build report.");
        }
    }
}
