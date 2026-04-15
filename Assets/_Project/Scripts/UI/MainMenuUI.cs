using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Main menu — entry point for the FTL-style roguelike run.
    /// "New Run" deals starter cards and opens the EventDeck hub.
    /// "Continue" loads an existing save (if one exists).
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
            if (SubtitleText) SubtitleText.text = "Card-Driven Tactical RPG";

            NewRunButton?.onClick.AddListener(OnNewRun);
            ContinueButton?.onClick.AddListener(OnContinue);
            QuitButton?.onClick.AddListener(OnQuit);

            // Show Continue only when a save exists
            bool hasSave = SaveSystem.SaveExists();
            if (ContinueRow)      ContinueRow.SetActive(hasSave);
            if (ContinueButton)   ContinueButton.gameObject.SetActive(hasSave);
        }

        private void OnNewRun()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("EventDeck");
        }

        private void OnContinue()
        {
            // Save is loaded automatically in GameManager.Awake; just navigate to the hub.
            if (GameManager.Instance != null)
                GameManager.Instance.GoToEventDeck();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("EventDeck");
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
