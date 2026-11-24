using UnityEngine;

public class PolishTool : ToolBase
{
    [Header("Configurações da Massa")]
    public Color32 fillingColor = new Color32(255, 255, 255, 255);

    [Header("Efeitos")]
    public ParticleSystem polishParticles;
    public AudioSource polishAudio;

    public override void Use(Vector3 hitPoint, Vector3 hitNormal, VoxelWorld world)
    {
        bool placed = world.PlaceVoxel(hitPoint, hitNormal, fillingColor);

        if (placed)
        {
            world.ForceRebuildCollider();

            CharacterController controller = Camera.main.transform.root.GetComponent<CharacterController>();
            if (controller != null)
                PushPlayerOutOfVoxels(controller, world);

            if (polishParticles != null)
            {
                polishParticles.transform.position = hitPoint + hitNormal * 0.1f;
                polishParticles.Play();
            }

            if (polishAudio != null)
                polishAudio.Play();
        }
    }

    private void PushPlayerOutOfVoxels(CharacterController controller, VoxelWorld world)
    {
        if (!IsPlayerInsideVoxel(controller, world)) return;

        Vector3 originalPos = controller.transform.position;
        controller.enabled = false;

        float step = Mathf.Max(world.BlockSize, 0.01f);
        int maxSteps = 30;

        for (int i = 1; i <= maxSteps; i++)
        {
            controller.transform.position = originalPos + Vector3.up * (step * i);
            if (!IsPlayerInsideVoxel(controller, world))
            {
                controller.enabled = true;
                return;
            }
        }

        // Não conseguiu sair (teto baixo demais?)
        controller.transform.position = originalPos;
        controller.enabled = true;
    }

    private bool IsPlayerInsideVoxel(CharacterController controller, VoxelWorld world)
    {
        // O centro REAL da cápsula em world space (transform.position está nos pés!)
        Vector3 worldPos = controller.transform.position;
        Vector3 capsuleCenter = worldPos + controller.transform.rotation * controller.center;

        float halfHeight = controller.height * 0.5f;
        float epsilon = world.BlockSize * 0.5f;

        // Amostra: logo acima dos pés, meio da cápsula, logo abaixo da cabeça
        Vector3 bottomSample = capsuleCenter + Vector3.down * (halfHeight - epsilon);
        Vector3 middleSample = capsuleCenter;
        Vector3 topSample = capsuleCenter + Vector3.up * (halfHeight - epsilon);

        return IsPointInsideSolid(bottomSample, world) ||
               IsPointInsideSolid(middleSample, world) ||
               IsPointInsideSolid(topSample, world);
    }

    private bool IsPointInsideSolid(Vector3 worldPos, VoxelWorld world)
    {
        Vector3 local = world.transform.InverseTransformPoint(worldPos) / world.BlockSize;
        int x = Mathf.FloorToInt(local.x);
        int y = Mathf.FloorToInt(local.y);
        int z = Mathf.FloorToInt(local.z);
        return world.GetVoxel(x, y, z) != 0;
    }
}