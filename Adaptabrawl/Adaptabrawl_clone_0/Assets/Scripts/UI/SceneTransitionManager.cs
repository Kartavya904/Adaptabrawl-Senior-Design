using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Adaptabrawl.UI
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private Animator transitionAnimator;
        [SerializeField] private string fadeInTrigger = "FadeIn";
        [SerializeField] private string fadeOutTrigger = "FadeOut";
        
        private static SceneTransitionManager instance;
        public static SceneTransitionManager Instance => instance;
        
        private bool isTransitioning = false;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void TransitionToScene(string sceneName)
        {
            if (isTransitioning) return;
            
            StartCoroutine(TransitionCoroutine(sceneName));
        }
        
        public void TransitionToScene(int sceneIndex)
        {
            if (isTransitioning) return;
            
            StartCoroutine(TransitionCoroutine(sceneIndex));
        }
        
        private IEnumerator TransitionCoroutine(string sceneName)
        {
            isTransitioning = true;
            
            // Fade out
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeOutTrigger);
            
            yield return new WaitForSeconds(transitionDuration);
            
            // Load scene
            SceneManager.LoadScene(sceneName);
            
            // Wait a frame for scene to load
            yield return null;
            
            // Fade in
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeInTrigger);
            
            yield return new WaitForSeconds(transitionDuration);
            
            isTransitioning = false;
        }
        
        private IEnumerator TransitionCoroutine(int sceneIndex)
        {
            isTransitioning = true;
            
            // Fade out
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeOutTrigger);
            
            yield return new WaitForSeconds(transitionDuration);
            
            // Load scene
            SceneManager.LoadScene(sceneIndex);
            
            // Wait a frame for scene to load
            yield return null;
            
            // Fade in
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeInTrigger);
            
            yield return new WaitForSeconds(transitionDuration);
            
            isTransitioning = false;
        }
        
        public void FadeOut()
        {
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeOutTrigger);
        }
        
        public void FadeIn()
        {
            if (transitionAnimator != null)
                transitionAnimator.SetTrigger(fadeInTrigger);
        }
    }
}

