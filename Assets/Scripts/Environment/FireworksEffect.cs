using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireworksEffect : MonoBehaviour
{
    [Tooltip("Firework particle prefab from the Project window. The Particle System must be on the root object.")]
    [SerializeField] ParticleSystem fireworkPrefab;

    [Tooltip("Optional. If empty, fireworks appear in front of the camera at random spots.")]
    [SerializeField] Transform[] spawnPoints;

    [Header("Camera placement (used when no spawn points are set)")]
    [SerializeField] float distanceFromCamera = 25f;
    [SerializeField] float spreadX = 12f;
    [SerializeField] float heightMin = 6f;
    [SerializeField] float heightMax = 14f;

    [Header("Timing")]
    [SerializeField, Min(1)] int count = 8;
    [SerializeField] float delayBetweenBursts = 0.25f;
    [Tooltip("Safety cleanup. Set longer than the longest firework.")]
    [SerializeField] float destroyAfter = 8f;

    [Header("Safety")]
    [Tooltip("Removes every script (except this kind of particle component) from each spawned copy, so an asset author's auto-destroy script can't kill it.")]
    [SerializeField] bool removeOtherScripts = true;

    [SerializeField] AudioSource sound;
    [SerializeField] bool logDebug = true;

    readonly List<GameObject> spawned = new List<GameObject>();
    GameManager gm;
    Coroutine routine;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (gm == null) return;

        gm.OnHighScoreBeaten += Play;
        gm.OnRunStarted += ClearAll;
    }

    void OnDisable()
    {
        if (gm == null) return;

        gm.OnHighScoreBeaten -= Play;
        gm.OnRunStarted -= ClearAll;
    }

    [ContextMenu("Test Fireworks")]
    public void Play()
    {
        if (logDebug) Debug.Log("[Fireworks] Play called.", this);

        if (fireworkPrefab == null)
        {
            Debug.LogWarning("[Fireworks] No firework prefab assigned.", this);
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        if (sound != null) sound.Play();

        for (int i = 0; i < count; i++)
        {
            SpawnOne(i);
            yield return new WaitForSeconds(delayBetweenBursts);
        }

        routine = null;
    }

    void SpawnOne(int index)
    {
        Vector3 pos;
        Transform parent;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            if (point == null)
            {
                Debug.LogWarning($"[Fireworks] Spawn point {index % spawnPoints.Length} is missing.", this);
                return;
            }
            pos = point.position;
            parent = point;
        }
        else
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Fireworks] Camera.main is null. Tag the active camera as MainCamera, or assign Spawn Points.", this);
                return;
            }

            Transform c = cam.transform;
            pos = c.position
                + c.forward * distanceFromCamera
                + c.right * Random.Range(-spreadX, spreadX)
                + Vector3.up * Random.Range(heightMin, heightMax);
            parent = c;
        }

        // Create the copy under an inactive object so nothing runs (Awake, Start, author scripts) until it is set up.
        GameObject staging = new GameObject("Firework Staging");
        staging.SetActive(false);

        GameObject instance = Instantiate(fireworkPrefab.gameObject, pos, Quaternion.identity, staging.transform);

        if (removeOtherScripts)
        {
            foreach (MonoBehaviour mb in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null) DestroyImmediate(mb);
        }

        foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }

        ParticleSystem p = instance.GetComponent<ParticleSystem>();
        if (p == null)
        {
            Debug.LogWarning("[Fireworks] The Particle System must be on the prefab's root object.", this);
            Destroy(staging);
            Destroy(instance);
            return;
        }

        instance.SetActive(true);
        instance.transform.SetParent(parent, true);
        Destroy(staging);

        p.Clear(true);
        p.Play(true);

        spawned.Add(instance);
        Destroy(instance, destroyAfter);

        if (logDebug) Debug.Log($"[Fireworks] Spawned {instance.name} at {pos}, isPlaying = {p.isPlaying}", instance);
    }

    void ClearAll()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        foreach (GameObject go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }
}