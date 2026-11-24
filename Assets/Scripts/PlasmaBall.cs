using UnityEngine;

public class PlasmaBall : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 30f;
    public float lifetime = 5f;
    public GameObject explosionVFX; // Partículas da explosão (Opcional)

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Move a bola para frente
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            Debug.LogError("A PlasmaBall precisa de um Rigidbody!");
        }

        Destroy(gameObject, lifetime); // Destrói se não bater em nada
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignora se bater no próprio jogador
        if (collision.gameObject.CompareTag("Player")) return;

        // 1. Cria a explosão visual
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // 2. Avisa o gerenciador de fim de jogo para começar a sequência
        EndGameManager endManager = FindObjectOfType<EndGameManager>();
        if (endManager != null)
        {
            endManager.StartEndingSequence();
        }

        // 3. Destrói a bola
        Destroy(gameObject);
    }
}