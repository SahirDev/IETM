// Download / open the printable Illustrated Parts Catalog (PDF) kept in StreamingAssets.
using System;
using System.IO;
using UnityEngine;

namespace IETM
{
    public partial class IETMApp
    {
        public void DownloadIpdPdf() => DownloadFile(ipdPdfFile);

        /// <summary>Downloads / opens a file kept under StreamingAssets (path relative to it).</summary>
        public void DownloadFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) { Toast("No file configured", Theme.Warn); return; }
            string src = Path.Combine(Application.streamingAssetsPath, relativePath);
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.OpenURL(src);                 // browser opens/downloads it
            Toast("Opening " + Path.GetFileName(src) + "…", Theme.Ok);
#elif UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL(src);
#else
            try
            {
                if (!File.Exists(src)) { Toast("File not found: StreamingAssets/" + relativePath, Theme.Danger); return; }
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(dir)) dir = Application.persistentDataPath;
                string dst = Path.Combine(dir, Path.GetFileName(src));
                File.Copy(src, dst, true);
                Application.OpenURL(new Uri(dst).AbsoluteUri);
                Toast("Saved to " + dst, Theme.Ok);
            }
            catch (Exception e)
            {
                Debug.LogWarning("IETM: PDF copy failed, opening in place. " + e.Message);
                Application.OpenURL(new Uri(src).AbsoluteUri);
            }
#endif
        }
    }
}
