using UnityEngine;
using TMPro;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 4f;
    public LayerMask pickupMask;
    public float pickupCooldown = 0.02f; // 🔥 pickup speed

    private float pickupTimer;

    [Header("Highlight")]
    public Material highlightMaterial;

    private GameObject currentItem;
    private Renderer currentRenderer;
    private Material[] originalMaterials;

    [Header("Inventory")]
    public int sashimi = 0;

    [Header("Order Settings")]
    public int minOrderAmount = 1;
    public int maxOrderAmount = 5;
    private int requiredAmount;

    [Header("UI")]
    public TMP_Text orderText;
    public TMP_Text inventoryText;
    public TMP_Text resultText;

    [Header("Interaction")]
    public float interactDistance = 4f;
    public LayerMask interactMask;
    public bool showRay = true;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        GenerateOrder();
        UpdateUI();
    }

    void Update()
    {
        FindClosestItem();

        if (showRay)
            DrawRay();

        HandlePickupHold();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TrySubmitOrder();
        }
    }

    // ================= HOLD TO PICKUP =================

    void HandlePickupHold()
    {
        pickupTimer -= Time.deltaTime;

        if (Input.GetKey(KeyCode.E) && currentItem != null && pickupTimer <= 0f)
        {
            PickupItem();
            pickupTimer = pickupCooldown;
        }
    }

    // ================= PICKUP + HIGHLIGHT =================

    void FindClosestItem()
    {
        Collider[] items = Physics.OverlapSphere(transform.position, pickupRange, pickupMask);

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider col in items)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = col.gameObject;
            }
        }

        if (currentItem != closest)
        {
            RemoveHighlight();
            currentItem = closest;
            ApplyHighlight();
        }
    }

    void ApplyHighlight()
    {
        if (currentItem == null) return;

        currentRenderer = currentItem.GetComponent<Renderer>();
        if (currentRenderer == null) return;

        originalMaterials = currentRenderer.materials;

        Material[] newMats = new Material[originalMaterials.Length + 1];

        for (int i = 0; i < originalMaterials.Length; i++)
            newMats[i] = originalMaterials[i];

        newMats[newMats.Length - 1] = highlightMaterial;

        currentRenderer.materials = newMats;
    }

    void RemoveHighlight()
    {
        if (currentRenderer != null && originalMaterials != null)
        {
            currentRenderer.materials = originalMaterials;
        }

        currentRenderer = null;
        originalMaterials = null;
    }

    void PickupItem()
    {
        if (currentItem != null && currentItem.CompareTag("Sashimi"))
        {
            sashimi++;
        }

        Destroy(currentItem);
        currentItem = null;

        UpdateUI();
    }

    // ================= ORDER SYSTEM =================

    void GenerateOrder()
    {
        requiredAmount = Random.Range(minOrderAmount, maxOrderAmount + 1);
        UpdateUI();
    }

    void TrySubmitOrder()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactMask))
        {
            if (hit.collider.CompareTag("SubmitStation"))
            {
                AttemptCompleteOrder();
            }
        }
    }

    void AttemptCompleteOrder()
    {
        if (sashimi >= requiredAmount)
        {
            sashimi -= requiredAmount;

            if (resultText != null)
                resultText.text = "Order Complete!";

            GenerateOrder();

            CancelInvoke(nameof(ClearResultText));
            Invoke(nameof(ClearResultText), 2f);
        }
        else
        {
            if (resultText != null)
                resultText.text = "Not Enough Sashimi!";
        }

        UpdateUI();
    }

    void ClearResultText()
    {
        if (resultText != null)
            resultText.text = "";
    }

    // ================= UI =================

    void UpdateUI()
    {
        if (orderText != null)
        {
            orderText.text =
                "<b>Order:</b>\n" +
                "Sashimi: " + requiredAmount;
        }

        if (inventoryText != null)
        {
            inventoryText.text =
                "Sashimi: " + sashimi;
        }
    }

    // ================= DEBUG =================

    void DrawRay()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);
    }
}