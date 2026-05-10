
using System;

public interface IProjectile
{
    void Launch(ProjectileRequest request, Action<IProjectile> onRelease);
}