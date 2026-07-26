using System.Collections;
using UnityEngine;
public interface IArrowEffect
{
    void OnHit(Weapon weapon, GameObject hitEntity, Vector3 hitPoint, bool isServer, Arrow arrow)
    {
        if (hitEntity.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(arrow.Damage, isServer);
        }
    }
}
public class DefaultArrowEffect : IArrowEffect { }

public class Arrow : MonoBehaviour
{
    public GameObject MyRend;
    public Transform ColliderPosition;
    public float ColliderRadius;
    internal float Damage;

    private Weapon Source;
    private Transform Root;
    private Vector3 Velocity;
    private float PassedTime;
    private bool IsServer;
    private bool Initialized;
    private bool Hit;
    private IArrowEffect Effect;
    private static readonly IArrowEffect DefaultEffect = new DefaultArrowEffect();

    private const float CATCH_UP_RATE = 0.08f;

    public void Initialize(Weapon source, Vector3 direction, float speed, float passedTime, float damage, bool isServer, Transform root, IArrowEffect effect)
    {
        Hit = false;
        Source = source;
        Velocity = direction.normalized * speed;
        PassedTime = passedTime;
        Damage = damage;
        IsServer = isServer;
        Initialized = true;
        Root = root;
        Effect = effect;
        if (isServer)
            MyRend.SetActive(false);
        else
            MyRend.SetActive(true);
    }
    private void Update()
    {
        if (!Initialized) return;
        if(Hit)
        {
            return;
        }
        Move(Time.deltaTime);
    }
    private void Move(float delta)
    {
        Velocity += Physics.gravity * delta;

        float passedTimeDelta = 0f;
        if (PassedTime > 0f)
        {
            float step = PassedTime * CATCH_UP_RATE;
            PassedTime -= step;
            if (PassedTime <= delta * 0.5f)
            {
                step += PassedTime;
                PassedTime = 0f;
            }
            passedTimeDelta = step;
        }

        float totalDelta = delta + passedTimeDelta;
        Vector3 movement = Velocity * totalDelta;

        // Cast along the movement vector to catch fast tunnelling
        if (!Hit && Physics.SphereCast(transform.position, ColliderRadius, movement.normalized, out RaycastHit hit, movement.magnitude))
        {
            CheckHit(hit.collider, hit.point);
        }

        transform.position += movement;

        if (Velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(Velocity);
    }

    private void CheckHit(Collider other, Vector3 hitPoint)
    {
        if (Hit) return;
        if (!IsValidHit(other)) return;

        var effectToUse = Effect ?? DefaultEffect;
        transform.SetParent(other.transform);
        effectToUse.OnHit(Source, other.gameObject, hitPoint, IsServer, this);

        Hit = true;
        StartCoroutine(ReturnToPool(other));
    }
    private bool IsValidHit(Collider col)
    {
        if (col == null) return false;
        if (col.transform == transform.root) return false;
        if (col.transform == Root) return false;
        return true;
    }
    private IEnumerator ReturnToPool(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            ArrowPoolManager.Instance.Return(this);
            yield break;
        }

        yield return new WaitForSeconds(1f);

        MyRend.SetActive(false);
        Initialized = false;
        ArrowPoolManager.Instance.Return(this);
    }
}
