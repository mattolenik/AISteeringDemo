using UnityEngine;

[ExecuteInEditMode]
public class PlayingField : MonoBehaviour
{
    public int MarkerDensity = 40;
    public GameObject TrackingPrefab;
    public int NumDroids = 20;
    public float SouthBound { get; private set; }
    public float TopBound { get; private set; }
    public float RightBound { get; private set; }
    public float LeftBound { get; private set; }
    public float Height { get; private set; }
    public float Width { get; private set; }

    void Start()
    {
        var bounds = GetComponent<MeshFilter>().sharedMesh.bounds;
        var extents = Vector3.Scale(bounds.extents, transform.localScale);
        LeftBound = -extents.x;
        RightBound = extents.x;
        TopBound = extents.z;
        SouthBound = -extents.z;
        Width = extents.x * 2f;
        Height = extents.z * 2f;
    }
}
