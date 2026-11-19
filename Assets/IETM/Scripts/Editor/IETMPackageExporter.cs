// IETM > Export IETM Package (.unitypackage): everything the Landing Gear IETM scene needs, in one file,
// to hand to a colleague. Unity packages from the Package Manager (glTFast, Input System, URP) are NOT
// inside a .unitypackage - see Assets/IETM/INSTALL.md for what the other project needs.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IETM.EditorTools
{
    public static class IETMPackageExporter
    {
        static readonly string[] Paths =
        {
            "Assets/IETM",                        // scripts, shaders, UI, data (JSON), scenes, README / INSTALL
            "Assets/3DModels/Landing_Gear 1.glb", // the animated landing gear model used by the scene
            "Assets/StreamingAssets/IETM",        // printable IPC (PDF)
        };

        [MenuItem("IETM/Export IETM Package (.unitypackage)", priority = 80)]
        public static void Export()
        {
            var existing = new List<string>();
            foreach (var p in Paths)
                if (AssetDatabase.IsValidFolder(p) || File.Exists(p)) existing.Add(p);
                else Debug.LogWarning("[IETM] Not found, skipped: " + p);

            string file = EditorUtility.SaveFilePanel("Export IETM package", "", "HELIOS_IETM_LandingGear.unitypackage", "unitypackage");
            if (string.IsNullOrEmpty(file)) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage(existing.ToArray(), file, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
            Debug.Log("[IETM] Exported " + file + "\nThe other project also needs: glTFast 6.18.0, Input System, URP (see Assets/IETM/INSTALL.md).");
        }
    }
}
