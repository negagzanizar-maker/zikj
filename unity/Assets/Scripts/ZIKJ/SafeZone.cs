using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SafeZone : MonoBehaviour
{
    InfectedMode mode;
    float spawnTime;
    float lifetime;

    public void Init(InfectedMode infectedMode, float radius, float life)
    {
        mode = infectedMode;
        lifetime = Mathf.Max(0.5f, life);
        spawnTime = Time.time;

        var col = GetComponent<SphereCollider>();
        col.radius = radius;
        col.isTrigger = true;
    }

    void Update()
    {
        if (Time.time - spawnTime < lifetime) return;

        mode?.RemoveZone(gameObject);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        var kart = other.GetComponentInParent<KartController>();
        if (kart != null)
            mode?.GrantImmunity(kart.kartIndex);
    }
}
