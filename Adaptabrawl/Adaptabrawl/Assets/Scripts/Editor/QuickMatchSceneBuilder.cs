using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adaptabrawl.Gameplay;
using Adaptabrawl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Adaptabrawl.Editor
{
    public static class QuickMatchSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/QuickMatchScene.unity";
        private const string StartScenePath = "Assets/Scenes/StartScene.unity";
        private const string SetupScenePath = "Assets/Scenes/SetupScene.unity";
        private const string ThemeFontAssetPath = "Assets/UniNeue-Trial-Heavy SDF.asset";
        private const string MenuBackgroundSpritePath = "Assets/Images/Adaptabrawl_Menu_BG.png";

        private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
        private static readonly Color CardColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);
        private static readonly Color AccentColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color SoftAccent = new Color(0.73f, 0.73f, 0.76f, 1f);
        private static readonly Color TextPrimary = new Color(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color TextSecondary = new Color(0.76f, 0.76f, 0.78f, 1f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.13f, 0.15f, 1f);
        private static readonly Color PositiveButtonColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        private static readonly Color OutlineColor = new Color(0.70f, 0.70f, 0.70f, 0.78f);

        private sealed class ArenaPresentationData
        {
            public readonly List<string> Names = new List<string>();
            public readonly List<Sprite> Backgrounds = new List<Sprite>();
        }

        [MenuItem("Tools/Adaptabrawl/Quick Match/Build Quick Match Scene")]
        public static void BuildOrUpdateQuickMatchScene()
        {
            Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                DestroyIfExists(scene, "QuickMatchCanvas");
                DestroyIfExists(scene, "QuickMatchController");
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            EnsureEventSystem(scene);

            var controllerGo = new GameObject("QuickMatchController");
            EditorSceneManager.MoveGameObjectToScene(controllerGo, scene);
            var quickMatchUi = controllerGo.AddComponent<QuickMatchSetupUI>();

            GameObject canvas = BuildQuickMatchCanvas(quickMatchUi);
            EditorSceneManager.MoveGameObjectToScene(canvas, scene);

            RegisterInBuildSettings(ScenePath, "SetupScene");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[QuickMatchSceneBuilder] QuickMatchScene was built or updated successfully.");
        }

        [MenuItem("Tools/Adaptabrawl/Quick Match/Inject Quick Match Button Into Start Scene")]
        public static void UpdateStartSceneForQuickMatch()
        {
            if (!File.Exists(StartScenePath))
            {
                Debug.LogError("[QuickMatchSceneBuilder] StartScene was not found under Assets/Scenes.");
                return;
            }

            Scene startScene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
            if (mainMenu == null)
            {
                Debug.LogError("[QuickMatchSceneBuilder] MainMenu was not found in StartScene.");
                return;
            }

            SerializedObject menuSo = new SerializedObject(mainMenu);
            var quickMatchProp = menuSo.FindProperty("quickMatchButton");
            var localPlayProp = menuSo.FindProperty("localPlayButton");
            var onlineProp = menuSo.FindProperty("onlineButton");
            var backProp = menuSo.FindProperty("backButton");
            var playOptionsProp = menuSo.FindProperty("playOptionsPanel");
            var focusOrderProp = menuSo.FindProperty("playOptionsFocusOrder");

            var localButton = localPlayProp?.objectReferenceValue as Button;
            var onlineButton = onlineProp?.objectReferenceValue as Button;
            var backButton = backProp?.objectReferenceValue as Button;
            var quickButton = quickMatchProp?.objectReferenceValue as Button;

            if (localButton == null)
            {
                Debug.LogError("[QuickMatchSceneBuilder] localPlayButton is missing from MainMenu.");
                return;
            }

            Transform parent = localButton.transform.parent;
            if (playOptionsProp?.objectReferenceValue is GameObject playOptionsPanel && playOptionsPanel != null)
                parent = localButton.transform.parent != null ? localButton.transform.parent : playOptionsPanel.transform;

            if (quickButton == null)
            {
                quickButton = Object.Instantiate(localButton, parent);
                quickButton.name = "QuickMatchButton";
            }

            ApplyButtonLabel(quickButton, "Quick Match");
            PositionQuickMatchButton(localButton, onlineButton, quickButton);

            quickMatchProp.objectReferenceValue = quickButton;
            if (focusOrderProp != null)
            {
                focusOrderProp.arraySize = 4;
                focusOrderProp.GetArrayElementAtIndex(0).objectReferenceValue = quickButton;
                focusOrderProp.GetArrayElementAtIndex(1).objectReferenceValue = localButton;
                focusOrderProp.GetArrayElementAtIndex(2).objectReferenceValue = onlineButton;
                focusOrderProp.GetArrayElementAtIndex(3).objectReferenceValue = backButton;
            }

            menuSo.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(startScene);
            EditorSceneManager.SaveScene(startScene, StartScenePath);

            Debug.Log("[QuickMatchSceneBuilder] StartScene was updated with a Quick Match button.");
        }

        private static GameObject BuildQuickMatchCanvas(QuickMatchSetupUI quickMatchUi)
        {
            ArenaPresentationData arenaPresentation = LoadArenaPresentationData();

            var canvasGo = new GameObject("QuickMatchCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = CreateUiRect("Root", canvasGo.transform, stretch: true);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = BackgroundColor;
            Sprite menuBackground = LoadSprite(MenuBackgroundSpritePath);
            if (menuBackground != null)
            {
                rootImage.sprite = menuBackground;
                rootImage.type = Image.Type.Sliced;
            }

            var overlay = CreateUiRect("Overlay", root.transform, stretch: true);
            overlay.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.78f);

            var title = CreateText("Title", root.transform, "QUICK MATCH", 42f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            SetAnchors(title.rectTransform, 0.5f, 1f, 0.5f, 1f, new Vector2(0f, -52f), new Vector2(960f, 64f));

            var subtitle = CreateText("Subtitle", root.transform, "Offline single-player setup with local model training and persistent CPU champions.", 18f, FontStyles.Normal, TextAlignmentOptions.Center, TextSecondary);
            SetAnchors(subtitle.rectTransform, 0.5f, 1f, 0.5f, 1f, new Vector2(0f, -104f), new Vector2(1120f, 32f));

            var content = CreateUiRect("Content", root.transform, stretch: false);
            SetAnchors(content.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, new Vector2(0f, -10f), new Vector2(-120f, -210f));
            content.GetComponent<RectTransform>().offsetMin = new Vector2(80f, 150f);
            content.GetComponent<RectTransform>().offsetMax = new Vector2(-80f, -120f);

            var leftColumn = CreateUiRect("LeftColumn", content.transform, stretch: false);
            leftColumn.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            SetAnchors(leftColumn.GetComponent<RectTransform>(), 0f, 0f, 0.48f, 1f, Vector2.zero, Vector2.zero);

            var rightColumn = CreateUiRect("RightColumn", content.transform, stretch: false);
            rightColumn.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            SetAnchors(rightColumn.GetComponent<RectTransform>(), 0.52f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            GameObject difficultyCard = CreateCard("DifficultyCard", leftColumn.transform, "Difficulty");
            SetAnchors(difficultyCard.GetComponent<RectTransform>(), 0f, 0.67f, 1f, 1f, Vector2.zero, new Vector2(0f, -12f));
            var difficultyPrev = CreateButton("PreviousDifficultyButton", difficultyCard.transform, "<", ButtonColor, 22f);
            var difficultyNext = CreateButton("NextDifficultyButton", difficultyCard.transform, ">", ButtonColor, 22f);
            var difficultyValue = CreateText("DifficultyValueText", difficultyCard.transform, "Trainer", 28f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            var difficultyDescription = CreateText("DifficultyDescriptionText", difficultyCard.transform, "", 15f, FontStyles.Normal, TextAlignmentOptions.Top, TextSecondary);
            SetAnchors(difficultyPrev.GetComponent<RectTransform>(), 0f, 0.58f, 0f, 0.58f, new Vector2(54f, -6f), new Vector2(74f, 54f));
            SetAnchors(difficultyNext.GetComponent<RectTransform>(), 1f, 0.58f, 1f, 0.58f, new Vector2(-54f, -6f), new Vector2(74f, 54f));
            SetAnchors(difficultyValue.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, new Vector2(0f, 0f), new Vector2(420f, 42f));
            SetAnchors(difficultyDescription.rectTransform, 0.5f, 0.18f, 0.5f, 0.18f, new Vector2(0f, 24f), new Vector2(560f, 118f));

            GameObject inputCard = CreateCard("InputCard", leftColumn.transform, "Input Mode");
            SetAnchors(inputCard.GetComponent<RectTransform>(), 0f, 0.36f, 1f, 0.63f, Vector2.zero, new Vector2(0f, -12f));
            var inputModeValue = CreateText("InputModeValueText", inputCard.transform, "Keyboard", 28f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            var inputModeHint = CreateText("InputModeHintText", inputCard.transform, "", 15f, FontStyles.Normal, TextAlignmentOptions.Center, TextSecondary);
            var toggleInputButton = CreateButton("ToggleInputModeButton", inputCard.transform, "Change Input", PositiveButtonColor, 18f, darkText: true);
            SetAnchors(inputModeValue.rectTransform, 0.5f, 0.68f, 0.5f, 0.68f, Vector2.zero, new Vector2(420f, 40f));
            SetAnchors(inputModeHint.rectTransform, 0.5f, 0.4f, 0.5f, 0.4f, Vector2.zero, new Vector2(560f, 78f));
            SetAnchors(toggleInputButton.GetComponent<RectTransform>(), 0.5f, 0.12f, 0.5f, 0.12f, Vector2.zero, new Vector2(260f, 48f));

            GameObject arenaCard = CreateCard("ArenaCard", leftColumn.transform, "Arena");
            SetAnchors(arenaCard.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.33f, Vector2.zero, Vector2.zero);
            var arenaPreview = CreateUiRect("ArenaPreviewImage", arenaCard.transform, stretch: false);
            arenaPreview.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.16f, 1f);
            ApplyOutline(arenaPreview, OutlineColor, 1f);
            SetAnchors(arenaPreview.GetComponent<RectTransform>(), 0.5f, 0.62f, 0.5f, 0.62f, Vector2.zero, new Vector2(540f, 154f));
            var arenaValue = CreateText("ArenaValueText", arenaCard.transform, arenaPresentation.Names.Count > 0 ? arenaPresentation.Names[0] : "Arena", 24f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            var arenaPrev = CreateButton("PreviousArenaButton", arenaCard.transform, "<", ButtonColor, 22f);
            var arenaNext = CreateButton("NextArenaButton", arenaCard.transform, ">", ButtonColor, 22f);
            SetAnchors(arenaValue.rectTransform, 0.5f, 0.18f, 0.5f, 0.18f, new Vector2(0f, 6f), new Vector2(420f, 34f));
            SetAnchors(arenaPrev.GetComponent<RectTransform>(), 0f, 0.18f, 0f, 0.18f, new Vector2(54f, 6f), new Vector2(74f, 48f));
            SetAnchors(arenaNext.GetComponent<RectTransform>(), 1f, 0.18f, 1f, 0.18f, new Vector2(-54f, 6f), new Vector2(74f, 48f));

            GameObject playerCard = CreateCard("PlayerCard", rightColumn.transform, "Player 1");
            SetAnchors(playerCard.GetComponent<RectTransform>(), 0f, 0.53f, 1f, 1f, Vector2.zero, new Vector2(0f, -12f));
            var playerName = CreateText("PlayerNameText", playerCard.transform, "Fighter", 26f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            var playerSummary = CreateText("PlayerSummaryText", playerCard.transform, "", 15f, FontStyles.Normal, TextAlignmentOptions.Center, TextSecondary);
            var playerPrev = CreateButton("PreviousPlayerButton", playerCard.transform, "<", ButtonColor, 22f);
            var playerNext = CreateButton("NextPlayerButton", playerCard.transform, ">", ButtonColor, 22f);
            SetAnchors(playerName.rectTransform, 0.5f, 0.60f, 0.5f, 0.60f, Vector2.zero, new Vector2(420f, 42f));
            SetAnchors(playerSummary.rectTransform, 0.5f, 0.38f, 0.5f, 0.38f, Vector2.zero, new Vector2(560f, 78f));
            SetAnchors(playerPrev.GetComponent<RectTransform>(), 0f, 0.60f, 0f, 0.60f, new Vector2(54f, -4f), new Vector2(74f, 52f));
            SetAnchors(playerNext.GetComponent<RectTransform>(), 1f, 0.60f, 1f, 0.60f, new Vector2(-54f, -4f), new Vector2(74f, 52f));

            GameObject opponentCard = CreateCard("OpponentCard", rightColumn.transform, "Opponent Preview");
            SetAnchors(opponentCard.GetComponent<RectTransform>(), 0f, 0.19f, 1f, 0.5f, Vector2.zero, new Vector2(0f, -12f));
            var opponentPortrait = CreateUiRect("OpponentPortraitImage", opponentCard.transform, stretch: false);
            opponentPortrait.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.16f, 1f);
            ApplyOutline(opponentPortrait, OutlineColor, 1f);
            var opponentName = CreateText("OpponentNameText", opponentCard.transform, "Random Preview", 24f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
            var opponentSummary = CreateText("OpponentSummaryText", opponentCard.transform, "", 15f, FontStyles.Normal, TextAlignmentOptions.Center, TextSecondary);
            var opponentPrev = CreateButton("PreviousOpponentButton", opponentCard.transform, "<", ButtonColor, 22f);
            var opponentNext = CreateButton("NextOpponentButton", opponentCard.transform, ">", ButtonColor, 22f);
            SetAnchors(opponentPortrait.GetComponent<RectTransform>(), 0.5f, 0.58f, 0.5f, 0.58f, Vector2.zero, new Vector2(160f, 160f));
            SetAnchors(opponentName.rectTransform, 0.5f, 0.26f, 0.5f, 0.26f, Vector2.zero, new Vector2(460f, 34f));
            SetAnchors(opponentSummary.rectTransform, 0.5f, 0.1f, 0.5f, 0.1f, new Vector2(0f, 10f), new Vector2(540f, 62f));
            SetAnchors(opponentPrev.GetComponent<RectTransform>(), 0f, 0.26f, 0f, 0.26f, new Vector2(54f, -4f), new Vector2(74f, 48f));
            SetAnchors(opponentNext.GetComponent<RectTransform>(), 1f, 0.26f, 1f, 0.26f, new Vector2(-54f, -4f), new Vector2(74f, 48f));

            GameObject modelCard = CreateCard("ModelCard", rightColumn.transform, "Champion Models");
            SetAnchors(modelCard.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.16f, Vector2.zero, Vector2.zero);
            var dummyInfo = CreateText("DummyModelInfoText", modelCard.transform, "Dummy", 16f, FontStyles.Normal, TextAlignmentOptions.Left, TextPrimary);
            var trainerInfo = CreateText("TrainerModelInfoText", modelCard.transform, "Trainer", 16f, FontStyles.Normal, TextAlignmentOptions.Left, TextPrimary);
            var extremeInfo = CreateText("ExtremeModelInfoText", modelCard.transform, "Extreme", 16f, FontStyles.Normal, TextAlignmentOptions.Left, TextPrimary);
            SetAnchors(dummyInfo.rectTransform, 0f, 0.5f, 0.33f, 0.5f, new Vector2(28f, 0f), new Vector2(-24f, 80f));
            SetAnchors(trainerInfo.rectTransform, 0.33f, 0.5f, 0.66f, 0.5f, new Vector2(18f, 0f), new Vector2(-18f, 80f));
            SetAnchors(extremeInfo.rectTransform, 0.66f, 0.5f, 1f, 0.5f, new Vector2(24f, 0f), new Vector2(-28f, 80f));

            var actionsRow = CreateUiRect("ActionsRow", root.transform, stretch: false);
            actionsRow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            SetAnchors(actionsRow.GetComponent<RectTransform>(), 0.5f, 0f, 0.5f, 0f, new Vector2(0f, 74f), new Vector2(900f, 56f));
            var retrainButton = CreateButton("RetrainModelsButton", actionsRow.transform, "Retrain Models", ButtonColor, 18f);
            var startButton = CreateButton("StartMatchButton", actionsRow.transform, "Start Match", PositiveButtonColor, 18f, darkText: true);
            var backButton = CreateButton("BackButton", actionsRow.transform, "Back To Menu", ButtonColor, 18f);
            SetAnchors(retrainButton.GetComponent<RectTransform>(), 0f, 0.5f, 0f, 0.5f, new Vector2(0f, 0f), new Vector2(250f, 52f));
            SetAnchors(startButton.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(250f, 52f));
            SetAnchors(backButton.GetComponent<RectTransform>(), 1f, 0.5f, 1f, 0.5f, new Vector2(0f, 0f), new Vector2(250f, 52f));

            var status = CreateText("StatusText", root.transform, "Ready.", 16f, FontStyles.Normal, TextAlignmentOptions.Center, TextSecondary);
            SetAnchors(status.rectTransform, 0.5f, 0f, 0.5f, 0f, new Vector2(0f, 26f), new Vector2(1100f, 26f));

            var uiSo = new SerializedObject(quickMatchUi);
            AssignReference(uiSo, "difficultyValueText", difficultyValue);
            AssignReference(uiSo, "difficultyDescriptionText", difficultyDescription);
            AssignReference(uiSo, "difficultyPreviousButton", difficultyPrev);
            AssignReference(uiSo, "difficultyNextButton", difficultyNext);
            AssignReference(uiSo, "inputModeValueText", inputModeValue);
            AssignReference(uiSo, "inputModeHintText", inputModeHint);
            AssignReference(uiSo, "toggleInputModeButton", toggleInputButton);
            AssignReference(uiSo, "arenaValueText", arenaValue);
            AssignReference(uiSo, "arenaPreviewImage", arenaPreview.GetComponent<Image>());
            AssignReference(uiSo, "arenaPreviousButton", arenaPrev);
            AssignReference(uiSo, "arenaNextButton", arenaNext);
            AssignReference(uiSo, "playerNameText", playerName);
            AssignReference(uiSo, "playerSummaryText", playerSummary);
            AssignReference(uiSo, "playerPortraitImage", null);
            AssignReference(uiSo, "playerPreviousButton", playerPrev);
            AssignReference(uiSo, "playerNextButton", playerNext);
            AssignReference(uiSo, "opponentNameText", opponentName);
            AssignReference(uiSo, "opponentSummaryText", opponentSummary);
            AssignReference(uiSo, "opponentPortraitImage", opponentPortrait.GetComponent<Image>());
            AssignReference(uiSo, "opponentPreviousButton", opponentPrev);
            AssignReference(uiSo, "opponentNextButton", opponentNext);
            AssignReference(uiSo, "dummyModelInfoText", dummyInfo);
            AssignReference(uiSo, "trainerModelInfoText", trainerInfo);
            AssignReference(uiSo, "extremeModelInfoText", extremeInfo);
            AssignReference(uiSo, "statusText", status);
            AssignReference(uiSo, "startMatchButton", startButton);
            AssignReference(uiSo, "retrainModelsButton", retrainButton);
            AssignReference(uiSo, "backButton", backButton);
            AssignStringList(uiSo, "availableArenas", arenaPresentation.Names);
            AssignSpriteList(uiSo, "arenaBackgrounds", arenaPresentation.Backgrounds);
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return canvasGo;
        }

        private static void DestroyIfExists(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == objectName)
                    Object.DestroyImmediate(root);
            }
        }

        private static void EnsureEventSystem(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<EventSystem>(true) != null)
                    return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.MoveGameObjectToScene(eventSystemGo, scene);
        }

        private static void RegisterInBuildSettings(string scenePath, string afterSceneName)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Any(entry => entry.path == scenePath))
                return;

            int insertIndex = scenes.Count;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (Path.GetFileNameWithoutExtension(scenes[i].path) == afterSceneName)
                {
                    insertIndex = i + 1;
                    break;
                }
            }

            scenes.Insert(insertIndex, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void PositionQuickMatchButton(Button localButton, Button onlineButton, Button quickButton)
        {
            if (localButton == null || quickButton == null)
                return;

            quickButton.transform.SetSiblingIndex(localButton.transform.GetSiblingIndex() + 1);

            var layoutGroup = localButton.transform.parent != null ? localButton.transform.parent.GetComponent<VerticalLayoutGroup>() : null;
            if (layoutGroup != null)
                return;

            var localRect = localButton.GetComponent<RectTransform>();
            var onlineRect = onlineButton != null ? onlineButton.GetComponent<RectTransform>() : null;
            var quickRect = quickButton.GetComponent<RectTransform>();
            if (localRect == null || quickRect == null)
                return;

            quickRect.anchorMin = localRect.anchorMin;
            quickRect.anchorMax = localRect.anchorMax;
            quickRect.pivot = localRect.pivot;
            quickRect.sizeDelta = localRect.sizeDelta;
            quickRect.localScale = localRect.localScale;
            quickRect.rotation = localRect.rotation;

            quickRect.anchoredPosition = onlineRect != null
                ? Vector2.Lerp(localRect.anchoredPosition, onlineRect.anchoredPosition, 0.5f)
                : localRect.anchoredPosition + new Vector2(0f, -72f);
        }

        private static void ApplyButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                tmpText.text = label;
                return;
            }

            Text legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null)
                legacyText.text = label;
        }

        private static GameObject CreateCard(string name, Transform parent, string title)
        {
            var card = CreateUiRect(name, parent, stretch: false);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = CardColor;
            ApplyOutline(card, OutlineColor, 1f);

            var titleText = CreateText($"{name}_Title", card.transform, title, 18f, FontStyles.Bold, TextAlignmentOptions.Left, TextPrimary);
            SetAnchors(titleText.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0f, -26f), new Vector2(-44f, 26f));
            titleText.margin = new Vector4(20f, 0f, 0f, 0f);
            return card;
        }

        private static GameObject CreateUiRect(string name, Transform parent, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = Vector2.zero;
            }

            return go;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var textComponent = go.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.enableWordWrapping = true;
            textComponent.raycastTarget = false;
            textComponent.font = LoadThemeFont();
            return textComponent;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, float fontSize, bool darkText = false)
        {
            var buttonGo = CreateUiRect(name, parent, stretch: false);
            var image = buttonGo.GetComponent<Image>();
            image.color = backgroundColor;
            ApplyOutline(buttonGo, OutlineColor, 1f);

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText($"{name}_Text", buttonGo.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, darkText ? Color.black : TextPrimary);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void ApplyOutline(GameObject target, Color color, float thickness)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
        }

        private static TMP_FontAsset LoadThemeFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontAssetPath);
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void AssignReference(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void AssignSpriteList(SerializedObject so, string propertyName, IReadOnlyList<Sprite> sprites)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                return;

            property.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void AssignStringList(SerializedObject so, string propertyName, IReadOnlyList<string> values)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                return;

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static ArenaPresentationData LoadArenaPresentationData()
        {
            var arenaPresentation = new ArenaPresentationData();

            if (!File.Exists(SetupScenePath))
                return arenaPresentation;

            Scene setupScene = EditorSceneManager.OpenScene(SetupScenePath, OpenSceneMode.Additive);
            try
            {
                ArenaSelectUI arenaUi = Object.FindObjectsByType<ArenaSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate.gameObject.scene == setupScene);
                if (arenaUi == null)
                    return arenaPresentation;

                SerializedObject arenaUiSo = new SerializedObject(arenaUi);
                ReadStringList(arenaUiSo.FindProperty("availableArenas"), arenaPresentation.Names);
                ReadSpriteList(arenaUiSo.FindProperty("arenaBackgrounds"), arenaPresentation.Backgrounds);
            }
            finally
            {
                EditorSceneManager.CloseScene(setupScene, true);
            }

            return arenaPresentation;
        }

        private static void ReadStringList(SerializedProperty property, ICollection<string> destination)
        {
            if (property == null || !property.isArray)
                return;

            for (int i = 0; i < property.arraySize; i++)
            {
                string value = property.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrWhiteSpace(value))
                    destination.Add(value);
            }
        }

        private static void ReadSpriteList(SerializedProperty property, ICollection<Sprite> destination)
        {
            if (property == null || !property.isArray)
                return;

            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is Sprite sprite)
                    destination.Add(sprite);
            }
        }
    }
}
