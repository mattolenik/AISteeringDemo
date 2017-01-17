using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Quick n dirty extension for the editor to display feeler vectors
[ExecuteInEditMode]
public class FeelerEditor : MonoBehaviour
{
    List<Tuple<float, float>> feelerParams;

    void Update()
    {
        var t = GetComponent<Droid>();
        feelerParams = t.GetFeelerParams();
        var tform = gameObject.transform;
        var pos = tform.position;
        foreach (var p in feelerParams)
        {
            var angle = p.First;
            var distance = p.Second;
            var f = Quaternion.AngleAxis(angle, tform.up) * (tform.forward * distance);
            Debug.DrawLine(pos, f * t.FeelerScale);
        }
    }
}