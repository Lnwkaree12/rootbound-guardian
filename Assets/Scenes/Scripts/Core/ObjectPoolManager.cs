using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // เปลี่ยน Key จาก GameObject เป็น string (ชื่อของ Prefab)
    private Dictionary<string, IObjectPool<GameObject>> poolDictionary
        = new Dictionary<string, IObjectPool<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ดึง Object จาก Pool
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        string key = prefab.name; // ใช้ชื่อ Prefab เป็น Key

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => { },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        GameObject obj = poolDictionary[key].Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    // คืน Object เข้า Pool โดยใช้วัตถุที่บินอยู่นั้นส่งกลับเข้ามาตรงๆ ได้เลย
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        // ตัดคำว่า "(Clone)" ออกจากชื่อ เช่น "Arrow(Clone)" -> "Arrow"
        string key = instance.name.Replace("(Clone)", "").Trim();

        if (poolDictionary.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Debug.LogWarning($"[ObjectPoolManager] ไม่พบ Pool สำหรับ {key}! สั่ง Destroy ทิ้งแทน");
            Destroy(instance);
        }
    }
}