using System.Collections;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public float startDelay = 3f;

    [Header("Personagens (Doutor e Jogador)")]
    public Sprite doctorSprite;
    public Sprite playerSprite;
    public AudioClip doctorBlip;
    public AudioClip playerBlip;

    [Header("Roteiro Inicial")]
    public DialogueLine[] startDialogueLines;

    void Start()
    {
        StartCoroutine(StartWithDelay());
    }

    private IEnumerator StartWithDelay()
    {
        yield return new WaitForSeconds(startDelay);

        if (dialogueManager != null)
        {
            // Configura o Doutor na esquerda e o Jogador na direita
            dialogueManager.SetupCharacters(doctorSprite, playerSprite, doctorBlip, playerBlip, 1.0f, 1.2f);
            dialogueManager.StartDialogue(startDialogueLines);
        }
    }
}