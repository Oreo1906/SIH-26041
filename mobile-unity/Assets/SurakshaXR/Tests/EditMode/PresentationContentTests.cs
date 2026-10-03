using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using SurakshaXR.Domain;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace SurakshaXR.Tests
{
    public sealed class PresentationContentTests
    {
        [Test] public void ShippedTablesContainEveryScenarioInteractionAndFeedback()
        {
            var content = new PreviewContent();
            foreach (var locale in new[] { "en", "hi", "sat" })
            {
                content.SelectLocale(locale);
                foreach (var module in content.Modules)
                {
                    var scenario = content.Scenario(module.moduleId);
                    ContentParser.Validate(scenario, content.Policy(module.moduleId));
                    Assert.That(content.Text(module.titleKey), Is.Not.Empty);
                    foreach (var step in scenario.steps)
                    {
                        Assert.That(content.Text(step.objectiveKey), Is.Not.Empty);
                        Assert.That(content.Text(step.narrationKey), Is.Not.Empty);
                        foreach (var action in step.allowedActions)
                        {
                            Assert.That(content.Text("action." + action.actionId), Is.Not.Empty);
                            Assert.That(content.Text(action.feedbackKey), Is.Not.Empty);
                        }
                    }
                }
            }
        }
        [Test] public void AllLocalesShareStableKeysAndFontAssetsAreBundled()
        {
            var reference = Resources.Load<StringTable>("Localization/en");
            foreach (var locale in new[] { "hi", "sat" })
                CollectionAssert.AreEquivalent(reference.Values.Select(x => x.Key), Resources.Load<StringTable>("Localization/" + locale).Values.Select(x => x.Key));
            foreach (var font in new[] { "NotoSans", "NotoSansDevanagari", "NotoSansOlChiki" })
                Assert.That(Resources.Load<Font>("Fonts/" + font + "-Regular"), Is.Not.Null);
            Assert.That(Resources.Load<Material>("PreviewMaterial"), Is.Not.Null);
        }
    }
}
