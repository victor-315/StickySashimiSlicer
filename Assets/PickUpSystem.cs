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
    public LayerMask plateMask;

    [Header("Order Settings")]
    public int minOrderAmount = 1;
    public int maxOrderAmount = 5;
    private int requiredAmount;

    [Header("Serve System")]
    public float interactDistance = 4f;
    public LayerMask interactMask;

    [Header("Hold Pickup Settings")]
    public float holdPickupInterval = 0.2f;
    private float holdPickupTimer = 0f;

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

    private int PlateLayer => Mathf.RoundToInt(Mathf.Log(plateMask.value, 2));

    // ================= PLATE MEMORY COMPONENT =================

    public class PlateData : MonoBehaviour
    {
        public int sashimiCount = 0;
    }

    // ================= HELPERS =================

    PlateData GetOrAddPlateData(GameObject plateObj)
    {
        PlateData pd = plateObj.GetComponent<PlateData>();
        if (pd == null) pd = plateObj.AddComponent<PlateData>();
        return pd;
    }

    void SyncPlateFromObject()
    {
        if (heldPlateObject != null)
            sashimiOnPlate = GetOrAddPlateData(heldPlateObject).sashimiCount;
    }

    void SyncPlateToObject()
    {
        if (heldPlateObject != null)
            GetOrAddPlateData(heldPlateObject).sashimiCount = sashimiOnPlate;
    }

    // ==========================================================

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

            holdPickupTimer = holdPickupInterval;
        }

        if (Input.GetKey(KeyCode.E) && currentItem != null && currentItem.CompareTag("Sashimi"))
        {
            holdPickupTimer -= Time.deltaTime;
            if (holdPickupTimer <= 0f)
            {
                PickupItem();
                holdPickupTimer = holdPickupInterval;
            }
        }

        if (Input.GetMouseButtonDown(0))
            TryPlateMouseInteract();

        if (Input.GetKeyDown(KeyCode.Q))
            DropHeldPlate();
    }

    // ================= PICKUP (sashimi / other items only) =================

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

    // ================= LMB — ALL PLATE PICKUP / PUTDOWN / SUBMIT / TRASH =================

    void TryPlateMouseInteract()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance, plateMask | interactMask))
            return;

        string tag = hit.collider.tag;

        if (tag == "Plate")
        {
            PickupPlateFromWorld(hit.collider.gameObject);
            return;
        }

        if (tag == "StationPlate")
        {
            if (holdingPlate)
            {
                ShowMessage("Already holding a plate!", 2f);
                return;
            }
            PickUpPlateFromStation();
            return;
        }

        if (tag == "PlatingStation")
        {
            if (plateAtStation && !holdingPlate)
            {
                PickUpPlateFromStation();
                return;
            }

            if (!holdingPlate)
            {
                ShowMessage("Grab a plate first!", 2f);
                return;
            }

            PlaceHeldPlateOnStation(hit.collider.transform);
            return;
        }

        if (tag == "SubmitStation")
        {
            TrySubmitOrder();
            return;
        }

        if (tag == "TrashStation")
        {
            TryTrashHeldPlate();
            return;
        }
    }

    // ================= PLATE — WORLD PICKUP =================

    void PickupPlateFromWorld(GameObject plateObj)
    {
        if (holdingPlate)
        {
            ShowMessage("Already holding a plate!", 2f);
            return;
        }

        holdingPlate = true;

        // Read existing sashimi count from the plate object itself
        sashimiOnPlate = GetOrAddPlateData(plateObj).sashimiCount;

        AttachPlateToHand(plateObj);

        ShowMessage("Plate picked up! (" + sashimiOnPlate + " sashimi on plate)", 2f);
        UpdateUI();
    }

    void AttachPlateToHand(GameObject plateObj)
    {
        plateObj.transform.SetParent(null);

        Rigidbody rb = plateObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        plateObj.tag = "Plate";
        plateObj.layer = PlateLayer;

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
            // Save current count back to the object before releasing it
            SyncPlateToObject();

            heldPlateObject.transform.SetParent(null);

            Rigidbody rb = heldPlateObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider c = heldPlateObject.GetComponent<Collider>();
            if (c != null) c.enabled = true;

            heldPlateObject.layer = PlateLayer;
            heldPlateObject.tag = "Plate";
            heldPlateObject.SetActive(true);
            heldPlateObject.transform.position =
                transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;

            heldPlateObject = null;
        }

        holdingPlate = false;
        sashimiOnPlate = 0;

        ShowMessage("Plate dropped.", 1.5f);
        UpdateUI();
    }

    // ================= STATION INTERACTION (E) — SASHIMI LOADING ONLY =================

    void TryInteractWithStation()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance, interactMask))
            return;

        string tag = hit.collider.tag;

        if (tag == "StationPlate" || tag == "PlatingStation")
            TryLoadSashimiOntoStationPlate();
    }

    // ================= PLATING STATION =================

    void PlaceHeldPlateOnStation(Transform stationTransform)
    {
        if (plateAtStation)
        {
            ShowMessage("Station already has a plate!", 2f);
            return;
        }

        // Save current sashimi count to the plate object before putting it down
        SyncPlateToObject();

        plateAtStation = true;
        sashimiAtStation = sashimiOnPlate;
        stationPlateObject = heldPlateObject;
        platingStationTransform = stationTransform;

        if (stationPlateObject != null)
        {
            stationPlateObject.transform.SetParent(null);

            Rigidbody rb = stationPlateObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            stationPlateObject.tag = "StationPlate";
            stationPlateObject.layer = interactLayer;

            Collider c = stationPlateObject.GetComponent<Collider>();
            if (c != null) c.enabled = true;

            stationPlateObject.SetActive(true);
            stationPlateObject.transform.position =
                stationTransform.position + Vector3.up * 0.8f;
        }

        holdingPlate = false;
        heldPlateObject = null;
        sashimiOnPlate = 0;

        ShowMessage("Plate set down. Press E to load sashimi.", 2f);
        UpdateUI();
    }

    void PickUpPlateFromStation()
    {
        holdingPlate = true;

        // Read sashimi count from the plate object itself
        if (stationPlateObject != null)
            sashimiOnPlate = GetOrAddPlateData(stationPlateObject).sashimiCount;
        else
            sashimiOnPlate = sashimiAtStation;

        if (stationPlateObject != null)
        {
            stationPlateObject.tag = "Plate";
            stationPlateObject.layer = PlateLayer;
            AttachPlateToHand(stationPlateObject);
        }

        plateAtStation = false;
        sashimiAtStation = 0;
        stationPlateObject = null;
        platingStationTransform = null;

        ShowMessage("Plate picked up! (" + sashimiOnPlate + " sashimi on plate)", 2f);
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

        // Keep PlateData in sync as sashimi is loaded
        if (stationPlateObject != null)
            GetOrAddPlateData(stationPlateObject).sashimiCount = sashimiAtStation;

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

        holdingPlate = false;
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
            holdingPlate = false;

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

    // ================= HIGHLIGHT (sashimi / non-plate items only) =================

    void FindClosestItem()
    {
        Collider[] items = Physics.OverlapSphere(transform.position, pickupRange, pickupMask);

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider col in items)
        {
            if (col.CompareTag("Plate") || col.CompareTag("StationPlate")) continue;

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