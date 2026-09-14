//using System.Collections.Generic;
//using System.IO;
//using Unity.Entities;
//using Unity.Entities.Editor;
//using Unity.Scenes;
//using Unity.Scenes.Editor;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class SubSceneBatchConverter : EditorWindow
//{
//    [Header("Folders")]
//    public string scenesFolder = "Assets/SubScenesRaw"; // folder containing raw scene files
//    public string subScenesFolder = "Assets/SubScenesBaked"; // folder to save generated SubScenes
//    public string chunkDataJsonPath = "Assets/StreamingAssets/chunk_data.json";

//    [MenuItem("Tools/Convert Scenes to SubScenes")]
//    public static void ShowWindow()
//    {
//        var window = GetWindow<SubSceneBatchConverter>();
//        window.titleContent = new GUIContent("SubScene Converter");
//        window.Show();
//    }

//    void OnGUI()
//    {
//        GUILayout.Label("Scene to SubScene Batch Converter", EditorStyles.boldLabel);
//        scenesFolder = EditorGUILayout.TextField("Raw Scenes Folder", scenesFolder);
//        subScenesFolder = EditorGUILayout.TextField("SubScenes Folder", subScenesFolder);
//        chunkDataJsonPath = EditorGUILayout.TextField("Chunk Data JSON Path", chunkDataJsonPath);

//        if (GUILayout.Button("Convert All Scenes"))
//        {
//            ConvertAllScenes();
//        }
//    }

//    void ConvertAllScenes()
//    {
//        if (!Directory.Exists(scenesFolder))
//        {
//            Debug.LogError($"Scenes folder not found: {scenesFolder}");
//            return;
//        }
//        if (!Directory.Exists(subScenesFolder))
//        {
//            Directory.CreateDirectory(subScenesFolder);
//        }

//        var sceneFiles = Directory.GetFiles(scenesFolder, "*.unity", SearchOption.TopDirectoryOnly);
//        var chunkCollection = new ChunkSubSceneDataCollection();
//        chunkCollection.ecsChunks.Clear();
//        chunkCollection.colliderChunks.Clear();

//        foreach (var sceneFile in sceneFiles)
//        {
//            string scenePath = sceneFile.Replace("\\", "/");
//            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

//            // Open the scene temporarily
//            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

//            // Create subscene asset
//            string subScenePath = Path.Combine(subScenesFolder, sceneName + ".unity");
//            subScenePath = AssetDatabase.GenerateUniqueAssetPath(subScenePath);

//            GameObject root = new GameObject(sceneName + "_Root");
//            Scene rootScene = scene; // attach to the opened scene
//            EditorSceneManager.MoveGameObjectToScene(root, scene);

//            // Create SubScene
//            SubSceneUtility.ConvertGameObjectHierarchyToSubScene(root, subScenePath, true); // true = convert and include in build
//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();

//            // Get baked scene GUID
//            var entitySceneGUID = subSceneAsset.SceneGUID;
//            Debug.Log($"Created SubScene: {subScenePath}, GUID: {entitySceneGUID}");

//            // Add to chunk data
//            var chunkData = new ChunkSubSceneData()
//            {
//                coordinateX = 0, // you can set actual coordinates later
//                coordinateZ = 0,
//                subSceneName = sceneName,
//                subSceneGUID = entitySceneGUID.ToString()
//            };
//            chunkCollection.ecsChunks.Add(chunkData);

//            // Close the raw scene
//            EditorSceneManager.CloseScene(scene, true);
//        }

//        // Save JSON
//        string json = JsonUtility.ToJson(chunkCollection, true);
//        File.WriteAllText(chunkDataJsonPath, json);
//        AssetDatabase.Refresh();
//        Debug.Log($"All scenes converted to SubScenes. JSON saved at {chunkDataJsonPath}");
//    }
//}
