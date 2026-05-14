using UnityEngine;

public static class RuntimeMaterials
{
    public static Material Make(Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.color = color;
        return material;
    }
}
