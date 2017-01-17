using UnityEngine;
using System.Collections.Generic;

public class PlayingField : MonoBehaviour
{
    public int MarkerDensity = 40;

    public GameObject TrackingPrefab;

    float southWall;
    float northWall;
    float eastWall;
    float westWall;
    float mapHeight;
    float mapWidth;
    List<GameObject> markers;

    void Start()
    {
        var bounds = GetComponent<MeshFilter>().mesh.bounds;
        var extents = Vector3.Scale(bounds.extents, transform.localScale);
        westWall = -extents.x;
        eastWall = extents.x;
        northWall = extents.z;
        southWall = -extents.z;
        mapWidth = extents.x * 2f;
        mapHeight = extents.z * 2f;
        markers = new List<GameObject>();
        GenerateMarkers();
    }

    void GenerateMarkers()
    {
        // Only generate the dots once, then reactivate them later
        var width =  mapWidth / MarkerDensity;
        var height = mapHeight / MarkerDensity;
        for (var x = westWall; x < eastWall; x += width)
        {
            for (var y = southWall; y < northWall; y += height)
            {
                var obj = Instantiate(TrackingPrefab);
                markers.Add(obj);
                obj.transform.position = new Vector3(x, 0, y);
                var bounds = obj.GetComponentInChildren<Collider>().bounds;
                var widthScale = width / bounds.size.x;
                var heightScale = height / bounds.size.z;
                var scale = obj.transform.localScale;
                obj.transform.localScale = new Vector3(scale.x * widthScale, 1f, scale.y * heightScale);
            }
        }
    }

    public void ClearMarkers()
    {
        foreach (var dot in markers)
        {
            dot.GetComponentInChildren<Collider>().enabled = true;
        }
    }
}
