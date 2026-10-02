using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerDataSave
{
    public int savedHealth;
    public List<ItemData> savedItems = new List<ItemData>();
}