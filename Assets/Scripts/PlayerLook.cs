using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;

    [Tooltip("Sensibilidade horizontal (yaw). 0.1 a 0.3 é típico.")]
    public float xSensitivity = 0.15f;

    [Tooltip("Sensibilidade vertical (pitch). Mesmo valor do horizontal para 1:1.")]
    public float ySensitivity = 0.15f;

    public void ProcessLook(Vector2 input)
    {
        // O Input System já entrega o delta do mouse deste frame.
        // Não multiplicar por Time.deltaTime.
        xRotation -= input.y * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * (input.x * xSensitivity));
    }
}