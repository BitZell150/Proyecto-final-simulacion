using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>Pruebas del Tutorial: la ruta prevista se completa y los atajos están bloqueados.</summary>
public class TutorialTests : NivelBot
{
    [UnityTest]
    public IEnumerator RutaCompleta_LlegaALaPuerta()
    {
        yield return Load("Tutorial");

        // 1. Foso pequeño de pinchos
        yield return JumpTo(6.8f);
        Assert.IsFalse(Dead, "Murió saltando el primer foso");

        // 2. Empujar la caja contra el muro
        var caja = Find("Nivel/Mecanismos/Caja");
        Hold(Key.D);
        yield return WaitUntilOrTimeout(() => caja.position.x >= 12.4f, 5f);
        Release(Key.D);
        Assert.GreaterOrEqual(caja.position.x, 12.4f, "La caja no llegó al muro");

        // Subir a la caja y a la meseta
        yield return JumpTo(12.4f);
        Assert.Greater(Pos.y, 2.5f, "No subió a la caja");
        yield return JumpTo(14.0f);
        Assert.Greater(Pos.y, 3.6f, "No subió a la meseta desde la caja");

        // 3. Bloque colgante: esperar en el borde a que se aleje
        yield return WalkTo(15.85f);
        Assert.IsFalse(Dead, "Murió caminando bajo el bloque colgante");
        var bloque = Find("Nivel/Peligros/BloqueColgante").gameObject;
        yield return WaitUntilOrTimeout(() => SafeToJump(Pos.x, 17.2f, 3.1f, new[] { bloque }, 0.75f, 5.1f, 6.3f), 5f);
        Debug.Log($"[Test] salto al escalón desde x={Pos.x:0.00}, y={Pos.y:0.00}; bloque en {bloque.transform.position.x:0.00}");
        yield return JumpTo(17.2f);
        Assert.IsFalse(Dead, "Murió saltando al escalón de la palanca");
        Assert.Greater(Pos.y, 4.6f, "No llegó al escalón de la palanca");

        // 4. Palanca
        yield return WalkTo(19.2f);
        Assert.IsTrue(DoorOpen(), "La palanca no abrió la puerta");

        // 5. Saltos hasta la puerta
        yield return JumpTo(20.95f);
        Assert.IsFalse(Dead, "Murió saltando a la plataforma 1");
        yield return JumpTo(22.5f);
        Assert.IsFalse(Dead, "Murió saltando a la plataforma 2");
        yield return JumpTo(24.4f);
        yield return WaitUntilOrTimeout(() => ActiveScene == "Nivel1", 3f);
        Assert.AreEqual("Nivel1", ActiveScene, "No terminó el nivel al cruzar la puerta");
    }

    [UnityTest]
    public IEnumerator Atajo_MesetaSinCaja_Imposible()
    {
        yield return Load("Tutorial");
        yield return Teleport(11.5f, 1f);
        float maxY = 0f;
        Hold(Key.D);
        for (int i = 0; i < 6; i++)
        {
            yield return Tap(Key.Space);
            float t = 0f;
            while (t < 0.5f) { maxY = Mathf.Max(maxY, Pos.y); t += Time.deltaTime; yield return null; }
        }
        Release(Key.D);
        Assert.Less(maxY, 3.1f + FeetToCenter, "Subió a la meseta sin la caja");
    }

    [UnityTest]
    public IEnumerator Atajo_SaltoDirectoAlRellano_Imposible()
    {
        yield return Load("Tutorial");
        yield return Teleport(19.2f, 4.2f);
        yield return JumpTo(24.4f);
        Assert.IsTrue(Dead || Pos.y < 5.8f, "Llegó al rellano de la puerta de un solo salto");
    }

    [UnityTest]
    public IEnumerator Puerta_CerradaSinPalanca()
    {
        yield return Load("Tutorial");
        yield return Teleport(24.4f, 5.8f);
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("Tutorial", ActiveScene, "La puerta dejó pasar sin la palanca");
    }

    [UnityTest]
    public IEnumerator BloqueColgante_Mata()
    {
        yield return Load("Tutorial");
        var bloque = Find("Nivel/Peligros/BloqueColgante");
        yield return Teleport(15.0f, 3.1f);
        Assert.IsFalse(Dead, "De pie bajo el bloque no debería morir");
        yield return WaitUntilOrTimeout(() => Mathf.Abs(bloque.position.x - 15.0f) < 0.3f, 4f);
        yield return Tap(Key.Space);
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(Dead, "Saltar bajo el bloque con pinchos no mató al jugador");
    }
}
