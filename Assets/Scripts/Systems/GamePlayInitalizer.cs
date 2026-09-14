using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GamePlayInitalizer : MonoBehaviour
{
    public Vector3 spawnPoint;
    public Camera mainCamera;
    public GameObject hud;
    public LoadingScreen loadingScreen;
    public SpeedSlider brakeSlider;
    public FireManager fireManager;
    public MissileLockUI missileLockUI;
    public CannonLockUI cannonLockUI;
    public BarsUI_Bridge barsUI_Bridge;
    public AmmoUI ammoUI;
    public ParticleSpeedController particleSpeedController;
    public SpeedDisplayUI speedDisplayUI;
    public TargetUI targetUI;
    public GameObject enemyWaveSpawner;
    public ChunkSubSceneManager chunkSceneManager1;
    private GyroController gyro;

    public SensitivityManager sensitivityManager;
    public SoundManager soundManager;
    public UIGyroTiltController uIGyroTiltController;

    [Header("UI Elements")]
    public Slider musicVolume;
    public Slider soundVolume;



    private GameObject playerShipGO;   // <- hold the spawned instance
    private Ship playerShip;            // convenience

    public void Start()
    {
        loadingScreen.gameObject.SetActive(true);
        Time.timeScale = 0f;
        loadingScreen.Loaded(25);
        // Spawn the ship and work with the instance
        playerShipGO = Instantiate(DataHolder.Instance.shipPrefabs[PlayerPrefs.GetInt("Hanger_shipIndex", 0)], spawnPoint, Quaternion.identity);
        playerShipGO.SetActive(false);

        playerShip = playerShipGO.GetComponent<Ship>();
        playerShip.LoadData();
        playerShip.Initalize();
        loadingScreen.Loaded(35);
        DataHolder.Instance.playerTrasnfrom = playerShipGO.transform;


        EquipmentManager equipmentManager = playerShipGO.GetComponent<EquipmentManager>();
        equipmentManager.InitializeEquipment();
        equipmentManager.LoadEquipmentData();
        loadingScreen.Loaded(37);
        // Wire systems using the live instance’s components
        brakeSlider.SetEngine(equipmentManager.equippedEngine, mainCamera);
        fireManager.SetEquipmentManager(equipmentManager);
        missileLockUI.Initialize(equipmentManager);
        cannonLockUI.Initialize(equipmentManager);
        loadingScreen.Loaded(40);

        var damageHandler = playerShipGO.GetComponent<DamageHandler>();
        barsUI_Bridge.Initialize(equipmentManager.equippedEngine, damageHandler);
        mainCamera.GetComponent<CameraShake>().Inititialize(damageHandler);
        
        // Be sure these UI refs are assigned in the Inspector
        ammoUI.Initialize(equipmentManager);

        speedDisplayUI.Initialize(equipmentManager.GetComponent<JoystickSpaceshipController>(), equipmentManager.equippedEngine);
        loadingScreen.Loaded(45);
        particleSpeedController = FindAnyObjectByType<ParticleSpeedController>(FindObjectsInactive.Include); loadingScreen.Loaded(60);
        if (particleSpeedController != null)
            particleSpeedController.Initialize(equipmentManager.equippedEngine);
        loadingScreen.Loaded(65);
        targetUI.Initialize(equipmentManager.equippedRadar);
        loadingScreen.Loaded(70);
        SetElementsActive();
        Transform playerCamera = FindAnyObjectByType<PlayerCameraTag>().transform;
        mainCamera.transform.parent = playerCamera;
        mainCamera.transform.localPosition = Vector3.zero;
        enemyWaveSpawner.SetActive(true);
        loadingScreen.Loaded(75);
        BGMusicManager.Instance.RefernceLookUp();
        gyro = playerShipGO.GetComponent<GyroController>();
        sensitivityManager.Initialize();
        sensitivityManager.SetGyroController(gyro);

        AddAudioSources(playerShipGO);
        chunkSceneManager1.SetTrackedTransform(playerShipGO.transform);
        //check if the chunks  are loaded
        loadingScreen.Loaded(80);
        StartCoroutine(WaitForChunksToLoad());



    }
    IEnumerator WaitForChunksToLoad()
    {
        while (!chunkSceneManager1.AreChunksLoaded())
        {
            yield return null;
        }

        loadingScreen.Loaded(85);
        yield return new WaitForSeconds(2f);
        loadingScreen.Loaded(100);
        Gyro();
    }

    void Gyro()
    {

        int a = PlayerPrefs.GetInt("GyroEnabled");
        if (a == 1)
        {
            sensitivityManager.InititializeWithWindow();
            gyro.SetGyroEnabled(true);
           
            uIGyroTiltController.Initialize(gyro);

        }
        else
        {
            hud.SetActive(true);
            Time.timeScale = 1.0f;
        }
        
    }

    void SetElementsActive()
    {
        playerShipGO.SetActive(true); // <- activate the instance
        fireManager.gameObject.SetActive(true);
        missileLockUI.gameObject.SetActive(true);
        cannonLockUI.gameObject.SetActive(true);
        barsUI_Bridge.gameObject.SetActive(true);
        ammoUI.gameObject.SetActive(true);
        if (particleSpeedController != null)
            particleSpeedController.gameObject.SetActive(true);
        speedDisplayUI.gameObject.SetActive(true);
        targetUI.gameObject.SetActive(true);
       
    }


    void AddAudioSources(GameObject rootObject)
    {
        AudioSource[] temp = rootObject.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource source in temp)
        {
            soundManager.AddAudioSource(source);
        }
    }

}
