using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SurakshaXR.AppServices;
using SurakshaXR.Domain;
using SurakshaXR.Infrastructure;
using SurakshaXR.Presentation;
using SurakshaXR.Security;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class TrainingSessionTests
    {
        private string path;
        private LocalStore store;
        private DemoIssuer issuer;
        private PreviewContent content;
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        [SetUp] public void Open()
        {
            path = Path.Combine(Path.GetTempPath(), "surakshaxr-session-" + Guid.NewGuid() + ".db");
            store = new LocalStore(SqliteConnection.Open(path)); store.SeedWorkers(); issuer = new DemoIssuer(); content = new PreviewContent();
        }
        [TearDown] public void Close() { store.Dispose(); File.Delete(path); }
        private TrainingSessionService Session(string moduleId, TrainingMode mode, bool requirePractice = true)
        {
            var module = content.Modules.Single(x => x.moduleId == moduleId);
            return new TrainingSessionService(store, store.Workers()[0], module, content.Scenario(moduleId), content.Policy(moduleId), content.Questions(moduleId), mode, "sim3d", issuer.Signer, issuer.Trust, Start, new SessionOptions { requirePractice = requirePractice });
        }
        private static void Scenario(TrainingSessionService session)
        {
            var tick = 0;
            while (session.Runtime.State == SessionState.Running)
            { var action = session.Runtime.CurrentStep.allowedActions.First(x => x.result == "correct"); session.Runtime.SubmitAction(action.actionId, Start.AddSeconds(++tick), tick * 1000); }
        }
        private static void Quiz(TrainingSessionService session, bool correct = true)
        {
            while (session.NeedsQuiz)
            {
                var question = session.CurrentQuestion;
                session.Answer(question.id, correct ? question.correctOptionIds : new[] { question.options.First(x => !question.correctOptionIds.Contains(x.id)).id });
            }
        }
        [TestCase("fire-response")][TestCase("gas-confined-space")]
        public void PracticeAssessmentCertificateAndRefresherSurviveOfflineReopen(string moduleId)
        {
            Assert.Throws<InvalidOperationException>(() => Session(moduleId, TrainingMode.Assessment));
            var practice = Session(moduleId, TrainingMode.Practice); Scenario(practice); practice.Complete(Start.AddMinutes(5));
            Assert.That(store.Certificates(store.Workers()[0].id), Is.Empty);
            var assessment = Session(moduleId, TrainingMode.Assessment); Scenario(assessment);
            Assert.Throws<InvalidOperationException>(() => assessment.Complete(Start.AddMinutes(10)));
            Quiz(assessment); assessment.Complete(Start.AddMinutes(10));
            var count = store.PendingEvents().Count; assessment.Complete(Start.AddMinutes(11));
            Assert.That(store.PendingEvents().Count, Is.EqualTo(count));
            store.Dispose(); store = new LocalStore(SqliteConnection.Open(path));
            var worker = store.Workers()[0]; var certificate = store.Certificates(worker.id).Single();
            Assert.That(store.Attempts(worker.id).Count, Is.EqualTo(2));
            Assert.That(certificate.score, Is.EqualTo(100));
            Assert.That(CertificateCodec.Verify(certificate.qr_payload, issuer.Trust).State, Is.EqualTo(VerificationState.VERIFIED_TRUSTED));
            Assert.That(store.Refreshers(worker.id).Single().due_at, Is.EqualTo("2026-12-28T12:10:00Z"));
            var attempt = store.Attempts(worker.id).Single(x => x.mode == "assessment");
            Assert.That(JsonConvert.DeserializeObject<QuestionAnswer[]>(attempt.question_results_json).Length, Is.EqualTo(5));
        }
        [Test] public void CriticalFailureStillMeasuresKnowledgeButNeverIssuesCertificate()
        {
            var session = Session("fire-response", TrainingMode.Assessment, false);
            while (session.Runtime.State == SessionState.Running)
            {
                var step = session.Runtime.CurrentStep;
                var action = step.allowedActions.FirstOrDefault(x => x.criticalFail) ?? step.allowedActions.First(x => x.result == "correct");
                session.Runtime.SubmitAction(action.actionId, Start.AddSeconds(1), 1000);
            }
            Assert.That(session.Runtime.CriticalFail, Is.True); Quiz(session); session.Complete(Start.AddMinutes(10));
            Assert.That(session.Result.Passed, Is.False); Assert.That(session.CertificateId, Is.Null);
            Assert.That(store.Certificates(store.Workers()[0].id), Is.Empty);
        }
        [Test] public void QuestionAnswersUseStableIdsAndCannotBeChangedAfterSubmission()
        {
            var session = Session("fire-response", TrainingMode.Assessment, false); Scenario(session);
            var first = session.CurrentQuestion;
            Assert.Throws<ArgumentException>(() => session.Answer(first.id, new[] { "unknown-option" }));
            Assert.That(session.AnswerCount, Is.Zero);
            session.Answer(first.id, first.correctOptionIds);
            Assert.Throws<InvalidOperationException>(() => session.Answer(first.id, first.correctOptionIds));
            Quiz(session, false); session.Complete(Start.AddMinutes(10));
            Assert.That(session.Result.QuizScore, Is.EqualTo(20)); Assert.That(session.Result.FinalScore, Is.EqualTo(84));
            Assert.That(session.WeakTopics.Count, Is.GreaterThan(0));
        }
        [Test] public void AllQuestionTopicAndDisplayKeysExistInEveryBundledLocale()
        {
            foreach (var locale in new[] { "en", "hi", "sat" })
            {
                content.SelectLocale(locale);
                foreach (var module in content.Modules) foreach (var question in content.Questions(module.moduleId).questions)
                {
                    Assert.That(content.Text(question.promptKey), Is.Not.Empty); Assert.That(content.Text(question.explanationKey), Is.Not.Empty);
                    foreach (var option in question.options) Assert.That(content.Text(option.textKey), Is.Not.Empty);
                    foreach (var tag in question.topicTags) Assert.That(content.Text("topic." + tag), Is.Not.Empty);
                }
            }
        }
        [TestCase("fire-response")][TestCase("gas-confined-space")]
        public void ShortRefresherCompletesOriginalAndSchedulesNextWithoutNewCertificate(string moduleId)
        {
            var assessment = Session(moduleId, TrainingMode.Assessment, false); Scenario(assessment); Quiz(assessment, false); assessment.Complete(Start.AddMinutes(10));
            var worker = store.Workers()[0]; var reminder = store.Refreshers(worker.id).Single();
            var module = content.Modules.Single(x => x.moduleId == moduleId);
            var plan = new RefresherPlan(content.Scenario(moduleId), content.MicroPolicy(moduleId), JsonConvert.DeserializeObject<string[]>(reminder.weak_tags_json));
            Assert.That(plan.Scenario.steps.Length, Is.InRange(1, 2));
            var session = new TrainingSessionService(store, worker, module, content.Scenario(moduleId), content.Policy(moduleId), content.Questions(moduleId), TrainingMode.Refresher, "sim3d", issuer.Signer, issuer.Trust, Start, new SessionOptions(), reminder, content.MicroPolicy(moduleId));
            Scenario(session); Assert.That(session.Runtime.Score, Is.EqualTo(100));
            Assert.That(session.QuestionCount, Is.EqualTo(3)); Quiz(session); session.Complete(Start.AddMinutes(20));
            var events = store.PendingEvents().Count; session.Complete(Start.AddMinutes(21)); Assert.That(store.PendingEvents().Count, Is.EqualTo(events));
            Assert.That(session.CertificateId, Is.Null);
            Assert.That(store.Certificates(worker.id).Count, Is.EqualTo(1));
            var records = store.Refreshers(worker.id);
            Assert.That(records.Single(x => x.id == reminder.id).status, Is.EqualTo("completed"));
            Assert.That(records.Single(x => x.id == reminder.id).completed_attempt_id, Is.EqualTo(session.AttemptId));
            Assert.That(records.Single(x => x.status == "pending").due_at, Is.EqualTo("2026-12-28T12:20:00Z"));
            store.Dispose(); store = new LocalStore(SqliteConnection.Open(path));
            Assert.That(store.Refreshers(worker.id).Count, Is.EqualTo(2));
        }
        [Test] public void EveryMicroPolicyMappingRetainsValidScenarioAndPerfectScore()
        {
            foreach (var module in content.Modules)
            {
                var mapping = content.MicroPolicy(module.moduleId);
                foreach (var tags in mapping.topicSteps.Keys.Select(x => new[] { x }).Concat(new[] { Array.Empty<string>(), mapping.topicSteps.Keys.Take(2).ToArray() }))
                {
                    var plan = new RefresherPlan(content.Scenario(module.moduleId), mapping, tags);
                    var runtime = new ScenarioRuntime(plan.Scenario, plan.Policy, TrainingMode.Refresher); runtime.Start();
                    while (runtime.State == SessionState.Running) runtime.SubmitAction(runtime.CurrentStep.allowedActions.First(x => x.result == "correct").actionId, Start, 0);
                    Assert.That(runtime.Score, Is.EqualTo(100));
                }
            }
        }
    }
}
