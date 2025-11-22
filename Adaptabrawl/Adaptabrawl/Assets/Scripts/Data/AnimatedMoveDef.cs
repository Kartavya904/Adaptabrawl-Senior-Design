using UnityEngine;

namespace Adaptabrawl.Data
{
    /// <summary>
    /// Extended MoveDef that connects to Shinabro animations.
    /// Automatically syncs with animation length and triggers hitboxes during animation.
    /// </summary>
    [CreateAssetMenu(fileName = "New Animated Move", menuName = "Adaptabrawl/Animated Move Definition")]
    public class AnimatedMoveDef : MoveDef
    {
        [Header("Animation Integration")]
        [Tooltip("The animation clip from Shinabro (e.g., Stander@Fighter_Attack1)")]
        public AnimationClip animationClip;
        
        [Tooltip("Animation parameter name (e.g., 'Attack1', 'Skill1')")]
        public string animatorTrigger = "Attack1";
        
        [Tooltip("Parameter type (Trigger, Bool, Int)")]
        public AnimatorParameterType parameterType = AnimatorParameterType.Trigger;
        
        [Header("Auto-Sync Settings")]
        [Tooltip("Automatically calculate frame data from animation length")]
        public bool autoCalculateFrames = true;
        
        [Tooltip("When in animation hitboxes activate (0-1, e.g., 0.3 = 30% through)")]
        [Range(0f, 1f)]
        public float hitboxActivationTime = 0.3f;
        
        [Tooltip("How long hitboxes stay active (0-1, e.g., 0.2 = 20% of animation)")]
        [Range(0f, 1f)]
        public float hitboxDuration = 0.2f;
        
        [Tooltip("Recovery time after animation (0-1, e.g., 0.3 = 30% of animation)")]
        [Range(0f, 1f)]
        public float recoveryPercentage = 0.3f;
        
        [Header("Animation Events")]
        [Tooltip("Use Unity Animation Events instead of frame calculation")]
        public bool useAnimationEvents = false;
        
        [Header("Combo System")]
        [Tooltip("Can this move chain into another?")]
        public bool canCombo = false;
        
        [Tooltip("What move can this chain into?")]
        public AnimatedMoveDef nextComboMove;
        
        [Tooltip("Time window to input next combo (seconds)")]
        public float comboWindow = 0.5f;
        
        [Header("Movement During Animation")]
        [Tooltip("Does character move forward during this animation?")]
        public bool hasForwardMovement = false;
        
        [Tooltip("Movement speed multiplier during animation")]
        [Range(0f, 2f)]
        public float movementSpeed = 1f;
        
        [Tooltip("Movement curve over animation time")]
        public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        /// <summary>
        /// Calculates frame data from animation clip length
        /// </summary>
        public void CalculateFrameData()
        {
            if (animationClip == null || !autoCalculateFrames) return;
            
            float animLength = animationClip.length;
            int totalAnimFrames = Mathf.RoundToInt(animLength * 60f); // 60 FPS
            
            // Calculate startup (time before hitbox)
            startupFrames = Mathf.RoundToInt(totalAnimFrames * hitboxActivationTime);
            
            // Calculate active frames (hitbox duration)
            activeFrames = Mathf.RoundToInt(totalAnimFrames * hitboxDuration);
            
            // Calculate recovery
            recoveryFrames = Mathf.RoundToInt(totalAnimFrames * recoveryPercentage);
            
            Debug.Log($"Auto-calculated frames for {moveName}: Startup={startupFrames}, Active={activeFrames}, Recovery={recoveryFrames}, Total={totalFrames}");
        }
        
        /// <summary>
        /// Gets animation length in frames
        /// </summary>
        public int GetAnimationLengthFrames()
        {
            if (animationClip == null) return totalFrames;
            return Mathf.RoundToInt(animationClip.length * 60f);
        }
        
        /// <summary>
        /// Gets animation length in seconds
        /// </summary>
        public float GetAnimationLength()
        {
            if (animationClip == null) return totalFrames / 60f;
            return animationClip.length;
        }
        
        private void OnValidate()
        {
            if (autoCalculateFrames && animationClip != null)
            {
                CalculateFrameData();
            }
        }
    }
    
    public enum AnimatorParameterType
    {
        Trigger,
        Bool,
        Int,
        Float
    }
}

