using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMP_LinkOpener : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI tmpText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        Debug.Log("[TMP_LinkOpener] Link opener initialized on: " + gameObject.name);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[TMP_LinkOpener] Text clicked at position: " + eventData.position);

        // Detect link
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, null);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            string url = linkInfo.GetLinkID();

            Debug.Log("[TMP_LinkOpener] Link detected! Opening URL: " + url);

            Application.OpenURL(url);
        }
        else
        {
            Debug.Log("[TMP_LinkOpener] No link detected at click position.");
        }
    }
}
