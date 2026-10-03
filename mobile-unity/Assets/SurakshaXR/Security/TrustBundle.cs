using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SurakshaXR.Domain;

namespace SurakshaXR.Security
{
    [Serializable] public sealed class TrustedSigner
    { public string signerId, displayName, publicKeyB64, validFrom, validTo; public bool revoked; }
    [Serializable] public sealed class TrustDocument
    { public int v, bundleVersion; public string issuedAt, rootSignature; public TrustedSigner[] signers; }
    public sealed class TrustBundle
    {
        private readonly Dictionary<string, TrustedSigner> signers;
        public int Version { get; }
        public string IssuedAt { get; }
        private TrustBundle(TrustDocument document)
        { Version = document.bundleVersion; IssuedAt = document.issuedAt; signers = document.signers.ToDictionary(x => x.signerId, Copy, StringComparer.Ordinal); }
        private static TrustedSigner Copy(TrustedSigner value) => new TrustedSigner { signerId = value.signerId, displayName = value.displayName, publicKeyB64 = value.publicKeyB64, validFrom = value.validFrom, validTo = value.validTo, revoked = value.revoked };
        public TrustedSigner Find(string signerId) => signers.TryGetValue(signerId, out var value) ? Copy(value) : null;
        public static TrustBundle Load(string json, byte[] rootPublicKey, int minimumVersion = 1)
        {
            var obj = CertificateCodec.ReadObject(json);
            CertificateCodec.ExactFields(obj, "v", "bundleVersion", "issuedAt", "signers", "rootSignature");
            if (obj["v"].Type != JTokenType.Integer || obj["bundleVersion"].Type != JTokenType.Integer || obj["signers"].Type != JTokenType.Array || obj["issuedAt"].Type != JTokenType.String || obj["rootSignature"].Type != JTokenType.String) throw new ArgumentException("Invalid trust document types.");
            foreach (var entry in (JArray)obj["signers"])
            {
                if (!(entry is JObject signer)) throw new ArgumentException("Invalid signer object.");
                if (signer["validTo"] == null) signer["validTo"] = JValue.CreateNull();
                CertificateCodec.ExactFields(signer, "signerId", "displayName", "publicKeyB64", "validFrom", "validTo", "revoked");
                if (signer["revoked"].Type != JTokenType.Boolean || signer.Properties().Any(x => x.Name != "revoked" && x.Value.Type != JTokenType.String && !(x.Name == "validTo" && x.Value.Type == JTokenType.Null))) throw new ArgumentException("Invalid signer types.");
            }
            var document = obj.ToObject<TrustDocument>(); var bytes = Canonicalize(document);
            if (document.bundleVersion < minimumVersion || !Ed25519Crypto.VerifySignature(bytes, CertificateCodec.DecodeBase64Url(document.rootSignature), rootPublicKey)) throw new ArgumentException("Untrusted or stale trust bundle.");
            return new TrustBundle(document);
        }
        public static byte[] Canonicalize(TrustDocument document)
        {
            if (document == null || document.v != 1 || document.bundleVersion < 1 || document.signers == null || document.signers.Length > 50) throw new ArgumentException("Invalid trust bundle.");
            CertificateCanonicalizer.ParseTime(document.issuedAt);
            if (document.signers.Any(x => x == null || string.IsNullOrWhiteSpace(x.signerId) || string.IsNullOrWhiteSpace(x.displayName)) || document.signers.Select(x => x.signerId).Distinct(StringComparer.Ordinal).Count() != document.signers.Length) throw new ArgumentException("Invalid or duplicate signer.");
            var text = new StringBuilder("{\"v\":1,\"bundleVersion\":").Append(document.bundleVersion.ToString(CultureInfo.InvariantCulture)).Append(",\"issuedAt\":").Append(CertificateCanonicalizer.Quote(document.issuedAt)).Append(",\"signers\":[");
            var first = true;
            foreach (var signer in document.signers.OrderBy(x => x.signerId, StringComparer.Ordinal))
            {
                var key = Convert.FromBase64String(signer.publicKeyB64);
                if (key.Length != 32 || Convert.ToBase64String(key) != signer.publicKeyB64) throw new ArgumentException("Invalid signer key.");
                var start = CertificateCanonicalizer.ParseTime(signer.validFrom);
                if (signer.validTo != null && CertificateCanonicalizer.ParseTime(signer.validTo) < start) throw new ArgumentException("Invalid signer dates.");
                if (!first) text.Append(','); first = false;
                text.Append("{\"signerId\":").Append(CertificateCanonicalizer.Quote(signer.signerId))
                    .Append(",\"displayName\":").Append(CertificateCanonicalizer.Quote(signer.displayName))
                    .Append(",\"publicKeyB64\":").Append(CertificateCanonicalizer.Quote(signer.publicKeyB64))
                    .Append(",\"validFrom\":").Append(CertificateCanonicalizer.Quote(signer.validFrom))
                    .Append(",\"validTo\":").Append(CertificateCanonicalizer.Quote(signer.validTo))
                    .Append(",\"revoked\":").Append(signer.revoked ? "true" : "false").Append('}');
            }
            return new UTF8Encoding(false, true).GetBytes(text.Append("]}").ToString());
        }
    }
}
