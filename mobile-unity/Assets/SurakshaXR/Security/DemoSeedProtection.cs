using System;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace SurakshaXR.Security
{
    // Prototype bootstrap only. Its wrapping key is recoverable from the APK.
    // Android Keystore provides separate at-rest wrapping after import.
    public static class DemoSeedProtection
    {
        public static byte[] Decrypt(string bootstrap)
        {
            var obj = CertificateCodec.ReadObject(bootstrap);
            CertificateCodec.ExactFields(obj, "demoOnly", "signerId", "nonceB64", "encryptedSeedB64", "demoWrappingKeyB64");
            if ((bool?)obj["demoOnly"] != true) throw new ArgumentException("This loader accepts demo issuers only.");
            var key = Convert.FromBase64String((string)obj["demoWrappingKeyB64"]);
            var nonce = Convert.FromBase64String((string)obj["nonceB64"]);
            var ciphertext = Convert.FromBase64String((string)obj["encryptedSeedB64"]);
            if (key.Length != 32 || nonce.Length != 12 || ciphertext.Length != 48) throw new ArgumentException("Invalid demo bootstrap.");
            try
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                cipher.Init(false, new AeadParameters(new KeyParameter(key), 128, nonce, Encoding.UTF8.GetBytes("SurakshaXR demo issuer v1")));
                var seed = new byte[cipher.GetOutputSize(ciphertext.Length)];
                var written = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, seed, 0);
                written += cipher.DoFinal(seed, written);
                if (written != 32) throw new ArgumentException("Invalid issuer seed.");
                return seed;
            }
            finally { Array.Clear(key, 0, key.Length); }
        }
    }
}
