using UnityEngine;
using TMPro;

public class OrderSystem : MonoBehaviour
{
    [Header("Order Settings")]
    public int minOrderAmount = 1;
    public int maxOrderAmount = 5;

    [Header("Player Inventory")]
    public int sashimi;

    [Header("UI (TMP)")]
    public TMP_Text orderText;
    public TMP_Text inventoryText;
    public TMP_Text resultText;

    [Header("Interaction")]
    public float interactDistance = 4f;
    public LayerMask interactMask;
    public bool showRay = true;

    private int requiredAmount;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        GenerateOrder();
        UpdateUI();
    }

    void Update()
    {
        if (showRay)
        {
            DrawRay();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TrySubmitOrder();
        }
    }

    void DrawRay()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);
    }

    void GenerateOrder()
    {
        requiredAmount = Random.Range(minOrderAmount, maxOrderAmount + 1);

        if (resultText != null)
            resultText.text = "";

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
            // ✅ success
            sashimi -= requiredAmount;

            if (resultText != null)
                resultText.text = "Order Complete!";

            GenerateOrder();
        }
        else
        {
            if (resultText != null)
                resultText.text = "Not Enough Sashimi!";
        }

        UpdateUI();
    }

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
                "<b>Inventory:</b>\n" +
                "Sashimi: " + sashimi;
        }
    }
}