using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using PathfindingLib;
using UnityEngine.UI;
using Random = System.Random;

public class Simulation : MonoBehaviour
{
    public Text GenerationCountLabel;

    public Text GenerationEtaLabel;

    public Text TimescaleLabel;

    public Slider TimescaleSlider;

    public TextAsset DemoData;

    [Tooltip("Starting generation lifespan in seconds")]
    public float StartingGenerationLength = 8;

    [Tooltip("Generation length approaches this asymptotically")]
    public float MaxGenerationLength = 50;

    [Tooltip("For debugging purposes in editor")]
    public double CurrentGenerationLength;

    public GameObject DroidPrefab;

    public PlayingField PlayingField;

    public int NumDroids = 20;

    [Tooltip("Random seed used by genetic algorithm")]
    public int GenomeSeed = 2;

    [Tooltip("Random seed used by neural network")]
    public int AiSeed = 10;

    float generationStart = -1f;
    float generationLength;
    Droid[] droids;
    Evolver genAlg;
    int deadDroids;
    int generationCount;
    string saveFilename;
    Random rnd;
    Random aiRnd;

    // Generation length function input (i.e. x-axis of its plot)
    float genLengthX;

    void Start()
    {
        saveFilename = Path.Combine(Application.persistentDataPath, "population.json");
        SetTimescale(TimescaleSlider.value);
        aiRnd = new Random(AiSeed);
        rnd = new Random(GenomeSeed);
        // Load initial data for demonstration purposes
        var data = LoadGenomes();
        NewSimulation(data);
    }

    GenerationRecord DefaultData()
    {
        return new GenerationRecord(new Genome[] { }, 1, StartingGenerationLength);
    }

    void NewSimulation(GenerationRecord data)
    {
        // Increment random seed so that each new run is not identical,
        // but still provides determinism, and increased variety.
        generationLength = data.GenLength;
        droids = CreateDroids(NumDroids);
        deadDroids = 0;
        var numWeights = droids[0].GetNumberOfWeights();
        genAlg = new Evolver(
            populationSize: NumDroids,
            mutationRate: 0.1f,
            crossoverRate: 0.7f,
            numWeights: numWeights,
            initialGenes: data.Genomes,
            elitism: 2,
            eliteCopies: 4);
        InitDroids();
        genLengthX = 1f;
        generationStart = Time.time;
        generationCount = data.GenCount;
        SetGenerationText();
    }

    void CleanupSimulation()
    {
        for (var i = 0; i < droids.Length; i++)
        {
            DestroyImmediate(droids[i].transform.parent.gameObject);
        }
    }

    void SetGenerationText()
    {
        GenerationCountLabel.text = generationCount.ToString("D");
    }

    void SetTimescale(float value)
    {
        Time.timeScale = value;
        TimescaleLabel.text = value.ToString("F1");
    }

    Droid[] CreateDroids(int num)
    {
        var result = new Droid[num];
        var colors = ColorUtils.GenerateDistinctColors(num, 0.7f, 1);
        for (var i = 0; i < num; i++)
        {
            result[i] = NewDroid(colors[i]);
        }
        return result;
    }

    Droid NewDroid(Color color)
    {
        var obj = Instantiate(DroidPrefab);
        var droid = obj.GetComponentInChildren<Droid>();
        droid.OnDeath += (sender, args) => deadDroids++;
        droid.SetColor(color);
        droid.Random = aiRnd;
        return droid;
    }

    void Update()
    {
        var elapsed = Time.time - generationStart;
        if (elapsed > generationLength || deadDroids == NumDroids)
        {
            StartCoroutine(NewGeneration());
        }
        // Cast is fine for rough ETA
        GenerationEtaLabel.text = ((int) (generationLength - elapsed)).ToString("###\\s");
    }

    IEnumerator NewGeneration()
    {
        var fitness = SelectFitnessValues(droids);
        genAlg.NewGeneration(fitness);
        generationLength = GenerationLength(genLengthX += 0.04f);
        CurrentGenerationLength = generationLength;
        generationCount++;
        SetGenerationText();
        generationStart = Time.time + 0.01f;
        deadDroids = 0;
        InitDroids();
        yield return new WaitForEndOfFrame();
    }

    float[] SelectFitnessValues(IList<Droid> d)
    {
        var result = new float[d.Count];
        for (var i = 0; i < d.Count; i++)
        {
            result[i] = d[i].JourneyLength;
        }
        return result;
    }

    void InitDroids()
    {
        for (var i = 0; i < droids.Length; i++)
        {
            droids[i].PutWeights(genAlg.Population[i]);
            droids[i].Reset();
            droids[i].Direction = Quaternion.AngleAxis(rnd.NextFloat(0f, 360f), Vector3.up) * Vector3.forward;
            droids[i].transform.position = new Vector3(rnd.NextFloat(-2, 2), 0.65f, rnd.NextFloat(-2, 2));
        }
    }

    float GenerationLength(float x)
    {
        // f(x)=a∙log(x)+b
        return MaxGenerationLength * Mathf.Log10(x) + StartingGenerationLength;
    }

    public void OnSaveClick()
    {
        // Export top fittest half of population
        var fittest = Arrays.FindBest(genAlg.Population, NumDroids / 2, (a, b) => a.Fitness.CompareTo(b.Fitness));
        SaveGenomes(fittest, generationCount, generationLength);
    }

    void SaveGenomes(Genome[] fittest, int generation, float length)
    {
        var json = JsonUtility.ToJson(new GenerationRecord(fittest, generation, length));
        File.WriteAllText(saveFilename, json);
    }

    GenerationRecord LoadGenomes()
    {
        var json = File.Exists(saveFilename) ? File.ReadAllText(saveFilename) : DemoData.text;
        var record = JsonUtility.FromJson<GenerationRecord>(json);
        return record == null || record.Genomes == null ? DefaultData() : record;
    }

    public void OnTrainClick()
    {
        CleanupSimulation();
        NewSimulation(DefaultData());
    }

    public void OnPlayClick()
    {
        CleanupSimulation();
        var data = LoadGenomes();
        if(data == null)
        {
            Debug.Log("No saved data");
            return;
        }
        NewSimulation(data);
    }

    public void OnTimescaleChange(float value)
    {
        SetTimescale(value);
    }

    // Record for JSON serialization
    [Serializable]
    class GenerationRecord
    {
        public Genome[] Genomes;
        public int GenCount;
        public float GenLength;

        public GenerationRecord(Genome[] genomes, int count, float length)
        {
            Genomes = genomes;
            GenCount = count;
            GenLength = length;
        }
    }
}