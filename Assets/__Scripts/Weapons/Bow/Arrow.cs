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

    private PlayerModule Source;
    private Vector3 Velocity;
    private float PassedTime;
    private bool IsServer;
    private bool Initialized;
    private bool Hit;
    private List<AbilityData> EffectArray = new();
    private const float CATCH_UP_STEP = 0.02f;
    private const int MAX_CATCH_UP_STEPS_PER_FRAME = 50;

    public void Initialize(PlayerModule source, Vector3 direction, float speed, float passedTime, float totalDamage, int[] effectArray, bool isServer)
    {
        Hit = false;
        Source = source;
        Velocity = direction.normalized * speed;
        PassedTime = passedTime;
        TotalDamage = totalDamage;
        IsServer = isServer;
        Initialized = true;
        MyRend.SetActive(true);
        //MyRend.SetActive(!isServer);
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
        if (Hit) return;

        int steps = 0;
        while (PassedTime > 0f && steps < MAX_CATCH_UP_STEPS_PER_FRAME)
        {
            float step = Mathf.Min(CATCH_UP_STEP, PassedTime);
            Move(step);
            PassedTime -= step;
            steps++;
            if (Hit) return;
        }

        Move(Time.deltaTime);
    }

    private void Move(float delta)
    {
        Velocity += Physics.gravity * delta;

        Vector3 movement = Velocity * delta;

        if (!Hit && Physics.SphereCast(transform.position, ColliderRadius, movement.normalized, out RaycastHit hit, movement.magnitude))
        {
            CheckHit(hit.collider, hit.point);
            if (Hit) return;
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
            if(IsServer)
            {
                data.OnServerHit(new HitContext { HitPoint = hitPoint, HitEntity = other.transform, Source = Source }, ref TotalDamage);
            }
            else
            {
                data.OnClientHit(hitPoint, other.transform);
            }
        }
        //Default Arrow Behavior
        if (IsServer)
        {
            if (EffectArray.Count == 0)
            {
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
        if (Source !=null && col.transform == Source.transform) return false;
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
