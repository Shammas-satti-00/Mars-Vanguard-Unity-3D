using UnityEngine;

public class ExitViaCollider : MonoBehaviour
{
    public float allottedTime = 5f;
    private float remainingTime = 0f;

    private bool isPlayerInside = false;
    private bool hasTriggeredGameOver = false;

    private int lastHandledFrame = -1;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PlayerCollider"))
            HandlePlayerInside();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("PlayerCollider"))
            HandlePlayerInside();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerCollider"))
            HandlePlayerExit();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("PlayerCollider"))
            HandlePlayerExit();
    }

    // ----------------------------------------
    // Main Logic
    // ----------------------------------------
    private void HandlePlayerInside()
    {
        if (hasTriggeredGameOver) return;

        // Prevent multiple increments on same frame
        if (lastHandledFrame == Time.frameCount)
            return;
        lastHandledFrame = Time.frameCount;

        // First time entering
        if (!isPlayerInside)
        {
            isPlayerInside = true;
            remainingTime = allottedTime;   // Start countdown at full time
           

            UIManager.Instance.SetTimerTextActive(true);
        }

        // Countdown using physics timestep
        remainingTime -= Time.fixedDeltaTime;
        if (remainingTime < 0) remainingTime = 0;

        // Update UI (show remaining time, not elapsed)
        UIManager.Instance.UpdateTimer(remainingTime);

        // When hits zero
        if (remainingTime <= 0f)
        {
           

            UIManager.Instance.SetTimerTextActive(false); // Hide UI
            hasTriggeredGameOver = true;

            GameManager.Instance.GameOverViaHanger();
        }
    }

    private void HandlePlayerExit()
    {
        if (hasTriggeredGameOver) return;

        // Avoid double firing
        if (lastHandledFrame == Time.frameCount)
            return;
        lastHandledFrame = Time.frameCount;

        isPlayerInside = false;
        remainingTime = allottedTime;

      
        UIManager.Instance.SetTimerTextActive(false);
        UIManager.Instance.UpdateTimer(remainingTime);
    }
}
