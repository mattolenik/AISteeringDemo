using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PathfindingLib;
using Random = System.Random;

[RequireComponent(typeof(Rigidbody))]
public class Droid : MonoBehaviour
{
    [Tooltip("Speed multiplier")]
    public float Speed = 1.0f;

    [Tooltip("Velocity vector will be clamped to this magnitude")]
    public float MaxSpeed = 1.2f;

    [Header("Specified by pairs of bodyAngle,length")]
    [TextArea(5, 10)]
    public string Feelers = "-20,1.5 20,1.5 0,2";

    [Tooltip("Shader on mesh that should have its color set to create tint effect")]
    public Shader TintShader;

    public GameObject DotPrefab;

    [Tooltip("This is controlled by the AI and is here for editor usefulness")]
    public float FeelerScale = 1f;

    /// <summary>
    /// Magnitude of velocity vector at time of death
    /// </summary>
    public float ImpactSpeed { get; private set; }

    /// <summary>
    /// Time in seconds since birth
    /// </summary>
    public float Lifetime { get; private set; }

    /// <summary>
    /// Current direction vector
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// How much unique ground has been covered
    /// </summary>
    public float JourneyLength { get; private set; }

    /// <summary>
    /// Occurs on death
    /// </summary>
    public event EventHandler<EventArgs> OnDeath = (sender, args) => { };

    /// <summary>
    /// Random instanced used for network
    /// </summary>
    public Random Random { get; set; }

    Rigidbody body;
    Transform head;
    NeuralNet brain;
    float[] inputs;
    float[] outputs;
    float birth;
    Color color;
    int obstacleLayer;
    int trackingLayer;
    Tuple<float, float>[] feelers;
    RaycastHit[] feelerHits;
    HashSet<Collider> hitTriggers;
    Sprite[] dots;

    void Awake()
    {
        foreach (Transform tform in transform.parent.transform)
        {
            if (String.Equals(tform.name, "head", StringComparison.OrdinalIgnoreCase))
            {
                head = tform;
                break;
            }
        }
        obstacleLayer = LayerMask.NameToLayer("Obstacles");
        trackingLayer = LayerMask.NameToLayer("Tracking");
        body = GetComponent<Rigidbody>();
        hitTriggers = new HashSet<Collider>();

        feelers = GetFeelerParams().ToArray();
        var numFeelers = feelers.Length;
        feelerHits = Enumerable.Repeat(default(RaycastHit), numFeelers).ToArray();

        // Create display dot sprites for hits
        dots = Enumerable.Range(0, numFeelers).
            Select(x => Instantiate(DotPrefab, transform.parent).GetComponent<Sprite>()).ToArray();

        // +1 for feeler scale input
        inputs = new float[numFeelers + 2];
        outputs = new float[3];

        brain = new NeuralNet(
            numInputs: inputs.Length,
            numOutputs: outputs.Length,
            numHiddenLayers: 2,
            neuronsPerHiddenLayer: inputs.Length + outputs.Length,
            bias: -1);
    }

    void Start()
    {
        birth = Time.time;
    }

    void FixedUpdate()
    {
        // Point head in direction of attempted movement
        head.position = transform.position;
        var angle = Vector3.Dot(head.transform.right, Direction) * (Mathf.Rad2Deg * Time.fixedDeltaTime * 12f);
        head.rotation *= Quaternion.AngleAxis(angle, Vector3.forward);

        var s = body.velocity.magnitude;
        // Learn stuff
        inputs[inputs.Length - 2] = Mathf.Clamp(FeelerScale, 0.375f, 1.875f);
        inputs[inputs.Length - 1] = Mathf.Clamp(s, -100f, 100f);
        brain.FeedForward(inputs, outputs, Tanh);

        // Outputs ranges from -1 to 1, scale to a range
        FeelerScale = Mathf.Clamp((outputs[2] + 1.5f) * 0.75f, 0.375f, 1.875f);
        Direction = Quaternion.AngleAxis(outputs[0] * 10f, Vector3.up) * Direction;

        // Prevent buildup of momentum by applying less force the faster it moves
        var force = Vector3.ClampMagnitude(Direction * (outputs[1] * Speed), MaxSpeed);
        var corrected = force - body.velocity * body.velocity.magnitude;

        // HACK: prevent invalid force, find a better way to root out runaway input
        if (!float.IsNaN(corrected.magnitude))
        {
            body.AddForce(corrected, ForceMode.Impulse);
        }
        CastFeelers();
    }

    void OnDrawGizmos()
    {
        if (!Debug.isDebugBuild) { return; }
        Gizmos.color = color;
        foreach (var hit in feelerHits ?? new RaycastHit[] { })
        {
            if (hit.collider != null)
            {
                Gizmos.DrawSphere(hit.point, 0.1f);
                Debug.DrawLine(body.position, hit.point, color);
            }
        }
    }

    void CastFeelers()
    {
        float angle, length;
        Vector3 v;
        RaycastHit hit;
        for (var i = 0; i < feelers.Length; i++)
        {
            angle = feelers[i].First;
            length = feelers[i].Second * FeelerScale;
            v = Quaternion.AngleAxis(angle, Vector3.up) * body.velocity.normalized;
            if (Physics.Raycast(body.position, v, out hit, length, 1 << obstacleLayer))
            {
                // Inputs are the distance to any hit, and the full length otherwise
                inputs[i] = hit.distance;
                dots[i].Draw(transform.position, hit.point);
            }
            else
            {
                inputs[i] = length;
            }
            feelerHits[i] = hit;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Ignore if collider is not a tracking marker, or if already inactive
        if (other.gameObject.layer != trackingLayer || !gameObject.activeSelf) { return; }

        // Ignore if the trigger was hit while moving backwards, or if already hit
        if (hitTriggers.Add(other) &&
            Vector3.Dot(Direction, other.gameObject.transform.position - transform.position) < 0)
        {
            JourneyLength++;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.layer != obstacleLayer) { return; }
        OnDeath(this, new EventArgs());
        transform.parent.gameObject.SetActive(false);
        Lifetime = Time.time - birth;
        ImpactSpeed = body.velocity.magnitude;
    }

    public void PutWeights(Genome genome)
    {
        brain.PutWeights(genome.ToList());
    }

    public int GetNumberOfWeights()
    {
        return brain.GetWeights().Count;
    }

    public List<Tuple<float, float>> GetFeelerParams()
    {
        var result = new List<Tuple<float, float>>();
        var pairs = Feelers.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split(',');
            var angle = float.Parse(parts[0]);
            var length = float.Parse(parts[1]);
            result.Add(Tuple.New(angle, length));
        }
        return result;
    }

    public void SetColor(Color c)
    {
        color = c;

        // Include renderer for the head, which is a sibling object
        var renderers = transform.parent.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var m in r.materials)
            {
                if (m.shader == TintShader)
                {
                    m.color = color;
                }
                else if (r is TrailRenderer)
                {
                    m.color = color * 2f;
                }
            }
        }
        foreach (var d in dots)
        {
            d.SetColor(c);
        }
    }

    public void Reset()
    {
        JourneyLength = 0;
        Lifetime = 0;
        birth = Time.time;
        hitTriggers.Clear();
        transform.parent.gameObject.SetActive(true);
    }

    float Tanh(float x)
    {
        var x2 = x * 2f;
        return (Mathf.Exp(x2) - 1f) / (Mathf.Exp(x2) + 1f);
    }
}