using UnityEngine;

namespace Adaptabrawl.Data
{
    /// <summary>
    /// Pre-configured hitbox templates for different attack types.
    /// Used by the Move Library Generator to automatically create appropriate hitboxes.
    /// </summary>
    public static class HitboxPresets
    {
        // Quick jab/punch attacks
        public static HitboxDefinition[] GetQuickAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Primary",
                    offset = new Vector2(0.8f, 0f),
                    size = new Vector2(0.9f, 0.7f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    knockbackMultiplier = 1f,
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Medium strength attacks
        public static HitboxDefinition[] GetMediumAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Primary",
                    offset = new Vector2(1f, 0.2f),
                    size = new Vector2(1.1f, 0.9f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    knockbackMultiplier = 1f,
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Heavy finisher attacks
        public static HitboxDefinition[] GetHeavyAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Sweetspot",
                    offset = new Vector2(1.5f, 0.3f),
                    size = new Vector2(0.6f, 0.6f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1.5f,
                    knockbackMultiplier = 1.4f,
                    isSweetspot = true,
                    gizmoColor = new Color(1f, 0.8f, 0f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Standard",
                    offset = new Vector2(1f, 0.1f),
                    size = new Vector2(1.3f, 1.1f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    knockbackMultiplier = 1f,
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Aerial attacks (downward angle)
        public static HitboxDefinition[] GetAerialAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Primary",
                    offset = new Vector2(0.7f, -0.5f),
                    size = new Vector2(1f, 1.2f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1.1f,
                    knockbackMultiplier = 1.2f,
                    knockbackDirectionOverride = new Vector2(0.5f, -1f),
                    gizmoColor = new Color(0.5f, 0.5f, 1f, 0.3f)
                }
            };
        }
        
        // Uppercut/launcher attacks
        public static HitboxDefinition[] GetLauncherHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Ground",
                    offset = new Vector2(0.7f, 0f),
                    size = new Vector2(0.9f, 1f),
                    activeStartFrame = 0,
                    activeEndFrame = 5,
                    damageMultiplier = 1f,
                    knockbackDirectionOverride = new Vector2(0.5f, 1.5f),
                    gizmoColor = new Color(0f, 1f, 1f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Upper",
                    offset = new Vector2(0.8f, 1.5f),
                    size = new Vector2(1f, 0.8f),
                    activeStartFrame = 3,
                    activeEndFrame = -1,
                    damageMultiplier = 1.2f,
                    knockbackDirectionOverride = new Vector2(0.3f, 2f),
                    gizmoColor = new Color(0f, 1f, 1f, 0.3f)
                }
            };
        }
        
        // Spinning/AOE attacks
        public static HitboxDefinition[] GetSpinAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Front",
                    offset = new Vector2(1.2f, 0f),
                    size = new Vector2(0.8f, 1.2f),
                    activeStartFrame = 0,
                    activeEndFrame = 3,
                    damageMultiplier = 1f,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Side",
                    offset = new Vector2(0f, 1.2f),
                    size = new Vector2(1.2f, 0.8f),
                    activeStartFrame = 4,
                    activeEndFrame = 7,
                    damageMultiplier = 1f,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Back",
                    offset = new Vector2(-1.2f, 0f),
                    size = new Vector2(0.8f, 1.2f),
                    activeStartFrame = 8,
                    activeEndFrame = 11,
                    damageMultiplier = 1.1f,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Side2",
                    offset = new Vector2(0f, -1.2f),
                    size = new Vector2(1.2f, 0.8f),
                    activeStartFrame = 12,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                }
            };
        }
        
        // Push attacks (high knockback, low damage)
        public static HitboxDefinition[] GetPushAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Push",
                    offset = new Vector2(1f, 0f),
                    size = new Vector2(1.2f, 1.4f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 0.8f,
                    knockbackMultiplier = 2f,
                    gizmoColor = new Color(0.5f, 0f, 1f, 0.3f)
                }
            };
        }
        
        // Stun attacks (lower damage, causes stun)
        public static HitboxDefinition[] GetStunAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Stun",
                    offset = new Vector2(0.9f, 0.5f),
                    size = new Vector2(1f, 0.8f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 0.7f,
                    knockbackMultiplier = 0.3f,
                    gizmoColor = new Color(1f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Dash/charge attacks (elongated forward)
        public static HitboxDefinition[] GetDashAttackHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Primary",
                    offset = new Vector2(1.5f, 0f),
                    size = new Vector2(1.8f, 0.9f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1.2f,
                    knockbackMultiplier = 1.3f,
                    gizmoColor = new Color(1f, 0f, 0f, 0.3f)
                }
            };
        }
        
        // Wide hammer/claymore swings
        public static HitboxDefinition[] GetWideSwingHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Sweetspot",
                    offset = new Vector2(1.8f, 0.4f),
                    size = new Vector2(0.7f, 0.7f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1.6f,
                    knockbackMultiplier = 1.5f,
                    isSweetspot = true,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Standard",
                    offset = new Vector2(1.2f, 0.2f),
                    size = new Vector2(1.5f, 1.5f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    knockbackMultiplier = 1f,
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Precise rapier/spear thrusts
        public static HitboxDefinition[] GetThrustHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Tip",
                    offset = new Vector2(1.8f, 0f),
                    size = new Vector2(0.4f, 0.4f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1.8f,
                    knockbackMultiplier = 1.2f,
                    isSweetspot = true,
                    gizmoColor = new Color(1f, 0f, 0.5f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Shaft",
                    offset = new Vector2(1.2f, 0f),
                    size = new Vector2(0.6f, 0.6f),
                    activeStartFrame = 0,
                    activeEndFrame = -1,
                    damageMultiplier = 1f,
                    knockbackMultiplier = 0.8f,
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f)
                }
            };
        }
        
        // Multi-hit rapid attacks
        public static HitboxDefinition[] GetMultiHitHitboxes()
        {
            return new HitboxDefinition[]
            {
                new HitboxDefinition
                {
                    name = "Hit1",
                    offset = new Vector2(0.8f, 0.5f),
                    size = new Vector2(0.7f, 0.6f),
                    activeStartFrame = 0,
                    activeEndFrame = 2,
                    damageMultiplier = 0.4f,
                    knockbackMultiplier = 0.2f,
                    gizmoColor = new Color(0f, 1f, 1f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Hit2",
                    offset = new Vector2(0.9f, 0f),
                    size = new Vector2(0.8f, 0.7f),
                    activeStartFrame = 3,
                    activeEndFrame = 5,
                    damageMultiplier = 0.4f,
                    knockbackMultiplier = 0.2f,
                    gizmoColor = new Color(0f, 1f, 1f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Hit3",
                    offset = new Vector2(1f, -0.3f),
                    size = new Vector2(0.9f, 0.8f),
                    activeStartFrame = 6,
                    activeEndFrame = 8,
                    damageMultiplier = 0.5f,
                    knockbackMultiplier = 0.3f,
                    gizmoColor = new Color(0f, 1f, 1f, 0.3f)
                },
                new HitboxDefinition
                {
                    name = "Finisher",
                    offset = new Vector2(1.2f, 0.2f),
                    size = new Vector2(1.1f, 1f),
                    activeStartFrame = 9,
                    activeEndFrame = -1,
                    damageMultiplier = 0.8f,
                    knockbackMultiplier = 1.5f,
                    gizmoColor = new Color(1f, 0.5f, 0f, 0.3f)
                }
            };
        }
    }
}

