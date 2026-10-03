using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SurakshaXR.Domain
{
    public sealed class ScenarioRuntime
    {
        private readonly ScenarioDefinition definition;
        private readonly ScenarioPolicy policy;
        private readonly Dictionary<string, StepDefinition> steps;
        private readonly Dictionary<string, decimal> rawScores;
        private readonly List<ActionRecord> actions = new List<ActionRecord>();
        private long lastElapsed;
        private DateTimeOffset? lastTime;
        public SessionState State { get; private set; } = SessionState.Ready;
        public TrainingMode Mode { get; }
        public string CurrentStepId { get; private set; }
        public bool CriticalFail { get; private set; }
        public IReadOnlyList<ActionRecord> Actions => actions.AsReadOnly();
        public event Action Changed;

        public ScenarioRuntime(ScenarioDefinition scenario, ScenarioPolicy runtimePolicy, TrainingMode mode)
        {
            ContentParser.Validate(scenario, runtimePolicy);
            definition = ContentParser.Copy(scenario); policy = ContentParser.Copy(runtimePolicy);
            steps = definition.steps.ToDictionary(x => x.stepId, StringComparer.Ordinal);
            rawScores = definition.scoring.categories.ToDictionary(x => x.Key, x => 0m, StringComparer.Ordinal);
            Mode = mode; CurrentStepId = policy.entryStepId;
        }

        // Copies prevent UI/renderer code from mutating runtime definitions.
        public StepDefinition CurrentStep => ContentParser.Copy(steps[CurrentStepId]);
        public IReadOnlyDictionary<string, decimal> CategoryScores => new ReadOnlyDictionary<string, decimal>(
            rawScores.ToDictionary(x => x.Key, x => Math.Max(0m, Math.Min(definition.scoring.categories[x.Key], x.Value)), StringComparer.Ordinal));
        public decimal Score => CategoryScores.Values.Sum();
        public IReadOnlyList<string> WeakTopics => Array.AsReadOnly(actions.Where(x => x.Result == "incorrect")
            .SelectMany(x => x.WeakTopicTags.Distinct(StringComparer.Ordinal)).GroupBy(x => x, StringComparer.Ordinal)
            .OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key).ToArray());

        public void Start() { RequireState(SessionState.Ready); State = SessionState.Running; Changed?.Invoke(); }
        public void Pause() { RequireState(SessionState.Running); State = SessionState.Paused; Changed?.Invoke(); }
        public void Resume() { RequireState(SessionState.Paused); State = SessionState.Running; Changed?.Invoke(); }
        public void Abort()
        {
            if (State != SessionState.Ready && State != SessionState.Running && State != SessionState.Paused)
                throw new InvalidOperationException("A terminal attempt cannot be aborted.");
            State = SessionState.Aborted; Changed?.Invoke();
        }
        public ActionRecord SubmitAction(string actionId, DateTimeOffset occurredAt, long elapsedMs)
        {
            RequireState(SessionState.Running);
            if (occurredAt.Offset != TimeSpan.Zero || elapsedMs < lastElapsed || elapsedMs < 0 || (lastTime.HasValue && occurredAt < lastTime.Value))
                throw new ArgumentException("Action clocks must be UTC and monotonic.");
            var action = steps[CurrentStepId].allowedActions.SingleOrDefault(x => x.actionId == actionId);
            if (action == null) throw new ArgumentException("Action is not allowed in the current step.");
            var record = new ActionRecord(CurrentStepId, action, occurredAt, elapsedMs);
            if (action.scoreDelta != 0) rawScores[policy.actionCategories[CurrentStepId + "/" + actionId]] += action.scoreDelta;
            actions.Add(record); lastElapsed = elapsedMs; lastTime = occurredAt;
            CriticalFail |= action.criticalFail;
            if (CriticalFail && definition.scoring.criticalFailForcesFail && Mode != TrainingMode.Practice)
                State = SessionState.Failed;
            else if (action.result == "correct")
            {
                if (action.nextStepId == null) State = SessionState.Completed;
                else CurrentStepId = action.nextStepId;
            }
            Changed?.Invoke();
            return record;
        }
        private void RequireState(SessionState expected)
        { if (State != expected) throw new InvalidOperationException("Operation requires " + expected + "."); }
    }
}
