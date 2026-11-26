using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IngredientInventory : MonoBehaviour
{
    [System.Serializable]
    public class InventoryData<t>
    {
        public t Data;
        public int amount;

        public enum InventoryType { Ingredient, ProcessedIngredient }
        public InventoryType type;
        public InventoryData(t data, InventoryType newType)
        {
            Data = data;
            amount = 1;
            type = newType;
        }
        public InventoryData(InventoryData<t> right)
        {
            Data = right.Data;
            amount = right.amount;
            type = right.type;
        }
    }
    public static IngredientInventory Instance;

    public List<InventoryData<IngredientData>> ingredientDepo;
    public List<InventoryData<IngredientData>> fridgeDepo;
    public List<InventoryData<IngredientData>> ingredientsInTray;

    public delegate void IngredientsInTrayChangedHandler();
    public event IngredientsInTrayChangedHandler OnIngredientsInTrayChanged;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void InitializeFridgeTempDepo()
    {
        fridgeDepo.Clear();
        fridgeDepo = ingredientDepo.Select(data => new InventoryData<IngredientData>(data)).ToList();
        ingredientsInTray.Clear();
        OnIngredientsInTrayChanged?.Invoke();
    }

    public InventoryData<IngredientData> SearchForInventoryDataByIngredient(IngredientData data, List<InventoryData<IngredientData>> sourceDepo)
    {
        if(sourceDepo == null)
        {
            return null;
        }
        foreach(InventoryData<IngredientData> invData in sourceDepo)
        {
            if(invData.Data == data)
            {
                return invData;
            }
        }

        return null;
    }

    public bool AddItemToIngredientTray(IngredientData ingredient)
    {
        InventoryData<IngredientData> searchResult_Fridge = SearchForInventoryDataByIngredient(ingredient, fridgeDepo);
        if (searchResult_Fridge == null)
        {
            Debug.Log("Fridge Does not have Ingredient");
            return false;
        }
        if (searchResult_Fridge.amount <= 0)
        {
            Debug.Log("Ingredient in Fridge Used Up");
            return false;
        }
        InventoryData<IngredientData> searchResult_Tray = SearchForInventoryDataByIngredient(ingredient, ingredientsInTray);
        if (searchResult_Tray != null)
        {
            searchResult_Tray.amount++;
            searchResult_Fridge.amount--;
            OnIngredientsInTrayChanged?.Invoke();
            return false;
        }
        else
        {
            ingredientsInTray.Add(new InventoryData<IngredientData>(searchResult_Fridge));
            searchResult_Fridge.amount--;
            OnIngredientsInTrayChanged?.Invoke();
            return true;
        }

    }

    public bool AddNewItemToIngredientTray(IngredientData ingredient)
    {
        InventoryData<IngredientData> searchResult_Tray = SearchForInventoryDataByIngredient(ingredient, ingredientsInTray);
        if (searchResult_Tray != null)
        {
            searchResult_Tray.amount++;
            OnIngredientsInTrayChanged?.Invoke();
            return false;
        }
        else
        {
            ingredientsInTray.Add(new InventoryData<IngredientData>(ingredient, InventoryData<IngredientData>.InventoryType.ProcessedIngredient));
            OnIngredientsInTrayChanged?.Invoke();
            return true;
        }

    }

    /// <summary>
    /// 从托盘视图同步数据到 ingredientsInTray
    /// 用于托盘确认时保证数据一致性（视图驱动数据）
    /// </summary>
    public void SyncIngredientsFromTray(List<Ingredient_Fridge> trayItems)
    {
        if (trayItems == null)
        {
            Debug.LogWarning("[IngredientInventory] SyncIngredientsFromTray: trayItems is null");
            return;
        }

        ingredientsInTray.Clear();

        Dictionary<IngredientData, int> ingredientCounts = new Dictionary<IngredientData, int>();

        foreach (var item in trayItems)
        {
            if (item == null || item.data == null) continue;

            if (ingredientCounts.ContainsKey(item.data))
            {
                ingredientCounts[item.data]++;
            }
            else
            {
                ingredientCounts[item.data] = 1;
            }
        }

        foreach (var kvp in ingredientCounts)
        {
            var fridgeData = SearchForInventoryDataByIngredient(kvp.Key, fridgeDepo);
            if (fridgeData != null)
            {
                var invData = new InventoryData<IngredientData>(fridgeData);
                invData.amount = kvp.Value;
                ingredientsInTray.Add(invData);
            }
            else
            {
                Debug.LogWarning($"[IngredientInventory] 食材 {kvp.Key.name} 在 fridgeDepo 中未找到");
            }
        }

        Debug.Log($"[IngredientInventory] 托盘数据同步完成，共 {ingredientsInTray.Count} 种食材");
        OnIngredientsInTrayChanged?.Invoke();
    }

    public List<IngredientData> getDataList(List<InventoryData<IngredientData>> targetList)
    {
        if(targetList == null || targetList.Count == 0)
        {
            return null;
        }
        List<IngredientData> result = new List<IngredientData>();
        foreach(var item in targetList)
        {
            result.Add(item.Data);
        }

        return result;
    }
}
