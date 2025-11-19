using Unity.FPS.Game;
using System.Collections;
using UnityEngine;
using Unity.FPS.Gameplay;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class ObjectiveReachPoint : Objective
    {
        [Tooltip("Visible transform that will be destroyed once the objective is completed")]
        public Transform DestroyRoot;

        [Header("Nivel a cargar al completar")]
        public string NextSceneName;

        void Awake()
        {
            if (DestroyRoot == null)
                DestroyRoot = transform;
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsCompleted)
                return;

            var player = other.GetComponent<PlayerCharacterController>();
            if (player != null)
            {
                CompleteObjective(string.Empty, string.Empty, "Objective complete : " + Title);

                Destroy(DestroyRoot.gameObject);

                if (!string.IsNullOrEmpty(NextSceneName))
                {
                    SceneTransitionManager.Instance.LoadSceneWithEffects(NextSceneName);
                }
                else
                {
                    Debug.LogWarning("No se asignó el nombre de la siguiente escena.");
                }
            }
        }
    }

        public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance;

        [Header("UI del Fade")]
        public Image FadeImage;

        [Header("Audio opcional")]
        public AudioSource AudioSource;

        [Header("Duraciones")]
        public float FadeDuration = 1f;
        public float DelayBeforeLoad = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadSceneWithEffects(string sceneName)
        {
            StartCoroutine(Transition(sceneName));
        }

        private IEnumerator Transition(string sceneName)
        {
            // FADE IN
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                if (FadeImage != null)
                    FadeImage.color = new Color(0, 0, 0, t / FadeDuration);
                yield return null;
            }

            // Reproducir audio si existe
            if (AudioSource != null)
                AudioSource.Play();

            // Espera antes de cargar
            yield return new WaitForSeconds(DelayBeforeLoad);

            // Cargar escena
            SceneManager.LoadScene(sceneName);

            // FADE OUT
            t = 0f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                if (FadeImage != null)
                    FadeImage.color = new Color(0, 0, 0, 1 - (t / FadeDuration));
                yield return null;
            }
        }
    }
}
