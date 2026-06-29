using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public MeshRenderer MyRend;
    public Transform ColliderPosition;
    public float ColliderRadius;
    public LayerMask HitLayer;

    private Transform Root;
    private Vector3 Velocity;
    private float PassedTime;
    private float Damage;
    private bool IsAuthority;
    private bool IsServer;
    private bool Initialized;
    private bool Hit;

    private const float CATCH_UP_RATE = 0.08f;

    public void Initialize(Vector3 direction, float speed, float passedTime, float damage, bool isAuthority, bool isServer, Transform root)
    {
        Hit = false;
        Velocity = direction.normalized * speed;
        PassedTime = passedTime;
        Damage = damage;
        IsAuthority = isAuthority;
        IsServer = isServer;
        Initialized = true;
        Root = root;
        if(isServer)
            MyRend.enabled = false;
        else
            MyRend.enabled = true;
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
        if (!Hit && Physics.SphereCast(transform.position, ColliderRadius, movement.normalized,
            out RaycastHit hit, movement.magnitude, HitLayer))
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
        Debug.Log("Is Valid Hit: " + IsValidHit(other));
        if (!IsValidHit(other)) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Damage, IsServer);
        }
        Debug.Log("[Client] Hit " + other.name);

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

        MyRend.enabled = false;
        Initialized = false;
        ArrowPoolManager.Instance.Return(this);
    }
}
