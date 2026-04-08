using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class EnemyMovement : MonoBehaviour, IMovement
    {
        private EnemyConfig config;
        
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        [Header("Sine Wave Movement")]
        [SerializeField] private bool useSineWave = true;
        [SerializeField] private float waveFrequency = 4f;  // How fast it waves back and forth
        [SerializeField] private float waveAmplitude = 5f;  // How wide the wave is

        [Header("Stop & Go Movement")]
        [SerializeField] private bool useStopAndGo = true;
        [SerializeField] private float moveDurationMin = 1f;
        [SerializeField] private float moveDurationMax = 2f;
        [SerializeField] private float stopDurationMin = 0.5f;
        [SerializeField] private float stopDurationMax = 1f;

        private float _timeOffset;
        private float _actualFrequency;
        private float _actualAmplitude;

        private bool _isMovingPhase = true;
        private float _phaseTimer;
        private Vector2 _lockedDirection = Vector2.zero;

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
            
            // Offset the time and slightly randomize freq/amp per enemy instance
            // so they never move in perfectly synchronized formation
            _timeOffset = Random.Range(0f, 100f);
            _actualFrequency = waveFrequency * Random.Range(0.8f, 1.2f);
            _actualAmplitude = waveAmplitude * Random.Range(0.8f, 1.2f);

            _isMovingPhase = true;
            _phaseTimer = Random.Range(moveDurationMin, moveDurationMax);
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            if (config == null) return;
            
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            MoveInDirection(direction);
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (config == null) return;
            
            if (useStopAndGo)
            {
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    _isMovingPhase = !_isMovingPhase;
                    _phaseTimer = _isMovingPhase 
                        ? Random.Range(moveDurationMin, moveDurationMax) 
                        : Random.Range(stopDurationMin, stopDurationMax);
                    
                    if (!_isMovingPhase)
                    {
                        _lockedDirection = Vector2.zero; // Clear locked aim when stopping
                    }
                }

                if (!_isMovingPhase)
                {
                    Stop();
                    return;
                }

                // Lock direction at the start of the moving phase
                if (_lockedDirection == Vector2.zero && direction != Vector2.zero)
                {
                    _lockedDirection = direction.normalized;
                }
            }

            Vector2 forwardDir = useStopAndGo ? _lockedDirection : direction.normalized;
            Vector2 targetVelocity = forwardDir * config.moveSpeed;
            
            if (useSineWave)
            {
                // Calculate perpendicular vector for sideways movement
                Vector2 perpendicularDir = new Vector2(-forwardDir.y, forwardDir.x);
                
                // Add wavy velocity using this specific enemy's offsets and modifiers
                float waveVelocity = Mathf.Cos((Time.time + _timeOffset) * _actualFrequency) * _actualAmplitude;
                targetVelocity += perpendicularDir * waveVelocity;
            }
            
            _currentVelocity = Vector2.MoveTowards(
                _currentVelocity,
                targetVelocity,
                config.acceleration * Time.deltaTime
            );

            ApplyMovement();
        }

        public void Stop()
        {
            if (config == null) return;
            
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, Vector2.zero, config.deceleration * Time.deltaTime);
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            transform.position += (Vector3)_currentVelocity * Time.deltaTime;
        }
    }
}