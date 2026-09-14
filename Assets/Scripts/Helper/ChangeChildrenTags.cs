//using UnityEngine;
//using UnityEditor;

//[DisallowMultipleComponent]
//public class ChangeChildrenTags : MonoBehaviour
//{
//    [ContextMenu("Change Children & Grandchildren Tags")]
//    private void ChangeTagsOfChildren()
//    {
//        string childTag = "Untagged";
//        string grandchildTag = "PlayerCollider";

//        // Validate tags exist
//        if (!IsTagDefined(childTag))
//        {
//            Debug.LogError($"Tag '{childTag}' is not defined in this project. Please create it first in the Tag Manager.");
//            return;
//        }

//        if (!IsTagDefined(grandchildTag))
//        {
//            Debug.LogError($"Tag '{grandchildTag}' is not defined in this project. Please create it first in the Tag Manager.");
//            return;
//        }

//        int childCount = 0;
//        int grandchildCount = 0;

//        // Iterate through direct children
//        foreach (Transform child in transform)
//        {
//            child.gameObject.tag = childTag;
//            childCount++;

//            // Iterate through each grandchild
//            foreach (Transform grandchild in child)
//            {
//                grandchild.gameObject.tag = grandchildTag;
//                grandchildCount++;
//            }
//        }

//        Debug.Log($"✅ Changed {childCount} children to '{childTag}' and {grandchildCount} grandchildren to '{grandchildTag}'.", this);
//    }

//    private bool IsTagDefined(string tagName)
//    {
//        foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
//        {
//            if (tag == tagName)
//                return true;
//        }
//        return false;
//    }
//}
