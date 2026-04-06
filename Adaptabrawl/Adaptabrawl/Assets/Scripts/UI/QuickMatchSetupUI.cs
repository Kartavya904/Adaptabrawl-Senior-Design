using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.AI;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Adaptabrawl.UI
{
    public sealed class QuickMatchSetupUI : MonoBehaviour
    {
        [Header("Difficulty")]
        [SerializeField] private TextMeshProUGUI difficultyValueText;
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
        [SerializeField] private Button difficultyPreviousButton;
        [SerializeField] private Button difficultyNextButton;

        [Header("Input Mode")]
        [SerializeField] private TextMeshProUGUI inputModeValueText;
        [SerializeField] private TextMeshProUGUI inputModeHintText;
        [SerializeField] private Button toggleInputModeButton;

        [Header("Arena")]
        [SerializeField] private TextMeshProUGUI arenaValueText;
        [SerializeField] private Image arenaPreviewImage;
        [SerializeField] private Button arenaPreviousButton;
        [SerializeField] private Button arenaNextButton;
        [SerializeField] private List<string> availableArenas = new List<string>
        {
            "Cascade Sanctum",
            "Ashen Crucible",
            "Aetherfall Citadel"
        };
        [SerializeField] private List<Sprite> arenaBackgrounds = new List<Sprite>();

        [Header("Player")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerSummaryText;
        [SerializeField] private Image playerPortraitImage;
        [SerializeField] private Button playerPreviousButton;
        [SerializeField] private Button playerNextButton;

        [Header("Opponent Preview")]
        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private TextMeshProUGUI opponentSummaryText;
        [SerializeField] private Image opponentPortraitImage;
        [SerializeField] private Button opponentPreviousButton;
        [SerializeField] private Button opponentNextButton;

        [Header("Model Summary")]
        [SerializeField] private TextMeshProUGUI dummyModelInfoText;
        [SerializeField] private TextMeshProUGUI trainerModelInfoText;
        [SerializeField] private TextMeshProUGUI extremeModelInfoText;

        [Header("Actions")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button retrainModelsButton;
        [SerializeField] private Button backButton;

        [Header("Navigation")]
        [SerializeField] private Selectable[] navigationOrder;

        private readonly List<FighterDef> availableFighters = new List<FighterDef>();
        private readonly System.Random opponentPreviewRandom = new System.Random();

        private QuickMatchDifficultyTier selectedDifficulty = QuickMatchDifficultyTier.Trainer;
        private int selectedInputDevice;
        private int selectedArenaIndex;
        private int selectedPlayerIndex;
        private int selectedOpponentPreviewIndex;
        private Coroutine retrainRoutine;

        private void Start()
        {
            QuickMatchModelStore.EnsureModelsReady();
            LoadAvailableFighters();
            RestoreStateFromSession();
            WireButtons();
            RefreshAll();
            WireNavigation();
        }

        private void LateUpdate()
        {
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame())
                return;

            if (BackInputUtility.IsTextInputFocused())
                return;

            ReturnToMainMenu();
        }

        private void OnDestroy()
        {
            if (retrainRoutine != null)
                StopCoroutine(retrainRoutine);
        }

        private void WireButtons()
        {
            if (difficultyPreviousButton != null)
                difficultyPreviousButton.onClick.AddListener(() => CycleDifficulty(-1));
            if (difficultyNextButton != null)
                difficultyNextButton.onClick.AddListener(() => CycleDifficulty(1));
            if (toggleInputModeButton != null)
                toggleInputModeButton.onClick.AddListener(ToggleInputMode);

            if (arenaPreviousButton != null)
                arenaPreviousButton.onClick.AddListener(() => CycleArena(-1));
            if (arenaNextButton != null)
                arenaNextButton.onClick.AddListener(() => CycleArena(1));

            if (playerPreviousButton != null)
                playerPreviousButton.onClick.AddListener(() => CyclePlayer(-1));
            if (playerNextButton != null)
                playerNextButton.onClick.AddListener(() => CyclePlayer(1));

            if (opponentPreviousButton != null)
                opponentPreviousButton.onClick.AddListener(() => CycleOpponentPreview(-1));
            if (opponentNextButton != null)
                opponentNextButton.onClick.AddListener(() => CycleOpponentPreview(1));

            if (startMatchButton != null)
                startMatchButton.onClick.AddListener(StartQuickMatch);
            if (retrainModelsButton != null)
                retrainModelsButton.onClick.AddListener(RequestRetrainModels);
            if (backButton != null)
                backButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void WireNavigation()
        {
            Selectable[] order = navigationOrder != null && navigationOrder.Length > 0
                ? navigationOrder
                : new Selectable[]
                {
                    difficultyPreviousButton,
                    difficultyNextButton,
                    toggleInputModeButton,
                    arenaPreviousButton,
                    arenaNextButton,
                    playerPreviousButton,
                    playerNextButton,
                    opponentPreviousButton,
                    opponentNextButton,
                    retrainModelsButton,
                    startMatchButton,
                    backButton
                };

            MenuNavigationGroup.ApplyVerticalChain(order, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(order);
        }

        private void LoadAvailableFighters()
        {
            availableFighters.Clear();
            FighterDef[] loaded = Resources.LoadAll<FighterDef>("Fighters");
            if (loaded != null && loaded.Length > 0)
            {
                availableFighters.AddRange(loaded
                    .Where(fighter => fighter != null)
                    .OrderBy(GetSelectionOrderRank)
                    .ThenBy(fighter => fighter.fighterName));
            }

            if (availableFighters.Count == 0)
                Debug.LogWarning("[QuickMatchSetupUI] No FighterDef assets were found in Resources/Fighters.");
        }

        private void RestoreStateFromSession()
        {
            if (QuickMatchSession.Instance != null && QuickMatchSession.Instance.TryGetCurrentConfig(out QuickMatchMatchConfig config))
            {
                selectedDifficulty = config.difficulty;
                selectedInputDevice = config.player1InputDevice;
                selectedArenaIndex = Mathf.Clamp(config.arenaIndex, 0, Mathf.Max(0, availableArenas.Count - 1));
                selectedPlayerIndex = Mathf.Max(0, FindFighterIndex(config.player1Fighter));
                selectedOpponentPreviewIndex = Mathf.Max(0, FindFighterIndex(config.player2Fighter));
                return;
            }

            selectedDifficulty = QuickMatchDifficultyTier.Trainer;
            selectedInputDevice = 0;
            selectedArenaIndex = 0;
            selectedPlayerIndex = 0;
            selectedOpponentPreviewIndex = GetRandomPreviewIndex(excludeIndex: selectedPlayerIndex);
        }

        private void RefreshAll()
        {
            RefreshDifficulty();
            RefreshInputMode();
            RefreshArena();
            RefreshPlayer();
            RefreshOpponentPreview();
            RefreshModelSummary();

            if (startMatchButton != null)
                startMatchButton.interactable = availableFighters.Count > 0;
        }

        private void RefreshDifficulty()
        {
            if (difficultyValueText != null)
                difficultyValueText.text = selectedDifficulty.ToString();

            if (difficultyDescriptionText == null)
                return;

            difficultyDescriptionText.text = selectedDifficulty switch
            {
                QuickMatchDifficultyTier.Dummy => "Low-pressure practice opponent. Slow reactions, readable habits, forgiving spacing.",
                QuickMatchDifficultyTier.Trainer => "Balanced sparring partner. Punishes obvious mistakes and teaches spacing discipline.",
                QuickMatchDifficultyTier.Extreme => "Fastest reactions, strongest punishes, and relentless pressure from the champion profile.",
                _ => string.Empty
            };
        }

        private void RefreshInputMode()
        {
            if (inputModeValueText != null)
                inputModeValueText.text = selectedInputDevice == 1 ? "Controller" : "Keyboard";

            if (inputModeHintText == null)
                return;

            var bindings = ControlBindingsContext.EnsureExists();
            if (selectedInputDevice == 1)
            {
                inputModeHintText.text = bindings.HasConnectedController
                    ? "Controller will drive Player 1. CPU stays on injected commands."
                    : "No controller is connected. Stay on keyboard or plug one in before switching.";
            }
            else
            {
                inputModeHintText.text = "Keyboard will drive Player 1. CPU uses the selected model tier.";
            }
        }

        private void RefreshArena()
        {
            if (arenaValueText != null)
                arenaValueText.text = availableArenas.Count > 0 ? availableArenas[selectedArenaIndex] : "No arenas";

            if (arenaPreviewImage == null)
                return;

            if (arenaBackgrounds.Count > 0 && selectedArenaIndex < arenaBackgrounds.Count && arenaBackgrounds[selectedArenaIndex] != null)
            {
                arenaPreviewImage.sprite = arenaBackgrounds[selectedArenaIndex];
                arenaPreviewImage.enabled = true;
            }
            else
            {
                arenaPreviewImage.enabled = false;
            }
        }

        private void RefreshPlayer()
        {
            FighterDef fighter = GetSelectedPlayerFighter();
            if (fighter == null)
            {
                if (playerNameText != null)
                    playerNameText.text = "No Fighter";
                if (playerSummaryText != null)
                    playerSummaryText.text = "Add FighterDef assets to Resources/Fighters.";
                if (playerPortraitImage != null)
                    playerPortraitImage.enabled = false;
                return;
            }

            if (playerNameText != null)
                playerNameText.text = fighter.fighterName;
            if (playerSummaryText != null)
                playerSummaryText.text = BuildFighterSummary(fighter);

            if (playerPortraitImage != null)
            {
                playerPortraitImage.sprite = fighter.portrait;
                playerPortraitImage.enabled = fighter.portrait != null;
            }
        }

        private void RefreshOpponentPreview()
        {
            FighterDef fighter = GetSelectedOpponentFighter();
            if (fighter == null)
            {
                if (opponentNameText != null)
                    opponentNameText.text = "No Opponent";
                if (opponentSummaryText != null)
                    opponentSummaryText.text = "Quick Match will use the selected model when a fighter is available.";
                if (opponentPortraitImage != null)
                    opponentPortraitImage.enabled = false;
                return;
            }

            if (opponentNameText != null)
                opponentNameText.text = $"Random Preview: {fighter.fighterName}";
            if (opponentSummaryText != null)
                opponentSummaryText.text = $"{BuildFighterSummary(fighter)}\nTier model: {selectedDifficulty}";

            if (opponentPortraitImage != null)
            {
                opponentPortraitImage.sprite = fighter.portrait;
                opponentPortraitImage.enabled = fighter.portrait != null;
            }
        }

        private void RefreshModelSummary()
        {
            if (dummyModelInfoText != null)
                dummyModelInfoText.text = BuildModelSummary(QuickMatchDifficultyTier.Dummy);
            if (trainerModelInfoText != null)
                trainerModelInfoText.text = BuildModelSummary(QuickMatchDifficultyTier.Trainer);
            if (extremeModelInfoText != null)
                extremeModelInfoText.text = BuildModelSummary(QuickMatchDifficultyTier.Extreme);
        }

        private string BuildModelSummary(QuickMatchDifficultyTier tier)
        {
            if (!QuickMatchModelStore.TryLoadChampion(tier, out _, out QuickMatchModelMetadata metadata) || metadata == null)
                return $"{tier}: missing";

            return $"{tier}: {metadata.evaluation.value:F1} score | {metadata.evaluation.normalizedScore:F3} norm\n{metadata.policyId}";
        }

        private void CycleDifficulty(int direction)
        {
            int difficultyCount = System.Enum.GetValues(typeof(QuickMatchDifficultyTier)).Length;
            int next = ((int)selectedDifficulty + direction + difficultyCount) % difficultyCount;
            selectedDifficulty = (QuickMatchDifficultyTier)next;
            RefreshDifficulty();
            RefreshOpponentPreview();
        }

        private void ToggleInputMode()
        {
            if (selectedInputDevice == 0 && !ControlBindingsContext.EnsureExists().HasConnectedController)
            {
                SetStatus("No controller detected. Quick Match stayed on keyboard.");
                RefreshInputMode();
                return;
            }

            selectedInputDevice = selectedInputDevice == 0 ? 1 : 0;
            RefreshInputMode();
            SetStatus($"Player 1 input: {(selectedInputDevice == 1 ? "Controller" : "Keyboard")}");
        }

        private void CycleArena(int direction)
        {
            if (availableArenas.Count == 0)
                return;

            selectedArenaIndex = (selectedArenaIndex + direction + availableArenas.Count) % availableArenas.Count;
            RefreshArena();
        }

        private void CyclePlayer(int direction)
        {
            if (availableFighters.Count == 0)
                return;

            selectedPlayerIndex = (selectedPlayerIndex + direction + availableFighters.Count) % availableFighters.Count;
            if (availableFighters.Count > 1 && selectedOpponentPreviewIndex == selectedPlayerIndex)
                selectedOpponentPreviewIndex = GetRandomPreviewIndex(excludeIndex: selectedPlayerIndex);
            RefreshPlayer();
            RefreshOpponentPreview();
        }

        private void CycleOpponentPreview(int direction)
        {
            if (availableFighters.Count == 0)
                return;

            selectedOpponentPreviewIndex = (selectedOpponentPreviewIndex + direction + availableFighters.Count) % availableFighters.Count;
            RefreshOpponentPreview();
        }

        private void StartQuickMatch()
        {
            if (availableFighters.Count == 0)
            {
                SetStatus("Quick Match cannot start until at least one FighterDef exists in Resources/Fighters.");
                return;
            }

            var config = new QuickMatchMatchConfig
            {
                difficulty = selectedDifficulty,
                player1Role = QuickMatchPlayerRole.Human,
                player2Role = QuickMatchPlayerRole.Cpu,
                player1InputDevice = selectedInputDevice,
                player2InputDevice = 0,
                arenaIndex = selectedArenaIndex,
                arenaName = availableArenas.Count > 0 ? availableArenas[selectedArenaIndex] : "Arena",
                arenaSprite = selectedArenaIndex < arenaBackgrounds.Count ? arenaBackgrounds[selectedArenaIndex] : null,
                player1Fighter = GetSelectedPlayerFighter(),
                player2Fighter = GetSelectedOpponentFighter(),
                opponentPreviewIndex = selectedOpponentPreviewIndex,
                opponentSelectionSeed = Environment.TickCount,
                opponentSelectionWasRandomized = true,
                player1DisplayName = "Player 1",
                player2DisplayName = $"{selectedDifficulty} CPU"
            };

            if (QuickMatchModelStore.TryLoadChampion(selectedDifficulty, out QuickMatchHeuristicModel model, out QuickMatchModelMetadata metadata) && metadata != null)
                config.player2PolicyId = string.IsNullOrWhiteSpace(model.policyId) ? metadata.policyId : model.policyId;

            QuickMatchSession.EnsureExists().Activate(config);
            SceneManager.LoadScene("GameScene");
        }

        private void RequestRetrainModels()
        {
            if (retrainRoutine != null)
                return;

            retrainRoutine = StartCoroutine(RetrainModelsRoutine());
        }

        private IEnumerator RetrainModelsRoutine()
        {
            if (retrainModelsButton != null)
                retrainModelsButton.interactable = false;

            SetStatus("Retraining Quick Match heuristic champions...");
            yield return null;

            QuickMatchTrainingReport report = QuickMatchTrainer.TrainChampionSet(Environment.TickCount, saveToPersistent: true);
            RefreshModelSummary();
            SetStatus(report.BuildSummary());

            if (retrainModelsButton != null)
                retrainModelsButton.interactable = true;

            retrainRoutine = null;
        }

        private void ReturnToMainMenu()
        {
            QuickMatchSession.Instance?.ClearSession();
            SceneManager.LoadScene("StartScene");
        }

        private FighterDef GetSelectedPlayerFighter()
        {
            return availableFighters.Count == 0 ? null : availableFighters[Mathf.Clamp(selectedPlayerIndex, 0, availableFighters.Count - 1)];
        }

        private FighterDef GetSelectedOpponentFighter()
        {
            return availableFighters.Count == 0 ? null : availableFighters[Mathf.Clamp(selectedOpponentPreviewIndex, 0, availableFighters.Count - 1)];
        }

        private int FindFighterIndex(FighterDef fighter)
        {
            if (fighter == null)
                return 0;

            for (int i = 0; i < availableFighters.Count; i++)
            {
                if (availableFighters[i] == fighter)
                    return i;
            }

            return 0;
        }

        private int GetRandomPreviewIndex(int excludeIndex)
        {
            if (availableFighters.Count <= 1)
                return 0;

            int previewIndex = opponentPreviewRandom.Next(availableFighters.Count);
            if (previewIndex == excludeIndex)
                previewIndex = (previewIndex + 1) % availableFighters.Count;

            return previewIndex;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private static string BuildFighterSummary(FighterDef fighter)
        {
            return $"{fighter.playStyle} | HP {fighter.maxHealth:0} | SPD {fighter.moveSpeed:0.0} | DMG x{fighter.baseDamageMultiplier:0.00}";
        }

        private static int GetSelectionOrderRank(FighterDef fighter)
        {
            if (fighter == null)
                return int.MaxValue;

            return fighter.playStyle switch
            {
                FighterPlayStyle.Balanced => 0,
                FighterPlayStyle.Strength => 1,
                FighterPlayStyle.Defense => 2,
                FighterPlayStyle.Invasion => 3,
                _ => 4
            };
        }
    }
}
