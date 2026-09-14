//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using System.IO;

//[InitializeOnLoad]
//public static class AutoSceneBackup
//{
//    static AutoSceneBackup()
//    {
//        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
//    }

//    private static void OnPlayModeStateChanged(PlayModeStateChange state)
//    {
//        // Trigger before entering play mode
//        if (state == PlayModeStateChange.ExitingEditMode)
//        {
//            SaveAndBackupScene();
//        }
//    }

//    private static void SaveAndBackupScene()
//    {
//        var scene = EditorSceneManager.GetActiveScene();

//        if (!scene.isDirty && scene.path != "")
//        {
//            Debug.Log("Scene not modified — skipping auto backup.");
//            return;
//        }

//        // Save the current scene
//        if (!EditorSceneManager.SaveScene(scene))
//        {
//            Debug.LogError("Failed to save scene before Play Mode.");
//            return;
//        }

//        // Create backup path
//        string originalPath = scene.path;
//        string directory = Path.GetDirectoryName(originalPath);
//        string filename = Path.GetFileNameWithoutExtension(originalPath);
//        string extension = Path.GetExtension(originalPath);

//        string timeStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
//        string backupPath = Path.Combine(directory, filename + "_Backup_" + timeStamp + extension);

//        // Duplicate scene file
//        File.Copy(originalPath, backupPath, true);

//        Debug.Log($"Scene auto-saved and backed up:\n{backupPath}");
//    }
//}
