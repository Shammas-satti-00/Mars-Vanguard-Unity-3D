using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BoostButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Events")]
    [Tooltip("Called once when the pointer enters the UI element.")]
    public UnityEvent OnEnter;
    
    [Tooltip("Called once when the pointer exits the UI element.")]
    public UnityEvent OnExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke();
    }
}
