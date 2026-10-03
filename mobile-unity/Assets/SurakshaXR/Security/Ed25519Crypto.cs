using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using SurakshaXR.Domain;

namespace SurakshaXR.Security
{
    public sealed class Ed25519Crypto : ICryptoSigner, ICertificateSignatureVerifier
    {
        private readonly Ed25519PrivateKeyParameters key;
        public string SignerId { get; }
        public Ed25519Crypto(string signerId, byte[] privateSeed)
        {
            if (string.IsNullOrWhiteSpace(signerId) || privateSeed?.Length != 32) throw new ArgumentException("Invalid issuer configuration.");
            SignerId = signerId; key = new Ed25519PrivateKeyParameters(privateSeed, 0);
        }
        public byte[] Sign(byte[] bytes)
        { var signer = new Ed25519Signer(); signer.Init(true, key); signer.BlockUpdate(bytes, 0, bytes.Length); return signer.GenerateSignature(); }
        public byte[] GetPublicKey() => key.GeneratePublicKey().GetEncoded();
        public bool Verify(byte[] bytes, byte[] signature, byte[] publicKey) => VerifySignature(bytes, signature, publicKey);
        public static bool VerifySignature(byte[] bytes, byte[] signature, byte[] publicKey)
        {
            if (bytes == null || signature?.Length != 64 || publicKey?.Length != 32) return false;
            try { var verifier = new Ed25519Signer(); verifier.Init(false, new Ed25519PublicKeyParameters(publicKey, 0)); verifier.BlockUpdate(bytes, 0, bytes.Length); return verifier.VerifySignature(signature); }
            catch (ArgumentException) { return false; }
        }
    }
}
