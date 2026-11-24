using UnityEngine;

[CreateAssetMenu(menuName = "Dentista/Region Color Rules")]
public class RegionColorRules : ScriptableObject
{
    [System.Serializable]
    public class Rule
    {
        public VoxelRegion region;
        public Color rgb;
        [Tooltip("Distância Euclidiana RGB máxima (0-1). Use ~0.05 se as cores forem bem distintas.")]
        [Range(0.01f, 0.5f)] public float tolerance = 0.08f;
    }

    public Rule[] rules;

    public VoxelRegion Classify(Color c)
    {
        VoxelRegion best = VoxelRegion.Empty;
        float bestD2 = float.MaxValue;
        float bestTol2 = 0f;
        foreach (var r in rules)
        {
            float dr = c.r - r.rgb.r, dg = c.g - r.rgb.g, db = c.b - r.rgb.b;
            float d2 = dr * dr + dg * dg + db * db;
            float tol2 = r.tolerance * r.tolerance;
            if (d2 <= tol2 && d2 < bestD2) { bestD2 = d2; bestTol2 = tol2; best = r.region; }
        }
        return best;
    }
}