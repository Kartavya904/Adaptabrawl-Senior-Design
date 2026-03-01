using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Complete match setup wizard - creates entire playable match scene with one click!
    /// </summary>
    public class MatchSetupWizard : EditorWindow
    {
        private FighterDef player1Fighter;
        private FighterDef player2Fighter;
        private Vector2 scrollPosition;
        
        [MenuItem("Adaptabrawl/Match Setup Wizard")]
        public static void ShowWindow()
        {
            MatchSetupWizard window = GetWindow<MatchSetupWizard>("Match Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 20;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Match Setup Wizard", titleStyle);
            EditorGUILayout.Space(10);
            
            // Description
            EditorGUILayout.HelpBox(
                "🎮 Complete Match Setup in One Click!\n\n" +
                "This wizard will automatically:\n" +
                "✓ Create/configure the current scene\n" +
                "✓ Add ground and spawn points\n" +
                "✓ Set up both fighters\n" +
                "✓ Configure 2-player input\n" +
                "✓ Create health bars and UI\n" +
                "✓ Set up win conditions\n" +
                "✓ Configure camera\n\n" +
                "Just select your fighters and click Setup!",
                MessageType.Info
            );
            
            EditorGUILayout.Space(15);
            
            // Fighter Selection
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Select Fighters", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            player1Fighter = (FighterDef)EditorGUILayout.ObjectField(
                "Player 1 Fighter", 
                player1Fighter, 
                typeof(FighterDef), 
                false
            );
            
            if (player1Fighter != null)
            {
                EditorGUILayout.HelpBox(
                    $"✓ {player1Fighter.fighterName}\n" +
                    $"HP: {player1Fighter.maxHealth} | Speed: {player1Fighter.moveSpeed}",
                    MessageType.None
                );
            }
            
            EditorGUILayout.Space(5);
            
            player2Fighter = (FighterDef)EditorGUILayout.ObjectField(
                "Player 2 Fighter", 
                player2Fighter, 
                typeof(FighterDef), 
                false
            );
            
            if (player2Fighter != null)
            {
                EditorGUILayout.HelpBox(
                    $"✓ {player2Fighter.fighterName}\n" +
                    $"HP: {player2Fighter.maxHealth} | Speed: {player2Fighter.moveSpeed}",
                    MessageType.None
                );
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(15);
            
            // Setup Button
            EditorGUI.BeginDisabledGroup(player1Fighter == null || player2Fighter == null);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            buttonStyle.fixedHeight = 60;
            
            if (GUILayout.Button("🎮 Setup Complete Match!", buttonStyle))
            {
                SetupMatch();
            }
            
            EditorGUI.EndDisabledGroup();
            
            if (player1Fighter == null || player2Fighter == null)
            {
                EditorGUILayout.HelpBox("Please select both fighters to continue", MessageType.Warning);
            }
            
            EditorGUILayout.Space(15);
            
            // What will be created
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("What Will Be Created:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scene Objects:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Ground platform", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Player 1 spawn point", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Player 2 spawn point", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Game Manager (with all components)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Configured Main Camera", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("UI Elements:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Health bars for both players", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Player name displays", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Win/lose panel", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Scripts Added:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • FighterSpawner", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • TwoPlayerInputHandler", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • MatchManager", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Instructions
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("After Setup:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Press Play button", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. Fighters will spawn automatically", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. Use controls to play:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("   P1: F/G (attacks), WASD (move)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("   P2: NumPad1/2 (attacks), Arrows (move)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void SetupMatch()
        {
            if (player1Fighter == null || player2Fighter == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both fighters!", "OK");
                return;
            }
            
            if (!EditorUtility.DisplayDialog(
                "Setup Match", 
                "This will configure the current scene for a match. Continue?", 
                "Yes", 
                "Cancel"))
            {
                return;
            }
            
            EditorUtility.DisplayProgressBar("Match Setup", "Setting up match...", 0f);
            
            try
            {
                // Step 1: Create ground
                EditorUtility.DisplayProgressBar("Match Setup", "Creating ground...", 0.1f);
                CreateGround();
                
                // Step 2: Create spawn points
                EditorUtility.DisplayProgressBar("Match Setup", "Creating spawn points...", 0.2f);
                Transform p1Spawn = CreateSpawnPoint("Player1SpawnPoint", new Vector3(-3, 0, 0));
                Transform p2Spawn = CreateSpawnPoint("Player2SpawnPoint", new Vector3(3, 0, 0));
                
                // Step 3: Create/configure camera
                EditorUtility.DisplayProgressBar("Match Setup", "Configuring camera...", 0.3f);
                ConfigureCamera();
                
                // Step 4: Create UI
                EditorUtility.DisplayProgressBar("Match Setup", "Creating UI...", 0.4f);
                var uiElements = CreateMatchUI();
                
                // Step 5: Create Game Manager
                EditorUtility.DisplayProgressBar("Match Setup", "Creating Game Manager...", 0.6f);
                GameObject gameManager = CreateGameManager(p1Spawn, p2Spawn, uiElements);
                
                // Step 6: Configure layers
                EditorUtility.DisplayProgressBar("Match Setup", "Configuring layers...", 0.8f);
                ConfigureLayers();
                
                // Step 7: Save scene
                EditorUtility.DisplayProgressBar("Match Setup", "Saving scene...", 0.9f);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                
                EditorUtility.ClearProgressBar();
                
                // Success!
                EditorUtility.DisplayDialog(
                    "Match Setup Complete!", 
                    $"✓ Match setup successful!\n\n" +
                    $"Player 1: {player1Fighter.fighterName}\n" +
                    $"Player 2: {player2Fighter.fighterName}\n\n" +
                    $"Press the Play button to start your match!\n\n" +
                    $"Controls:\n" +
                    $"P1: F/G for attacks\n" +
                    $"P2: NumPad 1/2 for attacks",
                    "Play Now!"
                );
                
                // Optionally enter play mode
                if (EditorUtility.DisplayDialog("Play Match?", "Start playing the match now?", "Yes!", "Not Yet"))
                {
                    EditorApplication.isPlaying = true;
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Setup failed:\n{e.Message}", "OK");
                Debug.LogError($"Match Setup Error: {e}");
            }
        }
        
        private void CreateGround()
        {
            // Check if ground already exists
            GameObject existingGround = GameObject.Find("Ground");
            if (existingGround != null)
            {
                Debug.Log("Ground already exists, skipping creation");
                return;
            }
            
            // Create ground sprite
            GameObject ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0, -3, 0);
            ground.transform.localScale = new Vector3(20, 1, 1);
            ground.layer = LayerMask.NameToLayer("Default");
            
            // Add sprite renderer
            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sr.color = new Color(0.3f, 0.3f, 0.3f);
            
            // Add collider
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }
        
        private Transform CreateSpawnPoint(string name, Vector3 position)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                existing.transform.position = position;
                return existing.transform;
            }
            
            GameObject spawnPoint = new GameObject(name);
            spawnPoint.transform.position = position;
            
            Undo.RegisterCreatedObjectUndo(spawnPoint, $"Create {name}");
            return spawnPoint.transform;
        }
        
        private void ConfigureCamera()
        {
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }
            
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5;
            mainCam.backgroundColor = Color.black;
            
            Undo.RecordObject(mainCam, "Configure Camera");
        }
        
        private UIElements CreateMatchUI()
        {
            UIElements ui = new UIElements();
            
            // Create or find canvas
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MatchUI");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
                
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }
            
            // Create P1 health panel
            ui.p1HealthPanel = CreateHealthPanel(canvas.transform, "Player1HealthPanel", true);
            ui.p1HealthFill = CreateHealthFill(ui.p1HealthPanel.transform);
            ui.p1NameText = CreateNameText(ui.p1HealthPanel.transform, player1Fighter.fighterName);
            
            // Create P2 health panel
            ui.p2HealthPanel = CreateHealthPanel(canvas.transform, "Player2HealthPanel", false);
            ui.p2HealthFill = CreateHealthFill(ui.p2HealthPanel.transform);
            ui.p2NameText = CreateNameText(ui.p2HealthPanel.transform, player2Fighter.fighterName);
            
            // Create win panel
            ui.winPanel = CreateWinPanel(canvas.transform);
            ui.winText = CreateWinText(ui.winPanel.transform);
            
            return ui;
        }
        
        private GameObject CreateHealthPanel(Transform parent, string name, bool isPlayer1)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            
            if (isPlayer1)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(50, -50);
            }
            else
            {
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-50, -50);
            }
            
            rt.sizeDelta = new Vector2(400, 40);
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.3f, 0, 0, 1);
            
            Undo.RegisterCreatedObjectUndo(panel, $"Create {name}");
            return panel;
        }
        
        private Image CreateHealthFill(Transform parent)
        {
            GameObject fill = new GameObject("HealthFill");
            fill.transform.SetParent(parent, false);
            
            RectTransform rt = fill.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            Image image = fill.AddComponent<Image>();
            image.color = Color.green;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            
            Undo.RegisterCreatedObjectUndo(fill, "Create Health Fill");
            return image;
        }
        
        private TextMeshProUGUI CreateNameText(Transform parent, string playerName)
        {
            GameObject textObj = new GameObject("PlayerName");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = playerName;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            Undo.RegisterCreatedObjectUndo(textObj, "Create Name Text");
            return text;
        }
        
        private GameObject CreateWinPanel(Transform parent)
        {
            GameObject panel = new GameObject("WinPanel");
            panel.transform.SetParent(parent, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600, 300);
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            
            panel.SetActive(false);
            
            Undo.RegisterCreatedObjectUndo(panel, "Create Win Panel");
            return panel;
        }
        
        private TextMeshProUGUI CreateWinText(Transform parent)
        {
            GameObject textObj = new GameObject("WinText");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Player 1 Wins!";
            text.fontSize = 72;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.8f, 0f); // Gold
            
            Undo.RegisterCreatedObjectUndo(textObj, "Create Win Text");
            return text;
        }
        
        private GameObject CreateGameManager(Transform p1Spawn, Transform p2Spawn, UIElements ui)
        {
            GameObject existing = GameObject.Find("GameManager");
            if (existing != null)
            {
                Undo.RecordObject(existing, "Update GameManager");
            }
            else
            {
                existing = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(existing, "Create GameManager");
            }
            
            // Add FighterSpawner
            FighterSpawner spawner = existing.GetComponent<FighterSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<FighterSpawner>(existing);
            }
            
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("player1Fighter").objectReferenceValue = player1Fighter;
            spawnerSO.FindProperty("player2Fighter").objectReferenceValue = player2Fighter;
            spawnerSO.FindProperty("player1SpawnPoint").objectReferenceValue = p1Spawn;
            spawnerSO.FindProperty("player2SpawnPoint").objectReferenceValue = p2Spawn;
            spawnerSO.ApplyModifiedProperties();
            
            // Add TwoPlayerInputHandler
            TwoPlayerInputHandler inputHandler = existing.GetComponent<TwoPlayerInputHandler>();
            if (inputHandler == null)
            {
                inputHandler = Undo.AddComponent<TwoPlayerInputHandler>(existing);
            }
            
            SerializedObject inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("spawner").objectReferenceValue = spawner;
            inputSO.ApplyModifiedProperties();
            
            // Add MatchManager
            MatchManager matchManager = existing.GetComponent<MatchManager>();
            if (matchManager == null)
            {
                matchManager = Undo.AddComponent<MatchManager>(existing);
            }
            
            SerializedObject matchSO = new SerializedObject(matchManager);
            matchSO.FindProperty("spawner").objectReferenceValue = spawner;
            matchSO.FindProperty("player1HealthFill").objectReferenceValue = ui.p1HealthFill;
            matchSO.FindProperty("player2HealthFill").objectReferenceValue = ui.p2HealthFill;
            matchSO.FindProperty("player1NameText").objectReferenceValue = ui.p1NameText;
            matchSO.FindProperty("player2NameText").objectReferenceValue = ui.p2NameText;
            matchSO.FindProperty("winPanel").objectReferenceValue = ui.winPanel;
            matchSO.FindProperty("winText").objectReferenceValue = ui.winText;
            matchSO.ApplyModifiedProperties();
            
            return existing;
        }
        
        private void ConfigureLayers()
        {
            // Note: Layers must be manually added in Project Settings
            // This is just informational
            Debug.Log("Match Setup: Remember to configure layers in Edit → Project Settings → Tags and Layers");
            Debug.Log("Recommended layers: Fighter (8), Hitbox (9), Ground (10)");
        }
        
        private class UIElements
        {
            public GameObject p1HealthPanel;
            public GameObject p2HealthPanel;
            public Image p1HealthFill;
            public Image p2HealthFill;
            public TextMeshProUGUI p1NameText;
            public TextMeshProUGUI p2NameText;
            public GameObject winPanel;
            public TextMeshProUGUI winText;
        }
    }
}

