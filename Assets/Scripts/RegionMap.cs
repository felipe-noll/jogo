using UnityEngine;

public class RegionMap
{
    public byte[] Grid;
    public Vector3Int Size;

    public RegionMap(Vector3Int size)
    {
        Size = size;
        Grid = new byte[size.x * size.y * size.z];
    }

    int Idx(Vector3Int p) => p.x + p.y * Size.x + p.z * Size.x * Size.y;

    public bool InBounds(Vector3Int p) =>
        p.x >= 0 && p.y >= 0 && p.z >= 0 &&
        p.x < Size.x && p.y < Size.y && p.z < Size.z;

    public VoxelRegion Get(Vector3Int p) =>
        InBounds(p) ? (VoxelRegion)Grid[Idx(p)] : VoxelRegion.Empty;

    public void Set(Vector3Int p, VoxelRegion r)
    {
        if (InBounds(p)) Grid[Idx(p)] = (byte)r;
    }
}