using System;
using System.Globalization;
using System.Text;

namespace SurakshaXR.Domain
{
    [Serializable] public sealed class CertificatePayload
    {
        public int v = 1;
        public string certificateId, workerId, workerCode, workerDisplayName;
        public string moduleId, moduleVersion, attemptId;
        public int score;
        public string issuedAt, refresherDueAt, signerId;
    }
    public interface ICryptoSigner
    {
        string SignerId { get; }
        byte[] Sign(byte[] canonicalPayload);
        byte[] GetPublicKey();
    }
    public interface ICertificateSignatureVerifier
    { bool Verify(byte[] canonicalPayload, byte[] signature, byte[] publicKey); }

    public static class CertificateCanonicalizer
    {
        public static byte[] Canonicalize(CertificatePayload value)
        {
            ContentParser.Require(value != null && value.v == 1 && value.score >= 0 && value.score <= 100, "Invalid certificate version/score.");
            foreach (var id in new[] { value.certificateId, value.workerId, value.attemptId })
                ContentParser.Require(Guid.TryParseExact(id, "D", out var uuid) && uuid.ToString("D") == id, "Canonical lowercase UUID required.");
            foreach (var text in new[] { value.workerCode, value.workerDisplayName, value.moduleId, value.moduleVersion, value.signerId })
                ContentParser.Require(!string.IsNullOrWhiteSpace(text), "Certificate identity fields cannot be empty.");
            var issued = ParseTime(value.issuedAt);
            if (value.refresherDueAt != null)
                ContentParser.Require(ParseTime(value.refresherDueAt) >= issued, "Refresher date precedes issue.");
            var result = new StringBuilder("{\"v\":1");
            Add(result, "certificateId", value.certificateId);
            Add(result, "workerId", value.workerId);
            Add(result, "workerCode", value.workerCode);
            Add(result, "workerDisplayName", value.workerDisplayName);
            Add(result, "moduleId", value.moduleId);
            Add(result, "moduleVersion", value.moduleVersion);
            Add(result, "attemptId", value.attemptId);
            result.Append(",\"score\":").Append(value.score.ToString(CultureInfo.InvariantCulture));
            Add(result, "issuedAt", value.issuedAt);
            Add(result, "refresherDueAt", value.refresherDueAt);
            Add(result, "signerId", value.signerId);
            result.Append('}');
            return new UTF8Encoding(false, true).GetBytes(result.ToString());
        }
        public static DateTimeOffset ParseTime(string value)
        {
            ContentParser.Require(DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result), "Whole-second UTC timestamp required.");
            return result;
        }
        private static void Add(StringBuilder builder, string key, string value)
        {
            builder.Append(',').Append('"').Append(key).Append("\":");
            builder.Append(Quote(value));
        }
        public static string Quote(string value)
        {
            if (value == null) return "null";
            var builder = new StringBuilder();
            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c))
                {
                    ContentParser.Require(i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]), "Unpaired Unicode surrogate.");
                    builder.Append(c).Append(value[++i]);
                }
                else if (char.IsLowSurrogate(c)) throw new ArgumentException("Unpaired Unicode surrogate.");
                else if (c == '"' || c == '\\') builder.Append('\\').Append(c);
                else if (c < 32) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else builder.Append(c);
            }
            builder.Append('"');
            return builder.ToString();
        }
    }
}
