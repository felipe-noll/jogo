using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask drillableLayers;
    [SerializeField] private float maxDistance = 5f;

    [Header("Indicador de Bloco")]
    [SerializeField] private Transform blockIndicator;
    [SerializeField] private MeshRenderer indicatorRenderer;
    [Tooltip("Deixe um pouco maior que 1 (ex: 1.02) para evitar que o cubo pisque (Z-Fighting).")]
    [SerializeField] private float indicatorScaleMultiplier = 1.02f; // <-- NOVO CAMPO AQUI
    [Tooltip("Quanto o indicador flutua para fora da superfície para não afundar nela.")]
    [SerializeField] private float outwardOffset = 0.05f;

    [Tooltip("Intensidade do tremor. O valor antigo era 0.03f.")]
    [SerializeField] private float shakeIntensity = 0.03f;

    [Header("Referências")]
    [SerializeField] private ToolManager toolManager;

    private Color originalIndicatorColor;
    private ToolAnimator currentAnimator;

    private void Start()
    {
        if (indicatorRenderer != null)
            originalIndicatorColor = indicatorRenderer.material.color;
    }

    private void Update()
    {
        if (cam == null || toolManager == null) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (toolManager.CurrentTool != null)
        {
            if (currentAnimator == null || currentAnimator.gameObject != toolManager.CurrentTool.gameObject)
                currentAnimator = toolManager.CurrentTool.GetComponent<ToolAnimator>();
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance, drillableLayers, QueryTriggerInteraction.Ignore);

        VoxelWorld target = null;
        if (hasHit) target = hit.collider.GetComponentInParent<VoxelWorld>();

        bool isHeld = mouse.leftButton.isPressed;
        bool isDown = mouse.leftButton.wasPressedThisFrame;

        // --- NOVA VERIFICAÇÃO PARA A ANIQUILADORA ---
        // Se for a aniquiladora, ela atira mesmo que não esteja mirando num VoxelWorld
        if (toolManager.CurrentTool is BacteriaAnnihilatorTool)
        {
            if (isDown)
            {
                toolManager.CurrentTool.Use(Vector3.zero, Vector3.zero, null); // Passa nulo, pois a arma não usa o ponto de impacto
                if (currentAnimator != null) currentAnimator.PlayPunch();
            }
        }
        else if (target != null)
        {
            // Lógica para as outras ferramentas (Broca, Massa, Cureta)
            if (toolManager.CurrentTool is DrillTool)
            {
                if (isHeld)
                {
                    toolManager.CurrentTool.Use(hit.point, hit.normal, target);
                    if (currentAnimator != null) currentAnimator.SetPushing(true);
                }
            }
            else
            {
                if (isDown)
                {
                    toolManager.CurrentTool.Use(hit.point, hit.normal, target);
                    if (currentAnimator != null) currentAnimator.PlayPunch();
                }
            }
        }

        if (!isHeld)
        {
            toolManager.CurrentTool.OnStopUsing();
            if (currentAnimator != null) currentAnimator.SetPushing(false);
        }

        UpdateIndicator(hasHit, hit, target, isHeld);
    }

    private void UpdateIndicator(bool hasHit, RaycastHit hit, VoxelWorld world, bool isHolding)
    {
        if (blockIndicator == null) return;
        blockIndicator.gameObject.SetActive(hasHit && world != null);

        if (!hasHit || world == null) return;

        float size = world.BlockSize;
        Vector3 inside = hit.point - hit.normal * (size * 0.5f);
        Vector3 grid = world.transform.InverseTransformPoint(inside) / size;
        Vector3 centerLocal = new Vector3(Mathf.FloorToInt(grid.x) + 0.5f, Mathf.FloorToInt(grid.y) + 0.5f, Mathf.FloorToInt(grid.z) + 0.5f) * size;

        // 1. Pega a posição do centro e EMPURRA para fora da superfície (usando a normal)
        Vector3 basePos = world.transform.TransformPoint(centerLocal);
        basePos += hit.normal * (size * outwardOffset);

        blockIndicator.rotation = Quaternion.identity;

        float baseSize = size * indicatorScaleMultiplier;
        float targetScale = baseSize;
        Color targetColor = originalIndicatorColor;

        if (isHolding && toolManager.CurrentTool is DrillTool drill)
        {
            float progress = drill.MiningProgress;

            // 2. Tremor Projetado: O tremor acontece APENAS no plano da face (não entra no bloco)
            Vector3 randomPoint = Random.insideUnitSphere;
            // ProjectOnPlane remove qualquer movimento que vá "para dentro" ou "para fora" do bloco
            Vector3 shakeOffset = Vector3.ProjectOnPlane(randomPoint, hit.normal) * (shakeIntensity * progress);

            blockIndicator.position = basePos + shakeOffset;

            // Encolhimento
            float shrinkMultiplier = Mathf.Lerp(1.0f, 0.9f, progress);
            targetScale = baseSize * shrinkMultiplier;

            // Cores
            if (progress < 0.5f)
                targetColor = Color.Lerp(originalIndicatorColor, Color.yellow, progress * 2f);
            else
                targetColor = Color.Lerp(Color.yellow, Color.red, (progress - 0.5f) * 2f);
        }
        else
        {
            // Se não estiver minerando, fica na posição base sem tremer
            blockIndicator.position = basePos;
        }

        blockIndicator.localScale = Vector3.Lerp(blockIndicator.localScale, Vector3.one * targetScale, Time.deltaTime * 15f);
        if (indicatorRenderer != null)
            indicatorRenderer.material.color = Color.Lerp(indicatorRenderer.material.color, targetColor, Time.deltaTime * 15f);
    }
}