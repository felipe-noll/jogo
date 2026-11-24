using UnityEngine;

[System.Serializable]
public class PaymentReport
{
    // Quantidades
    public int cavitiesFixed;
    public int cavitiesMissed;
    public int tartarCleaned;
    public int tartarMissed;
    public int tongueDamaged;
    public int gumDamaged;
    public int excessMassa;
    public int tartarBroken;
    public int unrepairedTeeth;

    // Valores Unitários
    public float valCavity;
    public float valTartar;
    public float penCavityMissed;
    public float penTartarMissed;
    public float penTongue;
    public float penGum;
    public float penExcessMassa;
    public float penTartarBroken;
    public float penUnrepairedTooth;

    public float TotalReward => (cavitiesFixed * valCavity) + (tartarCleaned * valTartar);

    public float TotalPenalty => (cavitiesMissed * penCavityMissed) + (tartarMissed * penTartarMissed) +
                                 (tongueDamaged * penTongue) + (gumDamaged * penGum) +
                                 (excessMassa * penExcessMassa) + (tartarBroken * penTartarBroken) +
                                 (unrepairedTeeth * penUnrepairedTooth);

    public float FinalPayment => TotalReward - TotalPenalty;
}