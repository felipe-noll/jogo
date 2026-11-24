using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public const int CHUNK_SIZE = 16;

    [Header("Fonte")]
    [SerializeField] private TextAsset voxFile;

    [Header("Escala")]
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float scaleMultiplier = 1f;

    [Header("Render")]
    [SerializeField] private Material blockMaterial;

    [Header("Progresso")]
    [Tooltip("Regras RGB → VoxelRegion. Arraste o asset MouthColors aqui.")]
    [SerializeField] private RegionColorRules regionColorRules;

    public float BlockSize => blockSize * scaleMultiplier;

    private byte[] voxels;
    private Color32[] palette;
    private int sx, sy, sz;
    private VoxelChunk[] chunks;

    private int nextFreeColorIndex = -1;
    public event System.Action<Vector3Int, byte> OnVoxelBroken;        // pos, cor original
    public event System.Action<Vector3Int, byte> OnVoxelPlaced;        // pos, cor nova
    public event System.Action<Vector3Int, byte, byte> OnVoxelCleaned; // pos, cor antiga, nova
    public RegionMap Regions { get; private set; }

    private readonly HashSet<VoxelChunk> dirtyChunksCache = new HashSet<VoxelChunk>();
    private readonly HashSet<VoxelChunk> colliderDirty = new HashSet<VoxelChunk>();

    // -------------------------------------------------------------------
    private void Awake()
    {
        if (voxFile == null) { Debug.LogError("VoxelWorld: voxFile não atribuído.", this); enabled = false; return; }

        var model = VoxLoader.Load(voxFile.bytes);
        sx = model.sizeX;
        sy = model.sizeY;
        sz = model.sizeZ;
        voxels = model.voxels;
        palette = model.palette;

        Debug.Log($"[VoxelWorld] Modelo: {sx}×{sy}×{sz} = {sx * sy * sz} blocos");

        BuildChunks();
        foreach (var c in chunks) { c.Rebuild(); c.RebuildCollider(); }

        if (regionColorRules != null)
        {
            Regions = RegionMapBuilder.Build(
                voxels,
                new Vector3Int(sx, sy, sz),
                idx => palette[idx],       // Color32 → Color (conversão implícita)
                regionColorRules
            );

            int tartar = 0, cavity = 0;
            foreach (byte b in Regions.Grid)
            {
                if (b == (byte)VoxelRegion.Tartar) tartar++;
                else if (b == (byte)VoxelRegion.Cavity) cavity++;
            }
            Debug.Log($"[VoxelWorld] RegionMap pronta — Tártaro={tartar}, Cáries={cavity}");
        }
        else
        {
            Debug.LogWarning("[VoxelWorld] regionColorRules não atribuído — Regions ficará null e o PatientSession não funcionará.");
        }
    }

    private void Update() => FlushColliders();

    /// <summary>Reconstrói os colliders pendentes imediatamente (sem esperar o próximo Update).</summary>
    public void ForceRebuildCollider() => FlushColliders();

    private void FlushColliders()
    {
        if (colliderDirty.Count == 0) return;
        foreach (var c in colliderDirty) c.RebuildCollider();
        colliderDirty.Clear();
    }

    // -------------------------------------------------------------------
    private void BuildChunks()
    {
        int cx = Mathf.CeilToInt(sx / (float)CHUNK_SIZE);
        int cy = Mathf.CeilToInt(sy / (float)CHUNK_SIZE);
        int cz = Mathf.CeilToInt(sz / (float)CHUNK_SIZE);

        chunks = new VoxelChunk[cx * cy * cz];

        for (int z = 0; z < cz; z++)
            for (int y = 0; y < cy; y++)
                for (int x = 0; x < cx; x++)
                {
                    var go = new GameObject($"Chunk {x},{y},{z}");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y, z) * CHUNK_SIZE * BlockSize;
                    go.layer = gameObject.layer;

                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = blockMaterial;

                    // Um MeshCollider por chunk: só os chunks sujos são re-baked.
                    var mc = go.AddComponent<MeshCollider>();
                    mc.convex = false;

                    var chunk = go.AddComponent<VoxelChunk>();
                    chunk.Init(this, new Vector3Int(x, y, z), mf, mc);
                    chunks[x + y * cx + z * cx * cy] = chunk;
                }
    }

    // -------------------------------------------------------------------
    public bool InBounds(int x, int y, int z) => x >= 0 && x < sx && y >= 0 && y < sy && z >= 0 && z < sz;
    private int Index(int x, int y, int z) => x + y * sx + z * sx * sy;

    public byte GetVoxel(int x, int y, int z) => InBounds(x, y, z) ? voxels[Index(x, y, z)] : (byte)0;
    public Color32 GetColor(byte idx) => palette[idx];

    public bool SetVoxel(int x, int y, int z, byte value)
    {
        if (!InBounds(x, y, z)) return false;
        int i = Index(x, y, z);
        if (voxels[i] == value) return false;
        voxels[i] = value;
        return true;
    }

    // -------------------------------------------------------------------
    /// <summary>
    /// Remove UM único voxel sob o ponto de impacto.
    /// Use com raycast: worldPoint = hit.point, worldNormal = hit.normal.
    /// Retorna a cor dele e o centro do bloco em coordenadas de mundo.
    /// </summary>
    public bool BreakVoxel(Vector3 worldPoint, Vector3 worldNormal,
                           out Color32 brokenColor, out Vector3 blockCenterWorld)
    {
        brokenColor = new Color32(255, 255, 255, 255);
        blockCenterWorld = worldPoint;

        Vector3 inside = worldPoint - worldNormal * (BlockSize * 0.5f);
        Vector3 local = transform.InverseTransformPoint(inside);
        Vector3 grid = local / BlockSize;

        int x = Mathf.FloorToInt(grid.x);
        int y = Mathf.FloorToInt(grid.y);
        int z = Mathf.FloorToInt(grid.z);

        if (!InBounds(x, y, z)) return false;

        byte voxel = GetVoxel(x, y, z);
        if (voxel == 0) return false;

        brokenColor = GetColor(voxel);

        // Centro do bloco em espaço local → mundo
        Vector3 centerLocal = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * BlockSize;
        blockCenterWorld = transform.TransformPoint(centerLocal);

        SetVoxel(x, y, z, 0);

        OnVoxelBroken?.Invoke(new Vector3Int(x, y, z), voxel);

        RebuildAround(x, y, z, true);
        return true;
    }

    /// <summary>
    /// Adiciona um voxel na face atingida.
    /// </summary>
    public bool PlaceVoxel(Vector3 worldPoint, Vector3 worldNormal, Color32 color)
    {
        // Entra meio bloco "para fora" a partir da superfície atingida,
        // garantindo que estamos no voxel vazio adjacente correto.
        Vector3 outside = worldPoint + worldNormal * (BlockSize * 0.5f);
        Vector3 local = transform.InverseTransformPoint(outside);
        Vector3 grid = local / BlockSize;

        int x = Mathf.FloorToInt(grid.x);
        int y = Mathf.FloorToInt(grid.y);
        int z = Mathf.FloorToInt(grid.z);

        if (!InBounds(x, y, z)) return false;
        if (GetVoxel(x, y, z) != 0) return false;

        byte colorIndex = GetOrAddColorIndex(color);

        SetVoxel(x, y, z, colorIndex);

        OnVoxelPlaced?.Invoke(new Vector3Int(x, y, z), colorIndex);

        RebuildAround(x, y, z, true);
        return true;
    }

    /// <summary>
    /// Limpa gradualmente um voxel, interpolando a cor em direção à cor limpa.
    /// Só muda a cor (não altera a geometria), então não reconstrói o collider.
    /// </summary>
    public bool CleanVoxel(Vector3 worldPoint, Vector3 worldNormal, Color32 cleanColor, float cleanAmount, System.Func<Color32, bool> matchCondition)
    {
        Vector3 inside = worldPoint - worldNormal * (BlockSize * 0.5f);
        Vector3 local = transform.InverseTransformPoint(inside);
        Vector3 grid = local / BlockSize;

        int x = Mathf.FloorToInt(grid.x);
        int y = Mathf.FloorToInt(grid.y);
        int z = Mathf.FloorToInt(grid.z);

        if (!InBounds(x, y, z)) return false;

        byte voxel = GetVoxel(x, y, z);
        if (voxel == 0) return false;

        Color32 currentColor = GetColor(voxel);

        // Se não for tártaro (ou já estiver limpo), para aqui.
        if (!matchCondition(currentColor)) return false;

        // Interpola a cor atual em direção à cor limpa
        Color cCurrent = currentColor;
        Color cClean = cleanColor;
        Color blended = Color.Lerp(cCurrent, cClean, cleanAmount);

        // Se a diferença for muito pequena, "snapa" para a cor limpa exata
        float diffTotal = Mathf.Abs(blended.r - cClean.r) + Mathf.Abs(blended.g - cClean.g) + Mathf.Abs(blended.b - cClean.b);
        if (diffTotal < 0.05f) blended = cClean;

        byte newIndex = GetOrAddColorIndex(blended);
        if (newIndex == voxel) return false;

        SetVoxel(x, y, z, newIndex);

        // Só visual: o collider não muda.
        RebuildAround(x, y, z, false);

        // Só dispara o evento quando o voxel chega de fato ao estado "limpo".
        bool fullyCleaned = blended.r == cClean.r
                         && blended.g == cClean.g
                         && blended.b == cClean.b;

        if (fullyCleaned)
            OnVoxelCleaned?.Invoke(new Vector3Int(x, y, z), voxel, newIndex);

        return true;
    }

    /// <summary>
    /// Reconstrói a malha visual dos chunks afetados e, se pedido,
    /// marca seus colliders para reconstrução no próximo Update.
    /// </summary>
    private void RebuildAround(int x, int y, int z, bool geometryChanged)
    {
        dirtyChunksCache.Clear();
        MarkChunksAround(x, y, z, dirtyChunksCache);
        foreach (var c in dirtyChunksCache)
        {
            c.Rebuild();
            if (geometryChanged) colliderDirty.Add(c);
        }
    }

    // -------------------------------------------------------------------
    /// <summary>
    /// Procura uma cor na paleta. Se não existir, tenta adicionar no primeiro índice livre.
    /// Se a paleta estiver cheia (256 cores), reutiliza o índice 1.
    /// </summary>
    private byte GetOrAddColorIndex(Color32 targetColor)
    {
        // 1. Verifica se a cor já existe na paleta
        for (int i = 1; i < palette.Length; i++)
        {
            if (palette[i].r == targetColor.r && palette[i].g == targetColor.g && palette[i].b == targetColor.b)
                return (byte)i;
        }

        // 2. Se não existir, encontra o primeiro índice "livre"
        // (O VoxLoader preenche o resto da paleta com branco puro 255,255,255)
        if (nextFreeColorIndex == -1)
        {
            for (int i = 1; i < palette.Length; i++)
            {
                if (palette[i].r == 255 && palette[i].g == 255 && palette[i].b == 255 && palette[i].a == 255)
                {
                    nextFreeColorIndex = i;
                    break;
                }
            }

            // Se não achou branco, a paleta inteira está cheia.
            if (nextFreeColorIndex == -1) nextFreeColorIndex = palette.Length;
        }

        // 3. Se a paleta encheu, avisa e retorna o índice 1
        if (nextFreeColorIndex >= palette.Length)
        {
            Debug.LogWarning("Paleta cheia! Reutilizando cor.");
            return 1;
        }

        // 4. Adiciona a cor na posição livre
        palette[nextFreeColorIndex] = targetColor;
        nextFreeColorIndex++;

        return (byte)(nextFreeColorIndex - 1);
    }

    /// <summary>
    /// Retorna as coordenadas do grid do voxel atingido.
    /// </summary>
    public Vector3Int WorldToGrid(Vector3 worldPoint, Vector3 worldNormal)
    {
        Vector3 inside = worldPoint - worldNormal * (BlockSize * 0.5f);
        Vector3 local = transform.InverseTransformPoint(inside);
        Vector3 grid = local / BlockSize;
        return new Vector3Int(Mathf.FloorToInt(grid.x), Mathf.FloorToInt(grid.y), Mathf.FloorToInt(grid.z));
    }

    private void MarkChunksAround(int x, int y, int z, HashSet<VoxelChunk> set)
    {
        int cxc = Mathf.CeilToInt(sx / (float)CHUNK_SIZE);
        int cyc = Mathf.CeilToInt(sy / (float)CHUNK_SIZE);
        int czc = Mathf.CeilToInt(sz / (float)CHUNK_SIZE);

        int bx = x / CHUNK_SIZE, by = y / CHUNK_SIZE, bz = z / CHUNK_SIZE;
        int lx = x % CHUNK_SIZE, ly = y % CHUNK_SIZE, lz = z % CHUNK_SIZE;

        for (int dz = -1; dz <= 1; dz++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx != 0 && lx != 0 && lx != CHUNK_SIZE - 1) continue;
                    if (dy != 0 && ly != 0 && ly != CHUNK_SIZE - 1) continue;
                    if (dz != 0 && lz != 0 && lz != CHUNK_SIZE - 1) continue;

                    int nx = bx + dx, ny = by + dy, nz = bz + dz;
                    if (nx < 0 || nx >= cxc || ny < 0 || ny >= cyc || nz < 0 || nz >= czc) continue;

                    set.Add(chunks[nx + ny * cxc + nz * cxc * cyc]);
                }
    }

    private void OnDrawGizmosSelected()
    {
        if (Regions == null) return;
        var s = Regions.Size;
        float step = BlockSize * 2f;
        for (int x = 0; x < s.x; x += 2)
            for (int y = 0; y < s.y; y += 2)
                for (int z = 0; z < s.z; z += 2)
                {
                    var r = Regions.Get(new Vector3Int(x, y, z));
                    if (r == VoxelRegion.Empty) continue;

                    Gizmos.color = r switch
                    {
                        VoxelRegion.Tooth => Color.white,
                        VoxelRegion.Cavity => new Color(0.4f, 0.25f, 0.15f),
                        VoxelRegion.Tartar => Color.yellow,
                        VoxelRegion.Tongue => new Color(0.9f, 0.3f, 0.3f),
                        VoxelRegion.Gum => new Color(1f, 0.6f, 0.6f),
                        _ => Color.magenta
                    };
                    Vector3 worldCenter = transform.TransformPoint(
                        new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * BlockSize);
                    Gizmos.DrawCube(worldCenter, Vector3.one * step);
                }
    }
}