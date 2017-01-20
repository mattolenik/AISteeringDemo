using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrackerGenerator
{
    [MenuItem("Scripts/CreateTrackingTriggers")]
    static void Create()
    {
        var field = GameObject.FindGameObjectWithTag("field");
        var parent = GameObject.FindGameObjectWithTag("trackerparent");
        GenerateMarkers(field.GetComponent<PlayingField>(), parent.transform);
    }

    static void GenerateMarkers(PlayingField field, Transform parent)
    {
        Undo.RegisterFullObjectHierarchyUndo(parent, "create field triggers");
        // Only generate the dots once, then reactivate them later
        var width =  field.Width / field.MarkerDensity;
        var height = field.Height / field.MarkerDensity;
        for (var x = field.LeftBound; x < field.RightBound; x += width)
        {
            for (var z = field.SouthBound; z < field.TopBound; z += height)
            {
                var obj = GameObject.Instantiate(field.TrackingPrefab);
                var bounds = obj.GetComponent<Collider>().bounds;
                obj.transform.parent = parent;
                obj.transform.position = new Vector3(x, bounds.extents.y, z);
                var widthScale = width / bounds.size.x;
                var heightScale = height / bounds.size.z;
                var scale = obj.transform.localScale;
                obj.transform.localScale = new Vector3(scale.x * widthScale, obj.transform.localScale.y, scale.z * heightScale);
            }
        }
    }
}