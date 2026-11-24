using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum Speaker { Left, Right }

[System.Serializable]
public class DialogueLine
{
    public Speaker speaker;
    [TextArea(2, 5)]
    public string text;
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    [Header("Retratos (Esquerda e Direita)")]
    public Image leftPortrait;
    public Image rightPortrait;

    [Header("Configurações de Diálogo")]
    public float typingSpeed = 0.03f;

    [Header("Áudio (Vozes em Loop)")]
    public AudioSource typingAudio;

    private AudioClip currentLeftBlip;
    private AudioClip currentRightBlip;
    private float currentLeftPitch = 1.0f;
    private float currentRightPitch = 1.2f;

    [Header("Controle do Jogador")]
    public GameObject player;

    private int index = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;
    private DialogueLine[] currentLines;

    // AQUI ESTÁ A VARIÁVEL DO CALLBACK
    private System.Action onDialogueFinished;

    private Color activeColor = Color.white;
    private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    void Start()
    {
        dialoguePanel.SetActive(false);
        if (typingAudio != null)
        {
            typingAudio.loop = true;
            typingAudio.playOnAwake = false;
        }
    }

    public void SetupCharacters(Sprite leftSprite, Sprite rightSprite, AudioClip leftSound, AudioClip rightSound, float leftPitch = 1.0f, float rightPitch = 1.2f)
    {
        // Configura o lado Esquerdo
        if (leftPortrait != null)
        {
            leftPortrait.sprite = leftSprite;
            leftPortrait.enabled = (leftSprite != null); // Desativa se for nulo
        }

        // Configura o lado Direito
        if (rightPortrait != null)
        {
            rightPortrait.sprite = rightSprite;
            rightPortrait.enabled = (rightSprite != null); // Desativa se for nulo
        }

        currentLeftBlip = leftSound;
        currentRightBlip = rightSound;
        currentLeftPitch = leftPitch;
        currentRightPitch = rightPitch;
    }

    void Update()
    {
        if (!dialogueActive) return;

        if (EPressed())
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentLines[index].text;
                dialogueText.maxVisibleCharacters = currentLines[index].text.Length;
                isTyping = false;
                if (typingAudio != null) typingAudio.Stop();
            }
            else
            {
                index++;
                if (index < currentLines.Length)
                    ShowNextLine();
                else
                    EndDialogue();
            }
        }
    }

    // AQUI ESTÁ A NOVA ASSINATURA COM O CALLBACK
    public void StartDialogue(DialogueLine[] newLines, System.Action onFinished = null)
    {
        if (dialogueActive) return;

        currentLines = newLines;
        dialogueActive = true;
        index = 0;
        onDialogueFinished = onFinished; // Salva a função para chamar no final
        dialoguePanel.SetActive(true);

        if (player != null)
        {
            if (player.GetComponent<PlayerMotor>() != null) player.GetComponent<PlayerMotor>().enabled = false;
            if (player.GetComponent<PlayerLook>() != null) player.GetComponent<PlayerLook>().enabled = false;
            if (player.GetComponent<ToolController>() != null) player.GetComponent<ToolController>().enabled = false;
            if (player.GetComponent<CursorLock>() != null) player.GetComponent<CursorLock>().enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        DialogueLine currentLine = currentLines[index];

        if (currentLine.speaker == Speaker.Left)
        {
            leftPortrait.color = activeColor;
            rightPortrait.color = inactiveColor;
            if (typingAudio != null) { typingAudio.clip = currentLeftBlip; typingAudio.pitch = currentLeftPitch; }
        }
        else
        {
            leftPortrait.color = inactiveColor;
            rightPortrait.color = activeColor;
            if (typingAudio != null) { typingAudio.clip = currentRightBlip; typingAudio.pitch = currentRightPitch; }
        }

        typingCoroutine = StartCoroutine(TypeSentence(currentLine.text));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = sentence;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        if (typingAudio != null) typingAudio.Play();

        for (int i = 0; i <= dialogueText.textInfo.characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if (typingAudio != null) typingAudio.Stop();
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        dialoguePanel.SetActive(false);
        if (typingAudio != null) typingAudio.Stop();

        if (player != null)
        {
            if (player.GetComponent<PlayerMotor>() != null) player.GetComponent<PlayerMotor>().enabled = true;
            if (player.GetComponent<PlayerLook>() != null) player.GetComponent<PlayerLook>().enabled = true;
            if (player.GetComponent<ToolController>() != null) player.GetComponent<ToolController>().enabled = true;
            if (player.GetComponent<CursorLock>() != null) player.GetComponent<CursorLock>().enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // AQUI ELE CHAMA A FUNÇÃO DE IR PARA A CENA DE AGRADECIMENTO
        onDialogueFinished?.Invoke();
    }

    bool EPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E)) return true;
#endif
        return false;
    }
}