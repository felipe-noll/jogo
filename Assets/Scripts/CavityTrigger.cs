using System.Collections;
using UnityEngine;

public class CavityTrigger : MonoBehaviour
{
    [Header("Referências")]
    public AudioSource backgroundMusic;
    public DialogueManager dialogueManager;
    public GameObject player;

    [Header("Tempo de Espera")]
    [Tooltip("Segundos que o jogador fica no silêncio olhando para a bactéria antes do texto aparecer.")]
    public float delayBeforeDialogue = 2f;

    [Header("Personagens (Jogador e Bactéria)")]
    public Sprite playerSprite;
    public Sprite bacteriaSprite;
    public AudioClip playerBlip;
    public AudioClip bacteriaBlip;

    [Header("Diálogo da Bactéria")]
    public DialogueLine[] fallDialogueLines;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(TriggerSequence());
        }
    }

    private IEnumerator TriggerSequence()
    {
        // 1. Para a música
        if (backgroundMusic != null) backgroundMusic.Stop();

        // 2. Define o estado global
        GameSessionData.IsInCavity = true;

        // 3. Equipa a aniquiladora
        if (player != null)
        {
            ToolManager tm = player.GetComponent<ToolManager>();
            if (tm != null) tm.ForceEquipAnnihilator();
        }

        // 4. ESPERA O TEMPO DEFINIDO (O jogador vai ver a bactéria em silêncio)
        yield return new WaitForSeconds(delayBeforeDialogue);

        // 5. Configura os personagens e inicia o diálogo
        if (dialogueManager != null)
        {
            dialogueManager.SetupCharacters(playerSprite, bacteriaSprite, playerBlip, bacteriaBlip, 1.2f, 0.6f);
            dialogueManager.StartDialogue(fallDialogueLines);
        }
    }
}