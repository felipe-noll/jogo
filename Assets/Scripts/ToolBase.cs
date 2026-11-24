using UnityEngine;
public abstract class ToolBase : MonoBehaviour
{
    public float cooldown = 0.15f;

    public abstract void Use(Vector3 hitPoint, Vector3 hitNormal, VoxelWorld world);

    // Novo método: chamado quando o jogador solta o botão
    public virtual void OnStopUsing() { }

    public virtual void OnEquip() { gameObject.SetActive(true); }
    public virtual void OnUnequip() { gameObject.SetActive(false); }
}