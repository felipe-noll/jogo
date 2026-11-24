using UnityEngine;

public static class GameSessionData
{
    public static PaymentReport LastReport;
    public static float PlayerMoney = 0f;
    public static string NextLevelScene = "5";

    // --- VARIÁVEIS DE UPGRADES ---
    public static bool DrillUpgraded = false;
    public static bool CuretteUpgraded = false;
    public static bool HasAnnihilatorTool = false;
    public static bool IsInCavity = false;

    public static void ProcessPayment(PaymentReport report)
    {
        LastReport = report;
        PlayerMoney += report.FinalPayment;
    }
    public static void ResetData()
    {
        PlayerMoney = 0f;
        LastReport = null;
        NextLevelScene = "Level_2";

        DrillUpgraded = false;
        CuretteUpgraded = false;
        HasAnnihilatorTool = false;
        IsInCavity = false;

        Debug.Log("Dados do jogo resetados (Novo Jogo).");
    }
}