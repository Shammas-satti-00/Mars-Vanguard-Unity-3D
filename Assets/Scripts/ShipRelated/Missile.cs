using UnityEngine;

public class Missile : MonoBehaviour
{
    [HideInInspector] public int damage;
    [HideInInspector] public int speed = 20;
    [HideInInspector] public float lifetime = 10f;
    [HideInInspector] public float rotationSpeed = 120f;

    public Transform target;
    public string targetTag;
    public GameObject impactEffectPrefab;

    float lifeTimer;

    void Start()
    {
        lifeTimer = lifetime;
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            float step = rotationSpeed * Mathf.Deg2Rad * Time.deltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, dir, step, 0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        transform.position += transform.forward * speed * Time.deltaTime;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("Enviroment"))
            SpawnImpact(c.contacts[0].point);

        if (c.gameObject.CompareTag(targetTag))
        {
            var handler = c.gameObject.GetComponentInParent<DamageHandler>();
            if (handler) handler.TakeDamage(damage);

            SpawnImpact(c.contacts[0].point);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enviroment"))
            SpawnImpact(transform.position);

        if (other.CompareTag(targetTag))
        {
            var handler = other.GetComponentInParent<DamageHandler>();
            if (handler) handler.TakeDamage(damage);

            SpawnImpact(transform.position);
            Destroy(gameObject);
        }
    }

    void SpawnImpact(Vector3 pos)
    {
        if (impactEffectPrefab)
        {
            GameObject fx = Instantiate(impactEffectPrefab, pos, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}
