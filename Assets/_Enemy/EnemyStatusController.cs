using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Enemy
{
    public enum EnemyStatusType
    {
        Grabbed,
        Frozen,
        Stunned,
        Slowed,
        Dead
    }

    public class EnemyStatusController : MonoBehaviour
    {
        private class StatusEntry
        {
            public float EndTime;
            public float MoveSpeedMultiplier;
            public bool HasDuration;
        }

        private readonly Dictionary<EnemyStatusType, StatusEntry> _statuses = new Dictionary<EnemyStatusType, StatusEntry>();

        public event Action StatusChanged;

        public bool CanThink => !HasAnyBlockingStatus();
        public bool CanMove => !HasAnyBlockingStatus();
        public bool CanAttack => !HasAnyBlockingStatus();
        public bool AllowsExternalMovement => HasStatus(EnemyStatusType.Grabbed) || HasStatus(EnemyStatusType.Stunned);

        public float MoveSpeedMultiplier
        {
            get
            {
                if (!CanMove)
                    return 0f;

                float multiplier = 1f;
                foreach (StatusEntry entry in _statuses.Values)
                {
                    multiplier *= entry.MoveSpeedMultiplier;
                }

                return multiplier;
            }
        }

        public static EnemyStatusController FindFor(Component component)
        {
            if (component == null)
                return null;

            EnemyStatusController status = component.GetComponentInParent<EnemyStatusController>();
            if (status != null)
                return status;

            BaseEnemy enemy = component.GetComponentInParent<BaseEnemy>();
            return enemy != null
                ? enemy.GetComponent<EnemyStatusController>() ?? enemy.gameObject.AddComponent<EnemyStatusController>()
                : null;
        }

        private void Update()
        {
            if (_statuses.Count == 0)
                return;

            bool changed = false;
            float now = Time.time;
            List<EnemyStatusType> expiredStatuses = null;

            foreach (KeyValuePair<EnemyStatusType, StatusEntry> pair in _statuses)
            {
                if (!pair.Value.HasDuration || now < pair.Value.EndTime)
                    continue;

                if (expiredStatuses == null)
                    expiredStatuses = new List<EnemyStatusType>();

                expiredStatuses.Add(pair.Key);
            }

            if (expiredStatuses == null)
                return;

            for (int i = 0; i < expiredStatuses.Count; i++)
            {
                changed |= _statuses.Remove(expiredStatuses[i]);
            }

            if (changed)
                StatusChanged?.Invoke();
        }

        public void ApplyStatus(EnemyStatusType statusType, float duration = -1f, float moveSpeedMultiplier = 1f)
        {
            _statuses[statusType] = new StatusEntry
            {
                HasDuration = duration > 0f,
                EndTime = duration > 0f ? Time.time + duration : 0f,
                MoveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier)
            };

            StatusChanged?.Invoke();
        }

        public void RemoveStatus(EnemyStatusType statusType)
        {
            if (_statuses.Remove(statusType))
                StatusChanged?.Invoke();
        }

        public void ClearStatuses()
        {
            if (_statuses.Count == 0)
                return;

            _statuses.Clear();
            StatusChanged?.Invoke();
        }

        public bool HasStatus(EnemyStatusType statusType)
        {
            return _statuses.ContainsKey(statusType);
        }

        private bool HasAnyBlockingStatus()
        {
            return HasStatus(EnemyStatusType.Grabbed)
                || HasStatus(EnemyStatusType.Frozen)
                || HasStatus(EnemyStatusType.Stunned)
                || HasStatus(EnemyStatusType.Dead);
        }
    }
}
