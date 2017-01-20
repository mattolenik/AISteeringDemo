using System;
using UnityEditor;
using UnityEngine;

public class DroidGenerator
{
    [MenuItem("Scripts/CreateDroids")]
    public static void CreateDroids()
    {
        var parent = GameObject.FindGameObjectWithTag("droidparent");
        var field = GameObject.FindGameObjectWithTag("field").GetComponent<PlayingField>();
        Undo.RegisterFullObjectHierarchyUndo(parent, "creating droids");
        var sim = GameObject.FindGameObjectWithTag("simulation").GetComponent<Simulation>();
        var num = field.NumDroids;
        var colors = ColorUtils.GenerateDistinctColors(num, 0.7f, 1);
        for (var i = 0; i < num; i++)
        {
            NewDroid(sim, colors[i], parent.transform);
        }
    }

    static void NewDroid(Simulation sim, Color color, Transform parent)
    {
        var obj = GameObject.Instantiate(sim.DroidPrefab);
        obj.transform.parent = parent;
        var droid = obj.GetComponentInChildren<Droid>();
        droid.OnDeath += sim.OnDroidDeath;
        CreateDots(droid);
        droid.Color = color;
    }

    static void CreateDots(Droid d)
    {
        var numDots = d.GetFeelerCount();
        for(var i = 0; i < numDots; i++)
        {
            GameObject.Instantiate(d.DotPrefab, d.transform.parent);
        }
    }
}