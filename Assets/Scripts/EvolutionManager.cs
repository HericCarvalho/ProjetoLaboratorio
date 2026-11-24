using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EvolutionSnapshot
{
    public int version = 1;
    public int seed;
    public int generation;
    public float bestFitness;
    public float[] weights;
    public int populationSize;
    public float mutationRate;
    public float mutationStrength;
    public string savedAt;
}

public sealed class EvolutionManager : MonoBehaviour
{
    private const string SaveKey = "neuroevolution.best.v1";

    [Header("Cena")]
    [SerializeField] private EvolvingShip shipPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform goal;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private GameObject[] scenarios;
    [SerializeField] private Vector3[] scenarioGoalPositions;

    [Header("Evolucao")]
    [SerializeField, Min(4)] private int populationSize = 30;
    [SerializeField, Min(1)] private int eliteCount = 2;
    [SerializeField, Range(0f, 1f)] private float mutationRate = 0.08f;
    [SerializeField, Min(0f)] private float mutationStrength = 0.25f;
    [SerializeField, Min(1f)] private float episodeSeconds = 15f;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool validationMode;

    [Header("Fitness")]
    [SerializeField] private float progressWeight = 10f;
    [SerializeField] private float goalBonus = 100f;
    [SerializeField] private float collisionPenalty = -50f;
    [SerializeField] private float timePenalty = 1f;

    private readonly List<EvolvingShip> ships = new();
    private readonly List<Genome> population = new();
    private System.Random random;
    private Genome bestGenome;
    private float elapsed;
    private int finishedCount;
    private int currentScenario;
    private float referenceDistance;
    private string lastStatus = "";

    public int Generation { get; private set; }
    public float BestFitness { get; private set; }
    public float AverageFitness { get; private set; }
    public float BestEfficiency { get; private set; }
    public float ProgressWeight => progressWeight;
    public float GoalBonus => goalBonus;
    public float CollisionPenalty => collisionPenalty;
    public float TimePenalty => timePenalty;

    private void Start()
    {
        if (shipPrefab == null || spawnPoint == null || goal == null)
        {
            Debug.LogError("EvolutionManager precisa de Ship Prefab, Spawn Point e Goal.", this);
            enabled = false;
            return;
        }

        ActivateScenario(0);
        StartEvolution();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SaveBest();
        if (Input.GetKeyDown(KeyCode.L))
            LoadBest();
        if (Input.GetKeyDown(KeyCode.D))
            DeleteSave();
    }

    private void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        if (elapsed >= episodeSeconds)
        {
            EvolvingShip[] activeShips = ships.Where(ship => ship.IsActive).ToArray();
            foreach (EvolvingShip ship in activeShips)
                ship.Timeout();
        }
    }

    private void StartEvolution()
    {
        validationMode = false;
        Generation = 0;
        BestFitness = float.MinValue;
        BestEfficiency = 0f;
        population.Clear();

        random = new System.Random(randomSeed);
        for (int i = 0; i < populationSize; i++)
            population.Add(new Genome(NeuralNetwork.WeightCount, random));

        BeginGeneration();
    }

    public void ActivateScenario(int index)
    {
        if (scenarios == null || scenarios.Length == 0)
            return;

        currentScenario = Mathf.Clamp(index, 0, scenarios.Length - 1);
        for (int i = 0; i < scenarios.Length; i++)
        {
            if (scenarios[i] != null)
                scenarios[i].SetActive(i == currentScenario);
        }

        if (scenarioGoalPositions != null && currentScenario < scenarioGoalPositions.Length)
            goal.position = scenarioGoalPositions[currentScenario];

        Physics.SyncTransforms();
        if (pathfinder != null)
        {
            referenceDistance = pathfinder.CalculatePath(spawnPoint.position, goal.position);
            Debug.Log($"Cenario {currentScenario + 1} | Distancia de referencia: {referenceDistance:F2}m");
        }
    }

    public void ReportFinished(EvolvingShip ship)
    {
        finishedCount++;
        if (validationMode)
        {
            float eff = ship.GetEfficiency(referenceDistance);
            string effText = ship.ReachedGoal ? $"{eff:F3}" : "invalida";
            lastStatus = $"Validacao C{currentScenario + 1} | Chegou:{ship.ReachedGoal} Colidiu:{ship.Collided} Tempo:{ship.EpisodeTime:F2}s Dist:{ship.RealDistance:F2}m Ref:{referenceDistance:F2}m Efic:{effText}";
            Debug.Log(lastStatus);
            return;
        }

        if (finishedCount >= ships.Count)
            Evolve();
    }

    private void BeginGeneration()
    {
        elapsed = 0f;
        finishedCount = 0;

        int activeCount = validationMode ? 1 : population.Count;
        while (ships.Count < activeCount)
            ships.Add(Instantiate(shipPrefab, spawnPoint.position, spawnPoint.rotation, transform));

        for (int i = 0; i < ships.Count; i++)
        {
            if (i < activeCount)
            {
                Genome g = validationMode ? bestGenome : population[i];
                ships[i].Begin(this, g, goal, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                ships[i].gameObject.SetActive(false);
            }
        }
    }

    private void Evolve()
    {
        float bestEffThisGen = 0f;
        foreach (EvolvingShip ship in ships)
        {
            if (!ship.gameObject.activeSelf)
                continue;

            ship.Genome.Fitness = ship.Fitness;
            float eff = ship.GetEfficiency(referenceDistance);
            if (eff > bestEffThisGen)
                bestEffThisGen = eff;
        }

        population.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
        BestFitness = population[0].Fitness;
        AverageFitness = population.Average(genome => genome.Fitness);
        BestEfficiency = bestEffThisGen;
        bestGenome = population[0].Clone();

        Debug.Log($"Geracao {Generation}: melhor {BestFitness:F2}, media {AverageFitness:F2}, efic {BestEfficiency:F3}");

        var next = new List<Genome>(populationSize);
        int protectedCount = Mathf.Min(eliteCount, population.Count);
        for (int i = 0; i < protectedCount; i++)
            next.Add(population[i].Clone());

        while (next.Count < populationSize)
        {
            Genome parentA = TournamentPick();
            Genome parentB = TournamentPick();
            Genome child = Genome.Crossover(parentA, parentB, random);
            child.Mutate(random, mutationRate, mutationStrength);
            next.Add(child);
        }

        population.Clear();
        population.AddRange(next);
        Generation++;
        BeginGeneration();
    }

    private Genome TournamentPick()
    {
        const int tournamentSize = 3;
        Genome best = population[random.Next(population.Count)];
        for (int i = 1; i < tournamentSize; i++)
        {
            Genome contender = population[random.Next(population.Count)];
            if (contender.Fitness > best.Fitness)
                best = contender;
        }
        return best;
    }

    public void SaveBest()
    {
        Genome toSave = bestGenome;
        if (toSave == null && population.Count > 0)
            toSave = population[0].Clone();

        if (toSave == null)
        {
            lastStatus = "Nenhum genoma disponivel para salvar.";
            Debug.LogWarning(lastStatus);
            return;
        }

        float[] copy = new float[toSave.Weights.Length];
        Array.Copy(toSave.Weights, copy, toSave.Weights.Length);

        var snapshot = new EvolutionSnapshot
        {
            version = 1,
            seed = randomSeed,
            generation = Generation,
            bestFitness = toSave.Fitness,
            weights = copy,
            populationSize = populationSize,
            mutationRate = mutationRate,
            mutationStrength = mutationStrength,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(snapshot));
        PlayerPrefs.Save();
        lastStatus = $"Salvo: geracao {snapshot.generation}, fitness {snapshot.bestFitness:F2}";
        Debug.Log(lastStatus);
    }

    public void LoadBest()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            lastStatus = "Nenhum save encontrado.";
            Debug.LogWarning(lastStatus);
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey, "");
        EvolutionSnapshot snapshot = JsonUtility.FromJson<EvolutionSnapshot>(json);
        if (snapshot == null || snapshot.weights == null || snapshot.weights.Length != NeuralNetwork.WeightCount)
        {
            lastStatus = "Save incompativel: quantidade de pesos invalida.";
            Debug.LogWarning(lastStatus);
            return;
        }

        float[] copy = new float[snapshot.weights.Length];
        Array.Copy(snapshot.weights, copy, snapshot.weights.Length);
        bestGenome = new Genome(NeuralNetwork.WeightCount, new System.Random(snapshot.seed))
        {
            Weights = copy,
            Fitness = snapshot.bestFitness
        };
        Generation = snapshot.generation;
        BestFitness = snapshot.bestFitness;
        validationMode = true;
        BeginGeneration();

        lastStatus = $"Carregado: geracao {snapshot.generation}, seed {snapshot.seed}, fitness {snapshot.bestFitness:F2}";
        Debug.Log(lastStatus);
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        lastStatus = "Save apagado com sucesso.";
        Debug.Log(lastStatus);
    }

    private void RunValidation()
    {
        if (bestGenome == null)
        {
            lastStatus = "Evolua ou carregue um genoma antes de validar.";
            Debug.LogWarning(lastStatus);
            return;
        }

        validationMode = true;
        BeginGeneration();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 360, 260), GUI.skin.box);
        GUILayout.Label($"Geracao: {Generation} | Cenario: {currentScenario + 1} | Modo: {(validationMode ? "Validacao" : "Treino")}");
        GUILayout.Label($"Melhor Fitness: {BestFitness:F2} | Efic: {BestEfficiency:F3} | Ref: {referenceDistance:F2}m");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Salvar melhor (S)")) SaveBest();
        if (GUILayout.Button("Carregar melhor (L)")) LoadBest();
        if (GUILayout.Button("Apagar save (D)")) DeleteSave();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Validar")) RunValidation();
        if (GUILayout.Button("Treinar")) StartEvolution();
        GUILayout.EndHorizontal();

        if (scenarios != null && scenarios.Length > 0)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < scenarios.Length; i++)
            {
                if (GUILayout.Button($"Cenario {i + 1}"))
                {
                    ActivateScenario(i);
                    if (validationMode)
                        BeginGeneration();
                }
            }
            GUILayout.EndHorizontal();
        }

        if (!string.IsNullOrEmpty(lastStatus))
            GUILayout.Label(lastStatus);

        GUILayout.EndArea();
    }
}
