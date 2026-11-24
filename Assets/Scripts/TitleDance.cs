using UnityEngine;

public class TitleDance : MonoBehaviour
{
    [Header("Configurações do Efeito")]
    [Tooltip("O quanto ele vai subir e descer (em pixels ou metros).")]
    public float floatAmplitude = 15f;

    [Tooltip("A velocidade da flutuação.")]
    public float floatSpeed = 2f;

    [Tooltip("O quanto ele vai inclinar para os lados (em graus).")]
    public float rotationAmplitude = 3f;

    [Tooltip("A velocidade da inclinação.")]
    public float rotationSpeed = 1.5f;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        // Guarda a posição e rotação iniciais para saber onde ele deve "dançar"
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void Update()
    {
        // Movimento de "sobe e desce" (Eixo Y) usando uma onda senoidal
        float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);

        // Movimento de "zigue-zague" (Rotação no Eixo Z)
        float newRotZ = Mathf.Sin(Time.time * rotationSpeed) * rotationAmplitude;

        // Aplica o movimento
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        transform.localRotation = startRot * Quaternion.Euler(0, 0, newRotZ);
    }
}