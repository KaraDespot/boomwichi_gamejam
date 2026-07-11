using UnityEngine;

/// <summary>
/// Простой снаряд: летит вперёд и уничтожается при столкновении или по достижении дальности.
/// Пока просто логирует попадания.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Скорость полёта снаряда (единиц в секунду).")]
    [SerializeField]
    private float speed = 20f;

    [Tooltip("Максимальная дистанция, после которой снаряд уничтожается.")]
    [SerializeField]
    private float maxDistance = 20f;

    [Tooltip("Урон, который этот снаряд должен нанести при попадании.")]
    [SerializeField]
    private float damage = 10f;

    [Tooltip("Слои, по которым может быть нанесён урон.")]
    [SerializeField]
    private LayerMask hitLayers;

    /// <summary>
    /// Настраивает снаряд перед полётом. Вызывается оружием при создании снаряда.
    /// </summary>
    public void Setup(float damage, float maxDistance, float speed, LayerMask hitLayers)
    {
        this.damage = damage;
        this.maxDistance = maxDistance;
        this.speed = speed;
        this.hitLayers = hitLayers;
    }

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Движемся вперёд по локальному forward
        transform.position += transform.forward * (speed * Time.deltaTime);

        // Проверяем пройденную дистанцию
        float traveled = Vector3.Distance(_startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверка: слой объекта есть в маске hitLayers?
        // hitLayers.value  - целое число, где каждый бит = один слой (LayerMask)
        // other.gameObject.layer - номер слоя (0..31) у объекта, в который мы врезались
        // (1 << other.gameObject.layer) - двигаем 1 влево на номер слоя и получаем маску "только этот слой"
        // Операция & (один амперсанд) — это ПОБИТОВОЕ И, оно оставляет только те биты,
        // которые одновременно =1 и в hitLayers.value, и в (1 << layer).
        // Если результат == 0, значит ни один бит не совпал — этот слой НЕ входит в маску, выходим.
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        Debug.Log($"Снаряд попал в {other.name}, потенциальный урон: {damage}");

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage);

        Destroy(gameObject);
    }
}
