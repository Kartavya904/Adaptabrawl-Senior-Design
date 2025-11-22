using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the match flow, health bars, and win conditions.
/// </summary>
public class MatchManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the fighter spawner")]
    public FighterSpawner spawner;
    
    [Header("UI References")]
    [Tooltip("Player 1 health bar fill image")]
    public Image player1HealthFill;
    
    [Tooltip("Player 2 health bar fill image")]
    public Image player2HealthFill;
    
    [Tooltip("Player 1 name text")]
    public TextMeshProUGUI player1NameText;
    
    [Tooltip("Player 2 name text")]
    public TextMeshProUGUI player2NameText;
    
    [Tooltip("Win panel GameObject")]
    public GameObject winPanel;
    
    [Tooltip("Win text display")]
    public TextMeshProUGUI winText;
    
    [Header("Match Settings")]
    [Tooltip("Delay before match starts")]
    public float matchStartDelay = 1f;
    
    [Tooltip("Delay before auto-restart after match ends")]
    public float restartDelay = 5f;
    
    [Header("Runtime State")]
    [SerializeField] private bool matchActive = false;
    [SerializeField] private bool matchEnded = false;
    
    public bool IsMatchActive => matchActive;
    public bool HasMatchEnded => matchEnded;
    
    void Start()
    {
        StartCoroutine(InitializeMatch());
    }
    
    System.Collections.IEnumerator InitializeMatch()
    {
        yield return new WaitForSeconds(matchStartDelay);
        
        if (spawner == null)
        {
            Debug.LogError("MatchManager: FighterSpawner not assigned!");
            yield break;
        }
        
        // Subscribe to fighter events
        if (spawner.Player1 != null)
        {
            spawner.Player1.OnHealthChanged += UpdatePlayer1Health;
            spawner.Player1.OnDeath += OnPlayer1Death;
            
            if (player1NameText != null)
                player1NameText.text = spawner.Player1.FighterDef.fighterName;
            
            // Initialize health bar
            UpdatePlayer1Health(spawner.Player1.CurrentHealth, spawner.Player1.MaxHealth);
            
            Debug.Log("✓ Player 1 health tracking initialized");
        }
        else
        {
            Debug.LogWarning("Player 1 not spawned!");
        }
        
        if (spawner.Player2 != null)
        {
            spawner.Player2.OnHealthChanged += UpdatePlayer2Health;
            spawner.Player2.OnDeath += OnPlayer2Death;
            
            if (player2NameText != null)
                player2NameText.text = spawner.Player2.FighterDef.fighterName;
            
            // Initialize health bar
            UpdatePlayer2Health(spawner.Player2.CurrentHealth, spawner.Player2.MaxHealth);
            
            Debug.Log("✓ Player 2 health tracking initialized");
        }
        else
        {
            Debug.LogWarning("Player 2 not spawned!");
        }
        
        matchActive = true;
        
        if (winPanel != null)
            winPanel.SetActive(false);
        
        Debug.Log("✓ Match started!");
    }
    
    void UpdatePlayer1Health(float current, float max)
    {
        if (player1HealthFill != null)
        {
            player1HealthFill.fillAmount = current / max;
            
            // Color based on health
            if (current / max > 0.5f)
                player1HealthFill.color = Color.green;
            else if (current / max > 0.25f)
                player1HealthFill.color = Color.yellow;
            else
                player1HealthFill.color = Color.red;
        }
    }
    
    void UpdatePlayer2Health(float current, float max)
    {
        if (player2HealthFill != null)
        {
            player2HealthFill.fillAmount = current / max;
            
            // Color based on health
            if (current / max > 0.5f)
                player2HealthFill.color = Color.green;
            else if (current / max > 0.25f)
                player2HealthFill.color = Color.yellow;
            else
                player2HealthFill.color = Color.red;
        }
    }
    
    void OnPlayer1Death()
    {
        if (matchEnded) return;
        EndMatch($"{spawner.Player2.FighterDef.fighterName} Wins!", 2);
    }
    
    void OnPlayer2Death()
    {
        if (matchEnded) return;
        EndMatch($"{spawner.Player1.FighterDef.fighterName} Wins!", 1);
    }
    
    void EndMatch(string winnerText, int winnerNumber)
    {
        if (matchEnded) return;
        
        matchEnded = true;
        matchActive = false;
        
        Debug.Log($"Match Over! {winnerText}");
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            
            if (winText != null)
            {
                winText.text = winnerText;
                winText.color = winnerNumber == 1 ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.5f, 0.3f);
            }
        }
        
        // Auto restart after delay
        StartCoroutine(RestartAfterDelay(restartDelay));
    }
    
    System.Collections.IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log("Restarting match...");
        
        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    /// <summary>
    /// Manually restart the match
    /// </summary>
    public void RestartMatch()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (spawner != null)
        {
            if (spawner.Player1 != null)
            {
                spawner.Player1.OnHealthChanged -= UpdatePlayer1Health;
                spawner.Player1.OnDeath -= OnPlayer1Death;
            }
            
            if (spawner.Player2 != null)
            {
                spawner.Player2.OnHealthChanged -= UpdatePlayer2Health;
                spawner.Player2.OnDeath -= OnPlayer2Death;
            }
        }
    }
}

