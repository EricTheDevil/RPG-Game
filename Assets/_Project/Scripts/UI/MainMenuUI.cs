using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Main menu — entry point for the roguelike run.
    /// "New Run" generates a world map and starts the journey to the Demon Lord.
    /// "Continue" loads an existing save.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Title")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI SubtitleText;

        [Header("Actions")]
        public Button NewRunButton;
        public Button ContinueButton;
        public Button QuitButton;

        [Header("Continue hint")]
        [Tooltip("Shown only when a save file exists.")]
        public GameObject ContinueRow;

        private void Start()
        {
            if (TitleText)    TitleText.text    = "REALM OF TACTICS";
            if (SubtitleText) SubtitleText.text = "Journey to the Demon Lord";

            NewRunButton?.onClick.AddListener(OnNewRun);
            ContinueButton?.onClick.AddListener(OnContinue);
            QuitButton?.onClick.AddListener(OnQuit);

            bool hasSave = SaveSystem.SaveExists();
            if (ContinueRow)    ContinueRow.SetActive(hasSave);
            if (ContinueButton) ContinueButton.gameObject.SetActive(hasSave);
        }

        private void OnNewRun()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("WorldMap");
        }

        private void OnContinue()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GoToWorldMap();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("WorldMap");
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
