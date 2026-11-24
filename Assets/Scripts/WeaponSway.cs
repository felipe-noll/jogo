using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Configurações")]
    public float smooth = 8f;
    public float swayMultiplier = 2f;
    
    [Header("Movimento ao Andar (Bobbing)")]
    public float walkMultiplier = 0.015f;

    private Vector3 initialPos;
    private Quaternion initialRot; // Guarda a rotação que você definiu no Inspector

    void Start()
    {
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
    }

    void Update()
    {
        Mouse m = Mouse.current;
        if (m == null) return;

        Vector2 mouse = m.delta.ReadValue();
        
        // Calcula a rotação baseada no mouse
        Quaternion rotX = Quaternion.AngleAxis(-mouse.y * swayMultiplier, Vector3.right);
        Quaternion rotY = Quaternion.AngleAxis(mouse.x * swayMultiplier, Vector3.up);
        
        // Aplica a rotação do mouse EM CIMA da sua rotação inicial
        Quaternion targetRot = initialRot * rotX * rotY;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smooth);
        
        // Leve movimento de balanço ao andar
        Vector3 finalPos = initialPos + new Vector3(Mathf.Sin(Time.time * 10) * walkMultiplier, Mathf.Cos(Time.time * 5) * walkMultiplier, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPos, Time.deltaTime * smooth);
    }
}