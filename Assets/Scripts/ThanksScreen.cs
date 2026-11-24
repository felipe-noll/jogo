using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ThanksScreen : MonoBehaviour
{
    [Header("Transição de Cena")]
    public Image blackScreen;        // A imagem preta que vai clarear
    public float fadeInDuration = 2f; // Tempo que leva para clarear

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (blackScreen != null)
        {
            // Começa 100% preto
            blackScreen.color = new Color(0, 0, 0, 1);
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime / fadeInDuration;
            blackScreen.color = new Color(0, 0, 0, Mathf.Clamp01(t));
            yield return null;
        }

        // Desativa a imagem para não bloquear cliques no botão
        blackScreen.gameObject.SetActive(false);
    }

    public void VoltarAoMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SairDoJogo()
    {
        Application.Quit();
        Debug.Log("O jogo foi fechado!");
    }
}