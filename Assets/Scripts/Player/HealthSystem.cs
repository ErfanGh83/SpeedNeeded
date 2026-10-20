using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;

    [Tooltip("Seconds of immunity after a hit, so one crash can't hit you several times.")]
    [SerializeField] float invincibleSeconds = 1.5f;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public float Normalized => maxHealth > 0 ? (float)Health / maxHealth : 0f;
    public bool IsInvincible => Time.time < invincibleUntil;

    /// <summary>(current, max): for the health bar later.</summary>
    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged; // for hit flash / camera shake / sound later

    float invincibleUntil;

    void Awake()
    {
        Health = maxHealth;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted += ResetHealth;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted -= ResetHealth;
    }

    public void TakeDamage(int amount)
    {
        if (!GameManager.IsPlaying || IsInvincible || Health <= 0) return;

        Health = Mathf.Max(0, Health - amount);
        invincibleUntil = Time.time + invincibleSeconds;
        Debug.Log($"[Health] -{amount} -> {Health}/{maxHealth}");

        OnHealthChanged?.Invoke(Health, maxHealth);
        OnDamaged?.Invoke();

        if (Health <= 0 && GameManager.Instance != null)
            GameManager.Instance.GameOver("Out of health");
    }

    public void Heal(int amount)
    {
        if (Health <= 0) return;

        Health = Mathf.Min(maxHealth, Health + amount);
        Debug.Log($"[Health] +{amount} -> {Health}/{maxHealth}");
        OnHealthChanged?.Invoke(Health, maxHealth);
    }

    void ResetHealth()
    {
        Health = maxHealth;
        invincibleUntil = 0f;
        OnHealthChanged?.Invoke(Health, maxHealth);
    }
}