using UnityEngine;

public class ShowShipInMenu : MonoBehaviour
{
    [Header("Where to place the ship")]
    public Transform spawnAnchor;

    [Header("Prefs / Data")]
    [Tooltip("PlayerPrefs key that stores the selected ship name.")]
    public string selectedShipKey = "Selected_Ship_Name";

    [Tooltip("Fallback to this index if no prefs found or not matched.")]
    public int fallbackIndex = 0;

    private GameObject _spawned;

    void Start()
    {
        RefreshShip();
    }

    /// <summary>
    /// Destroys previous instance (if any) and spawns the currently selected ship.
    /// </summary>
    public void RefreshShip()
    {
        // Cleanup previous instance
        if (_spawned != null)
        {
            Destroy(_spawned);
            _spawned = null;
        }

        

        if (DataHolder.Instance == null || DataHolder.Instance.shipPrefabs == null || DataHolder.Instance.shipPrefabs.Length == 0)
        {
            Debug.LogError("[ShowShipInMenu] DataHolder.Instance.shipPrefabs is missing or empty.");
            return;
        }

        // 1) Get selected ship name from PlayerPrefs
        string selectedName = PlayerPrefs.GetString(selectedShipKey, string.Empty);

        // 2) Find the matching prefab by Ship._name
        GameObject[] list = DataHolder.Instance.shipPrefabs;
        GameObject prefabToSpawn = null;

        if (!string.IsNullOrEmpty(selectedName))
        {
            for (int i = 0; i < list.Length; i++)
            {
                var ship = list[i] != null ? list[i].GetComponent<Ship>() : null;
                if (ship != null && ship._name == selectedName)
                {
                    prefabToSpawn = list[i];
                    break;
                }
            }
        }

        // 3) Fallback if not found
        if (prefabToSpawn == null)
        {
            int idx = Mathf.Clamp(fallbackIndex, 0, list.Length - 1);
            prefabToSpawn = list[idx];
            var ship = prefabToSpawn != null ? prefabToSpawn.GetComponent<Ship>() : null;
            if (ship != null)
            {
                PlayerPrefs.SetString(selectedShipKey, ship._name);
                PlayerPrefs.Save();
            }
            Debug.LogWarning($"[ShowShipInMenu] Selected ship '{selectedName}' not found. Using fallback index {idx}.");
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("[ShowShipInMenu] No valid prefab to spawn.");
            return;
        }

        // 4) Instantiate and align under anchor
        _spawned = Instantiate(prefabToSpawn, spawnAnchor);
        _spawned.name = prefabToSpawn.name + "_MenuDisplay";
        _spawned.transform.localPosition = Vector3.zero;
        _spawned.transform.localRotation = Quaternion.identity;
        _spawned.transform.localScale = Vector3.one;

        // 5) Disable all behaviour scripts and cameras
        foreach (var mb in _spawned.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            mb.enabled = false;
        }

        foreach (var cam in _spawned.GetComponentsInChildren<Camera>(true))
            cam.enabled = false;
        foreach (var particle in _spawned.GetComponentsInChildren<ParticleSystem>(true))
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var audio in _spawned.GetComponentsInChildren<AudioSource>(true))
            audio.enabled = false;


        Debug.Log($"[ShowShipInMenu] Spawned ship '{_spawned.name}' at menu anchor.");
    }
}
