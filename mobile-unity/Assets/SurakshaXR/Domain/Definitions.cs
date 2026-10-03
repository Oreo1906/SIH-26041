using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SurakshaXR.Domain
{
    // JSON field names intentionally mirror the normative schemas.
    [Serializable] public sealed class ScenarioDefinition
    {
        public string scenarioId, moduleId, version, titleKey, descriptionKey;
        public string[] objectives;
        public AnchorDefinition[] anchors;
        public EntityDefinition[] entities;
        public StepDefinition[] steps;
        public ScoringDefinition scoring;
        public ContentValidation contentValidation;
    }
    [Serializable] public sealed class AnchorDefinition
    {
        public string id, arPlacementHint;
        public decimal[] simPosition, simRotationEuler;
    }
    [Serializable] public sealed class EntityDefinition
    {
        public string id, kind, anchorId, prefabKey, labelKey;
        public bool initiallyVisible = true;
    }
    [Serializable] public sealed class StepDefinition
    {
        public string stepId, objectiveKey, narrationKey;
        public decimal? timeLimitSec;
        public ActionDefinition[] allowedActions;
        public JObject[] visualCommands;
    }
    [Serializable] public sealed class ActionDefinition
    {
        public string actionId, result, feedbackKey, nextStepId;
        public decimal scoreDelta;
        public string[] weakTopicTags;
        public bool criticalFail;
    }
    [Serializable] public sealed class ScoringDefinition
    {
        public decimal passThreshold;
        public Dictionary<string, decimal> categories;
        public bool criticalFailForcesFail = true;
    }
    [Serializable] public sealed class ContentValidation
    {
        public string status, version, reviewer, reviewedAt;
    }
    [Serializable] public sealed class ScenarioPolicy
    {
        public string scenarioId, scenarioVersion, entryStepId;
        public string[] terminalStepIds;
        public Dictionary<string, string> actionCategories;
    }
    [Serializable] public sealed class ModuleDefinition
    {
        public string moduleId, version, titleKey, descriptionKey, questionBankId;
        public string[] scenarioIds, supportedLocales;
        public PassPolicy passPolicy;
        public RefresherPolicy refresherPolicy;
        public ContentValidation contentValidation;
    }
    [Serializable] public sealed class PassPolicy
    {
        public decimal passThreshold, scenarioWeight, quizWeight;
        public bool criticalFailForcesFail;
    }
    [Serializable] public sealed class RefresherPolicy
    {
        public bool enabled, prioritizeWeakTopics;
        public int? defaultIntervalDays;
    }
    [Serializable] public sealed class QuestionBank
    {
        public string questionBankId, moduleId, version;
        public QuestionDefinition[] questions;
    }
    [Serializable] public sealed class QuestionDefinition
    {
        public string id, promptKey, explanationKey, difficulty;
        public OptionDefinition[] options;
        public string[] correctOptionIds, topicTags;
    }
    [Serializable] public sealed class OptionDefinition { public string id, textKey; }

    public enum SessionState { Ready, Running, Paused, Completed, Failed, Aborted }
    public enum TrainingMode { Practice, Assessment, Refresher }

    public sealed class ActionRecord
    {
        public string StepId { get; }
        public string ActionId { get; }
        public string Result { get; }
        public DateTimeOffset OccurredAt { get; }
        public long ElapsedMs { get; }
        public decimal ScoreDelta { get; }
        public IReadOnlyList<string> WeakTopicTags { get; }
        public ActionRecord(string step, ActionDefinition action, DateTimeOffset time, long elapsed)
        {
            StepId = step; ActionId = action.actionId; Result = action.result;
            OccurredAt = time; ElapsedMs = elapsed; ScoreDelta = action.scoreDelta;
            WeakTopicTags = Array.AsReadOnly((string[])(action.weakTopicTags ?? Array.Empty<string>()).Clone());
        }
    }
}
