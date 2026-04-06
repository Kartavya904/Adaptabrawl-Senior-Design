using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Adaptabrawl.Attack;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("Fighter Selection")]
        [SerializeField] private List<FighterDef> availableFighters = new List<FighterDef>();

        [Header("Player 1 UI (Host)")]
        [Tooltip("Top label: e.g. 'Player 1'")]
        [SerializeField] private TextMeshProUGUI player1PlayerName;
        [Tooltip("Spawn the fighter preview under this Transform. If unset, uses the Image's transform (and hides the Image).")]
        [SerializeField] private Transform player1FighterContainer;
        [Tooltip("Optional. Hidden when preview is active; also used as container if Fighter Container is unset.")]
        [SerializeField] private Image player1FighterImage;
        [Tooltip("Bottom text: fighter name (Striker, Elusive, etc.)")]
        [SerializeField] private TextMeshProUGUI player1FighterName;
        [SerializeField] private Button player1LeftButton;
        [SerializeField] private Button player1RightButton;
        [SerializeField] private Button player1ConfirmButton;
        [SerializeField] private TextMeshProUGUI player1ReadyText;

        [Header("Player 2 UI (Client)")]
        [Tooltip("Top label: e.g. 'Player 2'")]
        [SerializeField] private TextMeshProUGUI player2PlayerName;
        [Tooltip("Spawn the fighter preview under this Transform. If unset, uses the Image's transform (and hides the Image).")]
        [SerializeField] private Transform player2FighterContainer;
        [Tooltip("Optional. Hidden when preview is active; also used as container if Fighter Container is unset.")]
        [SerializeField] private Image player2FighterImage;
        [Tooltip("Bottom text: fighter name (Striker, Elusive, etc.)")]
        [SerializeField] private TextMeshProUGUI player2FighterName;
        [SerializeField] private Button player2LeftButton;
        [SerializeField] private Button player2RightButton;
        [SerializeField] private Button player2ConfirmButton;
        [SerializeField] private TextMeshProUGUI player2ReadyText;

        [Header("Preview (fighter figure in slot)")]
        [Tooltip("Recommended for 3D models: empty GameObjects in the scene (e.g. P1 Preview Anchor at -3,0,0 and P2 at 3,0,0). The 3D preview is parented here. If your canvas is Overlay, also assign Raw Images below so the preview shows in Game view.")]
        [SerializeField] private Transform player1PreviewWorldAnchor;
        [SerializeField] private Transform player2PreviewWorldAnchor;
        [Tooltip("Optional. When set, the 3D preview is rendered to this texture so it shows in the Game view (required if using Screen Space - Overlay canvas, which draws on top of world-space previews). Add a Raw Image in the placeholder area and assign it here.")]
        [SerializeField] private RawImage player1PreviewRawImage;
        [SerializeField] private RawImage player2PreviewRawImage;
        [Tooltip("Resolution of the render texture for Raw Image preview (when Raw Image is assigned). Higher = less pixelation (e.g. 512 or 1024).")]
        [SerializeField] private int previewRenderTextureSize = 1024;
        [Tooltip("Local position of the P1 preview camera relative to its preview stage (world offset from P1 stage at x=-1000).")]
        [SerializeField] private Vector3 previewCameraLocalPositionP1 = new Vector3(936.7f, 2f, 3.29f);
        [Tooltip("Local position of the P2 preview camera relative to its preview stage (world offset from P2 stage at x=1000).")]
        [SerializeField] private Vector3 previewCameraLocalPositionP2 = new Vector3(-1047.5f, 2f, 3.29f);
        [Tooltip("Local euler rotation of the preview camera (X,Y,Z). Same for both players.")]
        [SerializeField] private Vector3 previewCameraLocalEuler = new Vector3(-8.6f, -150.11f, 0f);
        [Tooltip("Orthographic size of the preview camera (default 2.5).")]
        [SerializeField] private float previewCameraOrthographicSize = 2.5f;
        [Tooltip("If true, spawn the 3D preview at the world position that lines up with the Image placeholder on screen (no parenting to UI). Use to test if the model appears in the 3D scene at all.")]
        [SerializeField] private bool spawnPreviewAtImageWorldPosition = false;
        [Tooltip("When using Spawn Preview At Image World Position: distance from camera for the computed world position.")]
        [SerializeField] private float previewDistanceFromCamera = 10f;
        [Tooltip("Scale of the spawned fighter preview in the selection slot (world space).")]
        [SerializeField] private float previewScale = 0.5f;
        [Tooltip("When the container is under a Canvas (RectTransform), multiply scale by this so the fighter is visible in UI space. Try 50–200 if preview is too small.")]
        [SerializeField] private float previewScaleInUI = 100f;
        [Tooltip("Delay in seconds before the preview starts playing moves after a character is loaded/changed.")]
        [SerializeField] private float previewInitialDelay = 0.5f;
        [Tooltip("Delay in seconds between each move in the preview sequence (jump/air → special → heavy → light).")]
        [SerializeField] private float previewMoveDelay = 1.5f;
        [Tooltip("Animator trigger to play when fighter has no AnimatedMoveDef (e.g. runtime-created Striker/Elusive). Leave empty to skip.")]
        [SerializeField] private string defaultPreviewTrigger = "Idle";
        [Tooltip("When the fighter has no prefab (runtime Striker/Elusive), this sprite is shown so something is visible. Assign a simple sprite (e.g. silhouette).")]
        [SerializeField] private Sprite previewPlaceholderSprite;
        [Header("Preview Shadow Styling")]
        [Tooltip("Flatten preview fighters into a single shadow silhouette so they read like a ghost/shadow ninja instead of segmented pieces.")]
        [SerializeField] private bool useShadowSilhouettePreview = true;
        [Tooltip("Slight blue-black tint keeps previews readable while still looking like a clean shadow.")]
        [SerializeField] private Color previewSilhouetteColor = new Color(0.02f, 0.03f, 0.05f, 1f);

        [Header("Input Hints (assign TMP text inside each confirm button)")]
        [SerializeField] private TextMeshProUGUI player1ConfirmButtonText;
        [SerializeField] private TextMeshProUGUI player2ConfirmButtonText;

        [Header("Phase countdown (optional TMP — 3,2,1 before arena select)")]
        [SerializeField] private TextMeshProUGUI characterToArenaCountdownText;

        [Header("Navigation")]
        [SerializeField] private Button startButton; // Deprecated, Server loads automatically
        [SerializeField] private Button backButton;

        [Tooltip("Optional top-to-bottom focus order for gamepad UI. If empty, uses P1 arrows → confirm → P2 arrows → confirm → back.")]
        [SerializeField] private Selectable[] characterSelectFocusOrder;

        private GameObject _player1Preview;
        private GameObject _player2Preview;
        private bool _previewSequenceStartedP1;
        private bool _previewSequenceStartedP2;

        // Render-to-texture so preview shows in Game view when canvas is Overlay
        private Transform _previewStageP1;
        private Transform _previewStageP2;
        private UnityEngine.Camera _previewCameraP1;
        private UnityEngine.Camera _previewCameraP2;
        private RenderTexture _previewRTP1;
        private RenderTexture _previewRTP2;

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            // Load available fighters
            LoadAvailableFighters();

            // Setup Network RPC button listeners
            if (player1LeftButton != null) player1LeftButton.onClick.AddListener(() => RequestChangeSelection(-1, 1));
            if (player1RightButton != null) player1RightButton.onClick.AddListener(() => RequestChangeSelection(1, 1));
            if (player1ConfirmButton != null) player1ConfirmButton.onClick.AddListener(() => RequestConfirmSelection(1));

            if (player2LeftButton != null) player2LeftButton.onClick.AddListener(() => RequestChangeSelection(-1, 2));
            if (player2RightButton != null) player2RightButton.onClick.AddListener(() => RequestChangeSelection(1, 2));
            if (player2ConfirmButton != null) player2ConfirmButton.onClick.AddListener(() => RequestConfirmSelection(2));

            if (startButton != null) startButton.gameObject.SetActive(false); // Server handles transition automatically

            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.onClick.AddListener(RequestGoBack);
            }

            if (setupManager != null)
            {
                setupManager.OnCharacterConfigChanged += UpdateUI;
                setupManager.OnCharacterToArenaCountdownTick += OnCharacterToArenaCountdownTick;
            }

            SyncLobbyInputDevicesFromSetupManager();
            if (setupManager != null && availableFighters.Count > 0)
            {
                setupManager.ClampLocalFighterIndicesToRoster(availableFighters.Count);
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    setupManager.ServerClampFighterIndicesToRoster(availableFighters.Count);
            }

            UpdateUI();
            WireCharacterSelectMenuNavigation();
        }

        private void WireCharacterSelectMenuNavigation()
        {
            Selectable[] order = characterSelectFocusOrder != null && characterSelectFocusOrder.Length > 0
                ? characterSelectFocusOrder
                : new Selectable[]
                {
                    player1LeftButton, player1RightButton, player1ConfirmButton,
                    player2LeftButton, player2RightButton, player2ConfirmButton,
                    backButton
                };

            var list = new List<Selectable>();
            foreach (var s in order)
            {
                if (s != null) list.Add(s);
            }

            if (list.Count == 0) return;
            MenuNavigationGroup.ApplyVerticalChain(list, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(list);
        }

        private void OnEnable()
        {
            SyncLobbyInputDevicesFromSetupManager();
        }

        /// <summary>
        /// Keeps DontDestroyOnLoad lobby in sync with networked or local controller choices.
        /// </summary>
        private void SyncLobbyInputDevicesFromSetupManager()
        {
            if (setupManager == null) return;
            var lobby = LobbyContext.EnsureExists();
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            int p1 = networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex;
            int p2 = networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex;
            lobby.SetInputDevices(p1, p2);
        }

        private float _p1NavCooldown;
        private float _p2NavCooldown;
        private const float NAV_COOLDOWN = 0.25f;

        private void Update()
        {
            // Preview sequence: restart per-player when selection changes
            if (gameObject.activeInHierarchy)
            {
                if (_player1Preview != null && !_previewSequenceStartedP1)
                {
                    _previewSequenceStartedP1 = true;
                    TryStartPreviewSequence(_player1Preview);
                }
                if (_player2Preview != null && !_previewSequenceStartedP2)
                {
                    _previewSequenceStartedP2 = true;
                    TryStartPreviewSequence(_player2Preview);
                }
            }

            // Controller/keyboard navigation
            if (setupManager == null || availableFighters.Count == 0) return;

            // Only the instance wired to the arrow buttons should drive cycling. Scenes may add a second
            // CharacterSelectUI (e.g. a "manager" object that only holds the fighter roster); without this
            // guard both run Update and each gamepad step applies twice (roster of 4 looks like only 2 fighters).
            if (!HasArrowButtonsForNavigation())
                return;

            bool networked = NetworkManager.Singleton != null;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isLocal = CharacterSelectData.isLocalMatch;

            // Prefer LobbyContext for device type (set during controller config); fall back to setupManager
            int p1CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                          : (networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex);
            int p2CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                          : (networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex);
            bool r1 = networked ? setupManager.p1CharacterReady.Value : setupManager.LocalP1CharacterReady;
            bool r2 = networked ? setupManager.p2CharacterReady.Value : setupManager.LocalP2CharacterReady;

            _p1NavCooldown -= Time.deltaTime;
            _p2NavCooldown -= Time.deltaTime;

            bool p1CanInteract = isHost || isLocal || !networked;
            bool p2CanInteract = (!isHost || isLocal || !networked);

            bool phaseCountdown = setupManager.CharacterPhaseCountdownActive;
            if (!phaseCountdown)
            {
                if (!r1 && p1CanInteract)
                    HandleCharacterNavigation(1, p1CtrlIdx, p1CtrlIdx, p2CtrlIdx, ref _p1NavCooldown);

                if (!r2 && p2CanInteract)
                    HandleCharacterNavigation(2, p2CtrlIdx, p1CtrlIdx, p2CtrlIdx, ref _p2NavCooldown);

            }
        }

        private void OnCharacterToArenaCountdownTick(int n)
        {
            if (characterToArenaCountdownText == null) return;
            if (n <= 0)
            {
                characterToArenaCountdownText.gameObject.SetActive(false);
                return;
            }
            characterToArenaCountdownText.gameObject.SetActive(true);
            characterToArenaCountdownText.text = n.ToString();
        }

        /// <summary>
        /// True when this component owns the left/right UI used for character cycling (not a roster-only duplicate).
        /// </summary>
        private bool HasArrowButtonsForNavigation()
        {
            return (player1LeftButton != null && player1RightButton != null)
                || (player2LeftButton != null && player2RightButton != null);
        }

        private void HandleCharacterNavigation(int player, int controllerIndex, int p1Device, int p2Device, ref float cooldown)
        {
            int direction = 0;
            bool confirm = false;

            if (controllerIndex == 1) // Gamepad
            {
                Gamepad pad = null;
                LobbyContext.TryGetGamepadForPlayer(player, p1Device, p2Device, out pad);
                if (pad != null)
                {
                    if (cooldown <= 0f)
                    {
                        float h = pad.leftStick.ReadValue().x;
                        if (Mathf.Abs(h) < 0.3f) h = pad.dpad.ReadValue().x;
                        if (h > 0.5f) direction = 1;
                        else if (h < -0.5f) direction = -1;
                    }
                    confirm = pad.buttonSouth.wasPressedThisFrame; // X / Cross — lock in
                }
                else
                {
                    // No gamepad found for this player (e.g. both set to Controller but only 1 physical pad).
                    // Fall back to keyboard so the player can still navigate character select.
                    controllerIndex = 0;
                }
            }

            if (controllerIndex == 0) // Keyboard (or gamepad-fallback)
            {
                if (player == 1)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.D)) direction = 1;
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.A)) direction = -1;
                    confirm = UnityEngine.Input.GetKeyDown(KeyCode.F);
                }
                else
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) direction = 1;
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) direction = -1;
                    confirm = UnityEngine.Input.GetKeyDown(KeyCode.Return);
                }
            }

            if (direction != 0)
            {
                RequestChangeSelection(direction, player);
                cooldown = NAV_COOLDOWN;
            }
            if (confirm)
                RequestConfirmSelection(player);
        }

        private void OnDestroy()
        {
            if (setupManager != null)
            {
                setupManager.OnCharacterConfigChanged -= UpdateUI;
                setupManager.OnCharacterToArenaCountdownTick -= OnCharacterToArenaCountdownTick;
            }
            DestroyPreview(ref _player1Preview);
            DestroyPreview(ref _player2Preview);
            if (player1FighterImage != null) player1FighterImage.enabled = true;
            if (player2FighterImage != null) player2FighterImage.enabled = true;
            SetPreviewRawImageVisible(1, false);
            SetPreviewRawImageVisible(2, false);
            if (_previewRTP1 != null) { _previewRTP1.Release(); _previewRTP1 = null; }
            if (_previewRTP2 != null) { _previewRTP2.Release(); _previewRTP2 = null; }
            if (_previewStageP1 != null) { Destroy(_previewStageP1.gameObject); _previewStageP1 = null; }
            if (_previewStageP2 != null) { Destroy(_previewStageP2.gameObject); _previewStageP2 = null; }
        }

        private void LoadAvailableFighters()
        {
            // Always dynamically load from Resources to sync perfectly with the mid-match randomizer pool.
            var loaded = Resources.LoadAll<FighterDef>("Fighters");
            if (loaded != null && loaded.Length > 0)
            {
                availableFighters.Clear();
                availableFighters.AddRange(OrderFightersForSelection(loaded));
                Debug.Log($"[CharacterSelectUI] Loaded {loaded.Length} FighterDef(s) from Resources/Fighters/.");
                return;
            }

            // Final fallback: runtime-created archetypes (no prefab, placeholder visuals).
            Debug.LogWarning("[CharacterSelectUI] No FighterDef assets found. Using built-in Striker/Elusive archetypes. " +
                "To show real characters: assign FighterDef assets to 'Available Fighters' on this component, " +
                "or place them in Assets/Resources/Fighters/.");
            availableFighters.Add(FighterFactory.CreateStrikerFighter());
            availableFighters.Add(FighterFactory.CreateElusiveFighter());
            availableFighters.Sort(CompareFightersForSelection);
        }

        private static IEnumerable<FighterDef> OrderFightersForSelection(IEnumerable<FighterDef> fighters)
        {
            return fighters
                .Where(fighter => fighter != null)
                .OrderBy(GetSelectionOrderRank)
                .ThenBy(fighter => fighter.fighterName);
        }

        private static int CompareFightersForSelection(FighterDef left, FighterDef right)
        {
            if (left == right)
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            int rankComparison = GetSelectionOrderRank(left).CompareTo(GetSelectionOrderRank(right));
            if (rankComparison != 0)
                return rankComparison;

            return string.Compare(left.fighterName, right.fighterName, System.StringComparison.Ordinal);
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

        private void RequestChangeSelection(int direction, int targetPlayer)
        {
            if (setupManager == null || availableFighters.Count == 0) return;
            if (NetworkManager.Singleton != null)
                setupManager.ChangeCharacterServerRpc(NetworkManager.Singleton.LocalClientId, direction, availableFighters.Count, targetPlayer);
            else
                setupManager.LocalChangeCharacter(direction, availableFighters.Count, targetPlayer);
        }

        private void RequestConfirmSelection(int targetPlayer)
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null)
                setupManager.ToggleCharacterReadyServerRpc(NetworkManager.Singleton.LocalClientId, targetPlayer);
            else
                setupManager.LocalToggleCharacterReady(targetPlayer);
        }

        private void RequestGoBack()
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer)
                    setupManager.GoBackToControllerServerRpc();
            }
            else
            {
                setupManager.GoBackToControllerLocal();
            }
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isClient = networked && !isHost;
            bool isLocal = CharacterSelectData.isLocalMatch;

            // Indices and ready state: from network or local mirror
            int p1Idx = networked ? setupManager.p1FighterIndex.Value : setupManager.LocalP1FighterIndex;
            int p2Idx = networked ? setupManager.p2FighterIndex.Value : setupManager.LocalP2FighterIndex;
            bool r1 = networked ? setupManager.p1CharacterReady.Value : setupManager.LocalP1CharacterReady;
            bool r2 = networked ? setupManager.p2CharacterReady.Value : setupManager.LocalP2CharacterReady;

            // Lock input: Host = P1 only, Client = P2 only; local match or offline = both
            bool p1CanInteract = isHost || isLocal || !networked;
            bool p2CanInteract = isClient || isLocal || !networked;
            bool phaseCd = setupManager.CharacterPhaseCountdownActive;
            if (player1LeftButton != null) player1LeftButton.interactable = p1CanInteract && !r1 && !phaseCd;
            if (player1RightButton != null) player1RightButton.interactable = p1CanInteract && !r1 && !phaseCd;
            if (player1ConfirmButton != null) player1ConfirmButton.interactable = p1CanInteract && !phaseCd;
            if (player2LeftButton != null) player2LeftButton.interactable = p2CanInteract && !r2 && !phaseCd;
            if (player2RightButton != null) player2RightButton.interactable = p2CanInteract && !r2 && !phaseCd;
            if (player2ConfirmButton != null) player2ConfirmButton.interactable = p2CanInteract && !phaseCd;
            if (backButton != null) backButton.interactable = (!networked || isHost) && !phaseCd;

            // Top = player name, Bottom = fighter name; spawn preview figure in container
            var lobby = LobbyContext.Instance;
            if (player1PlayerName != null) player1PlayerName.text = lobby != null ? lobby.p1Name : "Player 1";
            if (player2PlayerName != null) player2PlayerName.text = lobby != null ? lobby.p2Name : "Player 2";

            lobby?.SetLastFighterIndices(p1Idx, p2Idx);

            if (availableFighters.Count > 0)
            {
                if (p1Idx >= 0 && p1Idx < availableFighters.Count)
                {
                    var fighter1 = availableFighters[p1Idx];
                    if (player1FighterName != null) player1FighterName.text = fighter1 != null ? fighter1.fighterName : "No Fighter";
                    CharacterSelectData.selectedFighter1 = fighter1;
                    LobbyContext.Instance?.SetP1Fighter(fighter1); // persist for rematch
                    SpawnPreviewInContainer(fighter1, GetPreviewContainerForPlayer(1), player1FighterImage, 1, ref _player1Preview);
                }

                if (p2Idx >= 0 && p2Idx < availableFighters.Count)
                {
                    var fighter2 = availableFighters[p2Idx];
                    if (player2FighterName != null) player2FighterName.text = fighter2 != null ? fighter2.fighterName : "No Fighter";
                    CharacterSelectData.selectedFighter2 = fighter2;
                    LobbyContext.Instance?.SetP2Fighter(fighter2); // persist for rematch
                    SpawnPreviewInContainer(fighter2, GetPreviewContainerForPlayer(2), player2FighterImage, 2, ref _player2Preview);
                }
            }
            else
            {
                if (player1FighterImage != null) player1FighterImage.enabled = true;
                if (player2FighterImage != null) player2FighterImage.enabled = true;
                SetPreviewRawImageVisible(1, false);
                SetPreviewRawImageVisible(2, false);
            }

            int p1CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                          : (networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex);
            int p2CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                          : (networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex);

            if (player1ReadyText != null)
            {
                player1ReadyText.text = LobbySetupInputHints.CharacterReadyStatusLine(p1CtrlIdx, r1);
                player1ReadyText.color = r1 ? Color.green : Color.white;
            }
            if (player2ReadyText != null)
            {
                player2ReadyText.text = LobbySetupInputHints.CharacterReadyStatusLine(p2CtrlIdx, r2);
                player2ReadyText.color = r2 ? Color.green : Color.white;
            }

            if (player1ConfirmButtonText != null)
                player1ConfirmButtonText.text = LobbySetupInputHints.CharacterConfirmButtonRich(p1CtrlIdx, r1, true);
            if (player2ConfirmButtonText != null)
                player2ConfirmButtonText.text = LobbySetupInputHints.CharacterConfirmButtonRich(p2CtrlIdx, r2, false);
        }

        /// <summary>
        /// Converts the Image placeholder's on-screen position to a world position so the 3D preview can be placed there.
        /// Uses the RectTransform's center in screen space, then UnityEngine.Camera.main.ScreenToWorldPoint.
        /// </summary>
        private static Vector3 GetWorldPositionFromImagePlaceholder(RectTransform rect, float distanceFromCamera)
        {
            if (rect == null || UnityEngine.Camera.main == null) return Vector3.zero;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 screenCenter = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            return UnityEngine.Camera.main.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, distanceFromCamera));
        }

        /// <summary>
        /// Resolves where to spawn the preview: RenderTexture stage (when RawImage set), world anchor, or UI container.
        /// </summary>
        private Transform GetPreviewContainerForPlayer(int player)
        {
            if (player == 1 && player1PreviewRawImage != null) return GetOrCreatePreviewStage(1);
            if (player == 2 && player2PreviewRawImage != null) return GetOrCreatePreviewStage(2);
            if (!spawnPreviewAtImageWorldPosition)
            {
                if (player == 1 && player1PreviewWorldAnchor != null) return player1PreviewWorldAnchor;
                if (player == 2 && player2PreviewWorldAnchor != null) return player2PreviewWorldAnchor;
            }
            if (player == 1) return GetPreviewContainer(player1FighterContainer, player1FighterImage);
            return GetPreviewContainer(player2FighterContainer, player2FighterImage);
        }

        private Transform GetOrCreatePreviewStage(int player)
        {
            if (player == 1)
            {
                if (_previewStageP1 != null) return _previewStageP1;
                float x = -1000f;
                var stage = new GameObject("PreviewStageP1").transform;
                stage.position = new Vector3(x, 0f, 0f);
                _previewStageP1 = stage;
                CreatePreviewCameraAndRT(1, stage, x);
                return stage;
            }
            if (player == 2)
            {
                if (_previewStageP2 != null) return _previewStageP2;
                float x = 1000f;
                var stage = new GameObject("PreviewStageP2").transform;
                stage.position = new Vector3(x, 0f, 0f);
                _previewStageP2 = stage;
                CreatePreviewCameraAndRT(2, stage, x);
                return stage;
            }
            return null;
        }

        private void CreatePreviewCameraAndRT(int player, Transform stage, float stageX)
        {
            int size = Mathf.Clamp(previewRenderTextureSize, 64, 2048);

            if (player == 1)
            {
                _previewRTP1 = new RenderTexture(size, size, 16);
                _previewRTP1.name = "PreviewRT_P1";
                var camObj = new GameObject("PreviewCameraP1");
                camObj.transform.SetParent(stage);
                camObj.transform.localPosition = previewCameraLocalPositionP1;
                camObj.transform.localEulerAngles = previewCameraLocalEuler;
                _previewCameraP1 = camObj.AddComponent<UnityEngine.Camera>();
                _previewCameraP1.orthographic = true;
                _previewCameraP1.orthographicSize = previewCameraOrthographicSize;
                _previewCameraP1.targetTexture = _previewRTP1;
                _previewCameraP1.clearFlags = CameraClearFlags.SolidColor;
                _previewCameraP1.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f);
                _previewCameraP1.cullingMask = -1;
                _previewCameraP1.depth = 0;
                if (player1PreviewRawImage != null) player1PreviewRawImage.texture = _previewRTP1;
                AddPreviewLight(camObj.transform);
            }
            else if (player == 2)
            {
                _previewRTP2 = new RenderTexture(size, size, 16);
                _previewRTP2.name = "PreviewRT_P2";
                var camObj = new GameObject("PreviewCameraP2");
                camObj.transform.SetParent(stage);
                camObj.transform.localPosition = previewCameraLocalPositionP2;
                camObj.transform.localEulerAngles = previewCameraLocalEuler;
                _previewCameraP2 = camObj.AddComponent<UnityEngine.Camera>();
                _previewCameraP2.orthographic = true;
                _previewCameraP2.orthographicSize = previewCameraOrthographicSize;
                _previewCameraP2.targetTexture = _previewRTP2;
                _previewCameraP2.clearFlags = CameraClearFlags.SolidColor;
                _previewCameraP2.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f);
                _previewCameraP2.cullingMask = -1;
                _previewCameraP2.depth = 0;
                if (player2PreviewRawImage != null) player2PreviewRawImage.texture = _previewRTP2;
                AddPreviewLight(camObj.transform);
            }
        }

        private static void AddPreviewLight(Transform cameraTransform)
        {
            var mainLight = new GameObject("PreviewLight_Main");
            mainLight.transform.SetParent(cameraTransform, false);
            mainLight.transform.localPosition = Vector3.zero;
            mainLight.transform.localRotation = Quaternion.identity;
            var light = mainLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
        }

        private void ApplyPreviewShadowSilhouette(GameObject previewObject)
        {
            if (!useShadowSilhouettePreview || previewObject == null)
                return;

            var shadowVisual = previewObject.GetComponent<ShadowSilhouetteVisual>();
            if (shadowVisual == null)
                shadowVisual = previewObject.AddComponent<ShadowSilhouetteVisual>();

            shadowVisual.Configure(previewSilhouetteColor, disableParticleSystems: true, disableTrails: true);
        }

        private static Transform GetPreviewContainer(Transform container, Image image)
        {
            if (container != null) return container;
            if (image != null) return image.transform;
            return null;
        }

        private void SetPreviewRawImageVisible(int playerIndex, bool visible)
        {
            if (playerIndex == 1 && player1PreviewRawImage != null)
                player1PreviewRawImage.enabled = visible;
            else if (playerIndex == 2 && player2PreviewRawImage != null)
                player2PreviewRawImage.enabled = visible;
        }

        private static void DestroyPreview(ref GameObject preview)
        {
            if (preview != null)
            {
                Destroy(preview);
                preview = null;
            }
        }

        private void SpawnPreviewInContainer(FighterDef def, Transform container, Image imageToHide, int playerIndex, ref GameObject currentPreview)
        {
            if (def == null)
            {
                if (imageToHide != null) imageToHide.enabled = true;
                SetPreviewRawImageVisible(playerIndex, false);
                DestroyPreview(ref currentPreview);
                return;
            }
            if (container == null)
            {
                if (imageToHide != null) imageToHide.enabled = true;
                SetPreviewRawImageVisible(playerIndex, false);
                DestroyPreview(ref currentPreview);
                return;
            }

            DestroyPreview(ref currentPreview);
            bool useRawImage = (playerIndex == 1 && player1PreviewRawImage != null) || (playerIndex == 2 && player2PreviewRawImage != null);
            if (useRawImage)
                SetPreviewRawImageVisible(playerIndex, true);

            bool useImageWorldPos = spawnPreviewAtImageWorldPosition && UnityEngine.Camera.main != null;
            Vector3 spawnPos;
            Transform parent;
            float scale;
            if (useImageWorldPos && (container is RectTransform rect))
            {
                spawnPos = GetWorldPositionFromImagePlaceholder(rect, previewDistanceFromCamera);
                parent = null;
                scale = previewScale;
                Debug.Log($"CharacterSelectUI: Spawn preview at Image world position = {spawnPos} (distanceFromCamera={previewDistanceFromCamera})");
            }
            else
            {
                spawnPos = container.position;
                parent = container;
                scale = container is RectTransform ? (previewScale * previewScaleInUI) : previewScale;
            }

            FighterController controller = FighterFactory.CreateFighter(def, spawnPos, true);
            if (controller == null) return;

            GameObject obj = controller.gameObject;
            obj.name = def.fighterName + "_Preview";

            var networkObjects = obj.GetComponentsInChildren<NetworkObject>(true);
            foreach (var no in networkObjects)
                DestroyImmediate(no);

            obj.transform.SetParent(parent, true);
            if (parent != null)
                obj.transform.localPosition = Vector3.zero;
            else
                obj.transform.position = spawnPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one * scale;

            // No game input or combat: prevent any movement/attack from triggering (e.g. mouse clicks in UI).
            var inputHandler = obj.GetComponent<Input.PlayerInputHandler>();
            if (inputHandler != null) inputHandler.enabled = false;
            var movement = obj.GetComponent<MovementController>();
            if (movement != null) movement.enabled = false;
            var attackSystem = obj.GetComponent<AttackSystem>();
            if (attackSystem != null) attackSystem.enabled = false;
            // Disable Shinabro platform controller so mouse clicks and legacy input don't drive attacks in previews.
            var shinabroController = obj.GetComponentInChildren<PlayerController_Platform>();
            if (shinabroController != null) shinabroController.enabled = false;
            foreach (var pi in obj.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>(true))
            {
                pi.enabled = false;
            }
            var rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }

            var rb3ds = obj.GetComponentsInChildren<Rigidbody>(true);
            foreach (var r3d in rb3ds)
            {
                r3d.constraints = RigidbodyConstraints.FreezeAll;
                r3d.linearVelocity = Vector3.zero;
                r3d.angularVelocity = Vector3.zero;
            }

            var animators = obj.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                anim.applyRootMotion = false;
            }

            currentPreview = obj;

            // Placeholder (no prefab): give the white quad a visible sprite so something shows
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null && previewPlaceholderSprite != null)
            {
                sr.sprite = previewPlaceholderSprite;
                sr.sortingOrder = 100;
            }

            ApplyPreviewShadowSilhouette(obj);

            if (imageToHide != null) imageToHide.enabled = !useRawImage;

            // Restart this player's preview sequence when selection changes (jump → special → light → heavy).
            if (playerIndex == 1) _previewSequenceStartedP1 = false;
            else if (playerIndex == 2) _previewSequenceStartedP2 = false;
        }

        private void TryStartPreviewSequence(GameObject previewObj)
        {
            if (previewObj == null) return;
            var bridge = previewObj.GetComponent<AnimationBridge>();
            var fc = previewObj.GetComponent<FighterController>();
            var animator = previewObj.GetComponentInChildren<Animator>();
            bool animatorUsable = animator != null && animator.runtimeAnimatorController != null;
            FighterDef def = fc != null ? fc.FighterDef : null;

            if (bridge != null && animatorUsable && def != null)
            {
                var sequence = GetPreviewSequence(def);
                if (sequence != null && sequence.Count > 0)
                    StartCoroutine(PlayPreviewSequence(bridge, def, sequence));
                else if (!string.IsNullOrEmpty(defaultPreviewTrigger))
                    animator.SetTrigger(defaultPreviewTrigger);
            }
            else if (animatorUsable && !string.IsNullOrEmpty(defaultPreviewTrigger))
            {
                animator.SetTrigger(defaultPreviewTrigger);
            }
        }

        private IEnumerator PlayPreviewSequence(AnimationBridge bridge, FighterDef def, List<AnimatedMoveDef> sequence)
        {
            if (bridge == null || sequence == null || sequence.Count == 0) yield break;

            // Wait briefly after the character loads/changes before starting the showcase.
            yield return new WaitForSeconds(previewInitialDelay);

            // Play the sequence once in order (jump/air → special → heavy → light) and then stop.
            for (int i = 0; i < sequence.Count && bridge != null; i++)
            {
                AnimatedMoveDef move = sequence[i];
                if (move != null)
                {
                    bridge.PlayMove(move);
                    float moveDuration = move.GetAnimationLength();
                    yield return new WaitForSeconds(Mathf.Clamp(moveDuration, 0.5f, 5f));
                }

                if (bridge == null) yield break;

                // Wait between moves, but not after the final one.
                if (i < sequence.Count - 1)
                    yield return new WaitForSeconds(previewMoveDelay);
            }
        }

        /// <summary>
        /// Builds the preview sequence: jump/air attack → one special → heavy → light (exactly this order when available).
        /// Uses explicit FighterDef extended fields when present, falling back to specials array/name heuristics.
        /// </summary>
        private static List<AnimatedMoveDef> GetPreviewSequence(FighterDef def)
        {
            var list = new List<AnimatedMoveDef>();
            if (def == null) return list;

            // Collect all specials as AnimatedMoveDef for selection.
            var specials = new List<AnimatedMoveDef>();
            if (def.specialMoves != null)
            {
                foreach (var m in def.specialMoves)
                {
                    if (m is AnimatedMoveDef am && am != null)
                        specials.Add(am);
                }
            }

            // 1) Jump / air attack:
            //    Prefer explicit FighterDef.jumpAttackPrimary, then FighterDef.aerialSpecial,
            //    then any special whose name hints "Air"/"Jump", otherwise first special.
            AnimatedMoveDef jumpMove = null;
            if (def.jumpAttackPrimary != null)
            {
                jumpMove = def.jumpAttackPrimary;
            }
            else if (def.aerialSpecial != null)
            {
                jumpMove = def.aerialSpecial;
            }
            else
            {
                foreach (var s in specials)
                {
                    if (s != null && (s.moveName.Contains("Air") || s.moveName.Contains("Jump")))
                    {
                        jumpMove = s;
                        break;
                    }
                }

                if (jumpMove == null && specials.Count > 0)
                    jumpMove = specials[0];
            }

            if (jumpMove != null)
                list.Add(jumpMove);

            // 2) One other special (different from jump)
            foreach (var s in specials)
            {
                if (s != null && s != jumpMove)
                {
                    list.Add(s);
                    break;
                }
            }

            // 3) Heavy attack
            if (def.heavyAttack is AnimatedMoveDef heavy && heavy != null)
                list.Add(heavy);

            // 4) Light attack
            if (def.lightAttack is AnimatedMoveDef light && light != null)
                list.Add(light);

            return list;
        }
    }

    // Static class to pass data between scenes
    public static class CharacterSelectData
    {
        public static Adaptabrawl.Data.FighterDef selectedFighter1;
        public static Adaptabrawl.Data.FighterDef selectedFighter2;
        public static bool isLocalMatch = false;
        // Preserved across scenes so rematch-with-different-characters restores controller types
        public static int finalP1ControllerIndex = 0;
        public static int finalP2ControllerIndex = 0;
    }
}
