// Assets/Editor/BatchSetDamageHandlerNames.cs
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BatchSetDamageHandlerNames
{
    // ---- Adjust if you only want the FIRST DamageHandler updated:
    // private const bool UPDATE_ALL_DAMAGE_HANDLERS = true;
    // Was: private const bool UPDATE_ALL_DAMAGE_HANDLERS = true;
    private static bool UPDATE_ALL_DAMAGE_HANDLERS = true; // not const => both branches remain reachable


    [MenuItem("Assets/EditorScripts/Set DamageHandler displayName from Targetable (Same Folder)")]
    private static void SetForFolderOfSelectedPrefab()
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No selection", "Select a prefab asset first.", "OK");
            return;
        }

        var selectedPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(selectedPath) || Path.GetExtension(selectedPath).ToLower() != ".prefab")
        {
            EditorUtility.DisplayDialog("Not a prefab", "Please right-click a prefab asset.", "OK");
            return;
        }

        var folder = Path.GetDirectoryName(selectedPath).Replace("\\", "/");
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

        int processed = 0;
        int updated = 0;
        int skippedNoTargetable = 0;
        int skippedNoDamageHandler = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Batch Update", "Collecting prefabs…", 0f);

            // Limit strictly to the same folder (exclude subfolders)
            var prefabPaths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace("\\", "/") == folder)
                .ToList();

            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string path = prefabPaths[i];
                float p = (i + 1f) / prefabPaths.Count;
                EditorUtility.DisplayProgressBar("Batch Update", $"Processing: {Path.GetFileName(path)}", p);

                bool didUpdate = ProcessSinglePrefab(path, out bool noTargetable, out bool noDamageHandler);

                processed++;
                if (didUpdate) updated++;
                if (noTargetable) skippedNoTargetable++;
                if (noDamageHandler) skippedNoDamageHandler++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "Batch Update Complete",
            $"Folder: {folder}\n" +
            $"Prefabs processed: {processed}\n" +
            $"Prefabs updated: {updated}\n" +
            $"Skipped (no Targetable found): {skippedNoTargetable}\n" +
            $"Skipped (no DamageHandler found): {skippedNoDamageHandler}",
            "OK"
        );
    }

    // Enables the menu item only when a prefab asset is selected
    [MenuItem("Assets/EditorScripts/Set DamageHandler displayName from Targetable (Same Folder)", true)]
    private static bool ValidateSetForFolderOfSelectedPrefab()
    {
        var selected = Selection.activeObject;
        if (selected == null) return false;

        var path = AssetDatabase.GetAssetPath(selected);
        return !string.IsNullOrEmpty(path) && Path.GetExtension(path).ToLower() == ".prefab";
    }

    /// <summary>
    /// Loads a prefab’s contents, finds the GameObject with Targetable, takes its name,
    /// and writes that into DamageHandler.displayName on children.
    /// </summary>
    private static bool ProcessSinglePrefab(string prefabPath, out bool noTargetable, out bool noDamageHandler)
    {
        noTargetable = false;
        noDamageHandler = false;
        bool didUpdate = false;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var targetable = root.GetComponentInChildren<Targetable>(true);
            if (targetable == null)
            {
                noTargetable = true;
                return false;
            }

            string targetName = targetable.gameObject.name;
            var handlers = root.GetComponentsInChildren<DamageHandler>(true);

            if (handlers == null || handlers.Length == 0)
            {
                noDamageHandler = true;
                return false;
            }

            if (UPDATE_ALL_DAMAGE_HANDLERS)
            {
                foreach (var dh in handlers)
                {
                    if (dh == null) continue;
                    Undo.RecordObject(dh, "Set DamageHandler displayName");
                    dh.displayName = targetName;
                    EditorUtility.SetDirty(dh);
                    didUpdate = true;
                }
            }
            else
            {
                // only update the first one
                var dh = handlers.FirstOrDefault(h => h != null);
                if (dh != null)
                {
                    Undo.RecordObject(dh, "Set DamageHandler displayName");
                    dh.displayName = targetName;
                    EditorUtility.SetDirty(dh);
                    didUpdate = true;
                }
                else
                {
                    noDamageHandler = true;
                }
            }

            if (didUpdate)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return didUpdate;
    }

}
#endif

// NOTE:
// - Assumes classes `Targetable` and `DamageHandler` exist, and `DamageHandler` has a public field or property `displayName`.
// - If `displayName` is a serialized field with a property wrapper, ensure it sets the serialized backing field.
