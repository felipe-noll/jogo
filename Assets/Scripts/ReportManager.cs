using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ReportManager : MonoBehaviour
{
    [Header("UI do Relatório")]
    public TMP_Text resultText;
    public TMP_Text moneyText;

    [Header("Efeito de Escrita")]
    public float typingDelay = 0.02f;
    public AudioSource typingAudio;
    public float moneyCountSpeed = 100f;

    [Header("Transição para a Loja")]
    public string shopSceneName = "4";
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    private bool isSkipped = false;

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

        PaymentReport r = GameSessionData.LastReport;
        if (r == null) return;

        // (Mantenha a montagem do texto igual ao código anterior)
        string rewards = "";
        if (r.tartarCleaned > 0) rewards += $"Tártaros limpos:     {r.tartarCleaned} x R$ {r.valTartar:F2} = +R$ {(r.tartarCleaned * r.valTartar):F2}\n";
        if (r.cavitiesFixed > 0) rewards += $"Cáries consertadas:  {r.cavitiesFixed} x R$ {r.valCavity:F2} = +R$ {(r.cavitiesFixed * r.valCavity):F2}\n";
        if (string.IsNullOrEmpty(rewards)) rewards = "(Nenhuma tarefa concluída)\n";

        string penalties = "";
        if (r.tartarMissed > 0) penalties += $"Tártaros esquecidos: {r.tartarMissed} x R$ {r.penTartarMissed:F2} = -R$ {(r.tartarMissed * r.penTartarMissed):F2}\n";
        if (r.cavitiesMissed > 0) penalties += $"Cáries esquecidas:   {r.cavitiesMissed} x R$ {r.penCavityMissed:F2} = -R$ {(r.cavitiesMissed * r.penCavityMissed):F2}\n";
        if (r.tongueDamaged > 0) penalties += $"Língua perfurada:    {r.tongueDamaged} x R$ {r.penTongue:F2} = -R$ {(r.tongueDamaged * r.penTongue):F2}\n";
        if (r.gumDamaged > 0) penalties += $"Gengiva perfurada:   {r.gumDamaged} x R$ {r.penGum:F2} = -R$ {(r.gumDamaged * r.penGum):F2}\n";
        if (r.excessMassa > 0) penalties += $"Massa em excesso:    {r.excessMassa} x R$ {r.penExcessMassa:F2} = -R$ {(r.excessMassa * r.penExcessMassa):F2}\n";
        if (r.tartarBroken > 0) penalties += $"Tártaro brocado:     {r.tartarBroken} x R$ {r.penTartarBroken:F2} = -R$ {(r.tartarBroken * r.penTartarBroken):F2}\n";
        if (r.unrepairedTeeth > 0) penalties += $"Dentes abertos:      {r.unrepairedTeeth} x R$ {r.penUnrepairedTooth:F2} = -R$ {(r.unrepairedTeeth * r.penUnrepairedTooth):F2}\n";
        if (string.IsNullOrEmpty(penalties)) penalties = "(Nenhum erro cometido)\n";

        string fullText =
            $"--- RELATÓRIO DE ATENDIMENTO ---\n\n" +
            $"<b>RECOMPENSAS:</b>\n{rewards}\n" +
            $"<b>PENALIDADES:</b>\n{penalties}\n" +
            $"--------------------------------\n" +
            $"<b>PAGAMENTO DA FASE: R$ {r.FinalPayment:F2}</b>";

        resultText.text = fullText;
        resultText.ForceMeshUpdate();
        int totalVisibleCharacters = resultText.textInfo.characterCount;
        resultText.maxVisibleCharacters = 0;

        StartCoroutine(TypewriterCoroutine(totalVisibleCharacters));
    }

    void Update()
    {
        bool skipPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) skipPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) skipPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) skipPressed = true;
#endif
        if (skipPressed) isSkipped = true;
    }

    private IEnumerator TypewriterCoroutine(int totalVisibleChars)
    {
        if (typingAudio != null) typingAudio.Play();
        for (int i = 0; i <= totalVisibleChars; i++)
        {
            if (isSkipped) { resultText.maxVisibleCharacters = totalVisibleChars; break; }
            resultText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingDelay);
        }
        resultText.maxVisibleCharacters = totalVisibleChars;
        if (typingAudio != null) typingAudio.Stop();
        StartCoroutine(CountMoneyCoroutine());
    }

    private IEnumerator CountMoneyCoroutine()
    {
        float currentMoney = 0f;
        float targetMoney = GameSessionData.PlayerMoney;
        if (moneyText != null)
        {
            while (currentMoney < targetMoney)
            {
                currentMoney += moneyCountSpeed * Time.deltaTime;
                if (currentMoney > targetMoney) currentMoney = targetMoney;
                moneyText.text = $"Dinheiro Total: R$ {currentMoney:F2}";
                yield return null;
            }
            moneyText.text = $"Dinheiro Total: R$ {targetMoney:F2}";
        }
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

    // --- BOTÃO DE IR PARA A LOJA ---
    public void BotaoIrParaLoja()
    {
        StartCoroutine(FadeOutAndLoad(shopSceneName));
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