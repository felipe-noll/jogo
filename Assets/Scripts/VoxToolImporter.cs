using System.Collections.Generic;
using UnityEngine;
using VoxReader.Interfaces;

public class VoxToolImporter : MonoBehaviour
{
    [Header("Fonte")]
    [Tooltip("Renomeie o arquivo .vox para .bytes e arraste aqui.")]
    [SerializeField] private TextAsset voxFile;

    [Header("Configurações")]
    [Tooltip("Tamanho de cada bloco em unidades do mundo.")]
    [SerializeField] private float blockSize = 0.05f;
    [SerializeField] private Material blockMaterial;

    private static readonly Vector3Int[] faceDirs =
    {
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    };

    private static readonly int[][] faceCorners =
    {
        new[]{0,1,1, 1,1,1, 1,1,0, 0,1,0},
        new[]{0,0,0, 1,0,0, 1,0,1, 0,0,1},
        new[]{1,0,1, 1,1,1, 1,1,0, 1,0,0},
        new[]{0,0,0, 0,1,0, 0,1,1, 0,0,1},
        new[]{0,0,1, 1,0,1, 1,1,1, 0,1,1},
        new[]{1,0,0, 0,0,0, 0,1,0, 1,1,0}
    };

    private void Awake()
    {
        if (voxFile == null) { Debug.LogError("Vox file not assigned.", this); return; }

        // Salva os bytes em um arquivo temporário para que o VoxReader possa lê-lo
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "tool_temp.vox");
        System.IO.File.WriteAllBytes(tempPath, voxFile.bytes);

        IVoxFile file = VoxReader.VoxReader.Read(tempPath);
        if (file.Models == null || file.Models.Length == 0) return;

        var colorOf = new Dictionary<long, Color32>();

        // 1. Coleta todos os voxels com suas cores
        foreach (var model in file.Models)
        {
            foreach (var v in model.Voxels)
            {
                int x = Mathf.RoundToInt(v.GlobalPosition.X);
                int y = Mathf.RoundToInt(v.GlobalPosition.Z); // Swap Y/Z para Unity
                int z = Mathf.RoundToInt(v.GlobalPosition.Y);

                long key = Pack(x, y, z);
                if (!colorOf.ContainsKey(key))
                {
                    colorOf[key] = new Color32(v.Color.R, v.Color.G, v.Color.B, v.Color.A);
                }
            }
        }

        if (colorOf.Count == 0) return;

        // 2. Constrói a mesh
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color32>();
        var tris = new List<int>();

        foreach (var kvp in colorOf)
        {
            Vector3Int pos = Unpack(kvp.Key);
            Color32 col = kvp.Value;

            for (int f = 0; f < 6; f++)
            {
                var n = faceDirs[f];
                // Pula faces internas (que estão em contato com outro voxel)
                if (colorOf.ContainsKey(Pack(pos.x + n.x, pos.y + n.y, pos.z + n.z)))
                    continue;

                int start = verts.Count;
                int[] c = faceCorners[f];

                for (int k = 0; k < 4; k++)
                {
                    verts.Add(new Vector3(pos.x + c[k * 3], pos.y + c[k * 3 + 1], pos.z + c[k * 3 + 2]) * blockSize);
                    normals.Add(n);
                    colors.Add(col);
                }

                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }
        }

        // 3. Aplica na MeshFilter
        var mesh = new Mesh { name = "ToolMesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        if (blockMaterial != null) GetComponent<MeshRenderer>().sharedMaterial = blockMaterial;
    }

    private static long Pack(int x, int y, int z)
    {
        long lx = (long)(x + 1048576) & 0x1FFFFF;
        long ly = (long)(y + 1048576) & 0x1FFFFF;
        long lz = (long)(z + 1048576) & 0x1FFFFF;
        return lx | (ly << 21) | (lz << 42);
    }

    private static Vector3Int Unpack(long key)
    {
        int x = (int)(key & 0x1FFFFF) - 1048576;
        int y = (int)((key >> 21) & 0x1FFFFF) - 1048576;
        int z = (int)((key >> 42) & 0x1FFFFF) - 1048576;
        return new Vector3Int(x, y, z);
    }
}