using UnityEngine;

public class CuretteTool : ToolBase
{
    [Header("Configurações de Limpeza")]
    [Tooltip("Quantos cliques são necessários para limpar 100% o bloco.")]
    public int clicksToClean = 3;

    [Header("Cores")]
    public Color32 cleanColor = new Color32(252, 250, 242, 255);
    public Color32 tartarColor = new Color32(255, 243, 179, 255);
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;

    [Header("Efeitos")]
    public ParticleSystem scrapeParticles;
    public AudioSource scrapeAudio;
    [Tooltip("Quantas partículas saem por clique de raspagem.")]
    public int particlesPerScrape = 8;
    [Tooltip("Tamanho base de cada pedaço (ajuste conforme BlockSize).")]
    public float particleSize = 0.03f;
    [Tooltip("Velocidade de saída dos pedaços.")]
    public float particleSpeed = 2f;
    private void Awake()
    {
        if (GameSessionData.CuretteUpgraded)
            clicksToClean = 1;
        else
            clicksToClean = 3; // <-- FORÇA O VALOR NORMAL SE NÃO TIVER UPGRADE
    }
    public override void Use(Vector3 hitPoint, Vector3 hitNormal, VoxelWorld world)
    {
        Vector3Int grid = world.WorldToGrid(hitPoint, hitNormal);

        // Cor do voxel ANTES de limpar (é o que está sendo raspado)
        Color32 scrapedColor = tartarColor;
        byte idx = world.GetVoxel(grid.x, grid.y, grid.z);
        if (idx != 0) scrapedColor = world.GetColor(idx);

        float amountPerClick = 1f / Mathf.Max(1, clicksToClean);
        bool cleaned = world.CleanVoxel(hitPoint, hitNormal, cleanColor, amountPerClick, IsTartar);

        if (cleaned)
        {
            if (scrapeParticles != null)
                SpawnScrapeBurst(grid, world, scrapedColor);
            if (scrapeAudio != null) scrapeAudio.Play();
        }
    }

    private void SpawnScrapeBurst(Vector3Int grid, VoxelWorld world, Color32 color)
    {
        Vector3 centerLocal = new Vector3(grid.x + 0.5f, grid.y + 0.5f, grid.z + 0.5f) * world.BlockSize;
        Vector3 centerWorld = world.transform.TransformPoint(centerLocal);
        float half = world.BlockSize * 0.5f;

        for (int i = 0; i < particlesPerScrape; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-half, half),
                Random.Range(-half, half),
                Random.Range(-half, half)
            );

            Vector3 dir = offset.sqrMagnitude > 1e-6f
                ? offset.normalized
                : Random.onUnitSphere;

            var emit = new ParticleSystem.EmitParams
            {
                position = centerWorld + offset,
                startColor = color,
                startSize = particleSize * Random.Range(0.6f, 1.4f),
                velocity = (dir + Random.insideUnitSphere * 0.4f) * particleSpeed,
                startLifetime = Random.Range(0.3f, 0.7f)
            };
            scrapeParticles.Emit(emit, 1);
        }
    }

    private bool IsTartar(Color32 c)
    {
        int margin = Mathf.RoundToInt(colorTolerance * 255f);

        if (Mathf.Abs(c.r - cleanColor.r) <= margin &&
            Mathf.Abs(c.g - cleanColor.g) <= margin &&
            Mathf.Abs(c.b - cleanColor.b) <= margin)
            return false;

        bool rInRange = c.r >= Mathf.Min(tartarColor.r, cleanColor.r) - margin &&
                        c.r <= Mathf.Max(tartarColor.r, cleanColor.r) + margin;
        bool gInRange = c.g >= Mathf.Min(tartarColor.g, cleanColor.g) - margin &&
                        c.g <= Mathf.Max(tartarColor.g, cleanColor.g) + margin;
        bool bInRange = c.b >= Mathf.Min(tartarColor.b, cleanColor.b) - margin &&
                        c.b <= Mathf.Max(tartarColor.b, cleanColor.b) + margin;

        return rInRange && gInRange && bInRange;
    }
}