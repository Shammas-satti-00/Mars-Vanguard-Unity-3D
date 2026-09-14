using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class SpaceshipEvents
{
    public UnityEvent onBoostStarted;
    public UnityEvent onBoostEnded;
    public UnityEvent<float> onBoostFuelChanged;
    public UnityEvent<Vector3> onMoved;
    public UnityEvent<Vector3> onPositionChanged;
}
