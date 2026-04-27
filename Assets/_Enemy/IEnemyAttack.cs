namespace _Scripts.Enemy
{
    public interface IEnemyAttack
    {
        void SetAttackActive(bool active);
        bool CanHit();
        void StartHitCooldown();
    }
}
