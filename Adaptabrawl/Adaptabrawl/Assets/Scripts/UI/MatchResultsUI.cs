using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using Adaptabrawl.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Adaptabrawl.UI
{
    public class MatchResultsUI : MonoBehaviour
    {
        [Header("Results Display")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI matchScoreText;
        [SerializeField] private TextMeshProUGUI roundResultsText;
        [Tooltip("Optional. Shows arena from GameContext when assigned.")]
        [SerializeField] private TextMeshProUGUI arenaNameText;

        [Header("Player 1 Results")]
        [SerializeField] private TextMeshProUGUI player1NameText;
        [SerializeField] private TextMeshProUGUI player1WinsText;
        [SerializeField] private Image player1Portrait;

        [Header("Player 2 Results")]
        [SerializeField] private TextMeshProUGUI player2NameText;
        [SerializeField] private TextMeshProUGUI player2WinsText;
        [SerializeField] private Image player2Portrait;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button rematchDifferentButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Controller / keyboard UI")]
        [Tooltip("If empty, uses Continue → Rematch → Change Characters → Main Menu.")]
        [SerializeField] private Selectable[] resultsFocusOrder;

        [Header("Button Labels")]
        [SerializeField] private TextMeshProUGUI continueButtonText;
        [SerializeField] private TextMeshProUGUI rematchButtonText;
        [SerializeField] private TextMeshProUGUI rematchDifferentButtonText;

        [Header("Presentation")]
        [Tooltip("If set, these objects are shown one after another. If empty, a default order is built from the fields above.")]
        [SerializeField] private GameObject[] revealSequence;
        [SerializeField] private float revealStaggerSeconds = 0.12f;
        [SerializeField] private Animator resultsAnimator;

        [Header("Auto main menu")]
        [Tooltip("After this many seconds with no button press, load StartScene (main menu).")]
        [SerializeField] private float autoMainMenuSeconds = 30f;

        private bool _resultsShown;
        private Coroutine _showRoutine;
        private Coroutine _autoMenuRoutine;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(Continue);
            if (rematchButton != null)
                rematchButton.onClick.AddListener(Rematch);
            if (rematchDifferentButton != null)
                rematchDifferentButton.onClick.AddListener(RematchDifferentCharacters);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (resultsPanel != null)
                resultsPanel.SetActive(false);

            LoadMatchResults();
        }

        private void OnDestroy()
        {
            CancelAutoMainMenuTimer();
            if (_showRoutine != null)
                StopCoroutine(_showRoutine);
        }

        private void LoadMatchResults()
        {
            bool fromStatic = MatchResultsData.hasResults;
            bool fromContext = GameContext.Instance != null && GameContext.Instance.TryGetLatestFinishedMatch(out _);
            if (fromStatic || fromContext)
                _showRoutine = StartCoroutine(ShowResultsRoutine());
            else
            {
                Debug.LogWarning("[MatchResultsUI] No MatchResultsData and no GameContext match history.");
                ReturnToMainMenu();
            }
        }

        private IEnumerator ShowResultsRoutine()
        {
            var steps = BuildRevealList();
            foreach (var go in steps)
            {
                if (go != null)
                    go.SetActive(false);
            }

            if (resultsPanel != null)
                resultsPanel.SetActive(true);

            DisplayResults();

            foreach (var go in steps)
            {
                if (go == null || ShouldSkipReveal(go)) continue;
                go.SetActive(true);
                if (revealStaggerSeconds > 0f)
                    yield return new WaitForSeconds(revealStaggerSeconds);
            }

            SetupResultsMenuNavigation();
            _resultsShown = true;

            if (resultsAnimator != null)
                resultsAnimator.SetTrigger("ShowResults");

            if (autoMainMenuSeconds > 0f)
                _autoMenuRoutine = StartCoroutine(AutoMainMenuTimerRoutine());
        }

        private List<GameObject> BuildRevealList()
        {
            var list = new List<GameObject>();
            void Add(GameObject go)
            {
                if (go != null && !list.Contains(go))
                    list.Add(go);
            }

            if (revealSequence != null && revealSequence.Length > 0)
            {
                foreach (var go in revealSequence)
                    Add(go);
                return list;
            }

            if (arenaNameText != null)
                Add(arenaNameText.gameObject);
            if (winnerText != null)
                Add(winnerText.gameObject);
            if (matchScoreText != null)
                Add(matchScoreText.gameObject);
            if (player1NameText != null)
                Add(PanelOrSelf(player1NameText.gameObject));
            if (player2NameText != null)
                Add(PanelOrSelf(player2NameText.gameObject));
            if (roundResultsText != null)
                Add(roundResultsText.gameObject);
            if (continueButton != null)
                Add(continueButton.gameObject);
            if (rematchButton != null)
                Add(rematchButton.gameObject);
            if (rematchDifferentButton != null)
                Add(rematchDifferentButton.gameObject);
            if (mainMenuButton != null)
                Add(mainMenuButton.gameObject);

            return list;
        }

        private static GameObject PanelOrSelf(GameObject leaf)
        {
            Transform p = leaf.transform.parent;
            if (p != null && p.name.Contains("Panel"))
                return p.gameObject;
            return leaf;
        }

        private bool ShouldSkipReveal(GameObject go)
        {
            if (arenaNameText != null && go == arenaNameText.gameObject)
                return string.IsNullOrWhiteSpace(arenaNameText.text);
            return false;
        }

        private IEnumerator AutoMainMenuTimerRoutine()
        {
            yield return new WaitForSeconds(autoMainMenuSeconds);
            _autoMenuRoutine = null;
            ReturnToMainMenu();
        }

        private void CancelAutoMainMenuTimer()
        {
            if (_autoMenuRoutine != null)
            {
                StopCoroutine(_autoMenuRoutine);
                _autoMenuRoutine = null;
            }
        }

        private void LateUpdate()
        {
            if (!_resultsShown || resultsPanel == null || !resultsPanel.activeSelf) return;
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame()) return;
            if (BackInputUtility.IsTextInputFocused()) return;
            Continue();
        }

        private void SetupResultsMenuNavigation()
        {
            Selectable[] order = resultsFocusOrder != null && resultsFocusOrder.Length > 0
                ? resultsFocusOrder
                : new Selectable[] { continueButton, rematchButton, rematchDifferentButton, mainMenuButton };

            order = order.Where(s => s != null).ToArray();
            if (order.Length == 0) return;

            MenuNavigationGroup.ApplyVerticalChain(order, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(order);
        }

        private void DisplayResults()
        {
            if (GameContext.Instance != null && GameContext.Instance.TryGetLatestFinishedMatch(out var rec))
            {
                DisplayResultsFromGameContext(rec);
                ApplyButtonLabels(rec.localMatch);
                return;
            }

            if (!MatchResultsData.hasResults || MatchResultsData.results == null)
            {
                Debug.LogWarning("[MatchResultsUI] No GameContext history and no MatchResultsData.");
                return;
            }

            var results = MatchResultsData.results;

            if (winnerText != null)
            {
                if (results.winner != null)
                {
                    winnerText.text = $"{GetFighterName(results.winner)} WINS!";
                    winnerText.color = results.winner == results.player1 ? Color.cyan : Color.yellow;
                }
                else
                {
                    winnerText.text = "DRAW!";
                    winnerText.color = Color.white;
                }
            }

            if (matchScoreText != null)
                matchScoreText.text = $"{results.player1Wins} - {results.player2Wins}";

            if (arenaNameText != null)
                arenaNameText.text = "";

            if (player1NameText != null)
                player1NameText.text = GetFighterName(results.player1);
            if (player1WinsText != null)
                player1WinsText.text = $"Wins: {results.player1Wins}";

            if (player2NameText != null)
                player2NameText.text = GetFighterName(results.player2);
            if (player2WinsText != null)
                player2WinsText.text = $"Wins: {results.player2Wins}";

            if (roundResultsText != null)
            {
                string roundText = "Round Results:\n";
                for (int i = 0; i < results.roundWinners.Count; i++)
                {
                    var roundWinner = results.roundWinners[i];
                    string winnerName = roundWinner != null ? GetFighterName(roundWinner) : "Draw";
                    roundText += $"Round {i + 1}: {winnerName}\n";
                }
                roundResultsText.text = roundText;
            }

            ApplyButtonLabels(MatchResultsData.isLocalMatch);
        }

        private void DisplayResultsFromGameContext(FinishedMatchRecord rec)
        {
            if (rec == null) return;

            if (arenaNameText != null)
            {
                arenaNameText.text = string.IsNullOrWhiteSpace(rec.arenaName)
                    ? ""
                    : $"Arena: {rec.arenaName}";
            }

            if (winnerText != null)
            {
                if (rec.outcome == "Draw")
                {
                    winnerText.text = "DRAW!";
                    winnerText.color = Color.white;
                }
                else if (rec.outcome == "P1")
                {
                    winnerText.text = $"{rec.p1FighterName} WINS!";
                    winnerText.color = Color.cyan;
                }
                else if (rec.outcome == "P2")
                {
                    winnerText.text = $"{rec.p2FighterName} WINS!";
                    winnerText.color = Color.yellow;
                }
                else
                {
                    winnerText.text = "MATCH OVER";
                    winnerText.color = Color.white;
                }
            }

            if (matchScoreText != null)
                matchScoreText.text = $"{rec.p1FinalWins} - {rec.p2FinalWins}";

            if (player1NameText != null)
                player1NameText.text = $"{rec.p1DisplayName}\n{rec.p1FighterName}";
            if (player1WinsText != null)
                player1WinsText.text = $"Wins: {rec.p1FinalWins}";

            if (player2NameText != null)
                player2NameText.text = $"{rec.p2DisplayName}\n{rec.p2FighterName}";
            if (player2WinsText != null)
                player2WinsText.text = $"Wins: {rec.p2FinalWins}";

            if (roundResultsText != null)
            {
                var snap = rec.roundWinnerCodesSnapshot;
                string roundText = "Round Results:\n";
                if (snap != null && snap.Count > 0)
                {
                    for (int i = 0; i < snap.Count; i++)
                    {
                        int code = snap[i];
                        string winnerName = code == 1 ? rec.p1FighterName : code == 2 ? rec.p2FighterName : "Draw";
                        roundText += $"Round {i + 1}: {winnerName}\n";
                    }
                }
                else
                    roundText += "(No round log)\n";
                roundResultsText.text = roundText;
            }
        }

        private void ApplyButtonLabels(bool isLocal)
        {
            if (continueButtonText != null)
                continueButtonText.text = isLocal ? "Back to Setup" : "Back to Online";
            if (rematchButtonText != null)
                rematchButtonText.text = "Rematch";
            if (rematchDifferentButtonText != null)
                rematchDifferentButtonText.text = "Change Characters";
        }

        private string GetFighterName(FighterController fighter)
        {
            if (fighter == null) return "Unknown";
            if (fighter.FighterDef != null)
                return fighter.FighterDef.fighterName;
            return fighter.name;
        }

        private void Continue()
        {
            CancelAutoMainMenuTimer();
            bool isLocal = MatchResultsData.isLocalMatch;
            MatchResultsData.Clear();
            if (isLocal)
                TransitionTo("SetupScene");
            else
                TransitionToOnlinePartyOrLobby();
        }

        private void Rematch()
        {
            CancelAutoMainMenuTimer();
            MatchResultsData.Clear();
            string gameScene = CharacterSelectData.isLocalMatch ? "GameScene" : "OnlineGameScene";
            TransitionTo(gameScene);
        }

        private void RematchDifferentCharacters()
        {
            CancelAutoMainMenuTimer();
            MatchResultsData.rematchSkipToCharacterSelect = true;
            TransitionTo("SetupScene");
        }

        private void ReturnToMainMenu()
        {
            CancelAutoMainMenuTimer();
            MatchResultsData.Clear();
            TransitionTo("StartScene");
        }

        private void TransitionToOnlinePartyOrLobby()
        {
            if (TryGetBuildIndexBySceneName(MainMenu.OnlinePartyRoomSceneName) >= 0)
                TransitionTo(MainMenu.OnlinePartyRoomSceneName);
            else
                TransitionTo("LobbyScene");
        }

        private static int TryGetBuildIndexBySceneName(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return i;
            }

            return -1;
        }

        private void TransitionTo(string sceneName)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }

    public static class MatchResultsData
    {
        public static bool hasResults = false;
        public static bool rematchRequested = false;
        public static bool isLocalMatch = false;
        public static bool rematchSkipToCharacterSelect = false;
        public static MatchResults results;

        public static void SetResults(MatchResults matchResults, bool local)
        {
            results = matchResults;
            hasResults = true;
            isLocalMatch = local;
        }

        public static void Clear()
        {
            hasResults = false;
            rematchRequested = false;
            isLocalMatch = false;
            rematchSkipToCharacterSelect = false;
            results = null;
        }
    }

    [System.Serializable]
    public class MatchResults
    {
        public FighterController player1;
        public FighterController player2;
        public FighterController winner;
        public int player1Wins;
        public int player2Wins;
        public List<FighterController> roundWinners = new List<FighterController>();
        public int totalRounds;
    }
}
