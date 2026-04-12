using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class DiveEnemySensor : MonoBehaviour, ITargetSensor
    {
        private EnemyConfig config;
        private Transform _player;
        private Camera _mainCamera;

        [Header("Viewport Settings")]
        [SerializeField] private float viewportInset = 0.05f;

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
            _mainCamera = Camera.main;
        }

        public bool HasTarget 
        {
            get
            {
                if (_player == null)
                {
                    // Finds the player dynamically (fixes issues with pooled/spawned players)
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) _player = playerObj.transform;
                }
                return _player != null;
            }
        }
        
        public Vector3 TargetPosition => HasTarget ? _player.position : Vector3.zero;

        private bool IsInViewport()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return false;

            Vector3 viewPos = _mainCamera.WorldToViewportPoint(transform.position);

            // Viewport coordinates are 0 to 1. The inset ensures it is safely inside the screen.
            bool inX = viewPos.x >= viewportInset && viewPos.x <= 1f - viewportInset;
            bool inY = viewPos.y >= viewportInset && viewPos.y <= 1f - viewportInset;
            
            // viewPos.z > 0 ensures the object isn't BEHIND the camera
            return inX && inY && viewPos.z > 0;
        }

        // --- ITargetSensor Implementation ---

        public bool IsTargetInDetectionRange()
        {
            // "Smells" the player globally - always true if the player exists, allowing it to chase from off-screen
            return HasTarget;
        }

        public bool IsTargetInAttackRange()
        {
            // Only wind up and dive when within the viewport
            return HasTarget && IsInViewport();
        }

        public bool IsTargetOutOfAttackRange()
        {
            // Out of attack range if off-screen
            return !HasTarget || !IsInViewport();
        }

        public bool IsTargetTooClose()
        {
            // Dive enemies want to dive no matter how close you are, so it never tries to back up
            return false;
        }

        // --- Visual Debugging ---
        private void OnDrawGizmos()
        {
            // Viewport-based attack range, standard circular gizmos not applicable here.
        }
    }
}
