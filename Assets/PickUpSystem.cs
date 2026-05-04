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

    [Header("Plating System")]
    public bool holdingPlate = false;
    public int sashimiOnPlate = 0;
    public int maxSashimiOnPlate = 5;
    public Transform plateHoldPoint;
    private GameObject heldPlateObject;

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
    public TMP_Text plateText;

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
                TryInteractWithStation();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropPlate();
        }
    }

    // ================= PICKUP =================

    void PickupItem()
    {
        if (currentItem == null) return;

        if (currentItem.CompareTag("Plate"))
        {
            PickupPlate();
            return;
        }

        if (currentItem.CompareTag("Sashimi"))
        {
            if (holdingPlate)
            {
                if (sashimiOnPlate >= maxSashimiOnPlate)
                {
                    ShowMessage("Plate Full!");
                    return;
                }

                sashimiOnPlate++;
                RemoveHighlight();
                Destroy(currentItem);
                currentItem = null;
                UpdateUI();
                return;
            }

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

    // ================= PLATING SYSTEM =================

    void PickupPlate()
    {
        if (holdingPlate)
        {
            ShowMessage("Already holding a plate!");
            return;
        }

        holdingPlate = true;
        sashimiOnPlate = 0;

        if (plateHoldPoint != null)
        {
            currentItem.transform.SetParent(plateHoldPoint);
            currentItem.transform.localPosition = Vector3.zero;
            currentItem.transform.localRotation = Quaternion.identity;
            heldPlateObject = currentItem;

            Collider plateCol = heldPlateObject.GetComponent<Collider>();
            if (plateCol != null) plateCol.enabled = false;
        }
        else
        {
            heldPlateObject = currentItem;
            heldPlateObject.SetActive(false);
        }

        RemoveHighlight();
        currentItem = null;

        ShowMessage("Plate picked up! Load sashimi, then serve.", 2f);
        UpdateUI();
    }

    void DropPlate()
    {
        if (!holdingPlate) return;

        if (heldPlateObject != null)
        {
            heldPlateObject.transform.SetParent(null);

            Collider plateCol = heldPlateObject.GetComponent<Collider>();
            if (plateCol != null) plateCol.enabled = true;

            heldPlateObject.SetActive(true);
            heldPlateObject.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
            heldPlateObject = null;
        }

        holdingPlate = false;
        sashimiOnPlate = 0;

        ShowMessage("Plate dropped.", 1.5f);
        UpdateUI();
    }

    // ================= STATION INTERACTION =================

    void TryInteractWithStation()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance, interactMask))
            return;

        if (hit.collider.CompareTag("SubmitStation"))
        {
            TrySubmitOrder();
        }
        else if (hit.collider.CompareTag("PlateStation"))
        {
            TryPickupPlateFromStation();
        }
    }

    void TrySubmitOrder()
    {
        if (!holdingPlate)
        {
            ShowMessage("You need a plated dish!", 2f);
            UpdateUI();
            return;
        }

        if (sashimiOnPlate >= requiredAmount)
        {
            sashimiOnPlate = 0;
            holdingPlate = false;

            if (heldPlateObject != null)
            {
                Destroy(heldPlateObject);
                heldPlateObject = null;
            }

            ShowMessage("Order Complete!", 2f);
            GenerateOrder();
        }
        else
        {
            ShowMessage("Not Enough Sashimi on Plate! (" + sashimiOnPlate + "/" + requiredAmount + ")", 2f);
        }

        UpdateUI();
    }

    void TryPickupPlateFromStation()
    {
        if (holdingPlate)
        {
            ShowMessage("Already holding a plate!", 2f);
            return;
        }

        holdingPlate = true;
        sashimiOnPlate = 0;
        heldPlateObject = null;

        ShowMessage("Plate picked up! Load sashimi, then serve.", 2f);
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
            inventoryText.text = "Sashimi (Bag): " + sashimi + " / " + maxSashimi;

        if (plateText != null)
        {
            if (holdingPlate)
                plateText.text = "<b>Plate:</b> " + sashimiOnPlate + " / " + requiredAmount + " sashimi";
            else
                plateText.text = "<b>Plate:</b> None";
        }
    }
}