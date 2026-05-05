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

    [Header("Layer Settings")]
    public int pickupLayer = 6;
    public int interactLayer = 7;

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

    private bool plateAtStation = false;
    private int sashimiAtStation = 0;
    private GameObject stationPlateObject;
    private Transform platingStationTransform;

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
            DropHeldPlate();
    }

    // ================= PICKUP =================

    void PickupItem()
    {
        if (currentItem == null) return;

        if (currentItem.CompareTag("Plate"))
        {
            PickupPlateFromWorld();
            return;
        }

        if (currentItem.CompareTag("Sashimi"))
        {
            if (sashimi >= maxSashimi)
            {
                ShowMessage("Bag Full!");
                return;
            }

            sashimi++;
            RemoveHighlight();
            Destroy(currentItem);
            currentItem = null;
            UpdateUI();
            return;
        }

        RemoveHighlight();
        Destroy(currentItem);
        currentItem = null;
        UpdateUI();
    }

    // ================= PLATE — WORLD PICKUP =================

    void PickupPlateFromWorld()
    {
        if (holdingPlate)
        {
            ShowMessage("Already holding a plate!");
            return;
        }

        holdingPlate = true;
        sashimiOnPlate = 0;

        AttachPlateToHand(currentItem);

        RemoveHighlight();
        currentItem = null;

        ShowMessage("Plate picked up! Take it to the Plating Station.", 2f);
        UpdateUI();
    }

    // Puts the plate in hand — disables its collider and physics while held
    void AttachPlateToHand(GameObject plateObj)
    {
        // Unparent cleanly first in case it was sitting on the station
        plateObj.transform.SetParent(null);

        Rigidbody rb = plateObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        plateObj.tag   = "Plate";
        plateObj.layer = pickupLayer;

        // Disable collider while held — we don't need it
        Collider c = plateObj.GetComponent<Collider>();
        if (c != null) c.enabled = false;

        if (plateHoldPoint != null)
        {
            plateObj.transform.SetParent(plateHoldPoint);
            plateObj.transform.localPosition = Vector3.zero;
            plateObj.transform.localRotation = Quaternion.identity;
        }

        plateObj.SetActive(true);
        heldPlateObject = plateObj;
    }

    // ================= DROP HELD PLATE (Q) =================

    void DropHeldPlate()
    {
        if (!holdingPlate) return;

        if (heldPlateObject != null)
        {
            heldPlateObject.transform.SetParent(null);

            Rigidbody rb = heldPlateObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider c = heldPlateObject.GetComponent<Collider>();
            if (c != null) c.enabled = true;

            heldPlateObject.layer = pickupLayer;
            heldPlateObject.tag   = "Plate";
            heldPlateObject.SetActive(true);
            heldPlateObject.transform.position =
                transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;

            heldPlateObject = null;
        }

        holdingPlate   = false;
        sashimiOnPlate = 0;

        ShowMessage("Plate dropped.", 1.5f);
        UpdateUI();
    }

    // ================= STATION INTERACTION =================

    void TryInteractWithStation()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Only care about the very first thing the ray hits on interactMask
        if (!Physics.Raycast(ray, out hit, interactDistance, interactMask))
            return;

        string tag = hit.collider.tag;

        if (tag == "SubmitStation")
            TrySubmitOrder();
        else if (tag == "PlatingStation")
            TryInteractWithPlatingStation(hit.collider.transform);
        else if (tag == "StationPlate")
            TryLoadSashimiOntoStationPlate();
        else if (tag == "TrashStation")
            TryTrashHeldPlate();
    }

    // ================= PLATING STATION =================

    void TryInteractWithPlatingStation(Transform stationTransform)
    {
        // No plate here — deposit the one we're holding
        if (!plateAtStation)
        {
            if (!holdingPlate)
            {
                ShowMessage("Grab a plate first!", 2f);
                return;
            }

            PlaceHeldPlateOnStation(stationTransform);
            return;
        }

        // Plate already here — pick it back up
        if (holdingPlate)
        {
            ShowMessage("Already holding a plate!", 2f);
            return;
        }

        PickUpPlateFromStation();
    }

    void PlaceHeldPlateOnStation(Transform stationTransform)
    {
        plateAtStation          = true;
        sashimiAtStation        = sashimiOnPlate;
        stationPlateObject      = heldPlateObject;
        platingStationTransform = stationTransform;

        if (stationPlateObject != null)
        {
            // Unparent from hand
            stationPlateObject.transform.SetParent(null);

            Rigidbody rb = stationPlateObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Switch to StationPlate tag and interactLayer so the raycast
            // hits the plate separately from the station beneath it
            stationPlateObject.tag   = "StationPlate";
            stationPlateObject.layer = interactLayer;

            // Re-enable collider so the raycast can actually hit it
            Collider c = stationPlateObject.GetComponent<Collider>();
            if (c != null) c.enabled = true;

            stationPlateObject.SetActive(true);
            stationPlateObject.transform.position =
                stationTransform.position + Vector3.up * 0.8f;
        }

        holdingPlate    = false;
        heldPlateObject = null;
        sashimiOnPlate  = 0;

        ShowMessage("Plate set down. Look at the plate and press E to load sashimi.", 2f);
        UpdateUI();
    }

    void PickUpPlateFromStation()
    {
        holdingPlate   = true;
        sashimiOnPlate = sashimiAtStation;

        if (stationPlateObject != null)
        {
            stationPlateObject.tag   = "Plate";
            stationPlateObject.layer = pickupLayer;
            AttachPlateToHand(stationPlateObject);
        }

        plateAtStation          = false;
        sashimiAtStation        = 0;
        stationPlateObject      = null;
        platingStationTransform = null;

        ShowMessage("Plate picked up! Take it to the serve station.", 2f);
        UpdateUI();
    }

    // ================= LOAD SASHIMI ONTO STATION PLATE =================

    void TryLoadSashimiOntoStationPlate()
    {
        if (!plateAtStation)
        {
            ShowMessage("No plate at station!", 2f);
            return;
        }

        if (sashimi <= 0)
        {
            ShowMessage("No sashimi in bag!", 2f);
            return;
        }

        if (sashimiAtStation >= maxSashimiOnPlate)
        {
            ShowMessage("Plate is full! (" + sashimiAtStation + "/" + maxSashimiOnPlate + ")", 2f);
            return;
        }

        sashimi--;
        sashimiAtStation++;
        ShowMessage("Loaded sashimi. (" + sashimiAtStation + "/" + maxSashimiOnPlate + ")", 1.5f);
        UpdateUI();
    }

    // ================= TRASH STATION =================

    void TryTrashHeldPlate()
    {
        if (!holdingPlate)
        {
            ShowMessage("Not holding anything to trash!", 2f);
            return;
        }

        if (heldPlateObject != null)
        {
            Destroy(heldPlateObject);
            heldPlateObject = null;
        }

        holdingPlate   = false;
        sashimiOnPlate = 0;

        ShowMessage("Plate trashed.", 2f);
        UpdateUI();
    }

    // ================= SUBMIT / SERVE =================

    void TrySubmitOrder()
    {
        if (!holdingPlate)
        {
            ShowMessage("You need a plated dish!", 2f);
            UpdateUI();
            return;
        }

        if (sashimiOnPlate == requiredAmount)
        {
            sashimiOnPlate = 0;
            holdingPlate   = false;

            if (heldPlateObject != null)
            {
                Destroy(heldPlateObject);
                heldPlateObject = null;
            }

            ShowMessage("Order Complete!", 2f);
            GenerateOrder();
        }
        else if (sashimiOnPlate < requiredAmount)
        {
            ShowMessage("Not enough sashimi! (" + sashimiOnPlate + "/" + requiredAmount + ")", 2f);
        }
        else
        {
            ShowMessage("Too much sashimi! (" + sashimiOnPlate + "/" + requiredAmount + ") — trash it and try again.", 2f);
        }

        UpdateUI();
    }

    // ================= ORDER =================

    void GenerateOrder()
    {
        requiredAmount = Random.Range(minOrderAmount, maxOrderAmount + 1);
        UpdateUI();
    }

    // ================= MESSAGE =================

    void ShowMessage(string msg, float time = 2f)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
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
            if (dist < minDist) { minDist = dist; closest = col.gameObject; }
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
        for (int i = 0; i < originalMaterials.Length; i++) newMats[i] = originalMaterials[i];
        newMats[newMats.Length - 1] = highlightMaterial;
        currentRenderer.materials = newMats;
    }

    void RemoveHighlight()
    {
        if (currentRenderer != null && originalMaterials != null)
            currentRenderer.materials = originalMaterials;
        currentRenderer = null;
        originalMaterials = null;
    }

    // ================= UI =================

    void UpdateUI()
    {
        if (orderText != null)
            orderText.text = "<b>Order:</b> " + requiredAmount + " sashimi";

        if (inventoryText != null)
            inventoryText.text = "Bag: " + sashimi + " / " + maxSashimi + " sashimi";

        if (plateText != null)
        {
            if (holdingPlate)
                plateText.text = "<b>Plate (held):</b> " + sashimiOnPlate + " / " + requiredAmount + " sashimi";
            else if (plateAtStation)
                plateText.text = "<b>Plate (station):</b> " + sashimiAtStation + " / " + maxSashimiOnPlate + " sashimi";
            else
                plateText.text = "<b>Plate:</b> None";
        }
    }
}