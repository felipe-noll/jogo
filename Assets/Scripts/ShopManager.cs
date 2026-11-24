using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("UI da Loja")]
    public TMP_Text moneyText;

    [Header("Botões de Compra")]
    public Button btnUpgradeDrill;
    public Button btnUpgradeCurette;
    public Button btnBuyAnnihilator;

    [Header("Transição de Cena")]
    public string nextSceneName = "Level_2";
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            StartCoroutine(FadeIn());
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"Dinheiro: R$ {GameSessionData.PlayerMoney:F2}";

        // Atualiza os botões se já foram comprados
        if (GameSessionData.DrillUpgraded) DesativarBotao(btnUpgradeDrill, "Broca Melhorada!");
        if (GameSessionData.CuretteUpgraded) DesativarBotao(btnUpgradeCurette, "Cureta Melhorada!");
        if (GameSessionData.HasAnnihilatorTool) DesativarBotao(btnBuyAnnihilator, "Ferramenta Adquirida!");
    }

    private void DesativarBotao(Button btn, string textoNovo)
    {
        if (btn != null)
        {
            btn.interactable = false;
            TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = textoNovo;
        }
    }

    // --- COMPRAS ---
    public void ComprarUpgradeBroca()
    {
        if (GameSessionData.DrillUpgraded || GameSessionData.PlayerMoney < 50f) return;

        GameSessionData.PlayerMoney -= 50f;
        GameSessionData.DrillUpgraded = true;
        UpdateUI();
    }

    public void ComprarUpgradeCureta()
    {
        if (GameSessionData.CuretteUpgraded || GameSessionData.PlayerMoney < 50f) return;

        GameSessionData.PlayerMoney -= 50f;
        GameSessionData.CuretteUpgraded = true;
        UpdateUI();
    }

    public void ComprarAniquiladora()
    {
        if (GameSessionData.HasAnnihilatorTool || GameSessionData.PlayerMoney < 100f) return;

        GameSessionData.PlayerMoney -= 100f;
        GameSessionData.HasAnnihilatorTool = true;
        UpdateUI();
    }

    public void BotaoProximaFase()
    {
        GameSessionData.NextLevelScene = nextSceneName;
        StartCoroutine(FadeOutAndLoad(nextSceneName));
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true;
            float elapsedTime = 0f;
            Color color = fadeImage.color;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                fadeImage.color = color;
                yield return null;
            }
        }
        SceneManager.LoadScene(sceneName);
    }
}