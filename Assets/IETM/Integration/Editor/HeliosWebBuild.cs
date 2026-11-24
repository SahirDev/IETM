// HELIOS / IETM - one-click WebGL build for desktop browsers and Unity Play (menu IETM > Build).
//   Apply WebGL Build Settings : the recommended Player settings (Brotli + decompression fallback, data caching,
//                                 1280x720, IL2CPP optimised for size, low stripping, linear colour, release build)
//                                 and HELIOS.unity as the only scene in the build.
//   Take WebGL Build            : applies the settings, saves the open scenes, builds to <project>/Builds/WebGL and
//                                 zips it to <project>/Builds/HELIOS_WebGL.zip (the file to upload to Unity Play).
//   Build And Run Locally       : the same build, then opens it in your browser from a local web server.
//   Open Build Folder           : shows <project>/Builds.
// Builds/ is ignored by git. The Web build support module must be installed (Unity Hub > Installs > Add modules).
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IETM.Integration.EditorTools
{
    public static class HeliosWebBuild
    {
        const string Scene = "Assets/HELIOS/Scenes/HELIOS.unity";
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string BuildsDir => Path.Combine(Root, "Builds");
        static string OutDir => Path.Combine(BuildsDir, "WebGL");
        static string ZipPath => Path.Combine(BuildsDir, "HELIOS_WebGL.zip");

        [MenuItem("IETM/Build/Apply WebGL Build Settings (Unity Play)", priority = 100)]
        public static void ApplySettings() { Apply(); Debug.Log("[HELIOS Build] WebGL settings applied (Brotli + fallback, caching, 1280x720, IL2CPP size, release). Scene: " + Scene); }

        [MenuItem("IETM/Build/Take WebGL Build (Unity Play)", priority = 101)]
        public static void TakeBuild() { Build(false); }

        [MenuItem("IETM/Build/Build And Run Locally", priority = 102)]
        public static void BuildAndRun() { Build(true); }

        [MenuItem("IETM/Build/Open Build Folder", priority = 103)]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(BuildsDir);
            EditorUtility.RevealInFinder(BuildsDir);
        }

        static bool Apply()
        {
            if (!File.Exists(Path.Combine(Root, Scene)))
            {
                EditorUtility.DisplayDialog("HELIOS Build", "Scene not found: " + Scene, "OK");
                return false;
            }
            // scenes: HELIOS only
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };

            // player
            var web = NamedBuildTarget.WebGL;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Low);      // Medium is smaller: test the buttons if you try it
            PlayerSettings.SetIl2CppCodeGeneration(web, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(web, Il2CppCompilerConfiguration.Release);

            // publishing (Unity Play: the server headers cannot be set, so the loader decompresses itself)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.WebGL.showDiagnostics = false;

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>The build folder's contents (index.html at the top of the zip) with the system's own zip tool.</summary>
        static void Zip(string dir, string zip)
        {
            var p = new System.Diagnostics.ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = dir };
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                p.FileName = "powershell";
                p.Arguments = "-NoProfile -Command \"Compress-Archive -Path '" + dir + "\\*' -DestinationPath '" + zip + "' -Force\"";
            }
            else { p.FileName = "/usr/bin/zip"; p.Arguments = "-r -q \"" + zip + "\" ."; }
            using (var proc = System.Diagnostics.Process.Start(p)) proc.WaitForExit(10 * 60 * 1000);
        }

        static void Build(bool run)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("HELIOS Build", "Web (WebGL) build support is not installed for this Unity version.\n\nUnity Hub > Installs > (this version) > Add modules > Web Build Support.", "OK");
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUtility.DisplayDialog("HELIOS Build", "The project will switch to the Web platform (assets are re-imported once; this can take a few minutes). Continue?", "Switch and build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Apply()) return;

            Directory.CreateDirectory(BuildsDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("HELIOS Build", "Build " + s.result + " (" + s.totalErrors + " errors). See the Console.", "OK");
                return;
            }

            string zipNote = "";
            try
            {
                if (File.Exists(ZipPath)) File.Delete(ZipPath);
                Zip(OutDir, ZipPath);
                zipNote = File.Exists(ZipPath)
                    ? "\nZip for Unity Play: " + ZipPath + " (" + (new FileInfo(ZipPath).Length / (1024f * 1024f)).ToString("0.0") + " MB)"
                    : "\n(zip not made: zip the WebGL folder's contents yourself)";
            }
            catch (System.Exception e) { zipNote = "\n(zip not made: " + e.Message + " - zip the WebGL folder's contents yourself)"; }

            string msg = "Build succeeded in " + s.totalTime.ToString(@"mm\:ss") + ": " + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB\n" + OutDir + zipNote;
            Debug.Log("[HELIOS Build] " + msg.Replace("\n", "  |  "));
            if (!run && EditorUtility.DisplayDialog("HELIOS Build", msg + "\n\nUpload the zip on Unity Play (or use the WebGL Publisher package).", "Open folder", "Close"))
                EditorUtility.RevealInFinder(File.Exists(ZipPath) ? ZipPath : OutDir);
        }
    }
}
