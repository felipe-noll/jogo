using UnityEngine;

[CreateAssetMenu(menuName = "Dentista/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identidade")]
    public string patientName = "Paciente 01";

    [Header("Recompensas por tarefa concluída (R$)")]
    public float valueTartar = 2.5f;
    public float valueCavity = 2.0f;

    [Header("Penalidades por tarefa esquecida (R$)")]
    public float penaltyMissedTartar = 2.5f;
    public float penaltyMissedCavity = 2.0f;

    [Header("Penalidades de dano (R$ por voxel)")]
    public float pTongue = 15f;
    public float pGum = 8f;
    public float pUnrepairedTooth = 1f;
    public float pExcessMassa = 2f;
    public float pTartarBroken = 1.5f;
}