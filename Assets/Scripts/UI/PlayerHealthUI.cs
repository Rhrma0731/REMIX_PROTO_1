using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private TMP_Text _hpText;

    private void OnEnable()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHpChanged += UpdateHP;
            UpdateHP(PlayerStats.Instance.CurrentHp, PlayerStats.Instance.MaxHp);
        }
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHpChanged -= UpdateHP;
            
            PlayerStats.Instance.OnHpChanged += UpdateHP;
            
            UpdateHP(PlayerStats.Instance.CurrentHp, PlayerStats.Instance.MaxHp);
        }
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHpChanged -= UpdateHP;
    }

    private void UpdateHP(float current, float max)
    {
        if (_fillImage != null)
            _fillImage.fillAmount = max > 0f ? current / max : 0f;

        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
