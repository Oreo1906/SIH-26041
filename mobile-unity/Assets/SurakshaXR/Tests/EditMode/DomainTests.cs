using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SurakshaXR.Domain;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class DomainTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path), Encoding.UTF8);
        private static ScenarioDefinition Scenario(string name) => ContentParser.Read<ScenarioDefinition>(Read("examples/demo_content/scenarios/" + name + "_v1.example.json"));
        private static ScenarioPolicy Policy(string name) => ContentParser.Read<ScenarioPolicy>(Read("demo/runtime-policies/" + name + ".json"));
        private static readonly DateTimeOffset Time = new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

        [TestCase("fire")][TestCase("gas")]
        public void HappyPathIsDeterministicAndIndependentOfStepOrder(string name)
        {
            var scenario = Scenario(name);
            var first = Run(scenario, Policy(name));
            Array.Reverse(scenario.steps);
            var second = Run(scenario, Policy(name));
            Assert.That(first.State, Is.EqualTo(SessionState.Completed));
            Assert.That(first.Score, Is.EqualTo(100m));
            Assert.That(second.Actions.Select(x => x.StepId + "/" + x.ActionId), Is.EqualTo(first.Actions.Select(x => x.StepId + "/" + x.ActionId)));
            Assert.That(second.CategoryScores, Is.EquivalentTo(first.CategoryScores));
        }
        private static ScenarioRuntime Run(ScenarioDefinition scenario, ScenarioPolicy policy)
        {
            var runtime = new ScenarioRuntime(scenario, policy, TrainingMode.Assessment);
            runtime.Start();
            var n = 0;
            while (runtime.State == SessionState.Running && n < 20)
            {
                runtime.SubmitAction(runtime.CurrentStep.allowedActions.First(x => x.result == "correct").actionId, Time.AddSeconds(n), n * 1000);
                n++;
            }
            return runtime;
        }
        [Test] public void IncorrectRetryDoesNotAdvanceAndKeepsPenalty()
        {
            var runtime = new ScenarioRuntime(Scenario("fire"), Policy("fire"), TrainingMode.Assessment);
            runtime.Start();
            runtime.SubmitAction("ignore_hazard", Time, 0);
            Assert.That(runtime.CurrentStepId, Is.EqualTo("fire_01_recognize"));
            runtime.SubmitAction("identify_hazard", Time.AddSeconds(1), 1000);
            Assert.That(runtime.Score, Is.EqualTo(10m));
            Assert.That(runtime.WeakTopics, Is.EqualTo(new[] { "fire.hazard_recognition" }));
            Assert.Throws<ArgumentException>(() => runtime.SubmitAction("identify_hazard", Time.AddSeconds(2), 2000));
        }
        [Test] public void PauseAndInvalidActionsCannotChangeScore()
        {
            var runtime = new ScenarioRuntime(Scenario("gas"), Policy("gas"), TrainingMode.Assessment);
            Assert.Throws<InvalidOperationException>(() => runtime.SubmitAction("inspect_detector_alarm", Time, 0));
            runtime.Start(); runtime.Pause();
            Assert.Throws<InvalidOperationException>(() => runtime.SubmitAction("inspect_detector_alarm", Time, 0));
            runtime.Resume();
            Assert.Throws<ArgumentException>(() => runtime.SubmitAction("unknown", Time, 0));
            Assert.That(runtime.Actions.Count, Is.Zero);
            runtime.SubmitAction("inspect_detector_alarm", Time, 100);
            Assert.Throws<ArgumentException>(() => runtime.SubmitAction("recognize_restricted_zone", Time.AddSeconds(1), 99));
        }
        [Test] public void ConfiguredCriticalFailureIsTerminalInAssessment()
        {
            var runtime = new ScenarioRuntime(Scenario("gas"), Policy("gas"), TrainingMode.Assessment);
            runtime.Start(); runtime.SubmitAction("inspect_detector_alarm", Time, 0);
            runtime.SubmitAction("enter_restricted_zone", Time.AddSeconds(1), 1000);
            Assert.That(runtime.State, Is.EqualTo(SessionState.Failed));
            Assert.That(runtime.CriticalFail, Is.True);
            Assert.Throws<InvalidOperationException>(() => runtime.SubmitAction("recognize_restricted_zone", Time.AddSeconds(2), 2000));
        }
        [Test] public void PracticeCanRetryButNeverIssueCertificate()
        {
            var runtime = new ScenarioRuntime(Scenario("gas"), Policy("gas"), TrainingMode.Practice);
            runtime.Start(); runtime.SubmitAction("inspect_detector_alarm", Time, 0);
            runtime.SubmitAction("enter_restricted_zone", Time.AddSeconds(1), 1000);
            Assert.That(runtime.State, Is.EqualTo(SessionState.Running));
            Assert.That(new AssessmentResult(100, 100, Pass(), false, TrainingMode.Practice).CertificateEligible, Is.False);
        }
        [Test] public void MutatingRendererCopyDoesNotMutateRuntime()
        {
            var scenario = Scenario("fire");
            var runtime = new ScenarioRuntime(scenario, Policy("fire"), TrainingMode.Assessment);
            scenario.steps[0].allowedActions[0].scoreDelta = 900;
            runtime.CurrentStep.allowedActions[0].scoreDelta = 800;
            runtime.Start(); runtime.SubmitAction("identify_hazard", Time, 0);
            Assert.That(runtime.Score, Is.EqualTo(20m));
        }
        [Test] public void MissingMappingAndCyclesAreRejected()
        {
            var policy = Policy("fire"); policy.actionCategories.Clear();
            Assert.Throws<ArgumentException>(() => ContentParser.Validate(Scenario("fire"), policy));
            var scenario = Scenario("fire"); scenario.steps.Last().allowedActions[0].nextStepId = "fire_01_recognize";
            Assert.Throws<ArgumentException>(() => ContentParser.Validate(scenario, Policy("fire")));
        }
        private static PassPolicy Pass() => new PassPolicy { passThreshold = 70, scenarioWeight = .8m, quizWeight = .2m, criticalFailForcesFail = true };
        [Test] public void FinalScoreUsesConfiguredWeightsAndCriticalFailure()
        {
            var result = new AssessmentResult(75, 80, Pass(), false, TrainingMode.Assessment);
            Assert.That(result.FinalScore, Is.EqualTo(76m));
            Assert.That(result.Passed, Is.True);
            Assert.That(new AssessmentResult(100, 100, Pass(), true, TrainingMode.Assessment).Passed, Is.False);
            Assert.That(new AssessmentResult(69.5m, 69.5m, Pass(), false, TrainingMode.Assessment).Passed, Is.False);
            Assert.That(new AssessmentResult(69.5m, 69.5m, Pass(), false, TrainingMode.Assessment).CertificateScore, Is.EqualTo(70));
        }
        [Test] public void QuestionSelectionIsStableAcrossReorderingAndAnswersUseSets()
        {
            var bank = new QuestionBank { questionBankId = "test-bank", moduleId = "fire-response", version = "test-1",
                questions = Enumerable.Range(1, 8).Select(n => new QuestionDefinition { id = "test-question-" + n, promptKey = "test.prompt", explanationKey = "test.explanation", topicTags = new[] { "test.topic" }, correctOptionIds = new[] { "a", "b" }, options = new[] { new OptionDefinition { id = "a", textKey = "a" }, new OptionDefinition { id = "b", textKey = "b" }, new OptionDefinition { id = "c", textKey = "c" } } }).ToArray() };
            var first = QuestionEngine.Select(bank, "fixed-attempt-seed", 5);
            Array.Reverse(bank.questions);
            Assert.That(QuestionEngine.Select(bank, "fixed-attempt-seed", 5).Select(x => x.id), Is.EqualTo(first.Select(x => x.id)));
            Assert.That(QuestionEngine.IsCorrect(first[0], new[] { "b", "a" }), Is.True);
            Assert.That(QuestionEngine.IsCorrect(first[0], new[] { "a" }), Is.False);
            Assert.Throws<ArgumentException>(() => QuestionEngine.IsCorrect(first[0], new[] { "a", "a" }));
            Assert.That(QuestionEngine.Score(new[] { true, true, false, false, true }), Is.EqualTo(60m));
        }
        [Test] public void RefresherSelectionUsesWeakTopicsAndConfiguredFallback()
        {
            var map = new Dictionary<string, string> { ["gas.ppe"] = "micro-ppe" };
            Assert.That(RefresherSelection.Select(new[] { "gas.ppe" }, map, "micro-general", 2), Is.EqualTo(new[] { "micro-ppe" }));
            Assert.That(RefresherSelection.Select(Array.Empty<string>(), map, "micro-general", 2), Is.EqualTo(new[] { "micro-general" }));
        }
        [Test] public void CanonicalCertificateMatchesSharedPythonVectors()
        {
            var vectors = ContentParser.Read<JArray>(Read("demo/test-vectors/certificate-canonical-v1.json"));
            foreach (var vector in vectors)
            {
                var payload = vector["payload"].ToObject<CertificatePayload>();
                var bytes = CertificateCanonicalizer.Canonicalize(payload);
                Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo((string)vector["canonicalJson"]));
                using (var sha = SHA256.Create())
                    Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(), Is.EqualTo((string)vector["sha256"]));
            }
        }
        [Test] public void CanonicalCertificateRejectsNonUtcAndInvalidUnicode()
        {
            var payload = ContentParser.Read<JArray>(Read("demo/test-vectors/certificate-canonical-v1.json"))[0]["payload"].ToObject<CertificatePayload>();
            payload.issuedAt = "2026-09-28T12:00:00+00:00";
            Assert.Throws<ArgumentException>(() => CertificateCanonicalizer.Canonicalize(payload));
            payload.issuedAt = "2026-09-28T12:00:00Z"; payload.workerDisplayName = "\ud800";
            Assert.Throws<ArgumentException>(() => CertificateCanonicalizer.Canonicalize(payload));
        }
    }
}
