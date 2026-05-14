using UnityEngine;

// Attach to the SafeZone prefab alongside a SphereCollider (Is Trigger = true)
[RequireComponent(typeof(SphereCollider))]
public class SafeZone : MonoBehaviour
{
    InfectedMode mode;
    float        spawnTime;
    float        lifetime;

    public void Init(InfectedMode m, float radius, float life)
    {
        mode      = m;
        lifetime  = life;
        spawnTime = Time.time;

        var col      = GetComponent<SphereCollider>();
        col.radius   = radius;
        col.isTrigger = true;
    }

    void Update()
    {
        if (Time.time - spawnTime >= lifetime)
        {
            mode.RemoveZone(gameObject);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var k = other.GetComponent<KartController>();
        if (k != null) mode.GrantImmunity(k.kartIndex);
    }
}
