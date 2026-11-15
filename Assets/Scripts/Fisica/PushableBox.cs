using UnityEngine;

// Caja que el jugador puede empujar, con rozamiento seco (modelo de Coulomb) calculado por código:
//   N = m·|g|                    fuerza normal sobre el suelo (o el techo, con la gravedad invertida)
//   En reposo: no se mueve mientras |F| <= μs·N (el rozamiento estático iguala a la fuerza aplicada)
//   Deslizando: rozamiento cinético f = -μk·N·signo(v)
//   a = (F + f) / m              (la integra PhysicsBody2D junto con la gravedad)
// Como N depende de |g|, con menos gravedad la caja roza menos, y una caja muy pesada no arranca.
// Se ejecuta antes que el jugador para que este vea en el mismo paso cuánto se movió la caja.
[DefaultExecutionOrder(-20)]
[RequireComponent(typeof(PhysicsBody2D))]
public class PushableBox : MonoBehaviour
{
    // Por debajo de esta rapidez la caja se considera en reposo
    private const float RestSpeed = 0.01f;

    [Header("Rozamiento con el suelo")]
    [SerializeField, Range(0f, 1.5f)] private float staticFriction = 0.5f;  // μs
    [SerializeField, Range(0f, 1.5f)] private float kineticFriction = 0.4f; // μk

    private PhysicsBody2D body;
    private float appliedForce;

    public PhysicsBody2D Body => body;

    // Fuerza horizontal mínima (N) para que la caja empiece a moverse estando apoyada: μs·m·|g|
    public float BreakawayForce => staticFriction * body.Mass * body.GravityAcceleration.magnitude;

    private void Awake()
    {
        body = GetComponent<PhysicsBody2D>();
        if (body == null) body = gameObject.AddComponent<PhysicsBody2D>();
    }

    // Suma una fuerza horizontal (N; positiva hacia la derecha) que actuará en el próximo paso
    public void Push(float force)
    {
        appliedForce += force;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float force = appliedForce;
        appliedForce = 0f;

        if (body.IsGrounded)
        {
            Vector2 velocity = body.Velocity;
            float normal = body.Mass * body.GravityAcceleration.magnitude;
            float maxStatic = staticFriction * normal;
            float kinetic = kineticFriction * normal;

            if (Mathf.Abs(velocity.x) < RestSpeed)
            {
                if (Mathf.Abs(force) <= maxStatic)
                {
                    // El rozamiento estático equilibra la fuerza: sigue en reposo
                    velocity.x = 0f;
                    force = 0f;
                }
                else
                {
                    // Se vence el rozamiento estático: empieza a deslizar con rozamiento cinético
                    force -= Mathf.Sign(force) * kinetic;
                }
            }
            else
            {
                float friction = -Mathf.Sign(velocity.x) * kinetic;

                // El rozamiento frena pero no invierte el movimiento: si en este paso la velocidad
                // cambiaría de signo, la caja se detiene
                float nextVelocity = velocity.x + (force + friction) / body.Mass * dt;
                if (Mathf.Sign(nextVelocity) != Mathf.Sign(velocity.x) && Mathf.Abs(force) <= maxStatic)
                {
                    velocity.x = 0f;
                    force = 0f;
                }
                else
                {
                    force += friction;
                }
            }

            body.Velocity = velocity;
        }

        body.AddForce(new Vector2(force, 0f));
        body.Step(dt);
    }
}
