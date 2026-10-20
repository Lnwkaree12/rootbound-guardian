using UnityEngine;

public enum ItemType { HealthPotion, Key }

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    //public Sprite icon;
    public ItemType itemType;
    public int healAmount = 20; // สำหรับขวดยาเพิ่มเลือด
}
