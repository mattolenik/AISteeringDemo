using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PathfindingLib;
using UnityEngine.UI;
using Random = System.Random;

public class Simulation : MonoBehaviour
{
    public Dropdown ImportList;

    public float TimeScale = 5f;

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

    // Generation length function input (i.e. x-axis of its plot)
    float genLengthX;

    IEnumerator Start()
    {
        rnd = new Random(GenomeSeed);
        aiRnd = new Random(AiSeed);
        generationLength = StartingGenerationLength;
        droids = new List<Droid>(NumDroids);
        CreateDroids();
        yield return new WaitForEndOfFrame();
        var numWeights = droids.First().GetNumberOfWeights();
        genAlg = new Evolver(
            populationSize: NumDroids,
            mutationRate: 0.1f,
            crossoverRate: 0.7f,
            numWeights: numWeights,
            elitism: 2,
            eliteCopies: 4);
        PopulateGenomes();
        genLengthX = 1f;
        RestoreImportList();
        generationStart = Time.time;
    }

    string Export()
    {
        var fittest = genAlg.Population.OrderByDescending(g => g.Fitness).First();
        var data = fittest.Export();
        return Convert.ToBase64String(data);
    }

    void Import(string data)
    {
        var genome = Genome.Import(Convert.FromBase64String(data));
        genAlg.Import(genome);
    }

    void CreateDroids()
    {
        var colors = ColorUtils.GenerateDistinctColors(NumDroids, 0.7f, 1);
        for (var i = 0; i < NumDroids; i++)
        {
            var droid = NewDroid(colors[i]);
            droids.Add(droid);
        }
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
        if (Time.time - generationStart > generationLength || deadDroids == NumDroids)
        {
            StartCoroutine(NewGeneration());
        }
        Time.timeScale = TimeScale;
    }

    IEnumerator NewGeneration()
    {
        PlayingField.ClearMarkers();
        var fitness = droids.Select(d => d.JourneyLength).ToArray();
        genAlg.NewGeneration(fitness);
        generationLength = GenerationLength(genLengthX += 0.04f);
        CurrentGenerationLength = generationLength;
        generationStart = Time.time + 0.01f;
        deadDroids = 0;
        PopulateGenomes();
        yield return new WaitForEndOfFrame();
    }

    void PopulateGenomes()
    {
        for (var i = 0; i < droids.Count; i++)
        {
            droids[i].PutWeights(genAlg.Population[i]);
            droids[i].Direction = Quaternion.AngleAxis(rnd.NextFloat(0f, 360f), Vector3.up) * Vector3.forward;
            droids[i].transform.position = new Vector3(rnd.NextFloat(-2, 2), 0.65f, rnd.NextFloat(-2, 2));
            droids[i].Reset();
        }
    }

    float GenerationLength(float x)
    {
        // f(x)=a∙log(x)+b
        return MaxGenerationLength * Mathf.Log10(x) + StartingGenerationLength;
    }

    public void OnExportClick()
    {
        var dataName = DateTime.Now.ToString("M-d HH:mm:ss");
        var data = Export();
        PlayerPrefs.SetString(dataName, data);
        ImportList.AddOptions(new List<string> { dataName });

        // Stash names in a comma separated string
        var exported = PlayerPrefs.GetString("exported", "") + dataName + ",";
        PlayerPrefs.SetString("exported", exported);
    }

    void RestoreImportList()
    {
        var exportedString = PlayerPrefs.GetString("exported", "");
        var exported = exportedString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        ImportList.ClearOptions();
        ImportList.AddOptions(exported);
    }

    public void OnImportClick()
    {
        var importName = ImportList.options[ImportList.value].text;
        var data = PlayerPrefs.GetString(importName);
        if (data == null)
        {
            throw new Exception("Could not find dataset " + importName);
        }
        //Import(data);
    }
}