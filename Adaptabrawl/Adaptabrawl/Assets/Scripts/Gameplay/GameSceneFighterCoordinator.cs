using UnityEngine;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Local GameScene-only coordinator for grounded body blocking.
    /// Prevents grounded horizontal crossover while still allowing jump-overs.
    /// </summary>
    public class GameSceneFighterCoordinator : MonoBehaviour
    {
        [Header("Spacing")]
        [SerializeField] private float minimumGroundSpacing = 1.1f;
        [SerializeField] private float airborneVerticalTolerance = 0.3f;
        [SerializeField] private float orderingDeadZone = 0.05f;

        private FighterController player1;
        private FighterController player2;
        private FighterController lastKnownLeftFighter;

        public void Initialize(FighterController p1, FighterController p2)
        {
            player1 = p1;
            player2 = p2;
            lastKnownLeftFighter = null;

            player1?.RegisterSceneCoordinator(this);
            player2?.RegisterSceneCoordinator(this);
        }

        private void LateUpdate()
        {
            if (!ShouldCoordinate())
                return;

            if (!TryGetOrderedFighters(out FighterController left, out FighterController right))
                return;

            if (ShouldBlockGroundCrossover())
                EnforceMinimumSpacing(left, right);
        }

        public float FilterHorizontalIntent(FighterController fighter, float desiredX)
        {
            if (fighter == null || Mathf.Abs(desiredX) < 0.01f)
                return desiredX;

            if (!ShouldBlockGroundCrossover())
                return desiredX;

            if (!TryGetOrderedFighters(out FighterController left, out FighterController right))
                return desiredX;

            float spacing = GetHorizontalSpacing(left, right);
            if (spacing > minimumGroundSpacing)
                return desiredX;

            if (fighter == left && desiredX > 0f)
                return 0f;

            if (fighter == right && desiredX < 0f)
                return 0f;

            return desiredX;
        }

        public void RefreshFacing()
        {
            // Phase 1 auto-facing is intentionally disabled for now.
        }

        private bool ShouldCoordinate()
        {
            return player1 != null
                && player2 != null
                && !player1.IsDead
                && !player2.IsDead
                && !player1.IsInputLocked
                && !player2.IsInputLocked;
        }

        private bool ShouldBlockGroundCrossover()
        {
            if (!ShouldCoordinate())
                return false;

            if (IsCollisionBypassed(player1) || IsCollisionBypassed(player2))
                return false;

            var p1Controller = player1.GetPlayerController();
            var p2Controller = player2.GetPlayerController();
            if (p1Controller == null || p2Controller == null)
                return false;

            if (p1Controller.isJump || p2Controller.isJump)
                return false;

            float verticalDelta = Mathf.Abs(player1.GetArenaPosition().y - player2.GetArenaPosition().y);
            return verticalDelta <= airborneVerticalTolerance;
        }

        private static bool IsCollisionBypassed(FighterController fighter)
        {
            if (fighter == null)
                return false;

            var evadeSystem = fighter.GetComponent<Adaptabrawl.Evade.EvadeSystem>();
            if (evadeSystem != null && evadeSystem.IsDodging)
                return true;

            var playerController = fighter.GetPlayerController();
            return playerController != null && playerController.IsDodgeAnimationActive();
        }

        private bool TryGetOrderedFighters(out FighterController left, out FighterController right)
        {
            left = null;
            right = null;

            if (player1 == null || player2 == null)
                return false;

            float player1X = player1.GetArenaPosition().x;
            float player2X = player2.GetArenaPosition().x;
            float delta = player1X - player2X;

            if (Mathf.Abs(delta) > orderingDeadZone || lastKnownLeftFighter == null)
                lastKnownLeftFighter = delta <= 0f ? player1 : player2;

            if (lastKnownLeftFighter == player1)
            {
                left = player1;
                right = player2;
            }
            else
            {
                left = player2;
                right = player1;
            }

            return left != null && right != null;
        }

        private void EnforceMinimumSpacing(FighterController left, FighterController right)
        {
            float leftX = left.GetArenaPosition().x;
            float rightX = right.GetArenaPosition().x;
            float spacing = rightX - leftX;

            if (spacing >= minimumGroundSpacing)
                return;

            float midpoint = (leftX + rightX) * 0.5f;
            Vector3 leftPosition = left.GetArenaPosition();
            Vector3 rightPosition = right.GetArenaPosition();

            leftPosition.x = midpoint - (minimumGroundSpacing * 0.5f);
            rightPosition.x = midpoint + (minimumGroundSpacing * 0.5f);

            left.SetArenaPosition(leftPosition);
            right.SetArenaPosition(rightPosition);
        }

        private static float GetHorizontalSpacing(FighterController left, FighterController right)
        {
            return right.GetArenaPosition().x - left.GetArenaPosition().x;
        }
    }
}
