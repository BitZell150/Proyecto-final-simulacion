using UnityEditor;

/// <summary>Reconstruye los tres niveles. Menú: Niveles > Construir todos.</summary>
public static class ConstruirTodo
{
    [MenuItem("Niveles/Construir todos")]
    public static void Construir()
    {
        TutorialBuilder.Construir();
        Nivel1Builder.Construir();
        Nivel2Builder.Construir();
    }
}
