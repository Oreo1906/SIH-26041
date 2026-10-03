using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SurakshaXR.Domain;
using SurakshaXR.Security;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class CertificateTests
    {
        private JObject data;
        [SetUp] public void Read() { data = CertificateCodec.ReadObject(File.ReadAllText(Path.Combine(Application.dataPath, "../../demo/test-vectors/certificate-signatures-v1.json"))); }
        private TrustBundle Trust(TrustDocument changed = null)
        {
            var document = changed ?? data["trustBundle"].ToObject<TrustDocument>();
            if (changed != null)
            {
                var root = new Ed25519Crypto("PUBLIC_TEST_ROOT", Convert.FromBase64String((string)data["testRootSeedB64"]));
                document.rootSignature = CertificateCodec.Base64Url(root.Sign(TrustBundle.Canonicalize(document)));
            }
            return TrustBundle.Load(JsonConvert.SerializeObject(document), Convert.FromBase64String((string)data["rootPublicKeyB64"]));
        }
        [Test] public void Ed25519AndRootTrustMatchPythonVectorsExactly()
        {
            var signer = new Ed25519Crypto("demo-site-key-01", Convert.FromBase64String((string)data["testPrivateSeedB64"]));
            CollectionAssert.AreEqual(Convert.FromBase64String((string)data["publicKeyB64"]), signer.GetPublicKey());
            Assert.That(Encoding.UTF8.GetString(TrustBundle.Canonicalize(data["trustBundle"].ToObject<TrustDocument>())), Is.EqualTo((string)data["trustCanonical"]));
            foreach (var vector in (JArray)data["vectors"])
            {
                var payload = vector["payload"].ToObject<CertificatePayload>();
                CollectionAssert.AreEqual(Convert.FromBase64String((string)vector["signatureB64"]), signer.Sign(CertificateCanonicalizer.Canonicalize(payload)));
                Assert.That(CertificateCodec.Issue(payload, signer), Is.EqualTo((string)vector["envelope"]));
                Assert.That(CertificateCodec.Verify((string)vector["envelope"], Trust()).State, Is.EqualTo(VerificationState.VERIFIED_TRUSTED));
            }
        }
        [TestCase("score")][TestCase("workerDisplayName")][TestCase("moduleId")][TestCase("issuedAt")][TestCase("signature")]
        public void TamperedCertificateNeverVerifies(string field)
        {
            var vector = data["vectors"][0]; var envelope = CertificateCodec.ReadObject((string)vector["envelope"]);
            if (field == "signature") { var bytes = CertificateCodec.DecodeBase64Url((string)envelope["sig"]); bytes[0] ^= 1; envelope["sig"] = CertificateCodec.Base64Url(bytes); }
            else
            {
                var payload = vector["payload"].ToObject<CertificatePayload>();
                if (field == "score") payload.score = 100;
                else if (field == "workerDisplayName") payload.workerDisplayName = "Altered";
                else if (field == "moduleId") payload.moduleId = "altered";
                else payload.issuedAt = "2026-09-28T12:00:01Z";
                envelope["payload"] = CertificateCodec.Base64Url(CertificateCanonicalizer.Canonicalize(payload));
            }
            Assert.That(CertificateCodec.Verify(envelope.ToString(Formatting.None), Trust()).State, Is.EqualTo(VerificationState.INVALID_SIGNATURE));
        }
        [Test] public void ChangedRootBundleRejectedAndUnknownOrRevokedSignerNotTrusted()
        {
            var doc = data["trustBundle"].ToObject<TrustDocument>(); doc.signers[0].revoked = true;
            Assert.Throws<ArgumentException>(() => TrustBundle.Load(JsonConvert.SerializeObject(doc), Convert.FromBase64String((string)data["rootPublicKeyB64"])));
            var envelope = (string)data["vectors"][0]["envelope"];
            Assert.That(CertificateCodec.Verify(envelope, Trust(doc)).State, Is.EqualTo(VerificationState.SIGNER_NOT_TRUSTED));
            doc.signers = Array.Empty<TrustedSigner>();
            Assert.That(CertificateCodec.Verify(envelope, Trust(doc)).State, Is.EqualTo(VerificationState.UNKNOWN_SIGNER_UNVERIFIED));
        }
        [Test] public void GeneratedQrDecodesAndVerifiesEntirelyInMemory()
        {
            var envelope = (string)data["vectors"][1]["envelope"];
            var decoded = CertificateCodec.DecodeQr(CertificateCodec.QrPixels(envelope), 512, 512);
            Assert.That(decoded, Is.EqualTo(envelope));
            Assert.That(CertificateCodec.Verify(decoded, Trust()).State, Is.EqualTo(VerificationState.VERIFIED_TRUSTED));
        }
        [Test] public void MalformedOversizedAndDuplicateQrDataHasBoundedFailure()
        {
            foreach (var malformed in new[] { "", "{}", "[]", new string('x', 8193), "{\"alg\":\"Ed25519\",\"alg\":\"none\"}", "{\"alg\":\"Ed25519\",\"payload\":\"%\",\"sig\":\"%\"}" })
                Assert.That(CertificateCodec.Verify(malformed, Trust()).State, Is.EqualTo(VerificationState.PARSE_ERROR));
        }
    }
}
