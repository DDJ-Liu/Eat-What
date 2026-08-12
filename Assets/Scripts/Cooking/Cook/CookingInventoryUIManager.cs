using System.Collections.Generic;
using UnityEngine;

public class CookingInventoryUIManager : MonoBehaviour
{
    public Transform IconsParent;
    public List<Ingredient_CookingIcon> InventoryItemIcons = new List<Ingredient_CookingIcon>();
    public List<IngredientData> InventoryDataPack = new List<IngredientData>();

    public int maxNum = 15;
    public int currentRow;

    public const int ItemPerRow = 5;

    public int MaxRow
    {
        get
        {
            if (InventoryDataPack.Count == 0) return 0;
            return Mathf.CeilToInt((float)InventoryDataPack.Count / ItemPerRow) - 1;
        }
    }

    void Start()
    {
        
        if(InventoryItemIcons.Count < 5)
        {
            foreach (Transform child in IconsParent)
            {
                InventoryItemIcons.Add(child.GetComponent<Ingredient_CookingIcon>());
            }
        }

        IngredientInventory.Instance.OnIngredientsInTrayChanged += SyncDataFromInventoryAndRefresh;

        SyncDataFromInventoryAndRefresh();
    }

    private void OnDisable()
    {
        IngredientInventory.Instance.OnIngredientsInTrayChanged -= SyncDataFromInventoryAndRefresh;

    }

    private void OnDestroy()
    {
        if(IngredientInventory.Instance != null) IngredientInventory.Instance.OnIngredientsInTrayChanged -= SyncDataFromInventoryAndRefresh;

    }

    public void Refresh()
    {
        int startIndex = currentRow * ItemPerRow;
        for (int i = 0; i < InventoryItemIcons.Count; i++)
        {
            int dataIndex = startIndex + i;
            if (dataIndex < InventoryDataPack.Count)
                InventoryItemIcons[i].setData(InventoryDataPack[dataIndex]);
            else
                InventoryItemIcons[i].ClearData();
        }
    }

    public void SyncDataFromInventoryAndRefresh()
    {
        if (IngredientInventory.Instance != null)
        {
            InventoryDataPack.Clear();
            InventoryDataPack = IngredientInventory.Instance.getDataList(IngredientInventory.Instance.ingredientsInTray);
        }
        Refresh();
    }

    public void NextRow()
    {
        if (currentRow >= MaxRow) return;
        OnBeforeRowFlip(true);
        currentRow++;
        Refresh();
    }

    public void PrevRow()
    {
        if (currentRow <= 0) return;
        OnBeforeRowFlip(false);
        currentRow--;
        Refresh();
    }

    // TODO: 翻行动画接入点 —— 后续若需要播放动画，把 currentRow++/Refresh 移入协程回调
    private void OnBeforeRowFlip(bool isNext) { }

    public void NextRowCondition(ConditionReceiver c)
    {
        c.ReportCondition(currentRow < MaxRow);
    }

    public void PrevRowCondition(ConditionReceiver c)
    {
        c.ReportCondition(currentRow > 0);
    }

    
}
