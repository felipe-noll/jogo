using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk : MonoBehaviour
{
    private VoxelWorld world;
    private Vector3Int coord;
    private MeshFilter mf;
    private MeshCollider mc;   // pode ser null — só visual neste caso
    private Mesh mesh;
    // Campos novos
    private Mesh colMesh;
    private static readonly List<Vector3> cVerts = new List<Vector3>(4096);
    private static readonly List<int> cTris = new List<int>(8192);
    private static bool[] cMask;

    public void RebuildCollider()
    {
        if (mc == null) return;

        int bs = VoxelWorld.CHUNK_SIZE;
        if (cMask == null) cMask = new bool[bs * bs];

        int ox = coord.x * bs, oy = coord.y * bs, oz = coord.z * bs;
        float s = world.BlockSize;

        cVerts.Clear();
        cTris.Clear();

        for (int d = 0; d < 6; d++)
        {
            int axis = d / 2;
            bool positive = (d % 2) == 0;
            int dir = positive ? 1 : -1;
            int nx = axis == 0 ? dir : 0;
            int ny = axis == 1 ? dir : 0;
            int nz = axis == 2 ? dir : 0;

            for (int k = 0; k < bs; k++)
            {
                bool any = false;

                // Máscara: voxel sólido com vizinho vazio (vizinho pode estar em outro chunk)
                for (int v = 0; v < bs; v++)
                    for (int u = 0; u < bs; u++)
                    {
                        int x, y, z;
                        if (axis == 0) { x = k; y = u; z = v; }
                        else if (axis == 1) { x = u; y = k; z = v; }
                        else { x = u; y = v; z = k; }

                        int wx = ox + x, wy = oy + y, wz = oz + z;
                        bool face = world.GetVoxel(wx, wy, wz) != 0 &&
                                    world.GetVoxel(wx + nx, wy + ny, wz + nz) == 0;

                        cMask[u + v * bs] = face;
                        any |= face;
                    }

                if (!any) continue;

                int plane = positive ? k + 1 : k;

                // Greedy merge
                for (int v = 0; v < bs; v++)
                    for (int u = 0; u < bs; u++)
                    {
                        if (!cMask[u + v * bs]) continue;

                        int w = 1;
                        while (u + w < bs && cMask[u + w + v * bs]) w++;

                        int h = 1;
                        bool grow = true;
                        while (grow && v + h < bs)
                        {
                            for (int i = 0; i < w; i++)
                                if (!cMask[u + i + (v + h) * bs]) { grow = false; break; }
                            if (grow) h++;
                        }

                        for (int dv = 0; dv < h; dv++)
                            for (int du = 0; du < w; du++)
                                cMask[u + du + (v + dv) * bs] = false;

                        EmitQuad(axis, plane, u, v, u + w, v + h, positive, s);
                    }
            }
        }

        if (cTris.Count == 0)
        {
            mc.sharedMesh = null;
            return;
        }

        if (colMesh == null)
        {
            colMesh = new Mesh { name = $"ChunkCol {coord}" };
            colMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            colMesh.MarkDynamic();
        }

        colMesh.Clear();
        colMesh.SetVertices(cVerts);
        colMesh.SetTriangles(cTris, 0);
        colMesh.RecalculateBounds();

        mc.sharedMesh = null;      // força o re-bake mesmo reutilizando a mesma Mesh
        mc.sharedMesh = colMesh;
    }

    private static Vector3 ToPos(int axis, int plane, int u, int v)
    {
        if (axis == 0) return new Vector3(plane, u, v);
        if (axis == 1) return new Vector3(u, plane, v);
        return new Vector3(u, v, plane);
    }

    private static void EmitQuad(int axis, int plane, int u0, int v0, int u1, int v1, bool positive, float s)
    {
        int i0 = cVerts.Count;
        cVerts.Add(ToPos(axis, plane, u0, v0) * s);
        cVerts.Add(ToPos(axis, plane, u1, v0) * s);
        cVerts.Add(ToPos(axis, plane, u1, v1) * s);
        cVerts.Add(ToPos(axis, plane, u0, v1) * s);

        // Mesmo winding que você já usava no EmitGreedyQuad
        bool flip = positive ? (axis == 1) : (axis != 1);
        if (!flip)
        {
            cTris.Add(i0); cTris.Add(i0 + 1); cTris.Add(i0 + 2);
            cTris.Add(i0); cTris.Add(i0 + 2); cTris.Add(i0 + 3);
        }
        else
        {
            cTris.Add(i0); cTris.Add(i0 + 2); cTris.Add(i0 + 1);
            cTris.Add(i0); cTris.Add(i0 + 3); cTris.Add(i0 + 2);
        }
    }
    // Públicas para o VoxelWorld reusar ao gerar a mesh de colisão global.
    public static readonly Vector3Int[] faceDirs =
    {
        new Vector3Int( 0,  1,  0), new Vector3Int( 0, -1,  0),
        new Vector3Int( 1,  0,  0), new Vector3Int(-1,  0,  0),
        new Vector3Int( 0,  0,  1), new Vector3Int( 0,  0, -1),
    };

    public static readonly int[][] faceCorners =
    {
        new[]{ 0,1,1,  1,1,1,  1,1,0,  0,1,0 }, // +Y
        new[]{ 0,0,0,  1,0,0,  1,0,1,  0,0,1 }, // -Y
        new[]{ 1,0,1,  1,1,1,  1,1,0,  1,0,0 }, // +X
        new[]{ 0,0,0,  0,1,0,  0,1,1,  0,0,1 }, // -X
        new[]{ 0,0,1,  1,0,1,  1,1,1,  0,1,1 }, // +Z
        new[]{ 1,0,0,  0,0,0,  0,1,0,  1,1,0 }, // -Z
    };

    public static readonly Vector3[] faceNormals =
    {
        Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    public void Init(VoxelWorld world, Vector3Int coord, MeshFilter mf, MeshCollider mc)
    {
        this.world = world;
        this.coord = coord;
        this.mf = mf;
        this.mc = mc;

        mesh = new Mesh { name = $"Chunk {coord}" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
    }

    public void Rebuild()
    {
        var verts = new List<Vector3>(2048);
        var normals = new List<Vector3>(2048);
        var colors = new List<Color32>(2048);
        var tris = new List<int>(4096);

        int bs = VoxelWorld.CHUNK_SIZE;
        int ox = coord.x * bs;
        int oy = coord.y * bs;
        int oz = coord.z * bs;
        float s = world.BlockSize;

        for (int z = 0; z < bs; z++)
            for (int y = 0; y < bs; y++)
                for (int x = 0; x < bs; x++)
                {
                    byte vox = world.GetVoxel(ox + x, oy + y, oz + z);
                    if (vox == 0) continue;

                    Color32 col = world.GetColor(vox);

                    for (int f = 0; f < 6; f++)
                    {
                        var n = faceDirs[f];
                        if (world.GetVoxel(ox + x + n.x, oy + y + n.y, oz + z + n.z) != 0) continue;

                        int start = verts.Count;
                        int[] c = faceCorners[f];

                        for (int k = 0; k < 4; k++)
                        {
                            verts.Add(new Vector3(x + c[k * 3], y + c[k * 3 + 1], z + c[k * 3 + 2]) * s);
                            normals.Add(faceNormals[f]);
                            colors.Add(col);
                        }

                        tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                        tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
                    }
                }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        // O collider agora é global (VoxelWorld). Aqui só cuidamos do visual.
        // A linha abaixo é um no-op se mc == null (nosso caso atual).
        if (mc != null)
        {
            mc.sharedMesh = (verts.Count == 0) ? null : mesh;
        }
    }
}