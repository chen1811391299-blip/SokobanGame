using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button btnPlay;
    public Button btnEditor;
    public Button btnQuit;

    void Start()
    {
        btnPlay?.onClick.AddListener(()   => { AudioManager.PlayMenu(); SceneTransition.LoadScene("LevelSelect"); });
        btnEditor?.onClick.AddListener(() => { AudioManager.PlayMenu(); SceneTransition.LoadScene("LevelEditor"); });
        btnQuit?.onClick.AddListener(()   => Application.Quit());
        AudioManager.PlayBGM(AudioManager.Instance?.bgmMainMenu);
    }
}
