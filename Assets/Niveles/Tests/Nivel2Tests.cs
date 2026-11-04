using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>Pruebas del Nivel 2 (examen): cada tramo por separado, más los atajos.</summary>
public class Nivel2Tests : NivelBot
{
    private const string Caja = "Nivel/Mecanismos/Caja";

    [UnityTest]
    public IEnumerator Tramo1_PilaresConBloquesEnContrafase()
    {
        yield return Load("Nivel2");
        var bloques = new[]
        {
            Find("Nivel/Peligros/BloqueColganteA").gameObject,
            Find("Nivel/Peligros/BloqueColganteB").gameObject,
        };
        float[] paradas = { 1.1f, 3.68f, 6.02f, 8.6f };
        yield return WalkTo(paradas[0]);
        for (int i = 0; i < paradas.Length - 1; i++)
        {
            int j = i;
            yield return WaitUntilOrTimeout(() => SafeToJump(Pos.x, paradas[j + 1], 1f, bloques, 0.25f, 3.2f, 4.4f), 4f);
            Debug.Log($"[Test] salto {i + 1} desde x={Pos.x:0.00}; bloques en {bloques[0].transform.position.x:0.00} / {bloques[1].transform.position.x:0.00}");
            yield return JumpTo(paradas[i + 1]);
            Assert.IsFalse(Dead, $"Murió en el salto {i + 1} de los pilares");
        }
        Assert.Greater(Pos.x, 8f, "No llegó a la cámara de la caja");
    }

    [UnityTest]
    public IEnumerator Tramo2y3_CajaPorElTechoYSubidaAlMirador()
    {
        yield return Load("Nivel2");
        var caja = Find(Caja);
        yield return Teleport(8.6f, 1f);

        // Invertir: la caja y el jugador caen al techo seguro
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.2f);
        Assert.IsFalse(Dead, "El techo de la cámara debería ser seguro");
        Assert.Greater(caja.position.y, 5.5f, "La caja no cayó al techo");

        // Empujar la caja por el techo hasta el tabique
        Hold(Key.D);
        yield return WaitUntilOrTimeout(() => caja.position.x >= 13.4f, 5f);
        yield return new WaitForSeconds(0.2f);
        Release(Key.D);
        Assert.GreaterOrEqual(caja.position.x, 13.4f, "La caja no llegó al tabique");

        // Recuperar g normal: la caja y el jugador caen al otro lado del foso
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.2f);
        Assert.IsFalse(Dead, "Murió al volver a g normal");
        Assert.Less(caja.position.y, 2f, "La caja no cayó al suelo");
        Assert.Greater(caja.position.x, 12.2f, "La caja cayó al foso");

        // Empujar la caja contra el mirador (pasa bajo el tabique)
        Hold(Key.D);
        yield return WaitUntilOrTimeout(() => caja.position.x >= 15.95f, 5f);
        Release(Key.D);
        Assert.IsFalse(Dead, "Murió empujando la caja bajo el tabique");
        Assert.GreaterOrEqual(caja.position.x, 15.95f, "La caja no llegó al mirador");

        // |g| = 0.5·g0, subir a la caja y saltar al mirador
        yield return TapTimes(Key.Q, 2);
        yield return JumpTo(16.0f);
        Assert.IsFalse(Dead, "Murió subiendo a la caja");
        yield return JumpTo(17.4f);
        Assert.IsFalse(Dead, "Murió saltando al mirador");
        Assert.Greater(Pos.y, 4.4f + 0.5f, "No llegó al mirador");
    }

    [UnityTest]
    public IEnumerator Tramo4a6_DosDianasPatioBovedaYPuerta()
    {
        yield return Load("Nivel2");
        yield return Teleport(18.3f, 4.4f);

        // g normal, 0.75·g0 → diana del patio
        yield return Tap(Key.Q);
        yield return Tap(Key.F);
        yield return WaitUntilOrTimeout(() => TargetHit("Nivel/Mecanismos/DianaPatio"), 3f);
        Assert.IsTrue(TargetHit("Nivel/Mecanismos/DianaPatio"), "No acertó la diana del patio");
        Assert.IsFalse(DoorOpen(), "La puerta no debería abrirse con una sola diana");

        // g invertida → diana bajo la losa
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        Assert.IsFalse(Dead, "El techo del nicho debería ser seguro");
        yield return Tap(Key.F);
        yield return WaitUntilOrTimeout(() => TargetHit("Nivel/Mecanismos/DianaLosa"), 3f);
        Assert.IsTrue(TargetHit("Nivel/Mecanismos/DianaLosa"), "No acertó la diana de la losa");
        Assert.IsTrue(DoorOpen(), "Las dos dianas no abrieron la puerta");

        // Bajar al patio, invertir hacia la bóveda, caminar y caer sobre la losa
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        yield return JumpTo(21.6f);
        Assert.IsFalse(Dead, "Murió bajando al patio");
        yield return WalkTo(21.6f);
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1.2f);
        Assert.IsFalse(Dead, "La bóveda debería ser segura");
        Assert.Greater(Pos.y, 11f, "No subió a la bóveda");
        yield return WalkTo(24.6f);
        yield return Tap(Key.G);
        yield return WaitUntilOrTimeout(() => ActiveScene == "MainMenu", 4f);
        Assert.AreEqual("MainMenu", ActiveScene, "No terminó el nivel en la puerta");
    }

    [UnityTest]
    public IEnumerator Atajo_PatioSinDianas_Reinicia()
    {
        yield return Load("Nivel2");
        yield return Teleport(18.3f, 4.4f);
        yield return JumpTo(21.6f);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(Dead, "Bajar al patio sin las dos dianas debería reiniciar");
    }

    [UnityTest]
    public IEnumerator Atajo_MiradorSinCaja_Imposible()
    {
        yield return Load("Nivel2");
        yield return Teleport(15.6f, 1f);
        yield return TapTimes(Key.Q, 2);
        yield return JumpTo(17.4f);
        Assert.IsTrue(Dead || Pos.y < 4.4f, "Subió al mirador sin la caja");
    }

    [UnityTest]
    public IEnumerator Atajo_SaltoInvertidoSobreElTabique_Imposible()
    {
        yield return Load("Nivel2");
        yield return Teleport(12.6f, 1f);
        yield return Tap(Key.G);
        yield return new WaitForSeconds(1f);
        Assert.IsFalse(Dead);
        yield return TapTimes(Key.Q, 2);
        yield return JumpTo(18f, 2.5f);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(Dead || Pos.x < 14.3f, "Cruzó el tabique con un salto invertido");
    }

    [UnityTest]
    public IEnumerator CajaAlFoso_Reinicia()
    {
        yield return Load("Nivel2");
        var caja = Find(Caja);
        yield return Teleport(8.6f, 1f);
        Hold(Key.D);
        yield return new WaitForSeconds(1.2f);
        Release(Key.D); // si se sigue empujando, el rozamiento con la pared del foso sostiene la caja
        yield return new WaitForSeconds(1.5f);
        Assert.IsTrue(Dead, $"La caja en el foso debería reiniciar el nivel (caja en {(caja != null ? caja.position : Vector3.zero)}, jugador en {Pos})");
    }

    [UnityTest]
    public IEnumerator Atajo_PilaresCorriendo_Mata()
    {
        yield return Load("Nivel2");
        Hold(Key.D);
        yield return WaitUntilOrTimeout(() => Dead || Pos.x > 8.2f, 3f);
        Release(Key.D);
        Assert.IsTrue(Dead, $"Cruzó los pilares corriendo sin saltar (debería caer al vacío) (x = {Pos.x:0.00})");
    }

    [UnityTest]
    public IEnumerator Pilares_SaltoConGMenor_Mata()
    {
        yield return Load("Nivel2");
        yield return Tap(Key.Q); // 0.75·g0: el salto llega a los pinchos del techo
        yield return Tap(Key.Space);
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(Dead, "Con |g| = 0.75·g0 el salto debería tocar el techo de pinchos");
    }
}
