using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PatientHUD : MonoBehaviour
{
    [Header("Sessão")]
    public PatientSession session;

    [Header("Confirmação")]
    public GameObject confirmPanel;
    public float confirmTimeout = 2.5f;

    [Header("Transição de Cena")]
    public string shopSceneName = "Shop";
    public Image fadeImage;         // Arraste a imagem preta aqui
    public float fadeDuration = 1.5f;

    private bool awaitingConfirm;
    private float confirmDeadline;
    private bool isTransitioning = false;

    void Start()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);

        // Garante que o fade da fase começa transparente
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    void Update()
    {
        if (session == null || session.SessionEnded || isTransitioning) return;

        if (awaitingConfirm && Time.time > confirmDeadline)
        {
            awaitingConfirm = false;
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        if (EnterPressed())
        {
            if (awaitingConfirm)
            {
                awaitingConfirm = false;
                if (confirmPanel != null) confirmPanel.SetActive(false);
                session.FinishAndShowReport(IrParaLoja);
            }
            else if (confirmPanel != null)
            {
                awaitingConfirm = true;
                confirmDeadline = Time.time + confirmTimeout;
                confirmPanel.SetActive(true);
            }
            else
            {
                session.FinishAndShowReport(IrParaLoja);
            }
        }
    }

    private void IrParaLoja(PaymentReport report)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        GameSessionData.ProcessPayment(report);

        if (fadeImage != null)
        {
            fadeImage.raycastTarget = true;
            StartCoroutine(FadeOutAndLoad());
        }
        else
        {
            SceneManager.LoadScene(shopSceneName);
        }
    }

    private IEnumerator FadeOutAndLoad()
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
        SceneManager.LoadScene(shopSceneName);
    }

    bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;
#endif
        return false;
    }
}