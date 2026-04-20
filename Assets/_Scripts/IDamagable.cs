using UnityEngine;

public interface IDamagable
{
    void TakeDamage(float damage);
    Transform GetTransform();
}
