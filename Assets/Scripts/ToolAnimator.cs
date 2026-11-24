using System.Collections;
using UnityEngine;

public class ToolAnimator : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Distância que a ferramenta avança (eixo Z).")]
    public float forwardDistance = 0.08f;
    [Tooltip("Distância que a ferramenta desce (eixo Y).")]
    public float downwardDistance = 0.04f;
    [Tooltip("Velocidade do movimento.")]
    public float speed = 15f;
    [Tooltip("Ângulo de inclinação ao usar (eixo X).")]
    public float tiltAngle = 15f;

    private Vector3 initialPos;
    private Quaternion initialRot;

    private Vector3 targetPos;
    private Quaternion targetRot;

    private Coroutine punchCoroutine;

    private void OnEnable()
    {
        // Usa OnEnable em vez de Awake para pegar a posição exata
        // DEPOIS que o ToolManager colocar a ferramenta na mão.
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;

        targetPos = initialPos;
        targetRot = initialRot;

        // Força a posição para evitar que ela "voe" no primeiro frame
        transform.localPosition = initialPos;
        transform.localRotation = initialRot;
    }

    private void Update()
    {
        // Move suavemente para o alvo (usado para o avanço contínuo da broca)
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }

    /// <summary>
    /// Avanço contínuo (Segurar botão da Broca).
    /// </summary>
    public void SetPushing(bool isPushing)
    {
        if (isPushing)
        {
            // Move para frente e inclina um pouco
            targetPos = initialPos + new Vector3(0, -downwardDistance, forwardDistance);
            targetRot = initialRot * Quaternion.Euler(tiltAngle, 0, 0);
        }
        else
        {
            // Volta ao normal
            targetPos = initialPos;
            targetRot = initialRot;
        }
    }

    /// <summary>
    /// Golpe rápido (Clique da Massa e Cureta).
    /// </summary>
    public void PlayPunch()
    {
        if (!gameObject.activeInHierarchy) return;

        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        Vector3 punchPos = initialPos + new Vector3(0, -downwardDistance * 1.5f, forwardDistance);
        Quaternion punchRot = initialRot * Quaternion.Euler(tiltAngle * 1.5f, 0, 0);

        // Vai pra frente rápido
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (speed * 2f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, punchPos, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, punchRot, t);
            yield return null;
        }

        // Volta um pouco mais devagar
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPos, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRot, t);
            yield return null;
        }

        transform.localPosition = initialPos;
        transform.localRotation = initialRot;
    }

    // Garante que a ferramenta volte ao normal se for desequipada no meio da animação
    private void OnDisable()
    {
        transform.localPosition = initialPos;
        transform.localRotation = initialRot;
        targetPos = initialPos;
        targetRot = initialRot;
    }
}