using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SurakshaXR.Domain;
using ZXing;
using ZXing.QrCode;

namespace SurakshaXR.Security
{
    public enum VerificationState { VERIFIED_TRUSTED, UNKNOWN_SIGNER_UNVERIFIED, SIGNER_NOT_TRUSTED, INVALID_SIGNATURE, UNSUPPORTED_VERSION, PARSE_ERROR }
    public sealed class VerificationResult
    {
        public VerificationState State { get; }
        public CertificatePayload Payload { get; }
        public VerificationResult(VerificationState state, CertificatePayload payload = null) { State = state; Payload = payload; }
    }
    public static class CertificateCodec
    {
        public const int MaximumEnvelopeLength = 8192;
        public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        public static byte[] DecodeBase64Url(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > MaximumEnvelopeLength || text.Any(x => !(x <= 127 && (char.IsLetterOrDigit(x) || x == '-' || x == '_')))) throw new ArgumentException("Invalid base64url.");
            var encoded = text.Replace('-', '+').Replace('_', '/'); encoded += new string('=', (4 - encoded.Length % 4) % 4);
            var bytes = Convert.FromBase64String(encoded);
            if (Base64Url(bytes) != text) throw new ArgumentException("Non-canonical base64url.");
            return bytes;
        }
        public static JObject ReadObject(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > MaximumEnvelopeLength) throw new ArgumentException("Invalid JSON size.");
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 16, FloatParseHandling = FloatParseHandling.Decimal })
            {
                var result = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new ArgumentException("Trailing JSON content.");
                return result;
            }
        }
        public static void ExactFields(JObject value, params string[] fields)
        { if (!new HashSet<string>(fields, StringComparer.Ordinal).SetEquals(value.Properties().Select(x => x.Name))) throw new ArgumentException("Unexpected JSON fields."); }
        public static string Issue(CertificatePayload payload, ICryptoSigner signer)
        {
            if (payload.signerId != signer.SignerId) throw new ArgumentException("Issuer mismatch.");
            var bytes = CertificateCanonicalizer.Canonicalize(payload);
            return JsonConvert.SerializeObject(new { alg = "Ed25519", payload = Base64Url(bytes), sig = Base64Url(signer.Sign(bytes)) });
        }
        public static VerificationResult Verify(string envelope, TrustBundle trust)
        {
            try
            {
                var qr = ReadObject(envelope); ExactFields(qr, "alg", "payload", "sig");
                if ((string)qr["alg"] != "Ed25519") return new VerificationResult(VerificationState.UNSUPPORTED_VERSION);
                var bytes = DecodeBase64Url((string)qr["payload"]); var signature = DecodeBase64Url((string)qr["sig"]);
                var obj = ReadObject(new UTF8Encoding(false, true).GetString(bytes));
                ExactFields(obj, "v", "certificateId", "workerId", "workerCode", "workerDisplayName", "moduleId", "moduleVersion", "attemptId", "score", "issuedAt", "refresherDueAt", "signerId");
                if (obj["v"].Type != JTokenType.Integer || (int)obj["v"] != 1) return new VerificationResult(VerificationState.UNSUPPORTED_VERSION);
                if (obj["score"].Type != JTokenType.Integer) throw new ArgumentException("Score must be an integer.");
                foreach (var property in obj.Properties().Where(x => x.Name != "v" && x.Name != "score"))
                    if (property.Value.Type != JTokenType.String && !(property.Name == "refresherDueAt" && property.Value.Type == JTokenType.Null)) throw new ArgumentException("Invalid certificate type.");
                var payload = obj.ToObject<CertificatePayload>(); var canonical = CertificateCanonicalizer.Canonicalize(payload);
                if (!bytes.SequenceEqual(canonical)) throw new ArgumentException("Non-canonical payload.");
                var signer = trust.Find(payload.signerId);
                if (signer == null) return new VerificationResult(VerificationState.UNKNOWN_SIGNER_UNVERIFIED, payload);
                if (!Ed25519Crypto.VerifySignature(bytes, signature, Convert.FromBase64String(signer.publicKeyB64))) return new VerificationResult(VerificationState.INVALID_SIGNATURE);
                var time = CertificateCanonicalizer.ParseTime(payload.issuedAt);
                var trusted = !signer.revoked && time >= CertificateCanonicalizer.ParseTime(signer.validFrom) && (signer.validTo == null || time <= CertificateCanonicalizer.ParseTime(signer.validTo));
                return new VerificationResult(trusted ? VerificationState.VERIFIED_TRUSTED : VerificationState.SIGNER_NOT_TRUSTED, payload);
            }
            catch (Exception error) when (error is ArgumentException || error is JsonException || error is System.FormatException || error is OverflowException)
            { return new VerificationResult(VerificationState.PARSE_ERROR); }
        }
        public static byte[] QrPixels(string envelope, int size = 512)
        {
            if (envelope.Length > MaximumEnvelopeLength || size < 256 || size > 1024) throw new ArgumentException("Invalid QR size.");
            var matrix = new QRCodeWriter().encode(envelope, BarcodeFormat.QR_CODE, size, size, new Dictionary<EncodeHintType, object> { { EncodeHintType.MARGIN, 4 }, { EncodeHintType.CHARACTER_SET, "UTF-8" } });
            var pixels = new byte[size * size * 4];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) { var offset = (y * size + x) * 4; var c = matrix[x, y] ? (byte)0 : (byte)255; pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = c; pixels[offset + 3] = 255; }
            return pixels;
        }
        public static string DecodeQr(byte[] rgba, int width, int height)
        {
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096 || rgba.Length != checked(width * height * 4)) throw new ArgumentException("Invalid QR image.");
            var reader = new BarcodeReaderGeneric { AutoRotate = true, Options = new ZXing.Common.DecodingOptions { TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE } } };
            return reader.Decode(new RGBLuminanceSource(rgba, width, height, RGBLuminanceSource.BitmapFormat.RGBA32))?.Text;
        }
    }
}
