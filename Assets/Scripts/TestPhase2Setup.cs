using UnityEngine;

public class TestPhase2Setup : MonoBehaviour
{
    [Header("Configurações de Teste")]
    public bool giveAnnihilator = true;
    public bool teleportToCavity = true;
    public Transform spawnPointCavity; // Arraste o Trigger aqui

    void Start()
    {
        // 1. Desbloqueia a aniquiladora e a equipa
        if (giveAnnihilator)
        {
            GameSessionData.HasAnnihilatorTool = true;

            // Acha o ToolManager no Player e força a aniquiladora
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                ToolManager tm = playerObj.GetComponent<ToolManager>();
                if (tm != null)
                {
                    tm.ForceEquipAnnihilator();
                }
            }
        }

        // 2. Teleporta o jogador para cima do trigger (pra ele cair dentro)
        if (teleportToCavity && spawnPointCavity != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Coloca o player 2 metros acima do trigger para ele cair
                playerObj.transform.position = spawnPointCavity.position + new Vector3(0, 2f, 0);
            }
        }
    }
}