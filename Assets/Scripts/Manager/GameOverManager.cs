using System.Collections;
using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _gameOverText;

    [Header("Fade In")]
    [SerializeField] private float _fadeDelay = 0.5f;
    [SerializeField] private float _fadeDuration = 0.8f;

    private CanvasGroup _panelCanvasGroup;

    private void Awake()
    {
        if (_gameOverPanel != null)
        {
            _panelCanvasGroup = _gameOverPanel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
                _panelCanvasGroup = _gameOverPanel.AddComponent<CanvasGroup>();

            _panelCanvasGroup.alpha = 0f;
            _gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnPlayerDeath += OnPlayerDeath;
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnPlayerDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // Brief pause before showing UI
        float delayElapsed = 0f;
        while (delayElapsed < _fadeDelay)
        {
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Freeze game
        Time.timeScale = 0f;

        // Show and fade in
        _gameOverPanel.SetActive(true);
        _panelCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _panelCanvasGroup.alpha = 1f;
    }
}
