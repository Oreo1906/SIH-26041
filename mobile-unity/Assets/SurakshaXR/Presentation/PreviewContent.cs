using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using SurakshaXR.Domain;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace SurakshaXR.Presentation
{
    public sealed class PreviewContent
    {
        public string Locale { get; private set; }
        private StringTable table;
        public readonly ModuleDefinition[] Modules;
        public PreviewContent()
        { VerifyBundledContent(); Modules = Read<ModuleDefinition[]>("modules"); SelectLocale("en"); }
        private static void VerifyBundledContent()
        {
            var manifest = Read<JObject>("content_manifest");
            foreach (var pair in (JObject)manifest["files"])
            {
                var name = pair.Key;
                if (name.Contains("..") || !name.EndsWith(".json", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid content path.");
                var asset = Resources.Load<TextAsset>("Content/" + name.Substring(0, name.Length - 5));
                if (asset == null) throw new InvalidOperationException("Bundled content missing.");
                using (var sha = SHA256.Create())
                {
                    var hash = BitConverter.ToString(sha.ComputeHash(asset.bytes)).Replace("-", "").ToLowerInvariant();
                    if (hash != (string)pair.Value) throw new InvalidOperationException("Bundled content integrity check failed.");
                }
            }
        }
        public void SelectLocale(string locale)
        {
            if (locale != "en" && locale != "hi" && locale != "sat") throw new ArgumentException("Unknown locale");
            table = Resources.Load<StringTable>("Localization/" + locale);
            if (table == null) throw new InvalidOperationException("Bundled string table unavailable.");
            Locale = locale;
        }
        public string Text(string key) => table.GetEntry(key)?.Value ?? throw new InvalidOperationException("Missing localized key: " + key);
        public static T Read<T>(string name)
        {
            var asset = Resources.Load<TextAsset>("Content/" + name);
            if (asset == null) throw new InvalidOperationException("Bundled content unavailable.");
            return ContentParser.Read<T>(asset.text);
        }
        public ScenarioDefinition Scenario(string moduleId) => Read<ScenarioDefinition>("scenarios/" + FileKey(moduleId) + "_v1");
        public ScenarioPolicy Policy(string moduleId) => Read<ScenarioPolicy>("policies/" + FileKey(moduleId));
        public QuestionBank Questions(string moduleId) => Read<QuestionBank>("questions/" + FileKey(moduleId) + "_v1");
        public SurakshaXR.AppServices.MicroTrainingPolicy MicroPolicy(string moduleId) => Read<SurakshaXR.AppServices.MicroTrainingPolicy>("micro/" + FileKey(moduleId));
        private static string FileKey(string id) => id == "fire-response" ? "fire" : id == "gas-confined-space" ? "gas" : throw new ArgumentException("Unknown module.");
    }
}
