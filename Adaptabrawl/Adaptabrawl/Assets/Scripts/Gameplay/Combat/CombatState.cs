namespace Adaptabrawl.Combat
{
    public enum CombatState
    {
        Idle,
        Startup, // Windup before attack
        Active, // Hitbox is active
        Recovery, // Cooldown after attack
        Blocking,
        Parrying,
        Dodging,
        Stunned, // Hitstun/blockstun
        Staggered,
        ArmorBroken
    }
}

