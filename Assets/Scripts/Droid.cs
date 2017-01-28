using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Rollaround;
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
    public float JourneyLength;

    /// <summary>
    /// Occurs on death
    /// </summary>
    public event EventHandler<EventArgs> OnDeath = (sender, args) => { };

    /// <summary>
    /// Random instanced used for network
    /// </summary>
    public Random Random { get; set; }

    public Color Color;

    public Rigidbody Body { get; private set; }

    Transform head;
    NeuralNet brain;
    float[] inputs;
    float[] outputs;
    float birth;
    int obstacleLayer;
    int trackingLayer;
    // Array of pairs (float[2]) of feeler [angle,length]
    float[][] feelers;
    RaycastHit[] feelerHits;
    HashSet<GameObject> hitTriggers;
    CollisionDot[] dots;

    // Avoid Unity startup events and let Simulation control the lifecycle
    public void Init()
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
        Body = GetComponent<Rigidbody>();
        hitTriggers = new HashSet<GameObject>();
        dots = transform.parent.GetComponentsInChildren<CollisionDot>(true);
        feelers = GetFeelerParams();
        var numFeelers = feelers.Length;
        feelerHits = Enumerable.Repeat(default(RaycastHit), numFeelers).ToArray();
        // #feelers +2 for feeler scale input and velocity
        inputs = new float[numFeelers + 2];
        // Turning angle, speed, and feeler scale
        outputs = new float[3];
        brain = new NeuralNet(
            numInputs: inputs.Length,
            numOutputs: outputs.Length,
            numHiddenLayers: 2,
            neuronsPerHiddenLayer: inputs.Length + outputs.Length,
            bias: -1);
        SetRendererColors();
        Epoch();
    }


    void FixedUpdate()
    {
        // Point head in direction of attempted movement
        head.position = transform.position;
        var angle = Vector3.Dot(head.transform.right, Direction) * (Mathf.Rad2Deg * Time.fixedDeltaTime * 12f);
        head.rotation *= Quaternion.AngleAxis(angle, Vector3.forward);

        // Learn stuff
        inputs[inputs.Length - 2] = Mathf.Clamp(FeelerScale, 0.375f, 1.875f);
        inputs[inputs.Length - 1] = Mathf.Clamp(Body.velocity.magnitude, -100f, 100f);
        brain.FeedForward(inputs, outputs, Tanh);

        // Outputs ranges from -1 to 1, scale to a range
        FeelerScale = Mathf.Clamp((outputs[2] + 1.5f) * 0.75f, 0.375f, 1.875f);
        Direction = Quaternion.AngleAxis(outputs[0] * 10f, Vector3.up) * Direction;

        // Prevent buildup of momentum by applying less force the faster it moves
        // HACK: figure out proper math instead of so many vector operations
        var force = Vector3.ClampMagnitude(Direction * (outputs[1] * Speed), MaxSpeed);
        var compForce = Vector3.ClampMagnitude(force - Body.velocity * Body.velocity.magnitude, MaxSpeed);
        Body.AddForce(compForce, ForceMode.Impulse);
        CastFeelers();
    }

    void OnDrawGizmos()
    {
        if (feelers == null) { return; }
        Gizmos.color = Color;
        foreach (var parms in feelers)
        {
            var castAngle = parms[0];
            var length = parms[1] * FeelerScale;
            var cast = Quaternion.AngleAxis(castAngle, Vector3.up) * Body.velocity.normalized;
            Debug.DrawLine(Body.position, Body.position + cast * length, Color);
        }
    }

    void CastFeelers()
    {
        float length;
        Vector3 cast;
        RaycastHit castHit;
        float castAngle;
        for (var i = 0; i < feelers.Length; i++)
        {
            castAngle = feelers[i][0];
            length = feelers[i][1] * FeelerScale;
            cast = Quaternion.AngleAxis(castAngle, Vector3.up) * Body.velocity.normalized;
            if (Physics.Raycast(Body.position, cast, out castHit, length, 1 << obstacleLayer))
            {
                // Inputs are the distance to any hit, and the full length otherwise
                inputs[i] = castHit.distance;
                dots[i].Draw(transform.position, castHit.point);
            }
            else
            {
                inputs[i] = length;
            }
            feelerHits[i] = castHit;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Ignore if collider is not a tracking marker, or if already inactive
        if (other.gameObject.layer != trackingLayer || !gameObject.activeSelf) { return; }

        // Ignore if the trigger was hit while moving backwards, or if already hit
        if (hitTriggers.Add(other.gameObject) &&
            Vector3.Dot(Direction, other.gameObject.transform.position - transform.position) < 0f)
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
        ImpactSpeed = Body.velocity.magnitude;
    }

    public void PutWeights(Genome genome)
    {
        brain.PutWeights(genome.Weights);
    }

    public int GetNumberOfWeights()
    {
        return brain.GetWeights().Count;
    }

    public float[][] GetFeelerParams()
    {
        // This comes from the editor, assuming correct input is OK
        var pairs = ParseFeelerDefinition(Feelers);
        var results = new float[pairs.Length][];
        for (var i = 0; i < pairs.Length; i++)
        {
            var parts = pairs[i].Split(',');
            var angle = float.Parse(parts[0]);
            var length = float.Parse(parts[1]);
            results[i] = new float[] { angle, length };
        }
        return results;
    }

    public int GetFeelerCount()
    {
        return ParseFeelerDefinition(Feelers).Length;
    }

    public string[] ParseFeelerDefinition(string text)
    {
        return text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    void SetRendererColors()
    {
        // Include renderer for the head, which is a sibling object
        var renderers = transform.parent.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var m in r.materials)
            {
                if (m != null && m.shader == TintShader)
                {
                    m.color = Color;
                }
                else if (r is TrailRenderer)
                {
                    m.color = Color * 2f;
                }
            }
        }
        foreach (var d in dots)
        {
            d.SetColor(Color);
        }
    }

    public void Epoch()
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