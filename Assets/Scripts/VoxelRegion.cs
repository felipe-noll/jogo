// VoxelRegion.cs
public enum VoxelRegion : byte
{
    Empty = 0,
    Tooth = 1, // dente saudável
    Cavity = 2, // cárie (sólido, deve ser removido com broca e preenchido com massa)
    Tartar = 3, // tártaro (sólido, deve ser limpo com cureta)
    Tongue = 4,
    Gum = 5
}