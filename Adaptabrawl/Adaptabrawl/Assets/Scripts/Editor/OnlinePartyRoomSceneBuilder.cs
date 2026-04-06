using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Networking;
using Adaptabrawl.UI;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Creates or updates <c>OnlinePartyRoomScene</c>: LAN party UI, NetworkManager, EventSystem,
    /// and registers the scene in Build Settings. Does not remove <c>LobbyScene</c>.
    /// </summary>
    public static class OnlinePartyRoomSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/OnlinePartyRoomScene.unity";
        private const string StartScenePath = "Assets/Scenes/StartScene.unity";
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string ThemeFontAssetPath = "Assets/UniNeue-Trial-Heavy SDF.asset";

        private static readonly Color BgDark = new Color(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color Panel = new Color(1f, 1f, 1f, 0.98f);
        private static readonly Color Accent = new Color(0f, 0f, 0f, 1f);
        private static readonly Color SoftLine = new Color(0f, 0f, 0f, 0.14f);
        private static readonly Color TextDim = new Color(0.33f, 0.33f, 0.33f, 1f);
        private static readonly Color TextMuted = new Color(0.46f, 0.46f, 0.46f, 1f);
        private static readonly Color TextHi = new Color(0.04f, 0.04f, 0.04f, 1f);
        private static readonly Color SlotSurface = new Color(0.985f, 0.985f, 0.985f, 1f);
        private static readonly Color ButtonSurface = new Color(1f, 1f, 1f, 1f);
        private static readonly Color InputSurface = new Color(0.965f, 0.965f, 0.965f, 1f);
        private static readonly Color ModalBackdrop = new Color(0f, 0f, 0f, 0.18f);

        [MenuItem("Tools/Adaptabrawl/Setup Online Party Room Scene")]
        public static void BuildOrUpdatePartyRoomScene()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/StartScene.unity")) ||
                !File.Exists(Path.Combine(Application.dataPath, "Scenes/LobbyScene.unity")))
            {
                EditorUtility.DisplayDialog(
                    "Adaptabrawl",
                    "Could not find StartScene or LobbyScene under Assets/Scenes.",
                    "OK");
                return;
            }

            Scene target;
            if (File.Exists(ScenePath))
            {
                target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                DestroyIfExists(target, "PartyOnlineCanvas");
                DestroyIfExists(target, "PartyOnlineDriver");
            }
            else
            {
                target = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            EnsureEventSystemInScene(target);
            EnsureNetworkManagerInScene(target);

            var driverGo = new GameObject("PartyOnlineDriver");
            EditorSceneManager.MoveGameObjectToScene(driverGo, target);
            var lobbyMgr = driverGo.AddComponent<LobbyManager>();
            var partyUi = driverGo.AddComponent<OnlinePartyRoomUI>();
            var quickLan = driverGo.AddComponent<LanVideoStyleQuickConnect>();

            using (var so = new SerializedObject(lobbyMgr))
            {
                var p = so.FindProperty("autoStartWhenBothPlayersConnected");
                if (p != null)
                    p.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var canvas = BuildPartyCanvas(lobbyMgr, partyUi, quickLan);
            EditorSceneManager.MoveGameObjectToScene(canvas, target);

            RegisterInBuildSettings(ScenePath);
            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target, ScenePath);

            EditorUtility.DisplayDialog(
                "Adaptabrawl — Online Party Room",
                "Scene saved to:\n" + ScenePath + "\n\n" +
                "Registered in Build Settings.\n" +
                "Includes Host / Join-as-client strip (Unity Netcode LAN tutorial pattern) plus auto-host, room code, and join modal.\n" +
                "Re-run this menu after pulling changes.",
                "OK");
        }

        private static void DestroyIfExists(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == objectName)
                    Object.DestroyImmediate(root);
            }
        }

        private static void EnsureEventSystemInScene(Scene target)
        {
            foreach (var root in target.GetRootGameObjects())
            {
                if (root.name == "EventSystem")
                    return;
            }

            var start = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Additive);
            GameObject es = null;
            foreach (var r in start.GetRootGameObjects())
            {
                if (r.name == "EventSystem")
                {
                    es = r;
                    break;
                }
            }

            if (es != null)
            {
                var copy = Object.Instantiate(es);
                copy.name = "EventSystem";
                EditorSceneManager.MoveGameObjectToScene(copy, target);
            }

            EditorSceneManager.CloseScene(start, false);
        }

        private static void EnsureNetworkManagerInScene(Scene target)
        {
            foreach (var root in target.GetRootGameObjects())
            {
                if (root.name == "NetworkManager")
                    return;
            }

            var lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            GameObject nm = null;
            foreach (var r in lobby.GetRootGameObjects())
            {
                if (r.name == "NetworkManager")
                {
                    nm = r;
                    break;
                }
            }

            if (nm != null)
            {
                var copy = Object.Instantiate(nm);
                copy.name = "NetworkManager";
                copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                EditorSceneManager.MoveGameObjectToScene(copy, target);
            }

            EditorSceneManager.CloseScene(lobby, false);
        }

        private static GameObject BuildPartyCanvas(LobbyManager lobbyMgr, OnlinePartyRoomUI partyUi,
            LanVideoStyleQuickConnect quickLan)
        {
            var canvasGo = new GameObject("PartyOnlineCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = CreateUiRect("Root", canvasGo.transform, true);
            SetImageColor(root, BgDark);

            var safe = CreateUiRect("SafeArea", root.transform, true);
            var safeRt = safe.GetComponent<RectTransform>();
            safeRt.offsetMin = new Vector2(72, 64);
            safeRt.offsetMax = new Vector2(-72, -64);

            var mainPanel = CreateUiRect("MainPanel", safe.transform, true);
            var mainCg = mainPanel.AddComponent<CanvasGroup>();
            mainCg.blocksRaycasts = true;
            mainCg.interactable = true;
            SetImageColor(mainPanel, Panel);
            AddOutline(mainPanel, Accent, 1f);

            var title = CreateTmp("Title", mainPanel.transform, "ONLINE PARTY", 42, FontStyles.Bold, TextAlignmentOptions.Center, TextHi);
            StretchTop(title.gameObject, -34f, 62f);

            var subtitle = CreateTmp("Subtitle", mainPanel.transform, "", 18, FontStyles.Normal, TextAlignmentOptions.Center, TextMuted);
            StretchTop(subtitle.gameObject, -104f, 88f);
            subtitle.enableWordWrapping = true;

            var codeLabel = CreateTmp("CodeLabel", mainPanel.transform, "ROOM CODE", 16, FontStyles.Bold, TextAlignmentOptions.Center, TextDim);
            StretchTop(codeLabel.gameObject, -206f, 24f);

            var codeBig = CreateTmp("RoomCodeBig", mainPanel.transform, "······", 86, FontStyles.Bold, TextAlignmentOptions.Center, TextHi);
            StretchTop(codeBig.gameObject, -242f, 88f);

            var direct = CreateTmp("DirectConnect", mainPanel.transform, "", 18, FontStyles.Normal, TextAlignmentOptions.Center, TextMuted);
            StretchTop(direct.gameObject, -338f, 72f);

            var slotsRow = CreateUiRect("SlotsRow", mainPanel.transform, false);
            var rowRt = slotsRow.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0.29f);
            rowRt.anchorMax = new Vector2(1f, 0.56f);
            rowRt.offsetMin = new Vector2(56f, 0f);
            rowRt.offsetMax = new Vector2(-56f, 0f);

            var h = slotsRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            var p1Slot = CreatePlayerSlot(slotsRow.transform, "Player1Slot", "PLAYER 1");
            var p2Slot = CreatePlayerSlot(slotsRow.transform, "Player2Slot", "PLAYER 2");
            var p1Tmp = p1Slot.GetComponentInChildren<TextMeshProUGUI>();
            var p2Tmp = p2Slot.GetComponentInChildren<TextMeshProUGUI>();

            var banner = CreateTmp("StatusBanner", mainPanel.transform, "", 20, FontStyles.Bold, TextAlignmentOptions.Center, TextDim);
            AnchorBanner(banner.gameObject, 0.13f, 0.2f);

            var quickBlock = CreateUiRect("VideoStyleQuickLan", mainPanel.transform, false);
            {
                var qbImg = quickBlock.GetComponent<Image>();
                qbImg.color = new Color(0f, 0f, 0f, 0f);
                qbImg.raycastTarget = false;
                AnchorBottomBar(quickBlock.GetComponent<RectTransform>(), 150f, 120f, 56f);
                var vlg = quickBlock.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 6f;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;

                var hintGo = CreateTmp("QuickHint", quickBlock.transform,
                    "Netcode LAN tutorial pattern: Start Host · peer joins with IPv4:port (two builds on this PC: 127.0.0.1:7777) or 6-digit code.",
                    14, FontStyles.Normal, TextAlignmentOptions.Center, TextMuted);
                hintGo.enableWordWrapping = true;
                var hintLe = hintGo.gameObject.AddComponent<LayoutElement>();
                hintLe.preferredHeight = 44f;
                hintLe.flexibleWidth = 1f;

                var quickRow = CreateUiRect("QuickRow", quickBlock.transform, false);
                {
                    var rowImg = quickRow.GetComponent<Image>();
                    rowImg.color = new Color(0f, 0f, 0f, 0f);
                    rowImg.raycastTarget = false;
                    var rowLe = quickRow.AddComponent<LayoutElement>();
                    rowLe.preferredHeight = 52f;
                    rowLe.flexibleWidth = 1f;

                    var hostBtnGo = CreateButtonChild(quickRow.transform, "QuickHost", "START HOST",
                        new Color(0.22f, 0.48f, 0.3f, 1f));
                    hostBtnGo.AddComponent<DisableButtonHoverFeedback>();
                    Object.DestroyImmediate(hostBtnGo.GetComponent<LayoutElement>());
                    var hostRt = hostBtnGo.GetComponent<RectTransform>();
                    hostRt.anchorMin = new Vector2(0f, 0f);
                    hostRt.anchorMax = new Vector2(0f, 1f);
                    hostRt.pivot = new Vector2(0f, 0.5f);
                    hostRt.sizeDelta = new Vector2(200f, 52f);
                    hostRt.anchoredPosition = new Vector2(0f, 0f);

                    var inputWrap = CreateUiRect("QuickClientInputWrap", quickRow.transform, false);
                    var inputWrapRt = inputWrap.GetComponent<RectTransform>();
                    inputWrapRt.anchorMin = new Vector2(0f, 0f);
                    inputWrapRt.anchorMax = new Vector2(1f, 1f);
                    inputWrapRt.offsetMin = new Vector2(212f, 0f);
                    inputWrapRt.offsetMax = new Vector2(-248f, 0f);
                    SetImageColor(inputWrap, InputSurface);
                    AddOutline(inputWrap, SoftLine, 1f);

                    var quickInput = inputWrap.AddComponent<TMP_InputField>();
                    var quickInputText = CreateTmp("QuickInputText", inputWrap.transform, "", 20, FontStyles.Normal,
                        TextAlignmentOptions.Left, TextHi);
                    var quickInputTextRt = quickInputText.GetComponent<RectTransform>();
                    quickInputTextRt.anchorMin = Vector2.zero;
                    quickInputTextRt.anchorMax = Vector2.one;
                    quickInputTextRt.offsetMin = new Vector2(12, 8);
                    quickInputTextRt.offsetMax = new Vector2(-12, -8);
                    quickInput.textComponent = quickInputText;
                    quickInput.textViewport = inputWrap.GetComponent<RectTransform>();
                    quickInput.lineType = TMP_InputField.LineType.SingleLine;

                    var quickPh = CreateTmp("QuickPlaceholder", inputWrap.transform,
                        "127.0.0.1:7777 or 192.168.x.x:7777 or room code", 20, FontStyles.Italic,
                        TextAlignmentOptions.Left, TextMuted);
                    var quickPhRt = quickPh.GetComponent<RectTransform>();
                    quickPhRt.anchorMin = Vector2.zero;
                    quickPhRt.anchorMax = Vector2.one;
                    quickPhRt.offsetMin = new Vector2(12, 8);
                    quickPhRt.offsetMax = new Vector2(-12, -8);
                    quickInput.placeholder = quickPh;

                    var joinClientGo = CreateButtonChild(quickRow.transform, "QuickJoinClient", "JOIN AS CLIENT",
                        ButtonSurface);
                    joinClientGo.AddComponent<DisableButtonHoverFeedback>();
                    Object.DestroyImmediate(joinClientGo.GetComponent<LayoutElement>());
                    var joinClientRt = joinClientGo.GetComponent<RectTransform>();
                    joinClientRt.anchorMin = new Vector2(1f, 0f);
                    joinClientRt.anchorMax = new Vector2(1f, 1f);
                    joinClientRt.pivot = new Vector2(1f, 0.5f);
                    joinClientRt.sizeDelta = new Vector2(236f, 52f);
                    joinClientRt.anchoredPosition = new Vector2(0f, 0f);
                    var joinClientText = joinClientGo.GetComponentInChildren<TextMeshProUGUI>();
                    if (joinClientText != null)
                    {
                        joinClientText.fontSize = 17f;
                        joinClientText.enableWordWrapping = false;
                    }

                    var statusTmp = CreateTmp("QuickConnectStatus", quickBlock.transform, "", 15, FontStyles.Normal,
                        TextAlignmentOptions.Center, TextDim);
                    var stLe = statusTmp.gameObject.AddComponent<LayoutElement>();
                    stLe.preferredHeight = 22f;
                    stLe.flexibleWidth = 1f;

                    var quickSo = new SerializedObject(quickLan);
                    quickSo.FindProperty("lobbyManager").objectReferenceValue = lobbyMgr;
                    quickSo.FindProperty("clientAddressInput").objectReferenceValue = quickInput;
                    quickSo.FindProperty("statusText").objectReferenceValue = statusTmp;
                    quickSo.ApplyModifiedPropertiesWithoutUndo();

                    hostBtnGo.GetComponent<Button>().onClick.AddListener(quickLan.OnClickStartHost);
                    joinClientGo.GetComponent<Button>().onClick.AddListener(quickLan.OnClickJoinAsClient);
                }
            }

            var joinBtn = CreateButton(mainPanel.transform, "JoinRoomButton", "JOIN A ROOM", ButtonSurface);
            AnchorBottomBar(joinBtn.GetComponent<RectTransform>(), 88f, 52f, 220f);

            var backBtn = CreateButton(mainPanel.transform, "BackButton", "BACK TO MENU", ButtonSurface);
            AnchorBottomBar(backBtn.GetComponent<RectTransform>(), 28f, 48f, 220f);

            var nav = canvasGo.AddComponent<MenuNavigationGroup>();
            var navSo = new SerializedObject(nav);
            var ord = navSo.FindProperty("orderedSelectables");
            if (ord != null)
            {
                ord.arraySize = 2;
                ord.GetArrayElementAtIndex(0).objectReferenceValue = joinBtn.GetComponent<Button>();
                ord.GetArrayElementAtIndex(1).objectReferenceValue = backBtn.GetComponent<Button>();
            }

            navSo.ApplyModifiedPropertiesWithoutUndo();

            var modalBackdrop = CreateUiRect("JoinModalBackdrop", canvasGo.transform, true);
            SetImageColor(modalBackdrop, ModalBackdrop);
            modalBackdrop.SetActive(false);
            var backdropBtn = modalBackdrop.AddComponent<Button>();
            backdropBtn.targetGraphic = modalBackdrop.GetComponent<Image>();

            var modalPanel = CreateUiRect("JoinModalPanel", modalBackdrop.transform, false);
            var mpRt = modalPanel.GetComponent<RectTransform>();
            mpRt.anchorMin = new Vector2(0.5f, 0.5f);
            mpRt.anchorMax = new Vector2(0.5f, 0.5f);
            mpRt.sizeDelta = new Vector2(720f, 520f);
            SetImageColor(modalPanel, Panel);
            AddOutline(modalPanel, Accent, 1f);

            var modalTitle = CreateTmp("ModalTitle", modalPanel.transform, "JOIN ANOTHER ROOM", 30, FontStyles.Bold, TextAlignmentOptions.Center, TextHi);
            StretchTop(modalTitle.gameObject, -28f, -72f);

            var modalHint = CreateTmp("ModalHint", modalPanel.transform,
                "Enter the host’s 6-digit code, or IPv4:port if discovery is blocked.", 20, FontStyles.Normal,
                TextAlignmentOptions.Center, TextMuted);
            StretchTop(modalHint.gameObject, -88f, -56f);

            var inputGo = CreateUiRect("InputWrapper", modalPanel.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.sizeDelta = new Vector2(620f, 56f);
            inputRt.anchoredPosition = new Vector2(0, 40f);
            var inputBg = inputGo.GetComponent<Image>();
            inputBg.color = InputSurface;
            AddOutline(inputGo, SoftLine, 1f);

            var inputField = inputGo.AddComponent<TMP_InputField>();
            var inputText = CreateTmp("InputText", inputGo.transform, "", 24, FontStyles.Normal, TextAlignmentOptions.Left, TextHi);
            var textRt = inputText.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16, 8);
            textRt.offsetMax = new Vector2(-16, -8);
            inputField.textComponent = inputText;
            inputField.textViewport = inputGo.GetComponent<RectTransform>();
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            var placeholder = CreateTmp("Placeholder", inputGo.transform, "6-digit code or 192.168.x.x:port", 24, FontStyles.Italic,
                TextAlignmentOptions.Left, TextMuted);
            var phRt = placeholder.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(16, 8);
            phRt.offsetMax = new Vector2(-16, -8);
            inputField.placeholder = placeholder;

            var lanList = CreateTmp("LanList", modalPanel.transform, "", 16, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextMuted);
            var lanRt = lanList.GetComponent<RectTransform>();
            lanRt.anchorMin = new Vector2(0.5f, 0.5f);
            lanRt.anchorMax = new Vector2(0.5f, 0.5f);
            lanRt.sizeDelta = new Vector2(620f, 120f);
            lanRt.anchoredPosition = new Vector2(0, -72f);

            var errTmp = CreateTmp("JoinError", modalPanel.transform, "", 18, FontStyles.Normal, TextAlignmentOptions.Center, TextDim);
            var errRt = errTmp.GetComponent<RectTransform>();
            errRt.anchorMin = new Vector2(0.5f, 0.5f);
            errRt.anchorMax = new Vector2(0.5f, 0.5f);
            errRt.sizeDelta = new Vector2(640f, 64f);
            errRt.anchoredPosition = new Vector2(0, -168f);

            var rowBtns = CreateUiRect("ModalButtons", modalPanel.transform, false);
            var rbRt = rowBtns.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 0f);
            rbRt.anchorMax = new Vector2(0.5f, 0f);
            rbRt.pivot = new Vector2(0.5f, 0f);
            rbRt.sizeDelta = new Vector2(640f, 56f);
            rbRt.anchoredPosition = new Vector2(0, 24f);
            var hBtns = rowBtns.AddComponent<HorizontalLayoutGroup>();
            hBtns.spacing = 24f;
            hBtns.childAlignment = TextAnchor.MiddleCenter;
            hBtns.childControlWidth = true;
            hBtns.childControlHeight = true;
            hBtns.childForceExpandWidth = true;
            hBtns.padding = new RectOffset(0, 0, 0, 0);

            var cancelModal = CreateButtonChild(rowBtns.transform, "CancelModal", "CANCEL", ButtonSurface);
            var confirmModal = CreateButtonChild(rowBtns.transform, "ConfirmJoin", "CONNECT", Accent);

            var partySo = new SerializedObject(partyUi);
            partySo.FindProperty("lobbyManager").objectReferenceValue = lobbyMgr;
            partySo.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            partySo.FindProperty("titleText").objectReferenceValue = title;
            partySo.FindProperty("subtitleText").objectReferenceValue = subtitle;
            partySo.FindProperty("roomCodeBigText").objectReferenceValue = codeBig;
            partySo.FindProperty("directConnectText").objectReferenceValue = direct;
            partySo.FindProperty("player1SlotText").objectReferenceValue = p1Tmp;
            partySo.FindProperty("player2SlotText").objectReferenceValue = p2Tmp;
            partySo.FindProperty("statusBannerText").objectReferenceValue = banner;
            partySo.FindProperty("joinRoomButton").objectReferenceValue = joinBtn.GetComponent<Button>();
            partySo.FindProperty("backToMenuButton").objectReferenceValue = backBtn.GetComponent<Button>();
            partySo.FindProperty("joinModalBackdrop").objectReferenceValue = modalBackdrop;
            partySo.FindProperty("joinModalPanel").objectReferenceValue = modalPanel;
            partySo.FindProperty("joinCodeInput").objectReferenceValue = inputField;
            partySo.FindProperty("joinModalConfirmButton").objectReferenceValue = confirmModal.GetComponent<Button>();
            partySo.FindProperty("joinModalCancelButton").objectReferenceValue = cancelModal.GetComponent<Button>();
            partySo.FindProperty("joinModalErrorText").objectReferenceValue = errTmp;
            partySo.FindProperty("discoveredLanRoomsText").objectReferenceValue = lanList;
            partySo.ApplyModifiedPropertiesWithoutUndo();

            var cancelB = cancelModal.GetComponent<Button>();
            cancelB.onClick.RemoveAllListeners();
            cancelB.onClick.AddListener(() => partyUi.CloseJoinModal());

            backdropBtn.onClick.RemoveAllListeners();
            backdropBtn.onClick.AddListener(() => partyUi.CloseJoinModal());

            return canvasGo;
        }

        private static GameObject CreatePlayerSlot(Transform parent, string name, string header)
        {
            var go = CreateUiRect(name, parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 220f;
            le.flexibleWidth = 1f;
            SetImageColor(go, SlotSurface);
            AddOutline(go, SoftLine, 1f);
            var tmp = CreateTmp("Body", go.transform, header, 24, FontStyles.Bold, TextAlignmentOptions.Center, TextHi);
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 12);
            rt.offsetMax = new Vector2(-12, -12);
            return go;
        }

        private static GameObject CreateButtonChild(Transform parent, string name, string label, Color c)
        {
            var go = CreateUiRect(name, parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.flexibleWidth = 1f;
            SetImageColor(go, c);
            AddOutline(go, Accent, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var t = CreateTmp("Text", go.transform, label, 20f, FontStyles.Bold, TextAlignmentOptions.Center,
                IsDarkColor(c) ? Panel : TextHi);
            StretchFull(t.gameObject);
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color c)
        {
            var go = CreateUiRect(name, parent, false);
            SetImageColor(go, c);
            AddOutline(go, Accent, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var t = CreateTmp("Text", go.transform, label, 22, FontStyles.Bold, TextAlignmentOptions.Center,
                IsDarkColor(c) ? Panel : TextHi);
            StretchFull(t.gameObject);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchTop(GameObject go, float topY, float height)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, topY);
            rt.sizeDelta = new Vector2(0, height);
        }

        private static void AnchorBanner(GameObject go, float anchorYMin, float anchorYMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, anchorYMin);
            rt.anchorMax = new Vector2(0.92f, anchorYMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AnchorBottomBar(RectTransform rt, float bottom, float height, float horizontalInset)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-horizontalInset * 2f, height);
            rt.anchoredPosition = new Vector2(0f, bottom);
        }

        private static GameObject CreateUiRect(string name, Transform parent, bool stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
                rt.sizeDelta = Vector2.zero;
            go.AddComponent<Image>();
            return go;
        }

        private static void SetImageColor(GameObject go, Color c)
        {
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = true;
        }

        private static void AddOutline(GameObject go, Color c, float t)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(t, -t);
        }

        private static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size, FontStyles style,
            TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            var font = LoadThemeFont();
            if (font != null)
                tmp.font = font;
            return tmp;
        }

        private static TMP_FontAsset LoadThemeFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontAssetPath);
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        private static bool IsDarkColor(Color color)
        {
            var luminance = (color.r * 0.299f) + (color.g * 0.587f) + (color.b * 0.114f);
            return luminance < 0.45f;
        }

        private static void RegisterInBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Any(s => s.path == path))
            {
                EditorBuildSettings.scenes = list.ToArray();
                return;
            }

            int insertAt = list.Count;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].path.Contains("LobbyScene"))
                {
                    insertAt = i + 1;
                    break;
                }
            }

            list.Insert(insertAt, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
