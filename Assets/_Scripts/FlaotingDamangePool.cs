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

    private void OnEnable() => player.OnHitTarget += SpawnDamage;
    private void OnDisable() => player.OnHitTarget -= SpawnDamage;

    private void SpawnDamage(Vector2 hitPoint, float actualDamage)
    {
        // 1. Get the object from the pool
        FloatingDamageText textObj = pool.Dequeue();

        // 2. Set the world position BEFORE activating
        // We use Vector3 to ensure Z is 0 (or slightly in front of the background)
        textObj.transform.position = new Vector3(hitPoint.x, hitPoint.y, 0);

        // 3. Add a tiny bit of random "jitter" so multiple hits don't overlap
        float jitterX = Random.Range(-0.3f, 0.3f);
        float jitterY = Random.Range(-0.1f, 0.1f);
        textObj.transform.position += new Vector3(jitterX, jitterY, 0);
        

        // 5. Activate and run your DOTween setup
        textObj.gameObject.SetActive(true);
    
        bool isCrit = Random.value > 0.8f; 
        textObj.Setup(actualDamage, isCrit);


        // 6. Push back to the end of the queue
        pool.Enqueue(textObj);
    }
}