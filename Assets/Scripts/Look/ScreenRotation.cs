using UnityEngine;

public class ScreenRotation : MonoBehaviour
{
    public static ScreenRotation Instance;

    // Static flags so they can be changed from anywhere
    public static bool allowLandscapeLeft = true;
    public static bool allowLandscapeRight = true;

    private void Awake()
    {
        // Basic singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        AutoRotate();
    }

    // Static rotation logic
    public static void AutoRotate()
    {
        switch (Input.deviceOrientation)
        {
            case DeviceOrientation.LandscapeLeft:
                if (allowLandscapeLeft)
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                break;

            case DeviceOrientation.LandscapeRight:
                if (allowLandscapeRight)
                    Screen.orientation = ScreenOrientation.LandscapeRight;
                break;

            case DeviceOrientation.FaceUp:
            case DeviceOrientation.FaceDown:
            case DeviceOrientation.Unknown:
                // ignore
                break;
        }
    }
}
