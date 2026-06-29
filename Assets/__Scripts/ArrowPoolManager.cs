using System.Collections.Generic;
using UnityEngine;

public class ArrowPoolManager : MonoBehaviour
{
    public static ArrowPoolManager Instance { get; private set; }

    [SerializeField] private Arrow ArrowPrefab;

    private readonly Queue<Arrow> Pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Arrow Get(Vector3 position, Quaternion rotation)
    {
        Arrow arrow = Pool.Count > 0 ? Pool.Dequeue() : CreateNew();

        arrow.transform.SetPositionAndRotation(position, rotation);
        arrow.gameObject.SetActive(true);
        return arrow;
    }

    public void Return(Arrow arrow)
    {
        arrow.gameObject.SetActive(false);
        arrow.transform.SetParent(transform);
        Pool.Enqueue(arrow);
    }

    private Arrow CreateNew()
    {
        Arrow arrow = Instantiate(ArrowPrefab, transform);
        arrow.gameObject.SetActive(false);
        return arrow;
    }
}