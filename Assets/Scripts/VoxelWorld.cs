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

    [Header("Colisão")]
    [Tooltip("Intervalo mínimo entre reconstruções do MeshCollider global.")]
    [SerializeField] private float colliderRebuildInterval = 0.1f;

    public float BlockSize => blockSize * scaleMultiplier;

    private byte[] voxels;
    private Color32[] palette;
    private int sx, sy, sz;
    private VoxelChunk[] chunks;

    private MeshCollider globalCollider;
    private Mesh collisionMesh;
    private bool collisionDirty;
    private float nextColliderRebuild;

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
        foreach (var c in chunks) c.Rebuild();

        BuildGlobalCollider();
    }

    private void Update()
    {
        if (collisionDirty && Time.time >= nextColliderRebuild)
        {
            RebuildGlobalColliderMesh();
            collisionDirty = false;
            nextColliderRebuild = Time.time + colliderRebuildInterval;
        }
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

                    // Sem MeshCollider aqui — o collider é um só, no objeto pai.
                    var chunk = go.AddComponent<VoxelChunk>();
                    chunk.Init(this, new Vector3Int(x, y, z), mf, null);
                    chunks[x + y * cx + z * cx * cy] = chunk;
                }
    }

    // -------------------------------------------------------------------
    private void BuildGlobalCollider()
    {
        globalCollider = gameObject.AddComponent<MeshCollider>();
        globalCollider.convex = false;

        collisionMesh = new Mesh { name = "WorldCollision" };
        collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        collisionMesh.MarkDynamic();

        RebuildGlobalColliderMesh();
    }

    private void RebuildGlobalColliderMesh()
    {
        var verts = new List<Vector3>(8192);
        var tris = new List<int>(16384);
        var vertMap = new Dictionary<long, int>(8192);

        // Processa as 6 direções de face
        for (int d = 0; d < 6; d++)
        {
            int axis = d / 2;              // 0=X, 1=Y, 2=Z
            bool positive = (d % 2) == 0;
            int dirSign = positive ? 1 : -1;

            int axisSize = axis == 0 ? sx : (axis == 1 ? sy : sz);
            int uSize, vSize;
            if (axis == 0) { uSize = sy; vSize = sz; }
            else if (axis == 1) { uSize = sx; vSize = sz; }
            else { uSize = sx; vSize = sy; }

            bool[,] mask = new bool[uSize, vSize];

            // Para cada fatia perpendicular ao eixo
            for (int k = 0; k < axisSize; k++)
            {
                // 1. Constrói máscara de faces visíveis nesta fatia
                for (int u = 0; u < uSize; u++)
                    for (int v = 0; v < vSize; v++)
                    {
                        int x, y, z;
                        if (axis == 0) { x = k; y = u; z = v; }
                        else if (axis == 1) { x = u; y = k; z = v; }
                        else { x = u; y = v; z = k; }

                        if (GetVoxel(x, y, z) == 0) { mask[u, v] = false; continue; }

                        int nx = x, ny = y, nz = z;
                        if (axis == 0) nx += dirSign;
                        else if (axis == 1) ny += dirSign;
                        else nz += dirSign;

                        mask[u, v] = GetVoxel(nx, ny, nz) == 0;
                    }

                // 2. Greedy merge: agrupa retângulos de faces iguais
                for (int u = 0; u < uSize; u++)
                    for (int v = 0; v < vSize; v++)
                    {
                        if (!mask[u, v]) continue;

                        // Estica em U
                        int uEnd = u;
                        while (uEnd + 1 < uSize && mask[uEnd + 1, v]) uEnd++;

                        // Estica em V
                        int vEnd = v;
                        bool canExpand = true;
                        while (canExpand && vEnd + 1 < vSize)
                        {
                            for (int uu = u; uu <= uEnd; uu++)
                                if (!mask[uu, vEnd + 1]) { canExpand = false; break; }
                            if (canExpand) vEnd++;
                        }

                        // Marca como consumido
                        for (int uu = u; uu <= uEnd; uu++)
                            for (int vv = v; vv <= vEnd; vv++)
                                mask[uu, vv] = false;

                        // 3. Emite o quad
                        int plane = positive ? k + 1 : k;
                        EmitGreedyQuad(verts, tris, vertMap,
                                       axis, plane, u, v, uEnd + 1, vEnd + 1, positive);
                    }
            }
        }

        var newMesh = new Mesh { name = "WorldCollision" };
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.SetVertices(verts);
        newMesh.SetTriangles(tris, 0);
        newMesh.RecalculateBounds();

        globalCollider.sharedMesh = null;
        globalCollider.sharedMesh = newMesh;

        if (collisionMesh != null) Destroy(collisionMesh);
        collisionMesh = newMesh;
    }

    private void EmitGreedyQuad(List<Vector3> verts, List<int> tris, Dictionary<long, int> vertMap,
                                int axis, int plane, int u0, int v0, int u1, int v1, bool positive)
    {
        var p0 = ToXYZ(axis, plane, u0, v0);
        var p1 = ToXYZ(axis, plane, u1, v0);
        var p2 = ToXYZ(axis, plane, u1, v1);
        var p3 = ToXYZ(axis, plane, u0, v1);

        int i0 = GetOrAddVertex(verts, vertMap, p0);
        int i1 = GetOrAddVertex(verts, vertMap, p1);
        int i2 = GetOrAddVertex(verts, vertMap, p2);
        int i3 = GetOrAddVertex(verts, vertMap, p3);

        // Correção de winding por eixo (garante normais outward)
        bool flip = positive ? (axis == 1) : (axis != 1);

        if (!flip)
        {
            tris.Add(i0); tris.Add(i1); tris.Add(i2);
            tris.Add(i0); tris.Add(i2); tris.Add(i3);
        }
        else
        {
            tris.Add(i0); tris.Add(i2); tris.Add(i1);
            tris.Add(i0); tris.Add(i3); tris.Add(i2);
        }
    }

    private (int, int, int) ToXYZ(int axis, int plane, int u, int v)
    {
        if (axis == 0) return (plane, u, v);
        if (axis == 1) return (u, plane, v);
        return (u, v, plane);
    }

    private int GetOrAddVertex(List<Vector3> verts, Dictionary<long, int> map, (int x, int y, int z) p)
    {
        long key = ((long)(p.x + 1048576) & 0x1FFFFF)
                 | (((long)(p.y + 1048576) & 0x1FFFFF) << 21)
                 | (((long)(p.z + 1048576) & 0x1FFFFF) << 42);

        if (map.TryGetValue(key, out int existing)) return existing;

        int idx = verts.Count;
        verts.Add(new Vector3(p.x, p.y, p.z) * BlockSize);
        map[key] = idx;
        return idx;
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
    public int Drill(Vector3 worldPoint, float worldRadius)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        Vector3 grid = local / BlockSize;

        int r = Mathf.CeilToInt(worldRadius / BlockSize);
        int cx = Mathf.FloorToInt(grid.x);
        int cy = Mathf.FloorToInt(grid.y);
        int cz = Mathf.FloorToInt(grid.z);
        float rSqr = worldRadius * worldRadius;

        var dirty = new HashSet<VoxelChunk>();
        int removed = 0;

        for (int z = cz - r; z <= cz + r; z++)
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (!InBounds(x, y, z)) continue;

                    Vector3 center = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * BlockSize;
                    if ((center - local).sqrMagnitude > rSqr) continue;

                    if (SetVoxel(x, y, z, 0))
                    {
                        removed++;
                        MarkChunksAround(x, y, z, dirty);
                    }
                }

        if (removed > 0) collisionDirty = true;

        foreach (var c in dirty) c.Rebuild();
        return removed;
    }
    /// <summary>
    /// Remove UM único voxel: o que está sob o ponto de impacto.
    /// Use com raycast — passa <paramref name="worldPoint"/> = hit.point e
    /// <paramref name="worldNormal"/> = hit.normal.
    /// </summary>
    /// <summary>
    /// Remove UM único voxel sob o ponto de impacto e devolve a cor dele.
    /// </summary>
    /// <summary>
    /// Remove UM único voxel sob o ponto de impacto.
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

        var dirty = new HashSet<VoxelChunk>();
        MarkChunksAround(x, y, z, dirty);
        foreach (var c in dirty) c.Rebuild();

        collisionDirty = true;
        return true;
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
    /// <summary>Reconstrói o collider global IMEDIATAMENTE (sem esperar o próximo Update).</summary>
    public void ForceRebuildCollider()
    {
        if (collisionDirty)
        {
            RebuildGlobalColliderMesh();
            collisionDirty = false;
            nextColliderRebuild = Time.time + colliderRebuildInterval;
        }
    }
}