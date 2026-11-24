using UnityEngine;

public static class PaymentCalculator
{
    public static PaymentReport Calculate(PatientSession s, LevelData d)
    {
        var r = new PaymentReport();

        // Tarefas feitas
        r.cavitiesFixed = s.FilledCavitiesCount;
        r.tartarCleaned = s.CleanedTartarCount;

        // Tarefas esquecidas (Total - Feitas)
        r.cavitiesMissed = Mathf.Max(0, s.CavityTotalCount - r.cavitiesFixed);
        r.tartarMissed = Mathf.Max(0, s.TartarTotalCount - r.tartarCleaned);

        // Erros
        r.tongueDamaged = s.TongueDamageCount;
        r.gumDamaged = s.GumDamageCount;
        r.excessMassa = s.ExcessMassaCount;
        r.tartarBroken = s.TartarBrokenCount;

        int unrepaired = 0;
        foreach (var kv in s.OriginalRegion)
            if (kv.Value == VoxelRegion.Tooth) unrepaired++;
        r.unrepairedTeeth = unrepaired;

        // Valores
        r.valCavity = d.valueCavity;
        r.valTartar = d.valueTartar;
        r.penCavityMissed = d.penaltyMissedCavity;
        r.penTartarMissed = d.penaltyMissedTartar;
        r.penTongue = d.pTongue;
        r.penGum = d.pGum;
        r.penExcessMassa = d.pExcessMassa;
        r.penTartarBroken = d.pTartarBroken;
        r.penUnrepairedTooth = d.pUnrepairedTooth;

        return r;
    }
}