//// ScriptableObject to store chunk data
//using System.Collections.Generic;
//using UnityEngine;

//[CreateAssetMenu(fileName = "ChunkSubSceneData", menuName = "Chunk System/Chunk SubScene Data")]
//public class ChunkSubSceneDataAsset : ScriptableObject
//{
//    public List<ChunkSubSceneData> chunkSubScenes = new List<ChunkSubSceneData>();
//    public float chunkSize = 1000f;
//    public string subScenePrefix = "Chunk_";

//    public Dictionary<Vector2Int, (string name, string guid)> GetChunkDictionary()
//    {
//        Dictionary<Vector2Int, (string, string)> dict = new Dictionary<Vector2Int, (string, string)>();
//        foreach (var chunk in chunkSubScenes)
//        {
//            dict[chunk.coordinate] = (chunk.subSceneName, chunk.subSceneGUID);
//        }
//        return dict;
//    }

//    public void SetChunkData(Dictionary<Vector2Int, (string name, string guid)> chunks, float size, string prefix)
//    {
//        chunkSubScenes.Clear();
//        chunkSize = size;
//        subScenePrefix = prefix;

//        foreach (var kvp in chunks)
//        {
//            chunkSubScenes.Add(new ChunkSubSceneData
//            {
//                coordinate = kvp.Key,
//                subSceneName = kvp.Value.name,
//                subSceneGUID = kvp.Value.guid
//            });
//        }
//    }
//}
