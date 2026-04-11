using UnityEngine;
using System.Collections.Generic;

namespace _Scripts.Enemy.Modules
{
    public class SlimeAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Ink Trail Settings")]
        [SerializeField] private GameObject inkPrefab;
        [Tooltip("Distance the slime needs to move before dropping the next ink trail (lower = closer spacing)")]
        [SerializeField] private float spawnDistance = 0.5f;
        [SerializeField] private int maxPoolSize = 30;
        
        [Header("Internal Reference")]
        private EnemyConfig _config;
        private List<GameObject> _inkPool = new List<GameObject>();
        private int _currentPoolIndex = 0;
        private Vector3 _lastSpawnPos;
        private bool _isAttacking;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            
            // Initialize Pool
            if (inkPrefab != null)
            {
                for (int i = 0; i < maxPoolSize; i++)
                {
                    GameObject ink = Instantiate(inkPrefab, transform.position, Quaternion.identity);
                    
                    // Keep the pool organized under a parent if desired, or let it live in the scene root
                    ink.transform.SetParent(null); // Optional: keep them outside the slime hierarchy
                    ink.SetActive(false);

                    _inkPool.Add(ink);
                }
            }
            else
            {
                Debug.LogWarning("No Ink Prefab assigned in SlimeAttackModule!");
            }
            
            _lastSpawnPos = transform.position;
        }

        public void SetAttackActive(bool active)
        {
            _isAttacking = active;
            if (active)
            {
                // Reset the tracking position so it starts checking immediately from the current location
                _lastSpawnPos = transform.position; 
            }
            else
            {
                // Optional: You could disable all active ink in the pool when the attack phase ends
                /*
                foreach (var ink in _inkPool)
                {
                    ink.SetActive(false);
                }
                */
            }
        }

        public bool CanHit()
        {
            // Return true if the slime's own body should deal damage when active
            // The ink prefabs handle their own collisions
            return _isAttacking; 
        }

        public void StartHitCooldown()
        {
            // Cooldown logic for the slime's body collision if needed
        }

        private void Update()
        {
            if (inkPrefab == null || _inkPool.Count == 0) return;

            // Check if the slime has moved far enough from the last spawn position
            if (Vector3.Distance(transform.position, _lastSpawnPos) >= spawnDistance)
            {
                SpawnInkTrail();
                _lastSpawnPos = transform.position;
            }
        }

        private void SpawnInkTrail()
        {
            // Grab next available ink from the circular pool
            GameObject ink = _inkPool[_currentPoolIndex];

            // If the ink has a lifetime/disable script, you might need to reset its state here
            ink.transform.position = transform.position;
            ink.transform.rotation = Quaternion.identity;

            // Recalculate config in case it was injected after Awake()
            if (_config == null) _config = GetComponentInParent<BaseEnemy>()?.Config;
            float inkDamage = _config != null ? _config.damage : 10f;
            
            // Setup the SlimeTrail component right before activation
            if (ink.TryGetComponent<SlimeTrail>(out SlimeTrail trailScript))
            {
                trailScript.Initialize(inkDamage);
            }

            ink.SetActive(true);
            
            // Advance the index, wrapping around to 0 if it reaches maxPoolSize
            _currentPoolIndex = (_currentPoolIndex + 1) % maxPoolSize;
        }
    }
}
