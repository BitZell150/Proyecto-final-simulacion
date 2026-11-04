using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>Pruebas del Nivel 1: gravedad invertida, disparo a la diana, puente y atajos.</summary>
public class Nivel1Tests : NivelBot
{
    private const string Diana = "Nivel/Mecanismos/Diana";

    [UnityTest]
    public IEnumerator RutaCompleta_LlegaALaPuerta()
    {
        yield return Load("Nivel1");

        // 1-2. Pasillo y escalones hasta el mirador
        yield return WalkTo(5.85f);
        yield return JumpTo(7.3f);
        Assert.Greater(Pos.y, 3.5f, "No subió al escalón");
        yield return JumpTo(9.2f);
        Assert.Greater(Pos.y, 4.6f, "No subió al mirador");
        yield return WalkTo(12.4f);

        // 3. Invertir la gravedad bajo la columna del nicho
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        Assert.IsFalse(Dead, "Murió al invertir la gravedad en el nicho");
        Assert.IsTrue(Inverted, "El jugador no se volteó");
        Assert.Greater(Pos.y, 5.5f, "No quedó de pie en el techo del nicho");

        // 4. |g| = 0.75·g0 y disparar
        yield return Tap(Key.Q);
        Assert.AreEqual(0.75f * 9.81f, Physics2D.gravity.y, 0.01f, "La magnitud no bajó a 0.75·g0");
        yield return Tap(Key.F);
        yield return WaitUntilOrTimeout(() => TargetHit(Diana), 3f);
        Assert.IsTrue(TargetHit(Diana), "La flecha no dio en la diana");

        // 5. Puente, volver a g normal y cruzar
        yield return new WaitForSeconds(1.8f);
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        Assert.IsFalse(Inverted, "No volvió a la gravedad normal");
        yield return WalkTo(25.5f, 6f);
        yield return WaitUntilOrTimeout(() => ActiveScene == "Nivel2", 3f);
        Assert.AreEqual("Nivel2", ActiveScene, "No cruzó el puente hasta la puerta");
    }

    [UnityTest]
    public IEnumerator Disparo_GravedadNormal_NoAcierta()
    {
        yield return Load("Nivel1");
        yield return Teleport(12.4f, 4.2f);
        yield return Tap(Key.F);
        yield return new WaitForSeconds(2.5f);
        Assert.IsFalse(TargetHit(Diana), "Con g normal la flecha no debería llegar a la diana");
    }

    [UnityTest]
    public IEnumerator Disparo_InvertidaConG0DesdeElCentro_NoAcierta()
    {
        yield return Load("Nivel1");
        yield return Teleport(12.4f, 4.2f);
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        yield return Tap(Key.F);
        yield return new WaitForSeconds(2.5f);
        Assert.IsFalse(TargetHit(Diana), "Con |g| = g0 desde el centro del nicho no debería acertar");
    }

    [UnityTest]
    public IEnumerator Atajo_SaltarElFosoConGMinima_Imposible()
    {
        yield return Load("Nivel1");
        yield return Teleport(12.9f, 4.2f);
        yield return TapTimes(Key.Q, 2);
        Assert.AreEqual(-0.5f * 9.81f, Physics2D.gravity.y, 0.01f);
        yield return JumpTo(24f, 3f);
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(Dead, "Saltó el foso sin el puente");
    }

    [UnityTest]
    public IEnumerator Pasillo_TechoSeguro()
    {
        yield return Load("Nivel1");
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.5f);
        Assert.IsFalse(Dead, "El techo del pasillo de práctica debería ser seguro");
        Assert.Greater(Pos.y, 3f);
    }

    [UnityTest]
    public IEnumerator Pozo_TechoConPinchosMata()
    {
        yield return Load("Nivel1");
        yield return Teleport(9.5f, 4.2f);
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.5f);
        Assert.IsTrue(Dead, "Invertir bajo el pozo debería matar");
    }

    [UnityTest]
    public IEnumerator Gravedad_NoCambiaEnElAire()
    {
        yield return Load("Nivel1");
        yield return Tap(Key.Space);
        yield return new WaitForSeconds(0.1f);
        yield return Tap(Key.G);
        Assert.Less(Physics2D.gravity.y, 0f, "La gravedad cambió en el aire");
    }
}
