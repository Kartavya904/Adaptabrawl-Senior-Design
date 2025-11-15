using UnityEngine;

namespace Adaptabrawl.Combat
{
    public class Hurtbox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController owner;
        
        [Header("Properties")]
        [SerializeField] private bool isActive = true;
        
        public FighterController Owner
        {
            get
            {
                if (owner == null)
                    owner = GetComponentInParent<FighterController>();
                return owner;
            }
        }
        
        public bool IsActive => isActive;
        
        public void SetActive(bool active)
        {
            isActive = active;
        }
    }
}

