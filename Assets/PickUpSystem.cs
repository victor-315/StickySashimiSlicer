using UnityEngine;
using UnityEngine.UI;
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
    public int maxSashimi = 50;
    public int rice = 0;
    // No rice cap

    [Header("Plating System")]
    public bool holdingPlate = false;
    public int sashimiOnPlate = 0;
    public int maxSashimiOnPlate = 5;
    public Transform plateHoldPoint;
    private GameObject heldPlateObject;

    [Header("Sashimi Visuals")]
    public GameObject sashimiPrefab;
    public float sashimiYRotRange = 180f;
    public float sashimiXZRotRange = 25f;
    public Vector3[] sashimiSlotOffsets = new Vector3[5]
    {
        new Vector3(-0.16f, 0.05f, 0.00f),
        new Vector3(-0.08f, 0.05f, 0.00f),
        new Vector3( 0.00f, 0.05f, 0.00f),
        new Vector3( 0.08f, 0.05f, 0.00f),
        new Vector3( 0.16f, 0.05f, 0.00f)
    };

    [Header("Layer Settings")]
    public int pickupLayer = 6;
    public int interactLayer = 7;
    public LayerMask plateMask;

    [Header("Order Settings")]
    public int minOrderAmount = 1;
    public int maxOrderAmount = 5;
    private int requiredAmount;

    [Header("Timer Settings")]
    public float initialOrderTimeLimit = 60f;
    public float timerDecreasePerOrder = 3f;   // How many seconds to shave off each order
    public float minOrderTimeLimit = 5f;        // Floor: never goes below this
    private float orderTimeLimit;               // Current effective time limit
    private float orderTimer = 0f;
    private bool timerRunning = false;

    [Header("Score Settings")]
    public float baseScore = 1000f;             // Max score achievable for an order
    private int currentScore = 0;
    private int highScore = 0;
    private int ordersCompleted = 0;

    [Header("Game Over Settings")]
    public float fadeDuration = 1.5f;           // Seconds for the black fade
    public Image fadeOverlay;                   // Full-screen black UI Image (alpha 0 at start)
    public TMP_Text gameOverText;               // "GAME OVER" label (hidden at start)
    public TMP_Text gameOverScoreText;          // Final score label (hidden at start)
    public TMP_Text gameOverHighScoreText;      // High score label on game over screen
    public GameObject gameOverPanel;            // Parent panel that holds all game-over UI
    public GameObject playAgainStationPrefab;   // Assign a simple cube/object in Inspector
    public Vector3 playAgainStationOffset = new Vector3(2f, 0f, 2f); // Spawn offset from player

    private bool isGameOver = false;
    private GameObject spawnedPlayAgainStation;

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
    public TMP_Text scoreText;      // Assign in Inspector — shows current score
    public TMP_Text highScoreText;  // Assign in Inspector — shows high score

    private bool plateAtStation = false;
    private int sashimiAtStation = 0;
    private GameObject stationPlateObject;
    private Transform platingStationTransform;

    private Camera cam;
    private Coroutine messageRoutine;

    private int PlateLayer => Mathf.RoundToInt(Mathf.Log(plateMask.value, 2));
    // Scans for the lowest set bit — safe even if multiple layers are ticked in interactMask
    private int InteractLayer
    {
        get
        {
            int mask = interactMask.value;
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0) return i;
            return 0;
        }
    }

    // ================= PLATE MEMORY COMPONENT =================

    public class PlateData : MonoBehaviour
    {
        public int sashimiCount = 0;
        public GameObject[] sashimiVisuals  = new GameObject[7];
        public Vector3[]    sashimiRotations = new Vector3[7];

        void OnDestroy()
        {
            PlateSpawner.currentPlateCount--;
        }
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

    void MakeVisualStatic(GameObject visual)
    {
        Rigidbody rb = visual.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        foreach (Rigidbody childRb in visual.GetComponentsInChildren<Rigidbody>(true))
            Destroy(childRb);
    }

    Vector3 RandomSashimiRotation()
    {
        return new Vector3(
            Random.Range(-sashimiXZRotRange, sashimiXZRotRange),
            Random.Range(-sashimiYRotRange,  sashimiYRotRange),
            Random.Range(-sashimiXZRotRange, sashimiXZRotRange)
        );
    }

    void AddSashimiVisual(GameObject plateObj)
    {
        if (sashimiPrefab == null) return;

        PlateData pd = GetOrAddPlateData(plateObj);
        int slot = pd.sashimiCount - 1;
        if (slot < 0 || slot >= sashimiSlotOffsets.Length) return;
        if (pd.sashimiVisuals[slot] != null) return;

        Vector3 rot = RandomSashimiRotation();
        pd.sashimiRotations[slot] = rot;

        GameObject visual = Instantiate(sashimiPrefab);
        MakeVisualStatic(visual);

        visual.transform.SetParent(plateObj.transform);
        visual.transform.localPosition = sashimiSlotOffsets[slot];
        visual.transform.localRotation = Quaternion.Euler(rot);

        pd.sashimiVisuals[slot] = visual;
    }

    void ClearSashimiVisuals(GameObject plateObj)
    {
        if (plateObj == null) return;
        PlateData pd = GetOrAddPlateData(plateObj);
        for (int i = 0; i < pd.sashimiVisuals.Length; i++)
        {
            if (pd.sashimiVisuals[i] != null)
            {
                Destroy(pd.sashimiVisuals[i]);
                pd.sashimiVisuals[i] = null;
            }
        }
    }

    void RebuildSashimiVisuals(GameObject plateObj)
    {
        if (plateObj == null) return;
        ClearSashimiVisuals(plateObj);
        PlateData pd = GetOrAddPlateData(plateObj);
        for (int i = 0; i < pd.sashimiCount; i++)
        {
            if (sashimiPrefab == null) break;
            if (i >= sashimiSlotOffsets.Length) break;

            GameObject visual = Instantiate(sashimiPrefab);
            MakeVisualStatic(visual);

            visual.transform.SetParent(plateObj.transform);
            visual.transform.localPosition = sashimiSlotOffsets[i];
            visual.transform.localRotation = Quaternion.Euler(pd.sashimiRotations[i]);

            pd.sashimiVisuals[i] = visual;
        }
    }

    // ==========================================================

    void Start()
    {
        cam = Camera.main;
        orderTimeLimit = initialOrderTimeLimit;

        // Ensure game over UI starts hidden
        if (gameOverPanel != null)            gameOverPanel.SetActive(false);
        if (gameOverText != null)             gameOverText.gameObject.SetActive(false);
        if (gameOverScoreText != null)        gameOverScoreText.gameObject.SetActive(false);
        if (gameOverHighScoreText != null)    gameOverHighScoreText.gameObject.SetActive(false);
        if (fadeOverlay != null)              fadeOverlay.gameObject.SetActive(false);

        GenerateOrder();
        UpdateUI();
    }

    void Update()
    {
        // During game over, only allow LMB so the player can click Play Again
        if (isGameOver)
        {
            if (Input.GetMouseButtonDown(0))
                TryPlateMouseInteract();
            return;
        }

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

        if (timerRunning)
        {
            orderTimer -= Time.deltaTime;
            // Clamp so it never goes below 0 in the UI
            orderTimer = Mathf.Max(0f, orderTimer);
            UpdateUI();
            if (orderTimer <= 0f)
                OnOrderExpired();
        }
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

    // ================= LMB — ALL PLATE PICKUP / PUTDOWN / SUBMIT / TRASH / RICE =================

    void TryPlateMouseInteract()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance, plateMask | interactMask | (1 << 9)))
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

        if (tag == "RiceStation")
        {
            TryCollectRice();
            return;
        }

        if (tag == "PlayAgainStation")
        {
            RestartGame();
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
        sashimiOnPlate = GetOrAddPlateData(plateObj).sashimiCount;

        AttachPlateToHand(plateObj);
        RebuildSashimiVisuals(plateObj);

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
            stationPlateObject.layer = InteractLayer;

            Collider c = stationPlateObject.GetComponent<Collider>();
            if (c != null) c.enabled = true;

            stationPlateObject.SetActive(true);
            stationPlateObject.transform.position =
                stationTransform.position + Vector3.up * 0.8f;

            RebuildSashimiVisuals(stationPlateObject);
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

        if (stationPlateObject != null)
            sashimiOnPlate = GetOrAddPlateData(stationPlateObject).sashimiCount;
        else
            sashimiOnPlate = sashimiAtStation;

        if (stationPlateObject != null)
        {
            stationPlateObject.tag = "Plate";
            stationPlateObject.layer = PlateLayer;
            AttachPlateToHand(stationPlateObject);
            RebuildSashimiVisuals(stationPlateObject);
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

        if (rice <= 0)
        {
            ShowMessage("No rice! Grab rice before plating.", 2f);
            return;
        }

        if (sashimiAtStation >= maxSashimiOnPlate)
        {
            ShowMessage("Plate is full! (" + sashimiAtStation + "/" + maxSashimiOnPlate + ")", 2f);
            return;
        }

        sashimi--;
        rice--;
        sashimiAtStation++;

        if (stationPlateObject != null)
        {
            PlateData pd = GetOrAddPlateData(stationPlateObject);
            pd.sashimiCount = sashimiAtStation;
            AddSashimiVisual(stationPlateObject);
        }

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
            ClearSashimiVisuals(heldPlateObject);
            Destroy(heldPlateObject);
            heldPlateObject = null;
        }

        holdingPlate = false;
        sashimiOnPlate = 0;

        ShowMessage("Plate trashed.", 2f);
        UpdateUI();
    }

    // ================= RICE STATION =================

    void TryCollectRice()
    {
        rice++;
        ShowMessage("Rice collected. (" + rice + " rice)", 1.5f);
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
            timerRunning = false;

            // --- SCORING ---
            // Score = baseScore * (timeRemaining / timeLimit), scaled to order size
            // Bigger orders and faster completions = more points
            float timeRatio = Mathf.Clamp01(orderTimer / orderTimeLimit);
            int earned = Mathf.RoundToInt(baseScore * timeRatio * requiredAmount);
            currentScore += earned;
            ordersCompleted++;

            if (currentScore > highScore)
                highScore = currentScore;

            sashimiOnPlate = 0;
            holdingPlate = false;

            if (heldPlateObject != null)
            {
                ClearSashimiVisuals(heldPlateObject);
                Destroy(heldPlateObject);
                heldPlateObject = null;
            }

            ShowMessage("Order Complete! +" + earned + " pts", 2f);
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

    void OnOrderExpired()
    {
        timerRunning = false;
        orderTimer = 0f;
        ShowMessage("Time's up! Order failed.", 2.5f);
        EndGame();
    }

    void GenerateOrder()
    {
        requiredAmount = Random.Range(minOrderAmount, maxOrderAmount + 1);

        // Decrease time limit each order, always clamped to the minimum floor
        orderTimeLimit = Mathf.Max(minOrderTimeLimit, initialOrderTimeLimit - timerDecreasePerOrder * ordersCompleted);

        orderTimer = orderTimeLimit;
        timerRunning = true;
        UpdateUI();
    }

    // ================= END GAME / RESTART =================

    void EndGame()
    {
        isGameOver = true;
        timerRunning = false;

        // Drop / clean up any held plate so it doesn't linger
        if (holdingPlate && heldPlateObject != null)
        {
            ClearSashimiVisuals(heldPlateObject);
            Destroy(heldPlateObject);
            heldPlateObject = null;
        }
        holdingPlate = false;
        sashimiOnPlate = 0;

        // Clean up station plate too
        if (stationPlateObject != null)
        {
            ClearSashimiVisuals(stationPlateObject);
            Destroy(stationPlateObject);
            stationPlateObject = null;
        }
        plateAtStation = false;

        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        // --- 1. Fade screen to black ---
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeOverlay.color = c;
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        // --- 2. Show Game Over UI ---
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER";
            gameOverText.color = Color.red;
            gameOverText.gameObject.SetActive(true);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Score: " + currentScore;
            gameOverScoreText.gameObject.SetActive(true);
        }

        if (gameOverHighScoreText != null)
        {
            gameOverHighScoreText.text = "Best: " + highScore;
            gameOverHighScoreText.gameObject.SetActive(true);
        }

        // --- 3. Fade black back out so the world is visible again ---
        if (fadeOverlay != null)
        {
            yield return new WaitForSeconds(0.4f); // brief hold on full black

            Color c = fadeOverlay.color;
            float elapsed = 0f;
            float fadeOutDuration = fadeDuration * 0.6f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }

        // --- 4. Spawn Play Again station in front of player ---
        SpawnPlayAgainStation();
    }

    void SpawnPlayAgainStation()
    {
        if (spawnedPlayAgainStation != null)
            Destroy(spawnedPlayAgainStation);

        Vector3 spawnPos = transform.position + transform.TransformDirection(playAgainStationOffset);

        if (playAgainStationPrefab != null)
        {
            spawnedPlayAgainStation = Instantiate(playAgainStationPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Fallback: create a simple cyan cube if no prefab is assigned
            spawnedPlayAgainStation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedPlayAgainStation.transform.position = spawnPos;
            spawnedPlayAgainStation.transform.localScale = new Vector3(1f, 1.2f, 1f);
            Renderer r = spawnedPlayAgainStation.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0f, 0.85f, 0.85f);
        }

        spawnedPlayAgainStation.tag = "PlayAgainStation";

        // Make sure it has a collider so the raycast can hit it
        if (spawnedPlayAgainStation.GetComponent<Collider>() == null)
            spawnedPlayAgainStation.AddComponent<BoxCollider>();

        // Set layer to match interactMask exactly — derived the same way as InteractLayer property
        spawnedPlayAgainStation.layer = 9;

        // Add a floating label above it
        GameObject labelObj = new GameObject("PlayAgainLabel");
        labelObj.transform.SetParent(spawnedPlayAgainStation.transform);
        labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = "PLAY AGAIN\n[Click]";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 24;
        tm.color = Color.white;
        tm.characterSize = 0.08f;
    }

    void RestartGame()
    {
        // Hide game over UI
        if (gameOverPanel != null)     gameOverPanel.SetActive(false);
        if (gameOverText != null)      gameOverText.gameObject.SetActive(false);
        if (gameOverScoreText != null) gameOverScoreText.gameObject.SetActive(false);
        if (gameOverHighScoreText != null) gameOverHighScoreText.gameObject.SetActive(false);
        if (fadeOverlay != null)       fadeOverlay.gameObject.SetActive(false);

        // Destroy play again station
        if (spawnedPlayAgainStation != null)
        {
            Destroy(spawnedPlayAgainStation);
            spawnedPlayAgainStation = null;
        }

        // Reset all game state
        sashimi = 0;
        rice = 0;
        holdingPlate = false;
        sashimiOnPlate = 0;
        heldPlateObject = null;
        plateAtStation = false;
        sashimiAtStation = 0;
        stationPlateObject = null;
        platingStationTransform = null;
        currentScore = 0;
        ordersCompleted = 0;
        orderTimeLimit = initialOrderTimeLimit;
        isGameOver = false;

        RemoveHighlight();
        currentItem = null;

        GenerateOrder();
        UpdateUI();
    }

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
        {
            int secs = Mathf.CeilToInt(orderTimer);
            orderText.text = "<b>Order:</b> " + requiredAmount + " sashimi\n<b>Time:</b> " + secs;
        }

        if (inventoryText != null)
            inventoryText.text = "Bag: " + sashimi + " / " + maxSashimi + " sashimi  |  Rice: " + rice;

        if (plateText != null)
        {
            if (holdingPlate)
                plateText.text = "<b>Plate (held):</b> " + sashimiOnPlate + " / " + requiredAmount + " sashimi";
            else if (plateAtStation)
                plateText.text = "<b>Plate (station):</b> " + sashimiAtStation + " / " + maxSashimiOnPlate + " sashimi";
            else
                plateText.text = "<b>Plate:</b> None";
        }

        if (scoreText != null)
            scoreText.text = "<b>Score:</b> " + currentScore;

        if (highScoreText != null)
            highScoreText.text = "<b>Best:</b> " + highScore;
    }
}