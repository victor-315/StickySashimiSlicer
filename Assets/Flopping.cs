using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Flopping : MonoBehaviour
{
    [Header("Flop Settings")]
    public float flopForce = 2f;
    public float upwardForce = 1f;
    public float torqueForce = 2f;

    public float minFlopDelay = 0.5f;
    public float maxFlopDelay = 1.5f;

    private Rigidbody rb;
    private float flopTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetNextFlop();
    }

    void Update()
    {
        flopTimer -= Time.deltaTime;

        if (flopTimer <= 0f)
        {
            Flop();
            SetNextFlop();
        }
    }

    void Flop()
    {
        // random sideways direction
        Vector3 dir = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        // add upward motion
        dir.y = upwardForce;

        // apply force
        rb.AddForce(dir * flopForce, ForceMode.Impulse);

        // add spin
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
    }

    void SetNextFlop()
    {
        flopTimer = Random.Range(minFlopDelay, maxFlopDelay);
    }
}