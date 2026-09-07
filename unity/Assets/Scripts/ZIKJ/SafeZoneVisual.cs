using UnityEngine;

public class SafeZoneVisual : MonoBehaviour
{
    Transform ring;
    Transform crown;
    Light beaconLight;
    Vector3 ringBase;
    Vector3 crownBase;

    void Awake()
    {
        ring = transform.Find("Safe Zone Ring");
        crown = transform.Find("Safe Zone Crown");
        beaconLight = GetComponentInChildren<Light>();

        if (ring != null)
            ringBase = ring.localScale;

        if (crown != null)
            crownBase = crown.localPosition;
    }

    void Update()
    {
        float pulse = Mathf.Sin(Time.time * 5.2f) * 0.5f + 0.5f;

        if (ring != null)
        {
            ring.Rotate(Vector3.up, 80f * Time.deltaTime, Space.Self);
            float scale = 1f + pulse * 0.08f;
            ring.localScale = new Vector3(ringBase.x * scale, ringBase.y, ringBase.z * scale);
        }

        if (crown != null)
        {
            crown.Rotate(Vector3.up, -95f * Time.deltaTime, Space.Self);
            crown.localPosition = crownBase + Vector3.up * (pulse * 0.28f);
        }

        if (beaconLight != null)
            beaconLight.intensity = 2.2f + pulse * 1.2f;
    }
}
