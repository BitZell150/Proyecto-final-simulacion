using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la sala PruebasFisica: rozamiento de las cajas según su masa y la gravedad,
/// inversiones limitadas con recargas y proyectiles parabólicos del cañón.
/// </summary>
public class PruebasFisicaTests : NivelBot
{
    private const float G0 = 9.81f;
    private const float EscalaG = 5f;      // escala de gravedad del jugador y de las cajas
    private const float FuerzaMax = 250f;  // PlayerController2D.pushForce
    private const float VCaminar = 8f;     // PlayerController2D.moveSpeed
    private const float MuK = 0.4f;        // PushableBox.kineticFriction

    private IEnumerator Cargar(bool conCanon)
    {
        yield return Load("PruebasFisica");
        if (!conCanon) Find("Nivel/Peligros/Lanzador").gameObject.SetActive(false);
    }

    /// <summary>Velocidad de un cuerpo con física por código (PhysicsBody2D.Velocity), leída por reflexión.</summary>
    private static Vector2 Velocidad(Transform t)
    {
        var body = t.GetComponent("PhysicsBody2D");
        return (Vector2)body.GetType().GetProperty("Velocity").GetValue(body);
    }

    private static int UsosGravedad()
    {
        var grav = GameObject.Find("Gravedad").GetComponent("GravityController");
        return (int)grav.GetType().GetProperty("InversionsLeft").GetValue(grav);
    }

    [UnityTest]
    public IEnumerator Cajas_LaRapidezDependeDeLaMasa()
    {
        yield return Cargar(conCanon: false);
        var liviana = Find("Nivel/Mecanismos/Caja");
        var mediana = Find("Nivel/Mecanismos/CajaMediana");

        // Régimen estacionario: F_max·(1 − v/v_caminar) = μk·m·g  →  v = v_caminar·(1 − μk·m·g / F_max)
        // (las dos cajas se empujan hacia la izquierda, donde tienen sitio libre)
        yield return Teleport(6.5f, 1f);
        Hold(Key.A);
        yield return WaitUntilOrTimeout(() => Velocidad(liviana).x < -0.5f, 3f);
        yield return new WaitForSeconds(0.2f);
        float vLiviana = -Velocidad(liviana).x;
        Release(Key.A);
        float esperadaLiviana = VCaminar * (1f - MuK * 1f * G0 * EscalaG / FuerzaMax);
        Assert.AreEqual(esperadaLiviana, vLiviana, 0.4f, "Rapidez de la caja de 1 kg");

        // Al soltarla, el rozamiento cinético la frena hasta detenerla
        yield return new WaitForSeconds(0.8f);
        Assert.AreEqual(0f, Velocidad(liviana).x, 0.001f, "La caja de 1 kg no se detuvo por rozamiento");

        yield return Teleport(10.5f, 1f);
        Hold(Key.A);
        yield return WaitUntilOrTimeout(() => Velocidad(mediana).x < -0.5f, 3f);
        yield return new WaitForSeconds(0.5f); // tarda más en llegar a su rapidez final: τ = m·v_caminar / F_max
        float vMediana = -Velocidad(mediana).x;
        Release(Key.A);
        float esperadaMediana = VCaminar * (1f - MuK * 4f * G0 * EscalaG / FuerzaMax);
        Assert.AreEqual(esperadaMediana, vMediana, 0.4f, "Rapidez de la caja de 4 kg");
        Assert.Less(vMediana, vLiviana - 0.5f, "La caja de 4 kg debería ir más lenta que la de 1 kg");
    }

    [UnityTest]
    public IEnumerator CajaPesada_SoloSeMueveConMenosGravedad()
    {
        yield return Cargar(conCanon: false);
        var pesada = Find("Nivel/Mecanismos/CajaPesada");
        float x0 = pesada.position.x;

        // Con g0: μs·m·g = 0.5·15·49.05 = 368 N > 250 N → no arranca
        yield return Teleport(15f, 1f);
        Hold(Key.A);
        yield return new WaitForSeconds(1f);
        Release(Key.A);
        Assert.AreEqual(x0, pesada.position.x, 0.01f, "La caja de 15 kg no debería moverse con g0");
        Assert.Greater(Pos.x, x0 + 1.2f, "El jugador atravesó la caja pesada");

        // Con |g| = 0.5·g0 la normal baja a la mitad: 184 N < 250 N → arranca
        yield return TapTimes(Key.Q, 2);
        Hold(Key.A);
        yield return new WaitForSeconds(0.6f);
        Release(Key.A);
        Assert.Less(pesada.position.x, x0 - 0.5f, "La caja de 15 kg debería moverse con |g| = 0.5·g0");
    }

    [UnityTest]
    public IEnumerator Gravedad_TresInversionesYRecarga()
    {
        yield return Cargar(conCanon: false);
        Assert.AreEqual(3, UsosGravedad());

        // 1ª inversión: al techo (seguro)
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.2f);
        Assert.IsTrue(Inverted, "No se invirtió la gravedad");
        Assert.IsFalse(Dead, "El techo debería ser seguro");
        Assert.Greater(Pos.y, 7.5f, "No cayó al techo");
        Assert.AreEqual(2, UsosGravedad());

        // La recarga del techo devuelve un uso
        yield return Teleport(10.5f, 9f - 1.63f - 0.04f);
        Assert.AreEqual(3, UsosGravedad(), "La recarga no devolvió el uso");

        // Gastar las 3: abajo, arriba, abajo
        for (int i = 0; i < 3; i++)
        {
            yield return Tap(Key.G);
            yield return new WaitForSeconds(1.2f);
        }
        Assert.AreEqual(0, UsosGravedad());
        Assert.IsFalse(Inverted, "Tras 4 inversiones la gravedad debería ser normal");

        // Sin usos ya no se invierte
        yield return Tap(Key.G);
        yield return new WaitForSeconds(0.5f);
        Assert.IsFalse(Inverted, "Se invirtió sin usos");
        Assert.Less(Pos.y, 2.5f);
    }

    [UnityTest]
    public IEnumerator Canon_ElProyectilSigueLaParabola()
    {
        yield return Cargar(conCanon: true);
        // El jugador (x = 2) está fuera de alcance: el cañón tira con su ángulo fijo

        GameObject proyectil = null;
        yield return WaitUntilOrTimeout(() => (proyectil = GameObject.Find("Proyectil(Clone)")) != null, 4f);
        Assert.IsNotNull(proyectil, "El cañón no disparó");

        var comp = proyectil.GetComponent("Projectile");
        const BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;
        var posField = comp.GetType().GetField("currentPosition", f);
        var velProp = comp.GetType().GetProperty("Velocity");

        yield return new WaitForFixedUpdate();
        Vector2 p0 = (Vector2)posField.GetValue(comp);
        Vector2 v0 = (Vector2)velProp.GetValue(comp);
        float t0 = Time.fixedTime;
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForFixedUpdate();
        Assert.IsTrue(proyectil != null, "El proyectil desapareció antes de tiempo");
        float t = Time.fixedTime - t0;
        Vector2 p = (Vector2)posField.GetValue(comp);

        // r(t) = r0 + v0·t + ½·g·t²  (el método de Euler se aparta ≈ ½·g·Δt·t, unos centímetros)
        Vector2 ideal = p0 + v0 * t + 0.5f * t * t * Physics2D.gravity;
        Assert.Less(Vector2.Distance(p, ideal), 0.1f, $"El proyectil no sigue la parábola (t = {t:0.00} s)");
    }

    [UnityTest]
    public IEnumerator Canon_ApuntaYMataAlJugador()
    {
        yield return Cargar(conCanon: true);
        yield return Teleport(15.5f, 1f); // dentro del alcance (≈ 14.7 u)
        yield return WaitUntilOrTimeout(() => Dead, 6f);
        Assert.IsTrue(Dead, "El cañón no alcanzó a un jugador quieto dentro de su alcance");
    }
}
