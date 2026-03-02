using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Adds a Game Manager (LocalGameManager + spawn points) to the current scene
    /// so you can test fighters without going through the full menu flow.
    /// </summary>
    public static class GameSceneSetupHelper
    {
        private const string MenuPath = "Adaptabrawl/Add Game Scene Manager to Scene";
        private const string Floor2DMenuPath = "Adaptabrawl/Add 2D Collider to Arena Floor (Cube)";

        [MenuItem(Floor2DMenuPath)]
        public static void Add2DColliderToArenaFloor()
        {
            EnsureArenaFloorHas2DCollider();
            EditorUtility.DisplayDialog("Arena Floor", "If the scene has a GameObject named 'Cube', a BoxCollider2D was added so 2D characters can stand on it. (3D BoxCollider and 2D physics do not interact.)", "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateAddGameSceneManager()
        {
            return !Application.isPlaying;
        }

        [MenuItem(MenuPath)]
        public static void AddGameSceneManagerToScene()
        {
            EnsureArenaFloorHas2DCollider();
            LocalGameManager existing = Object.FindFirstObjectByType<LocalGameManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                EditorUtility.DisplayDialog("Already Exists", "This scene already has a LocalGameManager. It has been selected. Assign Test Fighter P1/P2 and spawn points if needed.", "OK");
                return;
            }

            GameObject root = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(root, "Add Game Scene Manager");

            LocalGameManager manager = Undo.AddComponent<LocalGameManager>(root);

            // Match GameScene cube: top at y = -4.5 (cube at -4.75, scale.y 0.5). Spawn on platform like Player_Hammer (y = -4.47).
            const float spawnY = -4.47f;
            GameObject p1Spawn = new GameObject("Player1Spawn");
            p1Spawn.transform.SetParent(root.transform, false);
            p1Spawn.transform.localPosition = new Vector3(-5f, spawnY, 0f);
            Undo.RegisterCreatedObjectUndo(p1Spawn, "Add Player1Spawn");

            GameObject p2Spawn = new GameObject("Player2Spawn");
            p2Spawn.transform.SetParent(root.transform, false);
            p2Spawn.transform.localPosition = new Vector3(5f, spawnY, 0f);
            Undo.RegisterCreatedObjectUndo(p2Spawn, "Add Player2Spawn");

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("player1SpawnPoint").objectReferenceValue = p1Spawn.transform;
            so.FindProperty("player2SpawnPoint").objectReferenceValue = p2Spawn.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            if (!Application.isPlaying)
                MarkSceneDirty();

            EditorUtility.DisplayDialog("Game Manager Added",
                "A 'GameManager' object with LocalGameManager and spawn points was added to the scene.\n\n" +
                "To test Sharp Tooth (or any fighter):\n" +
                "1. Select the GameManager object.\n" +
                "2. In the Inspector, assign your fighter to Test Fighter P1 (and optionally Test Fighter P2).\n" +
                "3. Press Play.",
                "OK");
        }

        private static void MarkSceneDirty()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>
        /// The arena floor (Cube) uses a 3D BoxCollider. Characters use Rigidbody2D/Collider2D,
        /// so 2D and 3D physics do not interact. Unity does not allow BoxCollider2D on the same
        /// GameObject as BoxCollider, so we add a child "Floor2D" with BoxCollider2D.
        /// </summary>
        public static void EnsureArenaFloorHas2DCollider()
        {
            GameObject cube = GameObject.Find("Cube");
            if (cube == null)
            {
                Debug.LogWarning("GameSceneSetupHelper: No GameObject named 'Cube' found. Add a BoxCollider2D to your floor manually so 2D characters can collide with it.");
                return;
            }
            Transform existingChild = cube.transform.Find("Floor2D");
            if (existingChild != null)
            {
                if (existingChild.GetComponent<BoxCollider2D>() != null)
                {
                    Debug.Log("GameSceneSetupHelper: Floor2D child with BoxCollider2D already exists under Cube.");
                    return;
                }
            }
            GameObject floor2D;
            if (existingChild != null)
            {
                floor2D = existingChild.gameObject;
            }
            else
            {
                floor2D = new GameObject("Floor2D");
                floor2D.transform.SetParent(cube.transform, false);
                floor2D.transform.localPosition = Vector3.zero;
                floor2D.transform.localRotation = Quaternion.identity;
                floor2D.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(floor2D, "Create Floor2D");
            }
            BoxCollider2D c2d = floor2D.GetComponent<BoxCollider2D>();
            if (c2d == null)
                c2d = Undo.AddComponent<BoxCollider2D>(floor2D);
            if (c2d != null)
            {
                c2d.size = Vector2.one;
                c2d.isTrigger = false;
                c2d.usedByComposite = false;
            }
            MarkSceneDirty();
            Debug.Log("GameSceneSetupHelper: Added BoxCollider2D on child 'Floor2D' under Cube so 2D characters can stand on the arena floor.");
        }
    }
}
