using System.Collections.Generic;
using UnityEngine;

public class PatientSession : MonoBehaviour
{
    public LevelData levelData;
    public VoxelWorld world;

    // Estado
    private HashSet<Vector3Int> needsRepair = new();
    private Dictionary<Vector3Int, VoxelRegion> originalRegion = new();
    private HashSet<Vector3Int> filledCavities = new();
    private HashSet<Vector3Int> cleanedTartar = new();

    private int tongueDamage;
    private int gumDamage;
    private int excessMassa;
    private int tartarBroken;

    private int tartarTotal;
    private int cavityTotal;

    public bool SessionEnded { get; private set; }

    // --- Propriedades públicas lidas pelo PaymentCalculator ---
    public int CleanedTartarCount => cleanedTartar.Count;
    public int FilledCavitiesCount => filledCavities.Count;
    public int TartarTotalCount => tartarTotal;
    public int CavityTotalCount => cavityTotal;
    public int TongueDamageCount => tongueDamage;
    public int GumDamageCount => gumDamage;
    public int ExcessMassaCount => excessMassa;
    public int TartarBrokenCount => tartarBroken;
    public IReadOnlyDictionary<Vector3Int, VoxelRegion> OriginalRegion => originalRegion;

    void Start()
    {
        world.OnVoxelBroken += HandleBreak;
        world.OnVoxelPlaced += HandlePlace;
        world.OnVoxelCleaned += HandleClean;
        CountInitialTargets();
    }

    void OnDestroy()
    {
        if (world == null) return;
        world.OnVoxelBroken -= HandleBreak;
        world.OnVoxelPlaced -= HandlePlace;
        world.OnVoxelCleaned -= HandleClean;
    }

    void CountInitialTargets()
    {
        var s = world.Regions.Size;
        for (int x = 0; x < s.x; x++)
            for (int y = 0; y < s.y; y++)
                for (int z = 0; z < s.z; z++)
                {
                    var r = world.Regions.Get(new Vector3Int(x, y, z));
                    if (r == VoxelRegion.Tartar) tartarTotal++;
                    else if (r == VoxelRegion.Cavity) cavityTotal++;
                }
        Debug.Log($"[PatientSession] Tártaro={tartarTotal}, Cáries={cavityTotal}");
    }

    void HandleBreak(Vector3Int pos, byte _)
    {
        if (SessionEnded) return;
        var r = world.Regions.Get(pos);

        switch (r)
        {
            case VoxelRegion.Tooth:
            case VoxelRegion.Cavity:
            case VoxelRegion.Tongue:
            case VoxelRegion.Gum:
                if (needsRepair.Add(pos))
                    originalRegion[pos] = r;
                if (r == VoxelRegion.Cavity) filledCavities.Remove(pos);
                if (r == VoxelRegion.Tongue) tongueDamage++;
                if (r == VoxelRegion.Gum) gumDamage++;
                break;

            case VoxelRegion.Tartar:
                cleanedTartar.Remove(pos);
                tartarBroken++;
                break;
        }
    }

    void HandlePlace(Vector3Int pos, byte _)
    {
        if (SessionEnded) return;

        if (needsRepair.Contains(pos))
        {
            var original = originalRegion[pos];
            needsRepair.Remove(pos);
            originalRegion.Remove(pos);

            if (original == VoxelRegion.Cavity)
                filledCavities.Add(pos);
        }
        else
        {
            excessMassa++;
        }
    }

    void HandleClean(Vector3Int pos, byte _, byte __)
    {
        if (SessionEnded) return;
        if (world.Regions.Get(pos) == VoxelRegion.Tartar)
            cleanedTartar.Add(pos);
    }

    public void FinishAndShowReport(System.Action<PaymentReport> onReport)
    {
        if (SessionEnded) return;
        SessionEnded = true;
        var report = PaymentCalculator.Calculate(this, levelData);
        onReport?.Invoke(report);
    }
}