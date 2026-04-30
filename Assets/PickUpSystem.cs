using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 4f;
    public LayerMask pickupMask;

    [Header("Highlight")]
    public Material highlightMaterial;

    private GameObject currentItem;
    private Renderer currentRenderer;
    private Material[] originalMaterials;

    [Header("Inventory")]
    public int sashimi = 0;
    public int maxSashimi = 10;

    [Header("Order Settings")]
    public int minOrderAmount = 1;
    public int maxOrderAmount = 5;
    private int requiredAmount;

    [Header("Serve System")]
    public float interactDistance = 4f;
    public LayerMask interactMask;

    [Header("UI")]
    public TMP_Text orderText;
    public TMP_Text inventoryText;
    public TMP_Text resultText;

    private Camera cam;
    private Coroutine messageRoutine;

    void Start()
    {
        cam = Camera.main;
        GenerateOrder();
        UpdateUI();
    }

    void Update()
    {
        FindClosestItem();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentItem != null)
                PickupItem();
            else
                TrySubmitOrder();
        }
    }

    // ================= PICKUP =================

    void PickupItem()
    {
        if (currentItem == null) return;

        if (currentItem.CompareTag("Sashimi"))
        {
            if (sashimi >= maxSashimi)
            {
                ShowMessage("Bag Full!");
                return;
            }

            sashimi++;
        }

        RemoveHighlight();
        Destroy(currentItem);
        currentItem = null;

        UpdateUI();
    }

    // ================= SERVE TABLE FIX =================

    void TrySubmitOrder()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ✔ NOW USING CORRECT INTERACT MASK (SERVE TABLE REQUIRED)
        if (!Physics.Raycast(ray, out hit, interactDistance, interactMask))
            return;

        if (!hit.collider.CompareTag("SubmitStation"))
            return;

        if (sashimi >= requiredAmount)
        {
            sashimi -= requiredAmount;

            ShowMessage("Order Complete!", 2f);

            GenerateOrder();
        }
        else
        {
            ShowMessage("Not Enough Sashimi!", 2f);
        }

        UpdateUI();
    }

    // ================= ORDER =================

    void GenerateOrder()
    {
        requiredAmount = Random.Range(minOrderAmount, maxOrderAmount + 1);
        UpdateUI();
    }

    // ================= MESSAGE SYSTEM =================

    void ShowMessage(string msg, float time = 2f)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(MessageRoutine(msg, time));
    }

    IEnumerator MessageRoutine(string msg, float time)
    {
        resultText.text = msg;
        yield return new WaitForSeconds(time);
        resultText.text = "";
    }

    // ================= HIGHLIGHT =================

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

    // ================= UI =================

    void UpdateUI()
    {
        if (orderText != null)
            orderText.text = "<b>Order:</b>\nSashimi: " + requiredAmount;

        if (inventoryText != null)
            inventoryText.text = "Sashimi: " + sashimi + " / " + maxSashimi;
    }
}