using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinChanged;

    private int _coins;

    public int Coins => _coins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddCoins(int amount)
    {
        _coins += amount;
        OnCoinChanged?.Invoke(_coins);
    }
}
