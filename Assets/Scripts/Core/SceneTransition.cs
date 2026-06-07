using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    private Image _overlay;
    private const float FADE = 0.25f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        gameObject.AddComponent<CanvasScaler>();
        // No GraphicRaycaster — this canvas is display-only and must not block input

        var go = new GameObject("Overlay");
        go.transform.SetParent(transform, false);
        _overlay = go.AddComponent<Image>();
        _overlay.color = new Color(0, 0, 0, 0);
        var rt = _overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.FadeLoad(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeLoad(string sceneName)
    {
        yield return Fade(0f, 1f);
        SceneManager.LoadScene(sceneName);
        yield return Fade(1f, 0f);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < FADE)
        {
            t += Time.unscaledDeltaTime;
            _overlay.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / FADE));
            yield return null;
        }
        _overlay.color = new Color(0, 0, 0, to);
    }
}
