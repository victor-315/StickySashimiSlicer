using UnityEngine;

public class FishDestroyer : MonoBehaviour
{
    [Header("Settings")]
    public float interactDistance = 5f;
    public LayerMask interactMask;

    [Header("Spawn")]
    public GameObject spawnPrefab;
    public int minSpawn = 3;
    public int maxSpawn = 5;

    [Header("Soft Explosion")]
    public float explosionForce = 1.5f;   // 🔥 very weak force
    public float upwardBias = 0.5f;       // slight lift
    public float randomTorque = 2f;       // small spin

    [Header("Spread")]
    public float spawnSpread = 0.5f;

    [Header("Debug")]
    public bool showRay = true;
    public Color rayColor = Color.red;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (showRay)
            DrawRay();

        if (Input.GetMouseButtonDown(0))
            TryDestroyFish();
    }

    void DrawRay()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, rayColor);
    }

    void TryDestroyFish()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactMask))
        {
            GameObject obj = hit.collider.gameObject;

            if (obj.CompareTag("fish"))
            {
                Vector3 center = obj.transform.position;

                Destroy(obj);

                int count = Random.Range(minSpawn, maxSpawn + 1);

                for (int i = 0; i < count; i++)
                {
                    // 🎯 spawn position
                    Vector3 offset = new Vector3(
                        Random.Range(-spawnSpread, spawnSpread),
                        0.2f,
                        Random.Range(-spawnSpread, spawnSpread)
                    );

                    GameObject spawned = Instantiate(
                        spawnPrefab,
                        center + offset,
                        Random.rotation
                    );

                    Rigidbody rb = spawned.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        // 🔥 weak outward explosion
                        Vector3 dir = (spawned.transform.position - center).normalized;

                        if (dir == Vector3.zero)
                            dir = Random.insideUnitSphere;

                        dir.y = Mathf.Abs(dir.y) + upwardBias;

                        rb.AddForce(dir * explosionForce, ForceMode.Impulse);

                        // 🎲 tiny spin
                        rb.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
                    }
                }
            }
        }
    }
}