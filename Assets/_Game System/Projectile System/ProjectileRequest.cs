using UnityEngine;

public struct ProjectileRequest
{
    public string ProjectileID;
    public Vector3 Position;
    public Quaternion Rotation; 
    public Vector3 Direction; 
    public Transform Target;  
    public float Damage;
    public float Speed;
    public float Lifetime;
}
