using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SurakshaXR.Domain;

namespace SurakshaXR.AppServices
{
    public sealed class MicroTrainingPolicy
    {
        public string moduleId;
        public string[] generalStepIds;
        public Dictionary<string, string> topicSteps;
        public int maximumTopics = 2;
        public int questionCount = 3;
    }
    public sealed class RefresherPlan
    {
        public ScenarioDefinition Scenario { get; }
        public ScenarioPolicy Policy { get; }
        public RefresherPlan(ScenarioDefinition source, MicroTrainingPolicy mapping, IReadOnlyList<string> weakTopics)
        {
            if (mapping.moduleId != source.moduleId || mapping.maximumTopics < 1 || mapping.maximumTopics > 2 || mapping.questionCount < 2 || mapping.questionCount > 3) throw new ArgumentException("Invalid micro-training policy.");
            var selected = weakTopics.Where(mapping.topicSteps.ContainsKey).Take(mapping.maximumTopics).Select(x => mapping.topicSteps[x]).Distinct().ToArray();
            if (selected.Length == 0) selected = mapping.generalStepIds;
            if (selected == null || selected.Length < 1 || selected.Length > 2 || selected.Any(x => !source.steps.Any(step => step.stepId == x))) throw new ArgumentException("Invalid micro-scenario steps.");
            Scenario = JsonConvert.DeserializeObject<ScenarioDefinition>(JsonConvert.SerializeObject(source));
            // Retain original procedure order; stable semantic IDs identify the subset.
            Scenario.steps = Scenario.steps.Where(x => selected.Contains(x.stepId)).ToArray();
            Scenario.scenarioId = source.scenarioId + "-micro-" + string.Join("-", Scenario.steps.Select(x => x.stepId));
            Scenario.scoring.categories = new Dictionary<string, decimal>();
            Policy = new ScenarioPolicy { scenarioId = Scenario.scenarioId, scenarioVersion = Scenario.version, entryStepId = Scenario.steps[0].stepId,
                terminalStepIds = new[] { Scenario.steps.Last().stepId }, actionCategories = new Dictionary<string, string>() };
            for (var i = 0; i < Scenario.steps.Length; i++)
            {
                var step = Scenario.steps[i]; var category = "micro_" + step.stepId; var allocation = 100m / Scenario.steps.Length;
                var originalMaximum = step.allowedActions.Where(x => x.result == "correct").Max(x => x.scoreDelta);
                if (originalMaximum <= 0) throw new ArgumentException("Micro-training requires a scored correct action.");
                Scenario.scoring.categories[category] = allocation;
                foreach (var action in step.allowedActions)
                {
                    action.scoreDelta = action.scoreDelta * allocation / originalMaximum;
                    action.nextStepId = action.result == "correct" && i + 1 < Scenario.steps.Length ? Scenario.steps[i + 1].stepId : null;
                    if (action.scoreDelta != 0) Policy.actionCategories[step.stepId + "/" + action.actionId] = category;
                }
            }
            ContentParser.Validate(Scenario, Policy);
        }
    }
}
