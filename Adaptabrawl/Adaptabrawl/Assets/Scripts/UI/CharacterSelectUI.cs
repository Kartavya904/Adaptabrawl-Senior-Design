using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using System.Collections.Generic;

namespace Adaptabrawl.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("Fighter Selection")]
        [SerializeField] private List<FighterDef> availableFighters = new List<FighterDef>();
        
        [Header("Player 1 UI")]
        [SerializeField] private Transform player1FighterContainer;
        [SerializeField] private TextMeshProUGUI player1FighterName;
        [SerializeField] private Image player1FighterImage;
        [SerializeField] private Button player1LeftButton;
        [SerializeField] private Button player1RightButton;
        [SerializeField] private Button player1ConfirmButton;
        [SerializeField] private TextMeshProUGUI player1ReadyText;
        
        [Header("Player 2 UI")]
        [SerializeField] private Transform player2FighterContainer;
        [SerializeField] private TextMeshProUGUI player2FighterName;
        [SerializeField] private Image player2FighterImage;
        [SerializeField] private Button player2LeftButton;
        [SerializeField] private Button player2RightButton;
        [SerializeField] private Button player2ConfirmButton;
        [SerializeField] private TextMeshProUGUI player2ReadyText;
        
        [Header("Navigation")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        
        private int player1Selection = 0;
        private int player2Selection = 0;
        private bool player1Ready = false;
        private bool player2Ready = false;
        
        private FighterDef selectedFighter1;
        private FighterDef selectedFighter2;
        
        private void Start()
        {
            // Load available fighters
            LoadAvailableFighters();
            
            // Setup button listeners
            if (player1LeftButton != null)
                player1LeftButton.onClick.AddListener(() => ChangeSelection(1, -1));
            if (player1RightButton != null)
                player1RightButton.onClick.AddListener(() => ChangeSelection(1, 1));
            if (player1ConfirmButton != null)
                player1ConfirmButton.onClick.AddListener(() => ConfirmSelection(1));
            
            if (player2LeftButton != null)
                player2LeftButton.onClick.AddListener(() => ChangeSelection(2, -1));
            if (player2RightButton != null)
                player2RightButton.onClick.AddListener(() => ChangeSelection(2, 1));
            if (player2ConfirmButton != null)
                player2ConfirmButton.onClick.AddListener(() => ConfirmSelection(2));
            
            if (startButton != null)
                startButton.onClick.AddListener(StartMatch);
            
            if (backButton != null)
                backButton.onClick.AddListener(ReturnToMenu);
            
            UpdateUI();
        }
        
        private void LoadAvailableFighters()
        {
            // If no fighters loaded, create default fighters using FighterFactory static methods
            if (availableFighters.Count == 0)
            {
                // Create default fighters using static factory methods
                availableFighters.Add(FighterFactory.CreateStrikerFighter());
                availableFighters.Add(FighterFactory.CreateElusiveFighter());
                
                // Note: In production, you would typically load FighterDef ScriptableObjects
                // from Resources or assign them in the Unity Inspector
            }
        }
        
        private void ChangeSelection(int player, int direction)
        {
            if (availableFighters.Count == 0) return;
            
            if (player == 1)
            {
                player1Selection = (player1Selection + direction + availableFighters.Count) % availableFighters.Count;
                player1Ready = false;
            }
            else
            {
                player2Selection = (player2Selection + direction + availableFighters.Count) % availableFighters.Count;
                player2Ready = false;
            }
            
            UpdateUI();
        }
        
        private void ConfirmSelection(int player)
        {
            if (availableFighters.Count == 0) return;
            
            if (player == 1)
            {
                player1Ready = true;
                selectedFighter1 = availableFighters[player1Selection];
            }
            else
            {
                player2Ready = true;
                selectedFighter2 = availableFighters[player2Selection];
            }
            
            UpdateUI();
            
            // Check if both players are ready
            if (player1Ready && player2Ready)
            {
                if (startButton != null)
                    startButton.interactable = true;
            }
        }
        
        private void UpdateUI()
        {
            // Update Player 1 UI
            if (availableFighters.Count > 0 && player1Selection < availableFighters.Count)
            {
                var fighter1 = availableFighters[player1Selection];
                if (player1FighterName != null)
                    player1FighterName.text = fighter1 != null ? fighter1.fighterName : "No Fighter";
                
                // Update image if available
                // if (player1FighterImage != null && fighter1 != null && fighter1.fighterPortrait != null)
                //     player1FighterImage.sprite = fighter1.fighterPortrait;
            }
            
            if (player1ReadyText != null)
            {
                player1ReadyText.text = player1Ready ? "READY" : "SELECT";
                player1ReadyText.color = player1Ready ? Color.green : Color.white;
            }
            
            // Update Player 2 UI
            if (availableFighters.Count > 0 && player2Selection < availableFighters.Count)
            {
                var fighter2 = availableFighters[player2Selection];
                if (player2FighterName != null)
                    player2FighterName.text = fighter2 != null ? fighter2.fighterName : "No Fighter";
                
                // Update image if available
                // if (player2FighterImage != null && fighter2 != null && fighter2.fighterPortrait != null)
                //     player2FighterImage.sprite = fighter2.fighterPortrait;
            }
            
            if (player2ReadyText != null)
            {
                player2ReadyText.text = player2Ready ? "READY" : "SELECT";
                player2ReadyText.color = player2Ready ? Color.green : Color.white;
            }
            
            // Update start button
            if (startButton != null)
            {
                startButton.interactable = player1Ready && player2Ready;
            }
        }
        
        private void StartMatch()
        {
            if (!player1Ready || !player2Ready) return;
            
            // Store selected fighters in a persistent object or static variable
            CharacterSelectData.selectedFighter1 = selectedFighter1;
            CharacterSelectData.selectedFighter2 = selectedFighter2;
            CharacterSelectData.isLocalMatch = true;
            
            // Load game scene
            SceneManager.LoadScene("GameScene");
        }
        
        private void ReturnToMenu()
        {
            SceneManager.LoadScene("StartScene");
        }
    }
    
    // Static class to pass data between scenes
    public static class CharacterSelectData
    {
        public static Adaptabrawl.Data.FighterDef selectedFighter1;
        public static Adaptabrawl.Data.FighterDef selectedFighter2;
        public static bool isLocalMatch = false;
    }
}

