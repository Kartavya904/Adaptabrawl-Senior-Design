using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Sets this GameObject's <see cref="Image"/> sprite from <see cref="LobbyContext.lastArenaImage"/>.
    /// The backdrop should live on a <b>Screen Space - Camera</b> canvas (see <c>ArenaBackdropCanvas</c> in GameScene)
    /// so it sits behind 3D fighters; a full-screen image on an Overlay canvas would hide the match.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GameArenaBackground : MonoBehaviour
    {
        [Header("Target (optional — defaults to Image on this object)")]
        [SerializeField] private Image backgroundImage;

        [Header("Fallback only (if lobby has no stored sprite)")]
        [SerializeField] private List<Sprite> arenaBackgroundSprites = new List<Sprite>();

        [Tooltip("Shown when there is no sprite from lobby or fallback list.")]
        [SerializeField] private Color fallbackColor = new Color(0.12f, 0.12f, 0.18f, 1f);

        private void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
        }

        private void Start()
        {
            ApplyFromLobby();
        }

        /// <summary>Re-reads lobby (e.g. after rematch if you call this from elsewhere).</summary>
        public void ApplyFromLobby()
        {
            if (backgroundImage == null) return;

            var lobby = LobbyContext.Instance;
            Sprite s = lobby != null ? lobby.lastArenaImage : null;

            if (s == null && lobby != null && arenaBackgroundSprites != null && arenaBackgroundSprites.Count > 0)
            {
                int idx = lobby.lastArenaIndex;
                int clamped = Mathf.Clamp(idx, 0, arenaBackgroundSprites.Count - 1);
                s = arenaBackgroundSprites[clamped];
            }

            if (s != null)
            {
                backgroundImage.sprite = s;
                backgroundImage.color = Color.white;
                backgroundImage.enabled = true;
            }
            else
            {
                backgroundImage.sprite = null;
                backgroundImage.color = fallbackColor;
                backgroundImage.enabled = true;
            }

            // Full-screen backdrop must not eat UI raycasts or sit above 3D fighters (handled by ArenaBackdropCanvas).
            backgroundImage.raycastTarget = false;
        }
    }
}
