using UnityEngine;

// Zona que hace reaparecer al jugador si la toca (el vacío fuera del nivel, pinchos...).
// El área es la del BoxCollider2D (que queda como trigger para no frenar a nadie) y el solapamiento
// con la caja del jugador se comprueba a mano en cada paso de física.
[RequireComponent(typeof(BoxCollider2D))]
public class KillZone : MonoBehaviour
{
    private BoxCollider2D area;
    private PlayerController2D player;

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
        player = FindAnyObjectByType<PlayerController2D>();
    }

    private void FixedUpdate()
    {
        if (player == null || player.IsDead) return;

        Bounds bounds = area.bounds;
        if (PhysicsWorld.BoxesOverlap(bounds.center, bounds.size, player.Body.WorldCenter, player.Body.WorldSize))
        {
            player.Kill();
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);
    }
}
