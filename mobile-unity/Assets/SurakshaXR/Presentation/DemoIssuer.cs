using System;
using System.Linq;
using SurakshaXR.Domain;
using SurakshaXR.Security;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // docs08 prototype fallback. Bootstrap protection is recoverable from the APK.
    public sealed class DemoIssuer
    {
        public ICryptoSigner Signer { get; }
        public TrustBundle Trust { get; }
        public DemoIssuer()
        {
            var root = CertificateCodec.ReadObject(Read("root-public"));
            Trust = TrustBundle.Load(Read("trust-bundle"), Convert.FromBase64String((string)root["publicKeyB64"]));
            var bootstrap = Read("issuer-bootstrap");
            var signerId = (string)CertificateCodec.ReadObject(bootstrap)["signerId"];
            byte[] seed = null;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vault = new AndroidJavaObject("in.surakshaxr.storage.IssuerVault", activity))
                {
                    var stored = vault.Call<string>("read");
                    seed = stored == null ? DemoSeedProtection.Decrypt(bootstrap) : Convert.FromBase64String(stored);
                    if (stored == null) vault.Call("write", Convert.ToBase64String(seed));
                }
#else
                seed = DemoSeedProtection.Decrypt(bootstrap);
#endif
                Signer = new Ed25519Crypto(signerId, seed);
                var trusted = Trust.Find(signerId);
                if (trusted == null || !Signer.GetPublicKey().SequenceEqual(Convert.FromBase64String(trusted.publicKeyB64))) throw new InvalidOperationException("Demo issuer mismatch; existing provisioning is preserved.");
            }
            finally { if (seed != null) Array.Clear(seed, 0, seed.Length); }
        }
        private static string Read(string name) => Resources.Load<TextAsset>("DemoProvisioning/" + name)?.text ?? throw new InvalidOperationException("Offline demo provisioning missing.");
    }
}
