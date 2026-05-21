using UnityEngine;

public class PlateSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefabToSpawn;
    public float spawnInterval = 3f;
    public int maxPlates = 10;

    void Start()
    {
        InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval);
    }

    void SpawnObject()
    {
        if (GameObject.FindGameObjectsWithTag("Plate").Length >= maxPlates)
            return;

        Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
    }
}