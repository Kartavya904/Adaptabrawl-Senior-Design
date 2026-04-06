#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using Adaptabrawl.UI;
using Adaptabrawl.Networking;
using Adaptabrawl.Input;
using Unity.Netcode;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Builds the in-scene pause overlay (Canvas hierarchy + <see cref="MatchPauseController"/>).
    /// Run once per gameplay scene (GameScene, OnlineGameScene).
    /// </summary>
    public static class MatchPauseUISetup
    {
        private const string MenuPath = "Adaptabrawl/Add Match Pause UI to Open Scene";
        private const string ThemeFontAssetPath = "Assets/UniNeue-Trial-Heavy SDF.asset";

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.16f);
        private static readonly Color SurfaceColor = new Color(1f, 1f, 1f, 0.985f);
        private static readonly Color SecondarySurfaceColor = new Color(0.975f, 0.975f, 0.975f, 1f);
        private static readonly Color OutlineColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color SoftOutlineColor = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color PrimaryTextColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [MenuItem(MenuPath, true)]
        private static bool Validate() => !Application.isPlaying;

        [MenuItem(MenuPath)]
        public static void AddMatchPauseUI()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Match Pause", "Open GameScene or OnlineGameScene first.", "OK");
                return;
            }

            if (Object.FindFirstObjectByType<MatchPauseController>() != null)
            {
                var existing = Object.FindFirstObjectByType<MatchPauseController>();
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                EditorUtility.DisplayDialog("Match Pause", "This scene already has a MatchPauseController. It has been selected in the Hierarchy.", "OK");
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                Undo.RegisterCreatedObjectUndo(canvasGo, "Add Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(es, "Add EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            GameObject root = new GameObject("MatchPauseUI");
            Undo.RegisterCreatedObjectUndo(root, "Add MatchPauseUI");
            root.transform.SetParent(canvas.transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            StretchFull(rootRt);
            root.transform.SetAsLastSibling();

            var controller = Undo.AddComponent<MatchPauseController>(root);

            GameObject overlay = CreateChild(root, "PauseOverlay");
            var overlayRt = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRt);
            var dim = overlay.AddComponent<Image>();
            dim.color = OverlayColor;
            dim.raycastTarget = true;
            overlay.SetActive(false);

            GameObject requestPanel = CreateChild(overlay, "PauseRequestPanel");
            var requestPanelRt = requestPanel.GetComponent<RectTransform>();
            requestPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
            requestPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
            requestPanelRt.pivot = new Vector2(0.5f, 0.5f);
            requestPanelRt.sizeDelta = new Vector2(920f, 240f);
            var requestBg = requestPanel.AddComponent<Image>();
            requestBg.color = SurfaceColor;
            requestBg.raycastTarget = true;
            AddOutline(requestPanel, OutlineColor, 1f);
            var reqVert = requestPanel.AddComponent<VerticalLayoutGroup>();
            reqVert.childAlignment = TextAnchor.MiddleCenter;
            reqVert.spacing = 10f;
            reqVert.padding = new RectOffset(56, 56, 44, 44);
            var reqFit = requestPanel.AddComponent<ContentSizeFitter>();
            reqFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var reqLabelGo = CreateChild(requestPanel, "RequestLabel");
            var reqLabel = reqLabelGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(reqLabel);
            reqLabel.text = "PAUSE REQUEST";
            reqLabel.fontSize = 24;
            reqLabel.fontStyle = FontStyles.Bold;
            reqLabel.alignment = TextAlignmentOptions.Center;
            reqLabel.color = PrimaryTextColor;
            reqLabel.raycastTarget = false;
            var reqLabelLe = reqLabelGo.AddComponent<LayoutElement>();
            reqLabelLe.minHeight = 36f;

            var reqTmpGo = CreateChild(requestPanel, "RequestMessage");
            var reqTmp = reqTmpGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(reqTmp);
            reqTmp.text = "Pause request…";
            reqTmp.fontSize = 24;
            reqTmp.alignment = TextAlignmentOptions.Center;
            reqTmp.color = SecondaryTextColor;
            var reqTmpRt = reqTmpGo.GetComponent<RectTransform>();
            reqTmpRt.sizeDelta = new Vector2(780, 140);

            GameObject menuPanel = CreateChild(overlay, "PauseMenuPanel");
            var menuRt = menuPanel.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.5f, 0.5f);
            menuRt.anchorMax = new Vector2(0.5f, 0.5f);
            menuRt.pivot = new Vector2(0.5f, 0.5f);
            menuRt.sizeDelta = new Vector2(560, 452);

            var menuImg = menuPanel.AddComponent<Image>();
            menuImg.color = SurfaceColor;
            menuImg.raycastTarget = true;
            AddOutline(menuPanel, OutlineColor, 1f);

            var menuVert = menuPanel.AddComponent<VerticalLayoutGroup>();
            menuVert.padding = new RectOffset(40, 40, 36, 36);
            menuVert.spacing = 16f;
            menuVert.childAlignment = TextAnchor.MiddleCenter;
            menuVert.childControlHeight = true;
            menuVert.childControlWidth = true;
            menuVert.childForceExpandHeight = false;
            menuVert.childForceExpandWidth = true;

            var topRule = CreateChild(menuPanel, "TopRule");
            var topRuleImage = topRule.AddComponent<Image>();
            topRuleImage.color = SoftOutlineColor;
            var topRuleLe = topRule.AddComponent<LayoutElement>();
            topRuleLe.minHeight = 1f;
            topRuleLe.preferredHeight = 1f;

            var titleGo = CreateChild(menuPanel, "Title");
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(titleTmp);
            titleTmp.text = "PAUSED";
            titleTmp.fontSize = 34;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = PrimaryTextColor;
            titleTmp.raycastTarget = false;
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.minHeight = 48f;

            var subtitleGo = CreateChild(menuPanel, "Subtitle");
            var subtitleTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(subtitleTmp);
            subtitleTmp.text = "Match paused on this screen.";
            subtitleTmp.fontSize = 18;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.color = SecondaryTextColor;
            subtitleTmp.raycastTarget = false;
            var subtitleLe = subtitleGo.AddComponent<LayoutElement>();
            subtitleLe.minHeight = 28f;

            var midRule = CreateChild(menuPanel, "MidRule");
            var midRuleImage = midRule.AddComponent<Image>();
            midRuleImage.color = SoftOutlineColor;
            var midRuleLe = midRule.AddComponent<LayoutElement>();
            midRuleLe.minHeight = 1f;
            midRuleLe.preferredHeight = 1f;

            Button resume = CreateMenuButton(menuPanel, "ResumeButton", "Resume", destructive: false);
            Button restart = CreateMenuButton(menuPanel, "RestartButton", "Restart", destructive: false);
            Button changeCharacters = CreateMenuButton(menuPanel, "ChangeCharactersButton", "Change Characters", destructive: false);
            Button settings = CreateMenuButton(menuPanel, "SettingsButton", "Settings", destructive: false);
            Button mainMenu = CreateMenuButton(menuPanel, "MainMenuButton", "Main Menu", destructive: true);

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("pauseOverlayRoot").objectReferenceValue = overlay;
            so.FindProperty("pauseMenuPanel").objectReferenceValue = menuPanel;
            so.FindProperty("pauseRequestPanel").objectReferenceValue = requestPanel;
            so.FindProperty("pauseRequestMessage").objectReferenceValue = reqTmp;
            so.FindProperty("pausedTitleText").objectReferenceValue = titleTmp;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("restartButton").objectReferenceValue = restart;
            so.FindProperty("changeCharactersButton").objectReferenceValue = changeCharacters;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
            var focusProp = so.FindProperty("pauseMenuFocusOrder");
            focusProp.arraySize = 5;
            focusProp.GetArrayElementAtIndex(0).objectReferenceValue = resume;
            focusProp.GetArrayElementAtIndex(1).objectReferenceValue = restart;
            focusProp.GetArrayElementAtIndex(2).objectReferenceValue = changeCharacters;
            focusProp.GetArrayElementAtIndex(3).objectReferenceValue = settings;
            focusProp.GetArrayElementAtIndex(4).objectReferenceValue = mainMenu;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (scene.name == "OnlineGameScene")
            {
                var sync = Object.FindFirstObjectByType<OnlineTwoPlayerInputHandler>();
                if (sync != null)
                {
                    var netObj = sync.GetComponent<NetworkObject>();
                    if (netObj == null)
                        Debug.LogWarning("[MatchPauseUISetup] OnlineTwoPlayerInputHandler has no NetworkObject — add OnlineMutualPauseCoordinator manually to a spawned NetworkObject.");
                    else if (sync.GetComponent<OnlineMutualPauseCoordinator>() == null)
                    {
                        var coord = Undo.AddComponent<OnlineMutualPauseCoordinator>(sync.gameObject);
                        SerializedObject cso = new SerializedObject(controller);
                        cso.FindProperty("netCoordinator").objectReferenceValue = coord;
                        cso.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else
                    {
                        SerializedObject cso = new SerializedObject(controller);
                        cso.FindProperty("netCoordinator").objectReferenceValue = sync.GetComponent<OnlineMutualPauseCoordinator>();
                        cso.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                    Debug.LogWarning("[MatchPauseUISetup] No OnlineTwoPlayerInputHandler in scene — assign OnlineMutualPauseCoordinator on MatchPauseController manually.");
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(scene);

            EditorUtility.DisplayDialog("Match Pause",
                "Pause UI added under Canvas.\n\n" +
                "Local (GameScene): Esc or any gamepad Start/Options pauses.\n" +
                "Online (OnlineGameScene): both players must press pause; coordinator was added to OnlineSyncManager when possible.",
                "OK");
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Pause UI");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void ApplyDefaultTmp(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontAssetPath);
            if (font != null)
                tmp.font = font;
            else if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
        }

        private static void AddOutline(GameObject go, Color c, float thickness)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(thickness, -thickness);
        }

        private static Button CreateMenuButton(GameObject menuPanel, string name, string label, bool destructive)
        {
            var go = CreateChild(menuPanel, name);
            var img = go.AddComponent<Image>();
            img.color = destructive ? OutlineColor : SecondarySurfaceColor;
            AddOutline(go, OutlineColor, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            btn.colors = cb;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;

            var textGo = CreateChild(go, "Text");
            StretchFull(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(tmp);
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = destructive ? SurfaceColor : PrimaryTextColor;

            return btn;
        }
    }
}
#endif
