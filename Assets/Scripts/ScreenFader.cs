using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("A imagem preta que cobre a tela.")]
    public Image fadeImage;

    [Tooltip("Quanto tempo leva para a tela clarear (em segundos).")]
    public float fadeDuration = 2f;

    void Start()
    {
        if (fadeImage != null)
        {
            // Garante que a tela começa 100% preta
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;

            // Inicia a animação de clarear
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        // Desativa o clique na imagem para o jogador não ser bloqueado
        fadeImage.raycastTarget = false;

        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Calcula a nova opacidade (vai de 1 para 0)
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeImage.color = color;

            yield return null; // Espera o próximo frame
        }

        // Garante que terminou 100% transparente
        color.a = 0f;
        fadeImage.color = color;

        // Opcional: desativa o objeto para economizar processamento
        fadeImage.gameObject.SetActive(false);
    }
}