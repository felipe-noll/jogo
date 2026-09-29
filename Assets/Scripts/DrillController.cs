using UnityEngine;
using UnityEngine.InputSystem;

public class DrillController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask drillableLayers;
    [SerializeField] private float maxDistance = 5f;

    [Header("Ritmo da broca")]
    [Tooltip("Tempo mínimo (segundos) entre quebras enquanto segura o botão. 0.1 = 10 blocos/s.")]
    [SerializeField] private float breakCooldown = 0.12f;

    [Header("Mira sobre o bloco")]
    [SerializeField] private Transform blockIndicator;
    [SerializeField] private float indicatorScale = 1f;

    [Header("Partículas")]
    [SerializeField] private ParticleSystem breakParticles;
    [Tooltip("Quantidade de partículas por bloco quebrado.")]
    [SerializeField] private int particlesPerBreak = 15;
    [Tooltip("Tamanho de cada partícula (unidades do mundo).")]
    [SerializeField] private float particleSize = 0.04f;
    [Tooltip("Velocidade inicial das partículas.")]
    [SerializeField] private float particleSpeed = 1.5f;
    [Tooltip("Raio (unidades do mundo) em que as partículas se espalham ao redor do bloco quebrado.")]
    [SerializeField] private float particleSpread = 0.35f;
    [Header("Áudio")]
    [SerializeField] private AudioSource breakAudio;

    private float nextBreakTime;

    private void Update()
    {
        if (cam == null) return;
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance,
                                      drillableLayers, QueryTriggerInteraction.Ignore);

        VoxelWorld target = null;
        if (hasHit)
            target = hit.collider.GetComponentInParent<VoxelWorld>();

        // Segurar o botão quebra em ritmo controlado.
        if (mouse.leftButton.isPressed
    && target != null
    && Time.time >= nextBreakTime)
        {
            Color32 brokenColor;
            Vector3 blockCenter;

            if (target.BreakVoxel(hit.point, hit.normal, out brokenColor, out blockCenter))
            {
                nextBreakTime = Time.time + breakCooldown;

                // Partículas em volta do centro do bloco
                SpawnBreakParticles(blockCenter, target.BlockSize, brokenColor);

                if (breakAudio != null) breakAudio.Play();

                // Reconstrói collider e refaz o raycast
                target.ForceRebuildCollider();

                hasHit = Physics.Raycast(ray, out hit, maxDistance,
                                         drillableLayers, QueryTriggerInteraction.Ignore);
            }
        }

        UpdateIndicator(hasHit, hit);
    }

    // -------------------------------------------------------------------
    private void SpawnBreakParticles(Vector3 blockCenter, float blockSize, Color32 color)
    {
        if (breakParticles == null) return;

        // Metade do tamanho do bloco — as partículas saem dentro desse raio.
        float half = blockSize * 0.5f;

        for (int i = 0; i < particlesPerBreak; i++)
        {
            // Ponto aleatório DENTRO do cubo do bloco.
            Vector3 offset = new Vector3(
                Random.Range(-half, half),
                Random.Range(-half, half),
                Random.Range(-half, half));

            // Direção para fora, a partir do centro (as partículas "explodem").
            Vector3 outDir = offset.sqrMagnitude > 1e-6f ? offset.normalized : Random.onUnitSphere;

            var emit = new ParticleSystem.EmitParams
            {
                position = blockCenter + offset,
                startColor = color,
                startSize = particleSize * Random.Range(0.6f, 1.4f),
                // Cada partícula voa para fora do centro, com um pouquinho de aleatoriedade.
                velocity = (outDir + Random.insideUnitSphere * 0.4f) * particleSpeed,
                startLifetime = Random.Range(0.4f, 0.9f)
            };

            breakParticles.Emit(emit, 1);
        }
    }

    private void UpdateIndicator(bool hasHit, RaycastHit hit)
    {
        if (blockIndicator == null) return;

        blockIndicator.gameObject.SetActive(hasHit);
        if (!hasHit) return;

        var world = hit.collider.GetComponentInParent<VoxelWorld>();
        float size = world != null ? world.BlockSize : 1f;

        // Posiciona no centro do bloco que será quebrado.
        Vector3 inside = hit.point - hit.normal * (size * 0.5f);
        Vector3 local = world != null ? world.transform.InverseTransformPoint(inside)
                                      : inside;
        Vector3 grid = world != null ? local / size : inside;
        int x = Mathf.FloorToInt(grid.x);
        int y = Mathf.FloorToInt(grid.y);
        int z = Mathf.FloorToInt(grid.z);

        Vector3 centerLocal = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * size;
        Vector3 centerWorld = world != null
            ? world.transform.TransformPoint(centerLocal)
            : centerLocal;

        blockIndicator.position = centerWorld;
        blockIndicator.rotation = Quaternion.identity;
        blockIndicator.localScale = Vector3.one * size * indicatorScale;
    }
}