using UnityEngine;
using UnityEngine.InputSystem;

public class ToolManager : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste as ferramentas na ordem: 1-Broca, 2-Massa, 3-Cureta")]
    public ToolBase[] tools;

    [Tooltip("Onde as ferramentas ficam posicionadas (filho da câmera)")]
    public Transform toolHolder;

    private int currentIndex = -1;
    private float nextUseTime;

    public ToolBase CurrentTool => (currentIndex >= 0 && currentIndex < tools.Length) ? tools[currentIndex] : null;

    private void Start()
    {
        // Equipa a primeira ferramenta ao iniciar
        if (tools.Length > 0) SelectTool(0);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Se o jogador caiu no buraco da bactéria, bloqueia as ferramentas antigas!
        if (GameSessionData.IsInCavity)
        {
            if (kb.digit4Key.wasPressedThisFrame && GameSessionData.HasAnnihilatorTool)
            {
                SelectTool(3);
            }
            return; // O 'return' aqui impede que as teclas 1, 2 e 3 funcionem
        }

        // Lógica normal (fora do buraco)
        if (kb.digit1Key.wasPressedThisFrame) SelectTool(0);
        if (kb.digit2Key.wasPressedThisFrame) SelectTool(1);
        if (kb.digit3Key.wasPressedThisFrame) SelectTool(2);

        if (kb.digit4Key.wasPressedThisFrame && GameSessionData.HasAnnihilatorTool)
        {
            SelectTool(3);
        }
    }

    // Função que a Trigger vai chamar
    public void ForceEquipAnnihilator()
    {
        if (GameSessionData.HasAnnihilatorTool)
        {
            SelectTool(3);
        }
    }

    private void SelectTool(int index)
    {
        if (index == currentIndex || index < 0 || index >= tools.Length) return;

        if (CurrentTool != null) CurrentTool.OnUnequip();

        currentIndex = index;
        ToolBase newTool = tools[currentIndex];

        // --- CORREÇÃO AQUI ---
        // Verifica se a ferramenta é um Prefab (não está na cena)
        if (!newTool.gameObject.scene.IsValid())
        {
            // Instancia o Prefab na cena, como filho do ToolHolder
            GameObject instance = Instantiate(newTool.gameObject, toolHolder);
            instance.name = newTool.name; // Mantém o nome
            tools[currentIndex] = instance.GetComponent<ToolBase>(); // Atualiza a lista com a instância
            newTool = tools[currentIndex];
        }
        // ---------------------

        newTool.gameObject.SetActive(false);
        newTool.transform.SetParent(toolHolder, false);
        newTool.transform.localPosition = Vector3.zero;
        newTool.transform.localRotation = Quaternion.identity;
        newTool.gameObject.SetActive(true);

        newTool.OnEquip();
    }

    // Chamado pelo ToolController
    public bool CanUse()
    {
        if (CurrentTool == null) return false;
        if (Time.time < nextUseTime) return false;
        nextUseTime = Time.time + CurrentTool.cooldown;
        return true;
    }
}