using UnityEngine;

public class DrillTool : ToolBase
{
    [Header("Mineração")]
    public float timeToBreak = 1.0f;

    private float currentMiningTime = 0f;
    private Vector3Int currentTargetGrid;
    private bool hasTarget = false;
    private float lastUseTime = 0f;

    [Header("Efeitos Visuais")]
    public ParticleSystem breakParticles;
    public int particlesPerBreak = 20;
    public float particleSize = 0.03f;
    public float particleSpeed = 2f;

    [Header("Efeitos Sonoros")]
    [Tooltip("Som de quando o bloco quebra.")]
    public AudioSource breakAudio;
    [Tooltip("Som do motor da broca girando (Coloque em Loop no Inspector).")]
    public AudioSource drillLoopAudio;
    // --- ADICIONE ESTE MÉTODO AWKAE AQUI ---
    private void Awake()
    {
        if (GameSessionData.DrillUpgraded)
            timeToBreak = 0.3f;
        else
            timeToBreak = 1.0f; // <-- FORÇA O VALOR NORMAL SE NÃO TIVER UPGRADE
    }
    // ---------------------------------------
    public float MiningProgress => Mathf.Clamp01(currentMiningTime / timeToBreak);

    // Update é necessário para parar o som se o jogador mirar para o teto enquanto segura o botão
    private void Update()
    {
        if (drillLoopAudio != null && drillLoopAudio.isPlaying)
        {
            // Se o Use() não foi chamado nos últimos 0.1 segundos (jogador olhou pra longe do bloco)
            if (Time.time - lastUseTime > 0.1f)
            {
                drillLoopAudio.Stop();
            }
        }
    }

    public override void Use(Vector3 hitPoint, Vector3 hitNormal, VoxelWorld world)
    {
        lastUseTime = Time.time;

        // Toca o som do motor se ele não estiver tocando
        if (drillLoopAudio != null && !drillLoopAudio.isPlaying)
        {
            drillLoopAudio.Play();
        }

        Vector3Int grid = world.WorldToGrid(hitPoint, hitNormal);

        if (!hasTarget || grid != currentTargetGrid)
        {
            currentTargetGrid = grid;
            currentMiningTime = 0f;
            hasTarget = true;
        }

        currentMiningTime += Time.deltaTime;

        // Muda o tom do som do motor conforme a broca esquenta (fica mais agudo)
        if (drillLoopAudio != null)
        {
            drillLoopAudio.pitch = Mathf.Lerp(0.9f, 1.3f, MiningProgress);
        }

        Color32 blockColor = Color.white;
        byte idx = world.GetVoxel(grid.x, grid.y, grid.z);
        if (idx != 0) blockColor = world.GetColor(idx);

        if (breakParticles != null && Random.value < 0.3f)
        {
            EmitParticles(hitPoint, hitNormal, grid, world, blockColor);
        }

        if (currentMiningTime >= timeToBreak)
        {
            Color32 brokenColor;
            Vector3 blockCenter;

            if (world.BreakVoxel(hitPoint, hitNormal, out brokenColor, out blockCenter))
            {
                SpawnBurstParticles(blockCenter, world.BlockSize, brokenColor);
                if (breakAudio != null) breakAudio.Play();
            }

            currentMiningTime = 0f;
            hasTarget = false;
        }
    }

    public override void OnStopUsing()
    {
        currentMiningTime = 0f;
        hasTarget = false;

        // Para o som do motor
        if (drillLoopAudio != null && drillLoopAudio.isPlaying)
        {
            drillLoopAudio.Stop();
        }
    }

    private void EmitParticles(Vector3 hitPoint, Vector3 normal, Vector3Int grid, VoxelWorld world, Color32 color)
    {
        Vector3 centerLocal = new Vector3(grid.x + 0.5f, grid.y + 0.5f, grid.z + 0.5f) * world.BlockSize;
        Vector3 centerWorld = world.transform.TransformPoint(centerLocal);

        float half = world.BlockSize * 0.5f;
        Vector3 offset = new Vector3(
            Random.Range(-half, half),
            Random.Range(-half, half),
            Random.Range(-half, half)
        );

        Vector3 randomDir = (normal + Random.insideUnitSphere * 0.8f).normalized;

        var emit = new ParticleSystem.EmitParams
        {
            position = centerWorld + offset,
            startColor = color,
            startSize = particleSize * Random.Range(0.7f, 1.3f),
            velocity = randomDir * particleSpeed * 0.6f + Random.insideUnitSphere * 0.5f,
            startLifetime = Random.Range(0.3f, 0.6f)
        };
        breakParticles.Emit(emit, 1);
    }

    private void SpawnBurstParticles(Vector3 center, float size, Color32 color)
    {
        if (breakParticles == null) return;
        float half = size * 0.5f;
        for (int i = 0; i < particlesPerBreak; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-half, half), Random.Range(-half, half), Random.Range(-half, half));
            Vector3 dir = offset.sqrMagnitude > 1e-6f ? offset.normalized : Random.onUnitSphere;
            var emit = new ParticleSystem.EmitParams
            {
                position = center + offset,
                startColor = color,
                startSize = particleSize * Random.Range(0.6f, 1.4f),
                velocity = (dir + Random.insideUnitSphere * 0.4f) * particleSpeed,
                startLifetime = Random.Range(0.4f, 0.9f)
            };
            breakParticles.Emit(emit, 1);
        }
    }
}