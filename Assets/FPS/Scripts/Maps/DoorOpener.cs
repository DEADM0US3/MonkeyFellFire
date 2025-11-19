using UnityEngine;

public class DoorOpenerTrigger : MonoBehaviour
{
    [Header("Puerta a mover")]
    public Transform door;            // Objeto de puerta
    public float openHeight = 3f;     // Cuánto sube la puerta
    public float speed = 3f;          // Velocidad de apertura

    [Header("Colisión (opcional)")]
    public Collider doorCollider;     // Se desactiva cuando empieza a abrir

    private bool opened = false;
    private Vector3 initialPos;
    private Vector3 targetPos;

    void Start()
    {
        if (door != null)
        {
            initialPos = door.position;
            targetPos = initialPos + Vector3.up * openHeight;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !opened)
        {
            opened = true;

            if (doorCollider != null)
                doorCollider.enabled = false;

            Debug.Log("Puerta subiendo...");
        }
    }

    void Update()
    {
        if (opened && door != null)
        {
            // Smooth movimiento hacia arriba
            door.position = Vector3.Lerp(door.position, targetPos, Time.deltaTime * speed);
        }
    }
}
