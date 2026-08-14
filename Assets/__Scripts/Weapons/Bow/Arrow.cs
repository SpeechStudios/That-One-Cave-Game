using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public GameObject MyRend;
    public Transform ColliderPosition;
    public float ColliderRadius;
    internal float TotalDamage;
    internal float BaseDamage;

    private Transform Source;
    private Vector3 Velocity;
    private float PassedTime;
    private bool IsServer;
    private bool Initialized;
    private bool Hit;
    private List<AbilityData> EffectArray = new();

    private const float CATCH_UP_RATE = 0.08f;


    public void Initialize(Transform source, Vector3 direction, float speed, float passedTime, float baseDamage, float totalDamage, int[] effectArray, bool isServer)
    {
        Hit = false;
        Source = source;
        Velocity = direction.normalized * speed;
        PassedTime = passedTime;
        BaseDamage = baseDamage;
        TotalDamage = totalDamage;
        IsServer = isServer;
        Initialized = true;
        MyRend.SetActive(true);
        MyRend.SetActive(!isServer);
        foreach (int i in effectArray)
        {
            var data = Registry.GetAbilityData(i);
            EffectArray.Add(data);

            if (!isServer)
                data.SpawnInitalizeClientVisuals();
        }
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
        transform.SetParent(other.transform);

        foreach (var data in EffectArray)
        {
            data.OnHitFunction(new HitContext
            {
                HitPoint = hitPoint,
                HitEntity = other.transform,
                Source = Source,
                BaseDamage = BaseDamage,
                TotalDamage = TotalDamage,

            }, IsServer);
        }
        //Default Arrow Behavior
        if (IsServer)
        {
            if (EffectArray.Count == 0)
            {
                Debug.Log("Server Damage" + TotalDamage);
                if (other.transform.TryGetComponent<IDamageable>(out var explosionDamageable))
                    explosionDamageable.TakeDamage(TotalDamage);
            }
        }
        else
        {
            //VFX
        }
        
        Hit = true;
        StartCoroutine(ReturnToPool(other));
    }
    private bool IsValidHit(Collider col)
    {
        if (col == null) return false;
        if (col.transform == transform.root) return false;
        if (col.transform == Source.root) return false;
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
        EffectArray.Clear();
        ArrowPoolManager.Instance.Return(this);
    }
}
