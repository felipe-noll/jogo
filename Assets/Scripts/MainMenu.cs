using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Necessário para o Image

public class MainMenu : MonoBehaviour
{
    [Header("Configurações de Fade")]
    public Image fadeImageNoMenu;
    public float fadeDuration = 1f;

    [Header("Nome da Cena")]
    public string nomeCenaFase1 = "2";

    void Start()
    {
        // Garante que o mouse está visível e livre na tela de menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Se tiver a imagem de fade no menu, garante que ela começa transparente
        if (fadeImageNoMenu != null)
        {
            Color c = fadeImageNoMenu.color;
            c.a = 0f;
            fadeImageNoMenu.color = c;
            fadeImageNoMenu.raycastTarget = false;
        }
    }

    public void Jogar()
    {
        GameSessionData.ResetData();
        // Se a imagem de fade estiver configurada, faz a animação
        if (fadeImageNoMenu != null)
        {
            fadeImageNoMenu.raycastTarget = true; // Bloqueia cliques extras
            StartCoroutine(FadeOutAndLoad());
        }
        else
        {
            // Se não tiver imagem, só carrega direto
            SceneManager.LoadScene(nomeCenaFase1);
        }
    }

    private IEnumerator FadeOutAndLoad()
    {
        float elapsedTime = 0f;
        Color color = fadeImageNoMenu.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeImageNoMenu.color = color;
            yield return null;
        }

        // Garante que fique 100% preto antes de trocar de cena
        color.a = 1f;
        fadeImageNoMenu.color = color;

        SceneManager.LoadScene(nomeCenaFase1);
    }

    public void Sair()
    {
        Application.Quit();
        Debug.Log("O jogo fechou!"); // Só aparece no Editor
    }
}