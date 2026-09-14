#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BatchComponentRemoverOrAdderWindow : EditorWindow
{
    private enum Mode
    {
        Add,
        Remove
    }

    [SerializeField] private Mode mode = Mode.Add;

    [Tooltip("Drag ANY instance of the component you want to add/remove.\n" +
             "For example, drag a prefab or scene object that has that component.")]
    [SerializeField]
    private Component referenceComponent;

    [Tooltip("Folder that contains the prefabs to process.\n" +
             "Drag a folder from the Project window here.")]
    [SerializeField]
    private UnityEngine.Object folderObject; // should be a folder asset

    [Tooltip("If true, also processes prefabs in all subfolders.")]
    [SerializeField]
    private bool includeSubfolders = false;

    [Tooltip("If not empty, only prefabs whose name contains this substring will be processed.")]
    [SerializeField]
    private string nameContainsFilter = "";

    // ------------------ MENU ------------------

    [MenuItem("Assets/EditorScripts/Batch Add/Remove Component On Prefabs...")]
    private static void ShowWindowFromAssets()
    {
        var window = GetWindow<BatchComponentRemoverOrAdderWindow>("Batch Component Tool");
        window.Show();
    }

    // ------------------ GUI ------------------

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Component Remover / Adder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Determine base folder: first from field, then from selection
        string baseFolder = GetFolderPathFromField();
        if (string.IsNullOrEmpty(baseFolder))
        {
            baseFolder = GetBaseFolderFromSelection();
        }

        EditorGUILayout.LabelField("Base Folder:",
            string.IsNullOrEmpty(baseFolder) ? "<none / invalid>" : baseFolder);

        EditorGUILayout.Space();

        // Folder field
        folderObject = EditorGUILayout.ObjectField(
            new GUIContent("Target Folder",
                "Drag a folder from the Project window.\n" +
                "If left empty, the tool will try to use the currently selected folder/prefab."),
            folderObject,
            typeof(UnityEngine.Object),
            false
        );

        EditorGUILayout.Space();

        // Mode
        mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);

        // Reference component
        referenceComponent = (Component)EditorGUILayout.ObjectField(
            new GUIContent("Reference Component",
                "Drag ANY object that has the component you want to add/remove.\n" +
                "Only the TYPE of this component is used."),
            referenceComponent,
            typeof(Component),
            true // allow scene objects
        );

        if (referenceComponent != null)
        {
            EditorGUILayout.LabelField("Component Type:", referenceComponent.GetType().Name);
        }

        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        nameContainsFilter = EditorGUILayout.TextField("Name Contains Filter", nameContainsFilter);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "HOW TO USE:\n" +
            "1. (Optional) In Project window, select a prefab asset OR a folder.\n" +
            "2. Open this via Assets > EditorScripts > Batch Add/Remove...\n" +
            "3. Set Target Folder by dragging a folder, OR rely on the current selection.\n" +
            "4. Drag a Reference Component (from a prefab or scene object).\n" +
            "5. Choose Add or Remove.\n" +
            "6. Optionally toggle Include Subfolders and/or set a name filter.\n" +
            "7. Click 'Run On Prefabs In Folder'.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        bool canRun = !string.IsNullOrEmpty(baseFolder)
                      && AssetDatabase.IsValidFolder(baseFolder)
                      && referenceComponent != null;

        using (new EditorGUI.DisabledScope(!canRun))
        {
            if (GUILayout.Button("Run On Prefabs In Folder"))
            {
                RunOnFolder(baseFolder);
            }
        }
    }

    // ------------------ FOLDER HELPERS ------------------

    private string GetFolderPathFromField()
    {
        if (folderObject == null) return null;

        string path = AssetDatabase.GetAssetPath(folderObject);
        if (string.IsNullOrEmpty(path)) return null;

        return AssetDatabase.IsValidFolder(path) ? path : null;
    }

    private string GetBaseFolderFromSelection()
    {
        var selected = Selection.activeObject;
        if (selected == null) return null;

        string selectedPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(selectedPath)) return null;

        // Folder selected -> use that
        if (AssetDatabase.IsValidFolder(selectedPath))
        {
            return selectedPath;
        }

        // Prefab selected -> use its containing folder
        if (Path.GetExtension(selectedPath).Equals(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(selectedPath)?.Replace("\\", "/");
        }

        return null;
    }

    // ------------------ CORE LOGIC ------------------

    private void RunOnFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "Please assign a valid folder in 'Target Folder' or select one in the Project window.",
                "OK"
            );
            return;
        }

        if (referenceComponent == null)
        {
            EditorUtility.DisplayDialog(
                "No Reference Component",
                "Please drag a reference component into the 'Reference Component' field.",
                "OK"
            );
            return;
        }

        Type targetType = referenceComponent.GetType();
        if (targetType == null || !typeof(Component).IsAssignableFrom(targetType))
        {
            EditorUtility.DisplayDialog(
                "Invalid Reference",
                "The object you assigned does not provide a valid Component type.",
                "OK"
            );
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        var prefabPaths = guids.Select(AssetDatabase.GUIDToAssetPath);

        // If not including subfolders, restrict strictly to this folder (no children)
        if (!includeSubfolders)
        {
            prefabPaths = prefabPaths.Where(p =>
                Path.GetDirectoryName(p)?.Replace("\\", "/") == folder);
        }

        // Apply name filter if provided
        if (!string.IsNullOrEmpty(nameContainsFilter))
        {
            prefabPaths = prefabPaths.Where(p =>
                Path.GetFileNameWithoutExtension(p)
                    .IndexOf(nameContainsFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        var pathsList = prefabPaths.ToList();
        if (pathsList.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No Prefabs Found",
                "No prefabs matched the current folder / subfolder / filter settings.",
                "OK"
            );
            return;
        }

        int processed = 0;
        int modified = 0;
        int skippedAlreadyHad = 0;
        int skippedMissingComponent = 0;

        try
        {
            EditorUtility.DisplayProgressBar(
                "Batch Component Tool",
                "Processing prefabs…",
                0f
            );

            for (int i = 0; i < pathsList.Count; i++)
            {
                string path = pathsList[i];
                float progress = (i + 1f) / pathsList.Count;
                EditorUtility.DisplayProgressBar(
                    "Batch Component Tool",
                    $"Processing: {Path.GetFileName(path)}",
                    progress
                );

                bool didModify = ProcessSinglePrefab(
                    path,
                    targetType,
                    ref skippedAlreadyHad,
                    ref skippedMissingComponent
                );

                processed++;
                if (didModify) modified++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "Batch Component Tool Complete",
            $"Folder: {folder}\n" +
            $"Mode: {mode}\n" +
            $"Component Type: {targetType.Name}\n\n" +
            $"Prefabs processed: {processed}\n" +
            $"Prefabs modified: {modified}\n" +
            $"Skipped (already had component when adding): {skippedAlreadyHad}\n" +
            $"Skipped (no component found when removing): {skippedMissingComponent}",
            "OK"
        );
    }

    private bool ProcessSinglePrefab(
        string prefabPath,
        Type targetType,
        ref int skippedAlreadyHad,
        ref int skippedMissingComponent)
    {
        bool didModify = false;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            if (mode == Mode.Add)
            {
                var existing = root.GetComponent(targetType);
                if (existing != null)
                {
                    skippedAlreadyHad++;
                    return false;
                }

                Undo.AddComponent(root, targetType);
                didModify = true;
            }
            else // Mode.Remove
            {
                var comps = root.GetComponentsInChildren(targetType, true);
                if (comps == null || comps.Length == 0)
                {
                    skippedMissingComponent++;
                    return false;
                }

                foreach (var c in comps)
                {
                    if (c == null) continue;
                    Undo.DestroyObjectImmediate(c);
                    didModify = true;
                }
            }

            if (didModify)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return didModify;
    }
}
#endif
