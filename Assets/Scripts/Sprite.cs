using System.Collections;
using UnityEngine;

public class Sprite : MonoBehaviour
{
    public float Lifespan;
    public Shader LineShader;

    Material dotMat;
    Color color;
    Vector3 to;
    Vector3 from;
    LineRenderer lineRenderer;

    public void Draw(Vector3 from, Vector3 to)
    {
        this.from = from;
        this.to = to;
        gameObject.SetActive(true);
        transform.position = to;
        Invoke("Remove", Time.timeScale * Lifespan);
        color.a = 1f;
        dotMat.SetColor("_Color", color);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    void Awake()
    {
        dotMat = GetComponent<MeshRenderer>().material;
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Remove()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        transform.LookAt(Camera.main.transform.position);
        color = dotMat.GetColor("_Color");
        color.a -= Time.deltaTime / 2f;
        dotMat.SetColor("_Color", color);
        lineRenderer.startColor = lineRenderer.endColor = color;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
    }

    public void SetColor(Color c)
    {
        color = c;
    }
}