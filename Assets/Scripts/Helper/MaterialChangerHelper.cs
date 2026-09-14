using UnityEngine;

public class MaterialChangerHelper : MonoBehaviour
{
    public enum TargetLevel
    {
        ChildrenOnly,
        GrandchildrenOnly,
        Both
    }

    [Header("Material Settings")]
    [Tooltip("Select which objects' materials to change.")]
    public TargetLevel targetLevel = TargetLevel.Both;

    [Tooltip("Material to apply to selected objects.")]
    public Material newMaterial;

    [Header("Apply on Start")]
    [Tooltip("If true, applies material automatically on Start.")]
    public bool applyOnStart = true;

    void Start()
    {
        if (applyOnStart && newMaterial != null)
        {
            ApplyMaterial();
        }
    }

    [ContextMenu("Apply Material Now")]
    public void ApplyMaterial()
    {
        if (newMaterial == null)
        {
            Debug.LogWarning("No material assigned in MaterialChangerHelper on " + gameObject.name);
            return;
        }

        switch (targetLevel)
        {
            case TargetLevel.ChildrenOnly:
                ChangeChildrenMaterials(transform, false);
                break;

            case TargetLevel.GrandchildrenOnly:
                ChangeGrandchildrenMaterials(transform);
                break;

            case TargetLevel.Both:
                ChangeChildrenMaterials(transform, true);
                break;
        }

        Debug.Log($"Material applied ({targetLevel}) on {gameObject.name}");
    }

    private void ChangeChildrenMaterials(Transform parent, bool includeGrandchildren)
    {
        foreach (Transform child in parent)
        {
            ApplyMaterialToRenderer(child);

            if (includeGrandchildren)
                ChangeChildrenMaterials(child, true);
        }
    }

    private void ChangeGrandchildrenMaterials(Transform parent)
    {
        foreach (Transform child in parent)
        {
            foreach (Transform grandchild in child)
            {
                ApplyMaterialToRenderer(grandchild);
            }
        }
    }

    private void ApplyMaterialToRenderer(Transform obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = newMaterial;
        }
    }
}
