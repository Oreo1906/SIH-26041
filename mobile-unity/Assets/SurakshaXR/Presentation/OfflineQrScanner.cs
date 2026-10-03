using System;
using SurakshaXR.Security;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    public sealed class OfflineQrScanner : IDisposable
    {
        public WebCamTexture Camera { get; private set; }
        private float nextDecode;
        public void Start()
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) throw new InvalidOperationException("No camera available.");
            var chosen = devices[0]; foreach (var device in devices) if (!device.isFrontFacing) { chosen = device; break; }
            Camera = new WebCamTexture(chosen.name, 960, 720, 15); Camera.Play();
        }
        public string Read()
        {
            if (Camera == null || !Camera.isPlaying || !Camera.didUpdateThisFrame || Camera.width <= 16 || Time.realtimeSinceStartup < nextDecode) return null;
            nextDecode = Time.realtimeSinceStartup + .6f;
            var colors = Camera.GetPixels32(); var bytes = new byte[colors.Length * 4];
            for (var i = 0; i < colors.Length; i++) { bytes[i * 4] = colors[i].r; bytes[i * 4 + 1] = colors[i].g; bytes[i * 4 + 2] = colors[i].b; bytes[i * 4 + 3] = 255; }
            return CertificateCodec.DecodeQr(bytes, Camera.width, Camera.height);
        }
        public void Dispose() { if (Camera != null) { Camera.Stop(); UnityEngine.Object.Destroy(Camera); Camera = null; } }
    }
}
