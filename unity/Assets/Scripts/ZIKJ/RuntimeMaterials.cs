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

    public static Material Emissive(Color baseColor, Color glowColor, float intensity = 1.6f)
    {
        Material material = Make(baseColor);
        material.EnableKeyword("_EMISSION");

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", glowColor * Mathf.Max(0f, intensity));

        return material;
    }
}
