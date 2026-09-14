using PixelPlay.OffScreenIndicator;
using UnityEngine;
using System.Collections;

public class ActivateTarget : MonoBehaviour
{
    public void Start()
    {
        GetComponent<Target>().enabled = false;
        StartCoroutine(DelayFunction());



    }
    IEnumerator DelayFunction()
    {
        yield return new WaitForSeconds(0.6f);
        GetComponent<Target>().enabled = true;
    }
}