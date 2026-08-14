using System.Collections.Generic;
using UnityEngine;
using System;

public interface IDamageable
{
    void TakeDamage(float damage);
    void TakeDamageOverTime(DamageOverTimeProperties properties) { }
    void Heal(float health) { }
}
public class DamageOverTimeProperties
{
    public float Damage;
    public float Interval;
    public float Duration;
    public string EffectId;
    public int SourceId;

    public bool RefreshAllDotDurations;
    public int MaxStacks;
}
public class HealthState
{
    public float MaxValue;
    public float Value;

    public event Action OnHealthChanged;
    private readonly List<DotInstance> Dots = new();
    public bool HasActiveDots => Dots.Count > 0;
    private class DotInstance
    {
        public string EffectId;
        public int SourceId;

        public float DamagePerTick;
        public float TickInterval;
        public float TickTimer;
        public uint TicksRemaining;
    }

    public void Init(float startingHealth)
    {
        MaxValue = startingHealth;
        Value = startingHealth;
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (Value == MaxValue)
            Value += amount;
        MaxValue += amount;
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        Value = Mathf.Max(0f, Value - damage);
        OnHealthChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        Value = Mathf.Min(MaxValue, Value + amount);
        OnHealthChanged?.Invoke();
    }

    public void ApplyDot(DamageOverTimeProperties properties)
    {
        if (properties.Interval <= 0f || properties.Duration <= 0f)
            return;

        uint tickCount = (uint)Mathf.Max(1, Mathf.RoundToInt(properties.Duration / properties.Interval));

        if (properties.RefreshAllDotDurations)
        {
            foreach (DotInstance dot in Dots)
            {
                if (dot.EffectId == properties.EffectId && dot.SourceId == properties.SourceId)
                {
                    dot.TicksRemaining = tickCount;
                }
            }
        }

        if (properties.MaxStacks > 0)
        {
            int existingCount = Dots.FindAll(d => d.EffectId == properties.EffectId && d.SourceId == properties.SourceId).Count;
            if (existingCount >= properties.MaxStacks)
                return;
        }

        Dots.Add(new DotInstance
        {
            EffectId = properties.EffectId,
            SourceId = properties.SourceId,
            DamagePerTick = properties.Damage,
            TickInterval = properties.Interval,
            TickTimer = properties.Interval,
            TicksRemaining = tickCount
        });
    }
    public void TickDots(float deltaTime)
    {
        for (int i = Dots.Count - 1; i >= 0; i--)
        {
            DotInstance dot = Dots[i];
            dot.TickTimer -= deltaTime;

            if (dot.TickTimer <= 0f)
            {
                TakeDamage(dot.DamagePerTick);
                dot.TicksRemaining--;
                dot.TickTimer += dot.TickInterval;
            }

            if (dot.TicksRemaining <= 0)
            {
                Dots.RemoveAt(i);
            }
        }
    }
}
