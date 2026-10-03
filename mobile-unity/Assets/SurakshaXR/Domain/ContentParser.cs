using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SurakshaXR.Domain
{
    public static class ContentParser
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None, MaxDepth = 64,
            DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal
        };

        public static T Read<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2000000)
                throw new ArgumentException("Content is empty or oversized.");
            var result = JsonConvert.DeserializeObject<T>(json, Settings);
            if (result == null) throw new ArgumentException("Content cannot be null.");
            return result;
        }

        internal static T Copy<T>(T value) => Read<T>(JsonConvert.SerializeObject(value, Settings));
        internal static void Require(bool valid, string message)
        { if (!valid) throw new ArgumentException(message); }
        internal static bool Id(string id) => !string.IsNullOrWhiteSpace(id) && !id.Contains("/");
        private static void Unique(IEnumerable<string> values, string label)
        {
            var list = values.ToArray();
            Require(list.All(Id) && list.Distinct(StringComparer.Ordinal).Count() == list.Length,
                "Missing or duplicate " + label + ".");
        }

        public static void Validate(ScenarioDefinition definition, ScenarioPolicy policy)
        {
            Require(definition != null && policy != null, "Scenario and policy are required.");
            Require(Id(definition.scenarioId) && Id(definition.moduleId) && Id(definition.version), "Invalid scenario identity.");
            Require(policy.scenarioId == definition.scenarioId && policy.scenarioVersion == definition.version, "Policy identity/version mismatch.");
            Require(!string.IsNullOrWhiteSpace(definition.titleKey) && definition.objectives?.Length > 0, "Scenario title/objectives required.");
            Require(definition.contentValidation != null &&
                new[] { "demo-unvalidated", "review-pending", "validated" }.Contains(definition.contentValidation.status) &&
                Id(definition.contentValidation.version), "Validation metadata required.");
            Require(definition.anchors != null && definition.entities != null && definition.steps?.Length > 0, "Scenario collections required.");
            Unique(definition.anchors.Select(x => x.id), "anchor IDs");
            Unique(definition.entities.Select(x => x.id), "entity IDs");
            Unique(definition.steps.Select(x => x.stepId), "step IDs");
            foreach (var anchor in definition.anchors)
                Require(anchor.simPosition?.Length == 3 && (anchor.simRotationEuler == null || anchor.simRotationEuler.Length == 3), "Anchor needs three coordinates.");
            var anchors = new HashSet<string>(definition.anchors.Select(x => x.id), StringComparer.Ordinal);
            foreach (var entity in definition.entities)
                Require(Id(entity.kind) && anchors.Contains(entity.anchorId), "Unknown entity anchor/kind.");
            var steps = definition.steps.ToDictionary(x => x.stepId, StringComparer.Ordinal);
            Require(policy.entryStepId != null && steps.ContainsKey(policy.entryStepId), "Explicit entry step required.");
            Require(policy.terminalStepIds?.Length > 0 && policy.terminalStepIds.All(steps.ContainsKey), "Explicit terminal steps required.");
            Unique(policy.terminalStepIds, "terminal step IDs");
            Require(definition.scoring?.categories?.Count > 0 && policy.actionCategories != null, "Scoring/category mapping required.");
            Require(definition.scoring.categories.Values.All(x => x > 0) && definition.scoring.categories.Values.Sum() == 100m, "Category maxima must be positive and total 100.");
            Require(definition.scoring.passThreshold >= 0 && definition.scoring.passThreshold <= 100, "Invalid scenario threshold.");
            var actionKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in definition.steps)
            {
                Require(!string.IsNullOrWhiteSpace(step.objectiveKey) && step.allowedActions?.Length > 0, "Step objective/actions required.");
                Require(step.timeLimitSec == null || step.timeLimitSec > 0, "Time limit must be positive when present.");
                Unique(step.allowedActions.Select(x => x.actionId), "action IDs within step");
                Require(step.allowedActions.Any(x => x.result == "correct"), "Each step needs a correct action.");
                foreach (var action in step.allowedActions)
                {
                    var key = step.stepId + "/" + action.actionId;
                    actionKeys.Add(key);
                    Require(new[] { "correct", "incorrect", "neutral" }.Contains(action.result), "Invalid action result.");
                    Require(action.result != "correct" || action.scoreDelta >= 0, "Correct action cannot deduct points.");
                    Require(action.result != "incorrect" || action.scoreDelta <= 0, "Incorrect action cannot award points.");
                    Require(action.result != "neutral" || action.scoreDelta == 0, "Neutral action cannot score.");
                    Require(action.result == "correct" || action.nextStepId == null, "Only correct actions may advance.");
                    Require(action.nextStepId == null || steps.ContainsKey(action.nextStepId), "Unknown next step.");
                    Require(action.result != "correct" || action.nextStepId != null || policy.terminalStepIds.Contains(step.stepId), "Null success target must be declared terminal.");
                    Require(action.result != "incorrect" || !string.IsNullOrWhiteSpace(action.feedbackKey), "Incorrect action feedback required.");
                    Require(action.weakTopicTags == null || action.weakTopicTags.All(Id), "Invalid weak topic tag.");
                    if (action.scoreDelta != 0)
                        Require(policy.actionCategories.TryGetValue(key, out var category) && definition.scoring.categories.ContainsKey(category), "Scored action category missing.");
                }
            }
            Require(policy.actionCategories.All(x => actionKeys.Contains(x.Key) && definition.scoring.categories.ContainsKey(x.Value)), "Unknown category mapping.");
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            Visit(policy.entryStepId, steps, visiting, visited);
            Require(visited.Count == steps.Count, "Unreachable scenario steps.");
        }

        private static void Visit(string id, Dictionary<string, StepDefinition> steps, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(id)) return;
            Require(visiting.Add(id), "Correct-action cycle is not allowed.");
            foreach (var action in steps[id].allowedActions.Where(x => x.result == "correct" && x.nextStepId != null))
                Visit(action.nextStepId, steps, visiting, visited);
            visiting.Remove(id); visited.Add(id);
        }

        public static void Validate(ModuleDefinition module)
        {
            Require(module != null && Id(module.moduleId) && Id(module.version), "Module identity required.");
            Require(module.scenarioIds?.Length > 0 && Id(module.questionBankId), "Module content references required.");
            Unique(module.scenarioIds, "scenario IDs");
            Require(module.supportedLocales != null && new[] { "en", "hi", "sat" }.All(module.supportedLocales.Contains), "Required locales missing.");
            Validate(module.passPolicy);
            Require(module.refresherPolicy != null && (!module.refresherPolicy.enabled || module.refresherPolicy.defaultIntervalDays > 0), "Enabled refresher requires a configured interval.");
            Require(module.contentValidation != null && Id(module.contentValidation.version), "Module validation metadata required.");
        }

        public static void Validate(PassPolicy policy)
        {
            Require(policy != null && policy.passThreshold >= 0 && policy.passThreshold <= 100, "Invalid pass policy.");
            Require(policy.scenarioWeight >= 0 && policy.quizWeight >= 0 && policy.scenarioWeight + policy.quizWeight == 1m, "Score weights must total 1.");
        }

        public static void Validate(QuestionBank bank)
        {
            Require(bank != null && Id(bank.questionBankId) && Id(bank.moduleId) && Id(bank.version), "Question bank identity required.");
            Require(bank.questions?.Length >= 8, "Author at least eight questions per bank.");
            Unique(bank.questions.Select(x => x.id), "question IDs");
            foreach (var question in bank.questions)
            {
                Require(question.options?.Length >= 3 && question.options.Length <= 4, "Questions require 3-4 options.");
                Unique(question.options.Select(x => x.id), "option IDs");
                Require(question.options.All(x => !string.IsNullOrWhiteSpace(x.textKey)), "Option text key required.");
                Require(!string.IsNullOrWhiteSpace(question.promptKey) && !string.IsNullOrWhiteSpace(question.explanationKey), "Question text keys required.");
                Require(question.correctOptionIds?.Length > 0 && question.correctOptionIds.All(x => question.options.Any(o => o.id == x)), "Unknown or missing correct option.");
                Unique(question.correctOptionIds, "correct option IDs");
                Require(question.topicTags != null && question.topicTags.All(Id), "Question topic tags required.");
            }
        }
    }
}
