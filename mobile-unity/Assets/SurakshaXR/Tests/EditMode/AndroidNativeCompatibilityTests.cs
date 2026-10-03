using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace SurakshaXR.Tests
{
    public sealed class AndroidNativeCompatibilityTests
    {
        // Check real pinned plug-in binaries, not just architecture setting flags:
        // an APK can install yet fail to load AR when one provider library is absent.
        [TestCase("armeabi-v7a")]
        [TestCase("arm64-v8a")]
        public void PinnedArCoreIncludesEveryNativeProviderForPhoneAbi(string abi)
        {
            var package = PackageInfo.GetAllRegisteredPackages().Single(p => p.name == "com.unity.xr.arcore");
            var android = Path.Combine(package.resolvedPath, "Runtime", "Android");
            RequireNativeLibrary(Path.Combine(android, "UnityARCore.aar"), abi, "libUnityARCore.so");
            RequireNativeLibrary(Path.Combine(android, "ARPresto.aar"), abi, "libarpresto_api.so");
            RequireNativeLibrary(Path.Combine(android, "arcore_client.aar"), abi, "libarcore_sdk_c.so");
            RequireNativeLibrary(Path.Combine(android, "arcore_client.aar"), abi, "libarcore_sdk_jni.so");
        }

        private static void RequireNativeLibrary(string archive, string abi, string library)
        {
            Assert.That(File.Exists(archive), Is.True, archive);
            using (var zip = ZipFile.OpenRead(archive)) {
                var entry = zip.GetEntry("jni/" + abi + "/" + library);
                Assert.That(entry, Is.Not.Null, archive + " is missing " + abi + "/" + library);
                Assert.That(entry.Length, Is.GreaterThan(0));
            }
        }
    }
}
