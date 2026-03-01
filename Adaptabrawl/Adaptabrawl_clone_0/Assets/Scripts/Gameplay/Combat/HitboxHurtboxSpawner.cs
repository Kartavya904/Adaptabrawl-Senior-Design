using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using System.Collections.Generic;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Automatically creates and manages hitboxes and hurtboxes for a fighter.
    /// All configurations come from FighterDef and MoveDef - editable in Inspector.
    /// </summary>
    public class HitboxHurtboxSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController fighterController;
        
        [Header("Spawned Components")]
        [SerializeField] private List<HurtboxInstance> spawnedHurtboxes = new List<HurtboxInstance>();
        [SerializeField] private List<HitboxInstance> spawnedHitboxes = new List<HitboxInstance>();
        
        [Header("Runtime")]
        [SerializeField] private Transform hurtboxParent;
        [SerializeField] private Transform hitboxParent;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showLabels = true;
        
        private void Awake()
        {
            if (fighterController == null)
                fighterController = GetComponent<FighterController>();
        }
        
        private void Start()
        {
            CreateParents();
            SpawnHurtboxes();
        }
        
        /// <summary>
        /// Creates parent GameObjects to organize hitboxes and hurtboxes
        /// </summary>
        private void CreateParents()
        {
            if (hurtboxParent == null)
            {
                GameObject hurtboxParentObj = new GameObject("Hurtboxes");
                hurtboxParentObj.transform.SetParent(transform);
                hurtboxParentObj.transform.localPosition = Vector3.zero;
                hurtboxParent = hurtboxParentObj.transform;
            }
            
            if (hitboxParent == null)
            {
                GameObject hitboxParentObj = new GameObject("Hitboxes");
                hitboxParentObj.transform.SetParent(transform);
                hitboxParentObj.transform.localPosition = Vector3.zero;
                hitboxParent = hitboxParentObj.transform;
            }
        }
        
        /// <summary>
        /// Creates all hurtboxes defined in the FighterDef
        /// </summary>
        private void SpawnHurtboxes()
        {
            if (fighterController == null || fighterController.FighterDef == null)
            {
                Debug.LogWarning($"HitboxHurtboxSpawner: No FighterDef found on {gameObject.name}");
                return;
            }
            
            var fighterDef = fighterController.FighterDef;
            
            if (fighterDef.hurtboxes == null || fighterDef.hurtboxes.Length == 0)
            {
                Debug.LogWarning($"HitboxHurtboxSpawner: No hurtboxes defined for {fighterDef.fighterName}");
                return;
            }
            
            foreach (var hurtboxDef in fighterDef.hurtboxes)
            {
                CreateHurtbox(hurtboxDef);
            }
            
            Debug.Log($"HitboxHurtboxSpawner: Created {spawnedHurtboxes.Count} hurtboxes for {fighterDef.fighterName}");
        }
        
        /// <summary>
        /// Creates a single hurtbox from a definition
        /// </summary>
        private HurtboxInstance CreateHurtbox(HurtboxDefinition definition)
        {
            GameObject hurtboxObj = new GameObject($"Hurtbox_{definition.name}");
            hurtboxObj.transform.SetParent(hurtboxParent);
            hurtboxObj.transform.localPosition = definition.offset;
            hurtboxObj.layer = LayerMask.NameToLayer("Fighter"); // Ensure proper layer
            
            // Add collider
            BoxCollider2D collider = hurtboxObj.AddComponent<BoxCollider2D>();
            collider.size = definition.size;
            collider.isTrigger = true;
            
            // Add Hurtbox component
            Hurtbox hurtbox = hurtboxObj.AddComponent<Hurtbox>();
            hurtbox.SetActive(definition.isActive);
            
            // Create instance wrapper
            HurtboxInstance instance = new HurtboxInstance
            {
                definition = definition,
                gameObject = hurtboxObj,
                collider = collider,
                hurtbox = hurtbox
            };
            
            spawnedHurtboxes.Add(instance);
            
            return instance;
        }
        
        /// <summary>
        /// Spawns hitboxes for a specific move. Called by combat system when attack starts.
        /// </summary>
        public List<HitboxInstance> SpawnHitboxesForMove(MoveDef move)
        {
            if (move == null)
            {
                Debug.LogWarning("HitboxHurtboxSpawner: Cannot spawn hitboxes for null move");
                return null;
            }
            
            // Clear any existing hitboxes
            ClearHitboxes();
            
            List<HitboxInstance> newHitboxes = new List<HitboxInstance>();
            
            // Use hitboxDefinitions if available, otherwise fall back to legacy single hitbox
            if (move.hitboxDefinitions != null && move.hitboxDefinitions.Length > 0)
            {
                foreach (var hitboxDef in move.hitboxDefinitions)
                {
                    HitboxInstance instance = CreateHitbox(hitboxDef, move);
                    newHitboxes.Add(instance);
                }
            }
            else
            {
                // Legacy support: create single hitbox from old fields
                HitboxDefinition legacyDef = new HitboxDefinition
                {
                    name = "Primary",
                    offset = move.hitboxOffset,
                    size = move.hitboxSize,
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f
                };
                
                HitboxInstance instance = CreateHitbox(legacyDef, move);
                newHitboxes.Add(instance);
            }
            
            spawnedHitboxes = newHitboxes;
            
            Debug.Log($"HitboxHurtboxSpawner: Spawned {newHitboxes.Count} hitboxes for move '{move.moveName}'");
            
            return newHitboxes;
        }
        
        /// <summary>
        /// Creates a single hitbox from a definition
        /// </summary>
        private HitboxInstance CreateHitbox(HitboxDefinition definition, MoveDef move)
        {
            GameObject hitboxObj = new GameObject($"Hitbox_{definition.name}");
            hitboxObj.transform.SetParent(hitboxParent);
            hitboxObj.transform.localPosition = definition.offset;
            hitboxObj.layer = LayerMask.NameToLayer("Hitbox"); // Ensure proper layer
            
            // Add collider
            BoxCollider2D collider = hitboxObj.AddComponent<BoxCollider2D>();
            collider.size = definition.size;
            collider.isTrigger = true;
            collider.enabled = false; // Start disabled, will be enabled during active frames
            
            // Add visual component for debugging
            HitboxVisual visual = hitboxObj.AddComponent<HitboxVisual>();
            visual.SetColor(definition.gizmoColor);
            
            // Create instance wrapper
            HitboxInstance instance = new HitboxInstance
            {
                definition = definition,
                move = move,
                gameObject = hitboxObj,
                collider = collider,
                isActive = false
            };
            
            return instance;
        }
        
        /// <summary>
        /// Clears all spawned hitboxes
        /// </summary>
        public void ClearHitboxes()
        {
            foreach (var hitbox in spawnedHitboxes)
            {
                if (hitbox.gameObject != null)
                {
                    Destroy(hitbox.gameObject);
                }
            }
            spawnedHitboxes.Clear();
        }
        
        /// <summary>
        /// Activates hitboxes based on current frame
        /// </summary>
        public void UpdateHitboxes(int currentFrame)
        {
            foreach (var hitbox in spawnedHitboxes)
            {
                int endFrame = hitbox.definition.activeEndFrame == -1 
                    ? hitbox.move.activeFrames 
                    : hitbox.definition.activeEndFrame;
                
                bool shouldBeActive = currentFrame >= hitbox.definition.activeStartFrame 
                    && currentFrame < endFrame;
                
                if (hitbox.isActive != shouldBeActive)
                {
                    hitbox.isActive = shouldBeActive;
                    hitbox.collider.enabled = shouldBeActive;
                }
            }
        }
        
        /// <summary>
        /// Gets damage multiplier from hurtbox that was hit
        /// </summary>
        public float GetHurtboxDamageMultiplier(Collider2D hitCollider)
        {
            foreach (var hurtbox in spawnedHurtboxes)
            {
                if (hurtbox.collider == hitCollider)
                {
                    return hurtbox.definition.damageMultiplier;
                }
            }
            return 1f; // Default multiplier
        }
        
        /// <summary>
        /// Enables/disables specific hurtbox by name
        /// </summary>
        public void SetHurtboxActive(string hurtboxName, bool active)
        {
            foreach (var hurtbox in spawnedHurtboxes)
            {
                if (hurtbox.definition.name == hurtboxName)
                {
                    hurtbox.hurtbox.SetActive(active);
                    hurtbox.collider.enabled = active;
                }
            }
        }
        
        /// <summary>
        /// Gets all hurtboxes
        /// </summary>
        public List<HurtboxInstance> GetHurtboxes() => spawnedHurtboxes;
        
        /// <summary>
        /// Gets all hitboxes
        /// </summary>
        public List<HitboxInstance> GetHitboxes() => spawnedHitboxes;
        
        // Gizmo drawing for editor visualization
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw hurtboxes
            if (fighterController != null && fighterController.FighterDef != null)
            {
                var fighterDef = fighterController.FighterDef;
                if (fighterDef.hurtboxes != null)
                {
                    foreach (var hurtbox in fighterDef.hurtboxes)
                    {
                        Gizmos.color = hurtbox.gizmoColor;
                        Vector3 worldPos = transform.position + (Vector3)hurtbox.offset;
                        Gizmos.DrawCube(worldPos, hurtbox.size);
                        
                        // Draw wireframe
                        Gizmos.color = new Color(hurtbox.gizmoColor.r, hurtbox.gizmoColor.g, hurtbox.gizmoColor.b, 1f);
                        Gizmos.DrawWireCube(worldPos, hurtbox.size);
                        
                        #if UNITY_EDITOR
                        if (showLabels)
                        {
                            UnityEditor.Handles.Label(worldPos, $"Hurtbox: {hurtbox.name}\n×{hurtbox.damageMultiplier:F1}");
                        }
                        #endif
                    }
                }
            }
            
            // Draw active hitboxes
            foreach (var hitbox in spawnedHitboxes)
            {
                if (hitbox.isActive)
                {
                    Gizmos.color = hitbox.definition.gizmoColor;
                    Vector3 worldPos = transform.position + (Vector3)hitbox.definition.offset;
                    Gizmos.DrawCube(worldPos, hitbox.definition.size);
                    
                    Gizmos.color = new Color(hitbox.definition.gizmoColor.r, hitbox.definition.gizmoColor.g, hitbox.definition.gizmoColor.b, 1f);
                    Gizmos.DrawWireCube(worldPos, hitbox.definition.size);
                }
            }
        }
    }
    
    /// <summary>
    /// Runtime instance of a hurtbox
    /// </summary>
    [System.Serializable]
    public class HurtboxInstance
    {
        public HurtboxDefinition definition;
        public GameObject gameObject;
        public BoxCollider2D collider;
        public Hurtbox hurtbox;
    }
    
    /// <summary>
    /// Runtime instance of a hitbox
    /// </summary>
    [System.Serializable]
    public class HitboxInstance
    {
        public HitboxDefinition definition;
        public MoveDef move;
        public GameObject gameObject;
        public BoxCollider2D collider;
        public bool isActive;
    }
    
    /// <summary>
    /// Visual representation of hitbox for debugging
    /// </summary>
    public class HitboxVisual : MonoBehaviour
    {
        [SerializeField] private Color color = Color.green;
        private SpriteRenderer spriteRenderer;
        
        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(color.r, color.g, color.b, 0.3f);
            
            // Create a simple square sprite
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
        
        public void SetColor(Color newColor)
        {
            color = newColor;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(newColor.r, newColor.g, newColor.b, 0.3f);
            }
        }
    }
}

