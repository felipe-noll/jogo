using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configurações")]
    [Tooltip("O quanto o botão vai aumentar. 1.1 significa 10% maior.")]
    public float hoverScaleMultiplier = 1.1f;
    [Tooltip("A velocidade da animação (quanto maior, mais rápido).")]
    public float animationSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Interpola o tamanho atual para o tamanho alvo suavemente
        // Usa unscaledDeltaTime para funcionar mesmo se o jogo estiver pausado
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse entrou no botão
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Mouse saiu do botão
        targetScale = originalScale;
    }
}