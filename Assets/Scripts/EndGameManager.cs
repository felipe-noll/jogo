using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndGameManager : MonoBehaviour
{
    [Header("UI e Cena")]
    public Image whiteScreen;      // A imagem branca do flash
    public Image blackScreen;      // A imagem preta do fade out
    public DialogueManager dialogueManager;
    public string thanksSceneName = "Thanks";

    [Header("Diálogo Final do Jogador")]
    public Sprite playerSprite;
    public AudioClip playerBlip;
    public DialogueLine[] finalDialogue;

    [Header("Tempos e Efeitos")]
    public AudioSource explosionAudio;
    public float flashDuration = 0.5f;
    public float whiteScreenPause = 3f;
    public float fadeOutDuration = 1.5f; // Tempo que a tela leva pra ficar preta

    private bool isEnding = false;

    public void StartEndingSequence()
    {
        if (isEnding) return;
        isEnding = true;
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        // 1. Explosão
        if (explosionAudio != null) explosionAudio.Play();

        // 2. Tela fica branca
        if (whiteScreen != null)
        {
            whiteScreen.gameObject.SetActive(true);
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, flashDuration);
                whiteScreen.color = new Color(1, 1, 1, Mathf.Clamp01(t));
                yield return null;
            }
            whiteScreen.color = Color.white;
        }

        // 3. Espera na tela branca
        yield return new WaitForSeconds(whiteScreenPause);

        // 4. Diálogo Final do Jogador
        if (dialogueManager != null)
        {
            dialogueManager.SetupCharacters(playerSprite, null, playerBlip, null, 1.0f, 1.0f);
            dialogueManager.StartDialogue(finalDialogue, IrParaCenaDeAgradecimento);
        }
        else
        {
            IrParaCenaDeAgradecimento();
        }
    }

    private void IrParaCenaDeAgradecimento()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        // Garante que a tela preta está ativa e transparente
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            blackScreen.color = new Color(0, 0, 0, 0);

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / fadeOutDuration;
                blackScreen.color = new Color(0, 0, 0, Mathf.Clamp01(t));
                yield return null;
            }
            blackScreen.color = Color.black;
        }

        SceneManager.LoadScene(thanksSceneName);
    }
}