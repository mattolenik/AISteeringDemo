using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
    List<Droid> droids;
    Evolver genAlg;
    int deadDroids;
    Random rnd;
    Random aiRnd;
    int generationCount;
    string saveFilename;

    // Generation length function input (i.e. x-axis of its plot)
    float genLengthX;

    void Start()
    {
        saveFilename = Path.Combine(Application.persistentDataPath, "population.json");
        SetTimescale(TimescaleSlider.value);
        // Load initial data for demonstration purposes
        var genomes = DeserializeGenomes(DemoData.text);
        NewSimulation(genomes);
    }

    void NewSimulation()
    {
        NewSimulation(new Genome[] { });
    }

    void NewSimulation(Genome[] genomes)
    {
        rnd = new Random(GenomeSeed);
        aiRnd = new Random(AiSeed);
        generationLength = StartingGenerationLength;
        droids = CreateDroids();
        deadDroids = 0;
        var numWeights = droids.First().GetNumberOfWeights();
        genAlg = new Evolver(
            populationSize: NumDroids,
            mutationRate: 0.1f,
            crossoverRate: 0.7f,
            numWeights: numWeights,
            initialGenes: genomes,
            elitism: 2,
            eliteCopies: 4);
        InitDroids();
        genLengthX = 1f;
        generationStart = Time.time;
        generationCount = 1;
        SetGenerationText();
    }

    void CleanupSimulation()
    {
        droids.ForEach(d => DestroyImmediate(d.transform.parent.gameObject));
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

    List<Droid> CreateDroids()
    {
        var result = new List<Droid>();
        var colors = ColorUtils.GenerateDistinctColors(NumDroids, 0.7f, 1);
        for (var i = 0; i < NumDroids; i++)
        {
            var droid = NewDroid(colors[i]);
            result.Add(droid);
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
        var fitness = droids.Select(d => d.JourneyLength).ToArray();
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

    void InitDroids()
    {
        for (var i = 0; i < droids.Count; i++)
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
        var fittest = genAlg.Population.OrderByDescending(g => g.Fitness).Take(NumDroids / 2).ToArray();
        SaveGenomes(fittest);
    }

    void SaveGenomes(Genome[] fittest)
    {
        var json = JsonConvert.SerializeObject(fittest);
        File.WriteAllText(saveFilename, json);
    }

    Genome[] LoadGenomes()
    {
        var json = File.ReadAllText(saveFilename);
        return DeserializeGenomes(json);
    }

    Genome[] DeserializeGenomes(string json)
    {
        return JsonConvert.DeserializeObject<Genome[]>(json);
    }

    public void OnTrainClick()
    {
        CleanupSimulation();
        NewSimulation();
    }

    public void OnPlayClick()
    {
        CleanupSimulation();
        var genomes = LoadGenomes();
        NewSimulation(genomes);
    }

    public void OnTimescaleChange(float value)
    {
        SetTimescale(value);
    }
}