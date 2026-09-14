using UnityEngine;
using UnityEngine.UI;

public class DataHolder : MonoBehaviour
{
    public static DataHolder Instance;
    public float sensitivity = 1.0f;
    public float gyroSensitivity = 1.0f;
    public float volume = 1.0f;
    public string playerTag = "PlayerCollider";
    public Image defaultImage;
    public Transform playerTrasnfrom;

    public GameObject[] shipPrefabs;
    public GameObject[] projectilePrefabs;
    public GameObject[] muzzleFlashPrefab;
    public GameObject[] impactEffectPrefab;

    public Ship[] avaliableShips;
    public Engine[] avaliableEngines;
    public Shield[] avaliableShields;
    public Pilot[] avaliablePilots;
    public RepairModule[] repairModules;
    public Radar[] avaliableRadars;
    public CannonSlot[] avaliableCannons;
    public LauncherSlot[] avaliableLaunchers;
    public Sprite[] icons;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
