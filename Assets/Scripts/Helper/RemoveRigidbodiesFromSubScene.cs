#if UNITY_EDITOR
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class RemoveRigidbodiesFromSubScene : MonoBehaviour
{
    [ContextMenu("ECS/Remove All Rigidbodies From This SubScene")]
    private void RemoveAllRigidbodies()
    {
        var subScene = GetComponent<SubScene>();
        if (subScene == null)
        {
            Debug.LogError("❌ Thi GameObject doesn't have a SubScene component.");
            return;
        }

        // Get SubScene path (correct method)
        string scenePath = AssetDatabase.GetAssetPath(subScene.SceneAsset);
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("❌ Could not find SubScene asset path.");
            return;
        }

        // Load SubScene if not open
        Scene scene = subScene.EditingScene;
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        int removedCount = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var rbs = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                Undo.DestroyObjectImmediate(rb);
                removedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"✅ Removed {removedCount} Rigidbody components from SubScene '{subScene.name}'.");
    }
}
#endif
