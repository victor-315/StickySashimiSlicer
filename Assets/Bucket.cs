using UnityEngine;
using System.Collections.Generic;

public class Bucket : MonoBehaviour
{
    public int capacity = 10;
    private List<GameObject> items = new List<GameObject>();

    public int CurrentCount => items.Count;

    public bool TryAddItem(GameObject obj)
    {
        if (items.Count >= capacity)
            return false;

        items.Add(obj);
        obj.SetActive(false);
        return true;
    }

    public void RemoveItems(int amount)
    {
        int remove = Mathf.Min(amount, items.Count);

        for (int i = 0; i < remove; i++)
        {
            GameObject obj = items[0];
            items.RemoveAt(0);
            Destroy(obj);
        }
    }
}