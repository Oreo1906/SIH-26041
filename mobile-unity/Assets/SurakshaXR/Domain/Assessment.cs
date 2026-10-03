using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SurakshaXR.Domain
{
    public sealed class AssessmentResult
    {
        public decimal ScenarioScore { get; }
        public decimal QuizScore { get; }
        public decimal FinalScore { get; }
        public int CertificateScore { get; }
        public bool Passed { get; }
        public bool CertificateEligible { get; }
        public AssessmentResult(decimal scenario, decimal quiz, PassPolicy policy, bool critical, TrainingMode mode)
        {
            ContentParser.Validate(policy);
            ContentParser.Require(scenario >= 0 && scenario <= 100 && quiz >= 0 && quiz <= 100, "Scores must be 0-100.");
            ScenarioScore = scenario; QuizScore = quiz;
            FinalScore = scenario * policy.scenarioWeight + quiz * policy.quizWeight;
            CertificateScore = (int)Math.Round(FinalScore, 0, MidpointRounding.AwayFromZero);
            Passed = FinalScore >= policy.passThreshold && !(critical && policy.criticalFailForcesFail);
            // Refresher certificate issuance needs an explicit future module policy.
            CertificateEligible = Passed && mode == TrainingMode.Assessment;
        }
    }

    public static class QuestionEngine
    {
        public static QuestionDefinition[] Select(QuestionBank bank, string attemptSeed, int count)
        {
            ContentParser.Validate(bank);
            ContentParser.Require(!string.IsNullOrEmpty(attemptSeed) && count > 0 && count <= bank.questions.Length, "Invalid quiz seed/count.");
            return bank.questions.OrderBy(x => Hash(attemptSeed + "|" + x.id), StringComparer.Ordinal)
                .ThenBy(x => x.id, StringComparer.Ordinal).Take(count).Select(ContentParser.Copy).ToArray();
        }
        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        public static bool IsCorrect(QuestionDefinition question, IEnumerable<string> selected)
        {
            if (question == null || selected == null) throw new ArgumentException("Question and answer required.");
            var answers = selected.ToArray();
            ContentParser.Require(answers.Distinct(StringComparer.Ordinal).Count() == answers.Length &&
                answers.All(x => question.options.Any(o => o.id == x)), "Unknown or duplicate answer option.");
            return new HashSet<string>(answers, StringComparer.Ordinal).SetEquals(question.correctOptionIds);
        }
        public static decimal Score(IReadOnlyList<bool> results)
        {
            ContentParser.Require(results != null && results.Count > 0, "Quiz results required.");
            return results.Count(x => x) * 100m / results.Count;
        }
    }

    public static class RefresherSelection
    {
        public static string[] Select(IReadOnlyList<string> rankedWeakTopics, IDictionary<string, string> microScenarios, string generalScenarioId, int maximum)
        {
            ContentParser.Require(rankedWeakTopics != null && microScenarios != null && maximum > 0 && ContentParser.Id(generalScenarioId), "Refresher configuration required.");
            var selected = rankedWeakTopics.Where(microScenarios.ContainsKey).Select(x => microScenarios[x])
                .Distinct(StringComparer.Ordinal).Take(maximum).ToArray();
            ContentParser.Require(selected.All(ContentParser.Id), "Invalid micro-scenario ID.");
            return selected.Length == 0 ? new[] { generalScenarioId } : selected;
        }
    }
}
