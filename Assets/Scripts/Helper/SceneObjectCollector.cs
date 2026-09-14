//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine.SceneManagement;
//using System.IO;

//public class SceneObjectsCollector : EditorWindow
//{
//    [Header("Folder containing scenes")]
//    public string sceneFolderPath = "Assets/Scenes";

//    [Header("Parent Object to collect all objects under")]
//    public GameObject parentObject;

//    [MenuItem("Tools/Collect Scene Objects")]
//    public static void ShowWindow()
//    {
//        GetWindow<SceneObjectsCollector>("Scene Objects Collector");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Collect all objects from scenes into a single parent", EditorStyles.boldLabel);

//        sceneFolderPath = EditorGUILayout.TextField("Scene Folder Path", sceneFolderPath);
//        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);

//        if (GUILayout.Button("Collect Objects"))
//        {
//            if (parentObject == null)
//            {
//                Debug.LogError("Please assign a Parent Object first!");
//                return;
//            }
//            CollectAllSceneObjects();
//        }
//    }

//    private void CollectAllSceneObjects()
//    {
//        string[] scenePaths = Directory.GetFiles(sceneFolderPath, "*.unity", SearchOption.AllDirectories);

//        foreach (string scenePath in scenePaths)
//        {
//            // Open the scene additively
//            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
//            GameObject[] rootObjects = scene.GetRootGameObjects();

//            // Compute the dynamic root name to ignore
//            string sceneName = Path.GetFileNameWithoutExtension(scenePath); // e.g., "C_6_11"
//            string ignoreRootName = sceneName.Replace("C_", "ChunkRoot_"); // e.g., "ChunkRoot_6_11"

//            foreach (GameObject root in rootObjects)
//            {
//                if (root.name == ignoreRootName)
//                {
//                    // Only move its children
//                    foreach (Transform child in root.transform)
//                    {
//                        child.SetParent(parentObject.transform, true);
//                    }
//                }
//                else
//                {
//                    root.transform.SetParent(parentObject.transform, true);
//                }
//            }

//            // Close the scene after collecting
//            EditorSceneManager.CloseScene(scene, true);
//        }

//        Debug.Log("Finished collecting all objects from scenes.");
//    }
//}
