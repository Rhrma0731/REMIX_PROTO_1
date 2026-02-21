using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _coinText;

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinText;
            UpdateCoinText(CurrencyManager.Instance.Coins);
        }
    }

    private void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinText;
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinText;
            UpdateCoinText(CurrencyManager.Instance.Coins);
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinText;
    }

    private void UpdateCoinText(int amount)
    {
        if (_coinText != null)
            _coinText.text = amount.ToString();
    }
}
