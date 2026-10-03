using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SurakshaXR.Domain;
using SurakshaXR.Infrastructure;
using SurakshaXR.Security;

namespace SurakshaXR.AppServices
{
    public sealed class SessionOptions
    {
        public bool requirePractice = true;
        public int assessmentQuestionCount = 5;
    }
    public sealed class QuestionAnswer
    {
        public string questionId;
        public string[] selectedOptionIds;
        public bool correct;
    }
    // One owner of scenario -> quiz -> immutable records -> transactional completion.
    // Neither renderer nor UI needs to know how signing or persistence works.
    public sealed class TrainingSessionService
    {
        private readonly LocalStore store;
        private readonly WorkerRecord worker;
        private readonly ModuleDefinition module;
        private readonly ScenarioDefinition scenario;
        private readonly ICryptoSigner signer;
        private readonly TrustBundle trust;
        private readonly string renderer, startedAt;
        private readonly QuestionDefinition[] questions;
        private readonly List<QuestionAnswer> answers = new List<QuestionAnswer>();
        private AttemptRecord pendingAttempt;
        private CertificateRecord pendingCertificate;
        private RefresherRecord pendingRefresher;
        private readonly RefresherRecord sourceRefresher;
        private string completedRefresherId;
        public string AttemptId { get; }
        public ScenarioRuntime Runtime { get; }
        public AssessmentResult Result { get; private set; }
        public bool Saved { get; private set; }
        public string CertificateId => pendingCertificate?.id;
        public int AnswerCount => answers.Count;
        public int QuestionCount => questions.Length;
        public bool ScenarioEnded => Runtime.State == SessionState.Completed || Runtime.State == SessionState.Failed;
        public bool NeedsQuiz => ScenarioEnded && answers.Count < questions.Length;
        public QuestionDefinition CurrentQuestion => NeedsQuiz ? Copy(questions[answers.Count]) : null;
        public IReadOnlyList<string> WeakTopics => Runtime.Actions.Where(x => x.Result == "incorrect").SelectMany(x => x.WeakTopicTags)
            .Concat(answers.Where(x => !x.correct).SelectMany(x => questions.Single(q => q.id == x.questionId).topicTags))
            .GroupBy(x => x, StringComparer.Ordinal).OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key).ToArray();

        public TrainingSessionService(LocalStore store, WorkerRecord worker, ModuleDefinition module,
            ScenarioDefinition scenario, ScenarioPolicy policy, QuestionBank bank, TrainingMode mode,
            string renderer, ICryptoSigner signer, TrustBundle trust, DateTimeOffset startedAt, SessionOptions options,
            RefresherRecord refresher = null, MicroTrainingPolicy microPolicy = null)
        {
            if (renderer != "sim3d" && renderer != "ar") throw new ArgumentException("Unknown renderer.");
            ContentParser.Validate(module.passPolicy); ContentParser.Validate(bank);
            if (module.moduleId != scenario.moduleId || module.moduleId != bank.moduleId || module.questionBankId != bank.questionBankId || !module.scenarioIds.Contains(scenario.scenarioId)) throw new ArgumentException("Content identity mismatch.");
            if (mode == TrainingMode.Assessment && options.requirePractice && !store.HasCompletedPractice(worker.id, module.moduleId, module.version)) throw new InvalidOperationException("Complete practice before assessment.");
            if (mode == TrainingMode.Refresher)
            {
                if (refresher == null || microPolicy == null || refresher.worker_id != worker.id || refresher.module_id != module.moduleId || refresher.status != "pending") throw new ArgumentException("Pending refresher required.");
                sourceRefresher = Copy(refresher);
                var plan = new RefresherPlan(scenario, microPolicy, JsonConvert.DeserializeObject<string[]>(refresher.weak_tags_json));
                scenario = plan.Scenario; policy = plan.Policy;
            }
            this.store = store; this.worker = Copy(worker); this.module = Copy(module); this.scenario = Copy(scenario);
            this.signer = signer; this.trust = trust; this.renderer = renderer; this.startedAt = Utc(startedAt);
            AttemptId = Guid.NewGuid().ToString("D"); Runtime = new ScenarioRuntime(scenario, policy, mode);
            if (mode == TrainingMode.Practice) questions = Array.Empty<QuestionDefinition>();
            else if (mode == TrainingMode.Refresher)
            {
                var weak = JsonConvert.DeserializeObject<string[]>(refresher.weak_tags_json);
                questions = QuestionEngine.Select(bank, AttemptId, bank.questions.Length).OrderByDescending(x => x.topicTags.Count(weak.Contains)).Take(microPolicy.questionCount).ToArray();
            }
            else questions = QuestionEngine.Select(bank, AttemptId, options.assessmentQuestionCount);
            Runtime.Start();
        }
        public void Answer(string questionId, IEnumerable<string> optionIds)
        {
            if (!NeedsQuiz || CurrentQuestion.id != questionId || pendingAttempt != null) throw new InvalidOperationException("Answer does not belong to the current question.");
            var selected = optionIds.ToArray();
            if (selected.Length == 0) throw new ArgumentException("Select an answer.");
            answers.Add(new QuestionAnswer { questionId = questionId, selectedOptionIds = selected, correct = QuestionEngine.IsCorrect(CurrentQuestion, selected) });
        }
        public void Complete(DateTimeOffset completedAt)
        {
            if (!ScenarioEnded || NeedsQuiz) throw new InvalidOperationException("Finish scenario and knowledge assessment before saving.");
            if (Saved) return;
            // Keep the exact completion snapshot on a storage retry, including UUIDs/time/signature.
            if (pendingAttempt == null)
            {
                var time = Utc(completedAt);
                if (CertificateCanonicalizer.ParseTime(time) < CertificateCanonicalizer.ParseTime(startedAt)) throw new ArgumentException("Completion clock precedes start.");
                var practice = Runtime.Mode == TrainingMode.Practice;
                Result = new AssessmentResult(Runtime.Score, practice ? 0m : QuestionEngine.Score(answers.Select(x => x.correct).ToArray()), module.passPolicy, Runtime.CriticalFail, Runtime.Mode);
                var weak = JsonConvert.SerializeObject(WeakTopics);
                CertificateRecord certificate = null; RefresherRecord refresher = null;
                string due = null;
                if (!practice && module.refresherPolicy.enabled && (Runtime.Mode != TrainingMode.Refresher || Result.Passed))
                {
                    if (!module.refresherPolicy.defaultIntervalDays.HasValue || module.refresherPolicy.defaultIntervalDays.Value <= 0) throw new InvalidOperationException("Configured refresher interval required.");
                    due = Utc(completedAt.AddDays(module.refresherPolicy.defaultIntervalDays.Value));
                    refresher = new RefresherRecord { id = Guid.NewGuid().ToString("D"), worker_id = worker.id, module_id = module.moduleId, due_at = due, weak_tags_json = weak, status = "pending", created_at = time, updated_at = time };
                }
                completedRefresherId = Runtime.Mode == TrainingMode.Refresher && Result.Passed ? sourceRefresher.id : null;
                if (Result.CertificateEligible)
                {
                    var payload = new CertificatePayload { certificateId = Guid.NewGuid().ToString("D"), workerId = worker.id, workerCode = worker.worker_code, workerDisplayName = worker.display_name,
                        moduleId = module.moduleId, moduleVersion = module.version, attemptId = AttemptId, score = Result.CertificateScore, issuedAt = time, refresherDueAt = due, signerId = signer.SignerId };
                    var envelope = CertificateCodec.Issue(payload, signer);
                    if (CertificateCodec.Verify(envelope, trust).State != VerificationState.VERIFIED_TRUSTED) throw new InvalidOperationException("Local issuer is not trusted; completion has not been saved.");
                    certificate = new CertificateRecord { id = payload.certificateId, worker_id = worker.id, attempt_id = AttemptId, module_id = module.moduleId, module_version = module.version, score = payload.score,
                        issued_at = time, refresher_due_at = due, signer_id = signer.SignerId, payload_json = Encoding.UTF8.GetString(CertificateCanonicalizer.Canonicalize(payload)),
                        signature_b64 = (string)CertificateCodec.ReadObject(envelope)["sig"], qr_payload = envelope };
                }
                pendingAttempt = new AttemptRecord { id = AttemptId, worker_id = worker.id, module_id = module.moduleId, module_version = module.version, scenario_id = scenario.scenarioId,
                    renderer = renderer, mode = practice ? "practice" : Runtime.Mode == TrainingMode.Refresher ? "refresher" : "assessment", started_at = startedAt, completed_at = time, score_total = practice ? Runtime.Score : Result.FinalScore,
                    score_json = JsonConvert.SerializeObject(new { scenarioScore = Runtime.Score, quizScore = Result.QuizScore, finalScore = practice ? Runtime.Score : Result.FinalScore, categories = Runtime.CategoryScores }),
                    passed = !practice && Result.Passed, critical_fail = Runtime.CriticalFail, weak_tags_json = weak,
                    actions_json = JsonConvert.SerializeObject(Runtime.Actions.Select(x => new { actionId = x.ActionId, stepId = x.StepId, occurredAt = Utc(x.OccurredAt), elapsedMs = x.ElapsedMs, result = x.Result, scoreDelta = x.ScoreDelta, weakTopicTags = x.WeakTopicTags })),
                    question_results_json = JsonConvert.SerializeObject(answers), content_validation_version = scenario.contentValidation.version };
                pendingCertificate = certificate; pendingRefresher = refresher;
            }
            store.Complete(pendingAttempt, pendingCertificate, pendingRefresher, completedRefresherId); Saved = true;
        }
        private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        private static string Utc(DateTimeOffset value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }
}
