using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    [Serializable]
    public class BombType
    {
        public float speed = 3f;
        public float pushDelay = 0.5f;
        [Range(0f, 1f)] public float probability = 0.5f;
        public Bomb bombPrefab;
    }

    // Helper attached to spawned objects so the spawner gets notified when they die.
    private class SpawnOccupant : MonoBehaviour
    {
        public int spawnIndex;
        public Action<int> onDestroyed;

        private void OnDestroy()
        {
            onDestroyed?.Invoke(spawnIndex);
        }
    }

    [Header("Coin")]
    public Coin coinPrefabs;
    public int minScore = 100;
    public int maxScore = 300;
    public float coinSpeed = 6f;
    public float coinPushDelay = 0.3f;
    public float coinSpawnInterval = 0.5f;
    [Range(0, 1)] public float coinProbability = 0.5f;

    [Header("Bombs")]
    public BombType[] bombTypes;

    [Header("Spawn Points")]
    public Transform[] spawnLocations;

    [Header("Spawning")]
    public float baseSpawnInterval = 1.5f;
    public float minSpawnInterval = 0.25f;

    [Header("Wave Difficulty")]
    [Tooltip("Seconds between each wave increase.")]
    public float waveInterval = 60f;
    [Tooltip("Spawn-interval multiplier applied per wave (e.g. 0.85 = 15% faster spawns each wave).")]
    [Range(0.5f, 0.99f)]
    public float spawnIntervalMultiplierPerWave = 0.85f;
    [Tooltip("Extra speed added per wave as a fraction of base speed (0.15 = +15% speed per wave).")]
    public float speedIncreasePerWave = 0.15f;

    [Header("When all spawn points are FULL")]
    [Tooltip("Multiply fallSpeed for all active objects while all spawn points are occupied.")]
    public float fullSpeedMultiplier = 1.5f;

    public int Wave { get; private set; } = 1;

    /// <summary>Current speed multiplier based on wave progression.</summary>
    public float WaveSpeedMultiplier => 1f + (Wave - 1) * speedIncreasePerWave;

    private float coinSpawnTimer;
    private float bombSpawnTimer;
    private float waveTimer;

    // One occupant per spawn point
    private bool[] occupied;
    private Bomb[] activeBombs;
    private Coin[] activeCoins;
    private float[] bombBaseSpeeds;

    private void Awake()
    {
        if (spawnLocations == null) spawnLocations = Array.Empty<Transform>();

        int len = spawnLocations.Length;
        occupied = new bool[len];
        activeBombs = new Bomb[len];
        activeCoins = new Coin[len];
        bombBaseSpeeds = new float[len];
    }

    private bool gameOver;

    private void Update()
    {
        if (spawnLocations == null || spawnLocations.Length == 0) return;

        // Stop spawning and clear everything when game ends
        if (GameManager.Instance != null && !GameManager.Instance.StartGame)
        {
            if (!gameOver)
            {
                gameOver = true;
                DestroyAllActive();
            }
            return;
        }

        // Reset gameOver flag when a new game starts
        if (gameOver)
        {
            gameOver = false;
            Wave = 1;
            waveTimer = 0f;
            bombSpawnTimer = 0f;
            coinSpawnTimer = 0f;
        }

        // Wave progression
        waveTimer += Time.deltaTime;
        if (waveTimer >= waveInterval)
        {
            waveTimer -= waveInterval;
            Wave++;
        }

        // Bomb spawning
        float currentInterval = GetCurrentSpawnInterval();
        bombSpawnTimer += Time.deltaTime;
        if (bombSpawnTimer >= currentInterval)
        {
            bombSpawnTimer -= currentInterval;
            TrySpawnBomb();
        }

        // Coin spawning
        coinSpawnTimer += Time.deltaTime;

        if (coinSpawnTimer >= coinSpawnInterval)
        {
            coinSpawnTimer = 0f;
            if (coinProbability > Random.value)
                TrySpawnCoin();
        }

        // If ALL full, speed up movement of active objects
        float fullMul = AreAllSpawnPointsFull() ? fullSpeedMultiplier : 1f;
        ApplySpeedToActive(fullMul);
    }

    private float GetCurrentSpawnInterval()
    {
        float interval = baseSpawnInterval * Mathf.Pow(spawnIntervalMultiplierPerWave, Wave - 1);
        return Mathf.Max(minSpawnInterval, interval);
    }

    private void TrySpawnCoin()
    {
        if (coinPrefabs == null) return;

        int spawnIndex = GetVacantSpawnIndex();
        if (spawnIndex == -1) return; // no vacant point

        Transform spawn = spawnLocations[spawnIndex];

        Coin coin = Instantiate(coinPrefabs, spawn.position, Quaternion.identity);
        coin.fallSpeed = coinSpeed * WaveSpeedMultiplier;
        coin.pushDelay = coinPushDelay;
        coin.score = Random.Range(minScore / 10, maxScore / 10 + 1) * 10;
        if (coin.scoreText != null) coin.scoreText.text = coin.score.ToString();

        MarkOccupied(spawnIndex, coin: coin, bomb: null);
    }

    private void TrySpawnBomb()
    {
        int spawnIndex = GetVacantSpawnIndex();
        if (spawnIndex == -1) return; // no vacant point

        BombType type = PickBombTypeWeighted();
        Transform spawn = spawnLocations[spawnIndex];

        Bomb bomb = Instantiate(type.bombPrefab, spawn.position, Quaternion.identity);
        bomb.fallSpeed = type.speed * WaveSpeedMultiplier;
        bomb.pushDelay = type.pushDelay;

        bombBaseSpeeds[spawnIndex] = type.speed;
        MarkOccupied(spawnIndex, coin: null, bomb: bomb);
    }

    private void MarkOccupied(int index, Coin coin, Bomb bomb)
    {
        occupied[index] = true;
        activeCoins[index] = coin;
        activeBombs[index] = bomb;

        GameObject go = coin != null ? coin.gameObject : bomb.gameObject;

        // Add / reuse the callback component
        SpawnOccupant occ = go.GetComponent<SpawnOccupant>();
        if (occ == null) occ = go.AddComponent<SpawnOccupant>();

        occ.spawnIndex = index;
        occ.onDestroyed = OnOccupantDestroyed;
    }

    private void OnOccupantDestroyed(int index)
    {
        // if scene is closing, arrays might already be gone
        if (occupied == null || index < 0 || index >= occupied.Length) return;

        occupied[index] = false;
        activeCoins[index] = null;
        activeBombs[index] = null;
        bombBaseSpeeds[index] = 0f;
    }

    private int GetVacantSpawnIndex()
    {
        int count = 0;
        for (int i = 0; i < occupied.Length; i++)
            if (!occupied[i]) count++;

        if (count == 0) return -1;

        int pick = Random.Range(0, count);
        for (int i = 0; i < occupied.Length; i++)
        {
            if (!occupied[i])
            {
                if (pick == 0) return i;
                pick--;
            }
        }

        return -1;
    }

    private bool AreAllSpawnPointsFull()
    {
        for (int i = 0; i < occupied.Length; i++)
            if (!occupied[i]) return false;
        return occupied.Length > 0;
    }

    private void ApplySpeedToActive(float fullMultiplier)
    {
        float waveMul = WaveSpeedMultiplier;

        for (int i = 0; i < occupied.Length; i++)
        {
            if (!occupied[i]) continue;

            if (activeCoins[i] != null)
                activeCoins[i].fallSpeed = coinSpeed * waveMul * fullMultiplier;

            if (activeBombs[i] != null)
                activeBombs[i].fallSpeed = bombBaseSpeeds[i] * waveMul * fullMultiplier;
        }
    }

    private void DestroyAllActive()
    {
        // Destroy ALL items in the scene, not just tracked ones
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null)
                Destroy(allItems[i].gameObject);
        }

        // Clear tracking arrays
        for (int i = 0; i < occupied.Length; i++)
        {
            occupied[i] = false;
            activeCoins[i] = null;
            activeBombs[i] = null;
            bombBaseSpeeds[i] = 0f;
        }
    }

    private BombType PickBombTypeWeighted()
    {
        float total = 0f;
        for (int i = 0; i < bombTypes.Length; i++)
            total += Mathf.Max(0f, bombTypes[i].probability);

        if (total <= 0f)
            return bombTypes.Length > 0 ? bombTypes[0] : new BombType();

        float roll = Random.value * total;

        for (int i = 0; i < bombTypes.Length; i++)
        {
            float w = Mathf.Max(0f, bombTypes[i].probability);
            roll -= w;
            if (roll <= 0f)
                return bombTypes[i];
        }

        return bombTypes[bombTypes.Length - 1];
    }
}
