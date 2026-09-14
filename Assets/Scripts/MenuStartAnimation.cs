using UnityEngine;

public class MenuStartAnimation : MonoBehaviour
{
    [Header("Object to Move")]
    public Transform targetObject;

    [Tooltip("Offset added to the original position to reach the target")]
    public Vector3 targetOffset = new Vector3(0, 0, 5);

    [Tooltip("Movement speed")]
    public float moveSpeed = 3f;


    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool animating = false;

    void Awake()
    {
        if (targetObject == null)
        {
            Debug.LogError("MenuStartAnimation: No targetObject assigned!");
            return;
        }

        originalPosition = targetObject.position;
        targetPosition = originalPosition + targetOffset;
    }

    void Update()
    {
        if (!animating) return;

        targetObject.position = Vector3.MoveTowards(
            targetObject.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );


    }

    // -----------------------------
    // PUBLIC METHOD TO START
    // -----------------------------
    public void StartAnimation()
    {
        if (targetObject == null) return;

        originalPosition = targetObject.position;
        targetPosition = originalPosition + targetOffset;

        animating = true;

        Debug.Log("MenuStartAnimation: Animation started.");
    }


    // -----------------------------
    // CONTEXT MENU
    // -----------------------------
    [ContextMenu("Start Animation (Play Mode Only)")]
    private void ContextStartAnimation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Start Animation works only in Play Mode.");
            return;
        }

        StartAnimation();
    }

    [ContextMenu("Reset Position")]
    private void ResetPosition()
    {
        if (targetObject == null) return;

        targetObject.position = originalPosition;
        Debug.Log("MenuStartAnimation: Position reset.");
    }
}
