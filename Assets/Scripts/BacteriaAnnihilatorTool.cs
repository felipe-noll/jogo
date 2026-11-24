using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BacteriaAnnihilatorTool : ToolBase
{
    [Header("Tiro de Plasma")]
    public GameObject plasmaBallPrefab;
    public Transform firePoint; // Um objeto vazio na ponta da arma
    public float fireCooldown = 1f;

    private float nextFireTime = 0f;

    public override void Use(Vector3 hitPoint, Vector3 hitNormal, VoxelWorld world)
    {
        if (Time.time < nextFireTime) return;

        Debug.Log("Tentando atirar!"); // <-- ADICIONE AQUI

        if (plasmaBallPrefab != null && firePoint != null)
        {
            Instantiate(plasmaBallPrefab, firePoint.position, firePoint.rotation);
            Debug.Log("Bala instanciada!"); // <-- ADICIONE AQUI
            nextFireTime = Time.time + fireCooldown;
        }
        else
        {
            Debug.LogWarning("Faltando Prefab ou FirePoint no Inspector!"); // <-- ADICIONE AQUI
        }
    }
}