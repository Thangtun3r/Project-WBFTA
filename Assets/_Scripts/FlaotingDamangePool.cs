using UnityEngine;
using System.Collections.Generic;
public class FloatingDamagePool : MonoBehaviour
{
    [SerializeField] private PlayerAttack player;
    [SerializeField] private FloatingDamageText prefab;
    [SerializeField] private int poolSize = 30;

    private Queue<FloatingDamageText> pool = new Queue<FloatingDamageText>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            FloatingDamageText obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
        
            pool.Enqueue(obj);
        }
    }

    private void OnEnable()
    
    {
        player.OnHitTarget += SpawnDamage;
    } 
    private void OnDisable() => player.OnHitTarget -= SpawnDamage;

    public void SpawnDamage(Vector2 hitPoint, float actualDamage, bool isCrit) // Changed to PUBLIC
    {
        if (pool.Count == 0) return; // Safety check

        FloatingDamageText textObj = pool.Dequeue();
        
        textObj.transform.position = new Vector3(hitPoint.x, hitPoint.y, 0);

        float jitterX = UnityEngine.Random.Range(-0.3f, 0.3f);
        float jitterY = UnityEngine.Random.Range(-0.1f, 0.1f);
        textObj.transform.position += new Vector3(jitterX, jitterY, 0);

        textObj.gameObject.SetActive(true);
        textObj.Setup(actualDamage, isCrit);

        pool.Enqueue(textObj);
    }
}