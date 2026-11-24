// HELIOS / IETM - one-click WebGL build for desktop browsers and Unity Play (menu IETM > Build).
//   Apply WebGL Build Settings : the recommended Player settings (no compression, plain file names, data caching,
//                                 1280x720, IL2CPP optimised for size, low stripping, linear colour, release build)
//                                 and HELIOS.unity as the only scene in the build.
//   Take WebGL Build            : applies the settings, saves the open scenes and builds to
//                                 <project>/Builds/webgl_<Version>/ - Build/webgl_<Version>.data, .framework.js,
//                                 .loader.js, .wasm (uncompressed, so any web server can host them) + index.html;
//                                 Version = Player Settings > Version (e.g. 1.1). Also zipped next to it.
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
        // the folder name becomes the file names: Build/webgl_1.1.data, webgl_1.1.framework.js, ...
        static string BuildName => "webgl_" + (string.IsNullOrEmpty(PlayerSettings.bundleVersion) ? "1.0" : PlayerSettings.bundleVersion.Trim());
        static string OutDir => Path.Combine(BuildsDir, BuildName);
        static string ZipPath => Path.Combine(BuildsDir, BuildName + ".zip");

        [MenuItem("IETM/Build/Apply WebGL Build Settings", priority = 100)]
        public static void ApplySettings() { Apply(); Debug.Log("[HELIOS Build] WebGL settings applied (uncompressed, plain file names " + BuildName + ".*, caching, 1280x720, IL2CPP size, release). Scene: " + Scene); }

        [MenuItem("IETM/Build/Take WebGL Build", priority = 101)]
        public static void TakeBuild() { Build(false); }

        [MenuItem("IETM/Build/Build And Run Locally", priority = 102)]
        public static void BuildAndRun() { Build(true); }

        [MenuItem("IETM/Build/Quick Test Build And Run (faster, not for release)", priority = 110)]
        public static void QuickTest() { Build(true, quick: true); }

        [MenuItem("IETM/Build/Open Build Folder", priority = 103)]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(BuildsDir);
            EditorUtility.RevealInFinder(BuildsDir);
        }

        static bool Apply(bool quick = false)
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
            // quick test builds: IL2CPP Debug + "Shorter Build Time" code optimisation (several times faster to build,
            // bigger and a little slower to run); release builds: Release + "Disk Size"
            PlayerSettings.SetIl2CppCompilerConfiguration(web, quick ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
            SetWasmCodeOptimization(quick ? "BuildTimes" : "DiskSize");

            // publishing: uncompressed .data / .framework.js / .wasm with plain names (no .br / .gz / .unityweb, no hashes),
            // so any web server or page can host them without Content-Encoding headers
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
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

        /// <summary>File > Build Profiles > Web > Code Optimization, set by reflection (the type only exists when Web
        /// support is installed, so a direct reference would not compile without it).</summary>
        static void SetWasmCodeOptimization(string value)
        {
            try
            {
                var t = System.Type.GetType("UnityEditor.WebGL.UserBuildSettings, UnityEditor.WebGL.Extensions");
                var prop = t != null ? t.GetProperty("codeOptimization", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) : null;
                if (prop == null) return;
                var v = System.Enum.Parse(prop.PropertyType, value);
                prop.SetValue(null, v);
            }
            catch (System.Exception e) { Debug.Log("[HELIOS Build] Code Optimization not set (" + e.Message + "); set it in File > Build Profiles if needed."); }
        }

        static void Build(bool run, bool quick = false)
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
            if (!Apply(quick)) return;

            Directory.CreateDirectory(BuildsDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = quick ? OutDir + "_test" : OutDir,          // a test build never overwrites the release one
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
            if (!quick) try
            {
                if (File.Exists(ZipPath)) File.Delete(ZipPath);
                Zip(OutDir, ZipPath);
                zipNote = File.Exists(ZipPath)
                    ? "\nZip for Unity Play: " + ZipPath + " (" + (new FileInfo(ZipPath).Length / (1024f * 1024f)).ToString("0.0") + " MB)"
                    : "\n(zip not made: zip the WebGL folder's contents yourself)";
            }
            catch (System.Exception e) { zipNote = "\n(zip not made: " + e.Message + " - zip the WebGL folder's contents yourself)"; }

            string msg = (quick ? "Quick test build" : "Build") + " succeeded in " + s.totalTime.ToString(@"mm\:ss") + ": " + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB\n" + (quick ? OutDir + "_test" : OutDir) + zipNote;
            Debug.Log("[HELIOS Build] " + msg.Replace("\n", "  |  "));
            if (!run && EditorUtility.DisplayDialog("HELIOS Build", msg + "\n\nFiles: Build/" + BuildName + ".data, .framework.js, .loader.js, .wasm", "Open folder", "Close"))
                EditorUtility.RevealInFinder(Path.Combine(OutDir, "Build"));
        }
    }
}
