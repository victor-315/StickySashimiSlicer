using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 4f;
    public LayerMask pickupMask;

    [Header("UI")]
    public TMP_Text counterText;

    [Header("Highlight")]
    public Color highlightColor = Color.yellow;

    private Camera cam;
    private GameObject currentItem;
    private Renderer currentRenderer;
    private Color originalColor;

    private int itemCount = 0;

    void Start()
    {
        cam = Camera.main;
        UpdateUI();
    }

    void Update()
    {
        FindClosestItem();

        if (Input.GetKeyDown(KeyCode.E) && currentItem != null)
        {
            PickupItem();
        }
    }

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

        // Remove highlight from old item
        if (currentItem != null && currentItem != closest)
        {
            ResetHighlight();
        }

        currentItem = closest;

        // Apply highlight to new item
        if (currentItem != null)
        {
            Renderer rend = currentItem.GetComponent<Renderer>();

            if (rend != null && rend != currentRenderer)
            {
                currentRenderer = rend;
                originalColor = rend.material.color;
                rend.material.color = highlightColor;
            }
        }
    }

    void ResetHighlight()
    {
        if (currentRenderer != null)
        {
            currentRenderer.material.color = originalColor;
        }

        currentRenderer = null;
    }

    void PickupItem()
    {
        ResetHighlight();

        Destroy(currentItem);
        currentItem = null;

        itemCount++;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (counterText != null)
        {
            counterText.text = "Items: " + itemCount;
        }
    }
}