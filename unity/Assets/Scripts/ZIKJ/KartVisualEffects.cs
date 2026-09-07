using UnityEngine;

public class KartVisualEffects : MonoBehaviour
{
    Renderer infectedRing;
    Renderer immuneRing;
    Light statusLight;
    float spin;

    void Awake()
    {
        infectedRing = CreateRing("Infected Aura", new Color(1f, 0.04f, 0.08f), new Color(1f, 0f, 0.02f), 2.25f);
        immuneRing = CreateRing("Immunity Shield", new Color(0.08f, 1f, 0.42f), new Color(0.06f, 1f, 0.35f), 2.55f);

        var lightGo = new GameObject("Status Light");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        statusLight = lightGo.AddComponent<Light>();
        statusLight.type = LightType.Point;
        statusLight.range = 4.5f;
        statusLight.intensity = 0f;
        statusLight.shadows = LightShadows.None;

        SetState(false, false);
    }

    void Update()
    {
        spin += Time.deltaTime * 95f;

        if (infectedRing != null && infectedRing.enabled)
        {
            infectedRing.transform.localRotation = Quaternion.Euler(0f, spin, 0f);
            infectedRing.transform.localScale = PulseScale(2.25f, 0.16f, 5.5f);
        }

        if (immuneRing != null && immuneRing.enabled)
        {
            immuneRing.transform.localRotation = Quaternion.Euler(0f, -spin * 0.8f, 0f);
            immuneRing.transform.localScale = PulseScale(2.55f, 0.12f, 4.2f);
        }
    }

    public void SetState(bool infected, bool immune)
    {
        if (infectedRing != null)
            infectedRing.enabled = infected;

        if (immuneRing != null)
            immuneRing.enabled = immune;

        if (statusLight == null) return;

        if (infected)
        {
            statusLight.color = new Color(1f, 0.08f, 0.06f);
            statusLight.intensity = 2.6f;
        }
        else if (immune)
        {
            statusLight.color = new Color(0.08f, 1f, 0.38f);
            statusLight.intensity = 2.1f;
        }
        else
        {
            statusLight.intensity = 0f;
        }
    }

    Renderer CreateRing(string name, Color baseColor, Color glowColor, float radius)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        ring.transform.localScale = new Vector3(radius, 0.018f, radius);

        var collider = ring.GetComponent<Collider>();
        if (collider != null)
            UnityObjectUtil.Destroy(collider);

        var renderer = ring.GetComponent<Renderer>();
        renderer.material = RuntimeMaterials.Emissive(baseColor, glowColor, 2.8f);
        return renderer;
    }

    Vector3 PulseScale(float baseRadius, float amount, float speed)
    {
        float r = baseRadius + Mathf.Sin(Time.time * speed) * amount;
        return new Vector3(r, 0.018f, r);
    }
}
