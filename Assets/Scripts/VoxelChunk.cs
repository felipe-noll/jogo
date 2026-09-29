using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk : MonoBehaviour
{
    private VoxelWorld world;
    private Vector3Int coord;
    private MeshFilter mf;
    private MeshCollider mc;   // pode ser null — só visual neste caso
    private Mesh mesh;

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