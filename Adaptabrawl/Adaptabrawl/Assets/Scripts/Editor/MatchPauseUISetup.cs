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
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            dim.raycastTarget = true;
            overlay.SetActive(false);

            GameObject requestPanel = CreateChild(overlay, "PauseRequestPanel");
            StretchFull(requestPanel.GetComponent<RectTransform>());
            var reqVert = requestPanel.AddComponent<VerticalLayoutGroup>();
            reqVert.childAlignment = TextAnchor.MiddleCenter;
            reqVert.spacing = 16f;
            reqVert.padding = new RectOffset(48, 48, 48, 48);
            var reqFit = requestPanel.AddComponent<ContentSizeFitter>();
            reqFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var reqTmpGo = CreateChild(requestPanel, "RequestMessage");
            var reqTmp = reqTmpGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(reqTmp);
            reqTmp.text = "Pause request…";
            reqTmp.fontSize = 32;
            reqTmp.alignment = TextAlignmentOptions.Center;
            reqTmp.color = Color.white;
            var reqTmpRt = reqTmpGo.GetComponent<RectTransform>();
            reqTmpRt.sizeDelta = new Vector2(1600, 200);

            GameObject menuPanel = CreateChild(overlay, "PauseMenuPanel");
            var menuRt = menuPanel.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.5f, 0.5f);
            menuRt.anchorMax = new Vector2(0.5f, 0.5f);
            menuRt.pivot = new Vector2(0.5f, 0.5f);
            menuRt.sizeDelta = new Vector2(520, 420);

            var menuImg = menuPanel.AddComponent<Image>();
            menuImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);
            menuImg.raycastTarget = true;
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (uiSprite != null)
            {
                menuImg.sprite = uiSprite;
                menuImg.type = Image.Type.Sliced;
            }

            var menuVert = menuPanel.AddComponent<VerticalLayoutGroup>();
            menuVert.padding = new RectOffset(32, 32, 28, 28);
            menuVert.spacing = 14f;
            menuVert.childAlignment = TextAnchor.MiddleCenter;
            menuVert.childControlHeight = true;
            menuVert.childControlWidth = true;
            menuVert.childForceExpandHeight = false;
            menuVert.childForceExpandWidth = true;

            var titleGo = CreateChild(menuPanel, "Title");
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(titleTmp);
            titleTmp.text = "Paused";
            titleTmp.fontSize = 40;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.78f, 0.93f, 1f, 1f);
            titleTmp.raycastTarget = false;
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.minHeight = 56f;

            Button resume = CreateMenuButton(menuPanel, "ResumeButton", "Resume", destructive: false);
            Button settings = CreateMenuButton(menuPanel, "SettingsButton", "Settings", destructive: false);
            Button mainMenu = CreateMenuButton(menuPanel, "MainMenuButton", "Main Menu", destructive: true);

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("pauseOverlayRoot").objectReferenceValue = overlay;
            so.FindProperty("pauseMenuPanel").objectReferenceValue = menuPanel;
            so.FindProperty("pauseRequestPanel").objectReferenceValue = requestPanel;
            so.FindProperty("pauseRequestMessage").objectReferenceValue = reqTmp;
            so.FindProperty("pausedTitleText").objectReferenceValue = titleTmp;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
            var focusProp = so.FindProperty("pauseMenuFocusOrder");
            focusProp.arraySize = 3;
            focusProp.GetArrayElementAtIndex(0).objectReferenceValue = resume;
            focusProp.GetArrayElementAtIndex(1).objectReferenceValue = settings;
            focusProp.GetArrayElementAtIndex(2).objectReferenceValue = mainMenu;
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
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
        }

        private static Button CreateMenuButton(GameObject menuPanel, string name, string label, bool destructive)
        {
            var go = CreateChild(menuPanel, name);
            var img = go.AddComponent<Image>();
            img.color = destructive
                ? new Color(0.42f, 0.18f, 0.2f, 1f)
                : new Color(0.14f, 0.26f, 0.42f, 1f);
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (uiSprite != null)
            {
                img.sprite = uiSprite;
                img.type = Image.Type.Sliced;
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            if (destructive)
            {
                cb.highlightedColor = new Color(1f, 0.65f, 0.55f, 1f);
                cb.selectedColor = new Color(0.98f, 0.55f, 0.5f, 1f);
                cb.pressedColor = new Color(0.75f, 0.35f, 0.32f, 1f);
            }
            else
            {
                cb.highlightedColor = new Color(0.55f, 0.92f, 1f, 1f);
                cb.selectedColor = new Color(0.5f, 0.88f, 1f, 1f);
                cb.pressedColor = new Color(0.4f, 0.65f, 0.85f, 1f);
            }
            btn.colors = cb;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;

            var textGo = CreateChild(go, "Text");
            StretchFull(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaultTmp(tmp);
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
#endif
