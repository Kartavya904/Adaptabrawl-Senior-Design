using System;
using System.Collections.Generic;
using UnityEngine;
using Adaptabrawl.UI;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Per-run match data for the active game scene and a rolling history of finished matches.
    /// Complements <see cref="LobbyContext"/> (pre-game) with in-game and post-game facts.
    /// </summary>
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        public const int MaxMatchHistory = 64;

        [Header("Current match (updated when local game starts)")]
        public string arenaDisplayName = "";
        public int arenaIndex;
        public bool isLocalMatch;
        public string p1FighterName = "";
        public string p2FighterName = "";
        public string p1DisplayName = "";
        public string p2DisplayName = "";
        public float matchStartedRealtime;
        public readonly List<int> roundWinnerCodes = new List<int>();

        [Tooltip("Newest match first. Code: 1 = P1 won round, 2 = P2, 0 = draw.")]
        public readonly List<FinishedMatchRecord> matchHistory = new List<FinishedMatchRecord>();

        private FighterController _p1;
        private FighterController _p2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static GameContext EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameContext");
            return go.AddComponent<GameContext>();
        }

        /// <summary>Newest finished match (after <see cref="FinalizeMatch"/>), for Match Results UI.</summary>
        public bool TryGetLatestFinishedMatch(out FinishedMatchRecord rec)
        {
            if (matchHistory != null && matchHistory.Count > 0)
            {
                rec = matchHistory[0];
                return true;
            }
            rec = null;
            return false;
        }

        /// <summary>Call after fighters spawn in <see cref="LocalGameManager"/>.</summary>
        public void BeginLocalMatch(FighterController p1, FighterController p2)
        {
            _p1 = p1;
            _p2 = p2;
            roundWinnerCodes.Clear();

            matchStartedRealtime = Time.realtimeSinceStartup;
            var lobby = LobbyContext.Instance;
            isLocalMatch = lobby != null ? lobby.isLocalMatch : CharacterSelectData.isLocalMatch;
            if (lobby != null)
            {
                arenaIndex = lobby.lastArenaIndex;
                arenaDisplayName = lobby.lastArenaName;
                p1DisplayName = lobby.p1Name;
                p2DisplayName = lobby.p2Name;
            }
            else
            {
                arenaDisplayName = "";
                arenaIndex = 0;
                p1DisplayName = "Player 1";
                p2DisplayName = "Player 2";
            }

            p1FighterName = p1 != null && p1.FighterDef != null ? p1.FighterDef.fighterName : "";
            p2FighterName = p2 != null && p2.FighterDef != null ? p2.FighterDef.fighterName : "";
        }

        public void RecordRoundEnd(FighterController roundWinner)
        {
            if (_p1 == null) return;
            int code = 0;
            if (roundWinner == _p1) code = 1;
            else if (_p2 != null && roundWinner == _p2) code = 2;
            roundWinnerCodes.Add(code);
        }

        public void FinalizeMatch(FighterController matchWinner, int p1Wins, int p2Wins, int roundsPlayed)
        {
            var rec = new FinishedMatchRecord
            {
                arenaName = arenaDisplayName,
                arenaIndex = arenaIndex,
                localMatch = isLocalMatch,
                p1DisplayName = p1DisplayName,
                p2DisplayName = p2DisplayName,
                p1FighterName = p1FighterName,
                p2FighterName = p2FighterName,
                p1FinalWins = p1Wins,
                p2FinalWins = p2Wins,
                roundsPlayed = roundsPlayed,
                endedRealtime = Time.realtimeSinceStartup,
                outcome = OutcomeLabel(matchWinner),
                roundWinnerCodesSnapshot = new List<int>(roundWinnerCodes)
            };

            matchHistory.Insert(0, rec);
            while (matchHistory.Count > MaxMatchHistory)
                matchHistory.RemoveAt(matchHistory.Count - 1);
        }

        private string OutcomeLabel(FighterController matchWinner)
        {
            if (matchWinner == null) return "Draw";
            if (matchWinner == _p1) return "P1";
            if (matchWinner == _p2) return "P2";
            return "Unknown";
        }
    }

    [Serializable]
    public class FinishedMatchRecord
    {
        public string arenaName;
        public int arenaIndex;
        public bool localMatch;
        public string p1DisplayName;
        public string p2DisplayName;
        public string p1FighterName;
        public string p2FighterName;
        public int p1FinalWins;
        public int p2FinalWins;
        public string outcome;
        public int roundsPlayed;
        public float endedRealtime;
        public List<int> roundWinnerCodesSnapshot = new List<int>();
    }
}
