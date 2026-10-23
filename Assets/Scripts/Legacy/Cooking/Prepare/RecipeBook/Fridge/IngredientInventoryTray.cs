using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IngredientInventoryTray : MonoBehaviour
{
    #region FSM
    private IngredientTrayStateBase currentState;
    public IngredientTrayIdleState idleState = new IngredientTrayIdleState();
    public IngredientTrayHighlightState highlightState = new IngredientTrayHighlightState();
    public IngredientTrayOpenedState openedState = new IngredientTrayOpenedState();

    public void ChangeState(IngredientTrayStateBase newState)
    {
        if (currentState != null)
        {
            currentState.ExitState(this);
        }
        currentState = newState;
        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    public IngredientTrayStateBase GetCurrentState()
    {
        return currentState;
    }

    [TextArea, SerializeField] private string currentStateIdentifier;
    #endregion

    [Header("Highlight Float")]
    public Transform floatParent;
    [SerializeField] private float lerpSpeed = 5f;
    private Coroutine lerpCoroutine;

    [Header("ItemPos")]
    public Transform itemGridParent;
    public List<Transform> Pos = new List<Transform>();
    public List<Ingredient_Fridge> trayItems = new List<Ingredient_Fridge>();

    [Header("NextButton")]
    public GameObject NextButton_Out;
    public GameObject NextButton_Opened;

    private void Awake()
    {
        Pos = itemGridParent.Cast<Transform>()
            .OrderBy(t => t.name)
            .ToList();
    }

    void Start()
    {
        ChangeState(idleState);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentStateIdentifier = currentState.GetType().Name;
            currentState.UpdateState(this);
        }
    }

    public void onHighlight()
    {
        ChangeState(highlightState);
    }

    public void onIdle()
    {
        ChangeState(idleState);
    }

    public void onItemDroppedIn(MouseDraggableObject targetDraggable)
    {
        Ingredient_Fridge ingredient = targetDraggable.GetComponent<Ingredient_Fridge>();
        if (ingredient != null)
        {
            /*if(IngredientInventory.Instance.AddItemToIngredientTray(ingredient.data))
            {
                
            }*/
            AddItem(ingredient);
        }
        ingredient.onPlaceRight();
        ChangeState(idleState);
        //ingredient.transform.position = GetFirstEmptyPos().position;
    }

    /// <summary>
    /// 添加物体到托盘
    /// </summary>
    public void AddItem(Ingredient_Fridge item)
    {
        if (trayItems.Count >= Pos.Count)
        {
            Debug.LogWarning("Tray is full, cannot add more items.");
            return;
        }

        trayItems.Add(item);
        int index = trayItems.Count - 1;
        
        item.transform.SetParent(Pos[index]);
        item.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 从托盘移除物体
    /// </summary>
    public void RemoveItem(Ingredient_Fridge item)
    {
        int index = trayItems.IndexOf(item);
        if (index == -1)
        {
            Debug.LogWarning("Item not found in tray.");
            return;
        }

        trayItems.RemoveAt(index);

        // 重新排列剩余物体
        for (int i = index; i < trayItems.Count; i++)
        {
            trayItems[i].transform.position = Pos[i].position;
            trayItems[i].transform.SetParent(Pos[i]);
        }
    }

    /// <summary>
    /// 获取第一个空位置
    /// </summary>
    public Transform GetFirstEmptyPos()
    {
        if (trayItems.Count >= Pos.Count)
        {
            return null;
        }
        return Pos[trayItems.Count];
    }

    public void StartLerpToTargetY(float targetY)
    {
        if (lerpCoroutine != null)
        {
            StopCoroutine(lerpCoroutine);
        }
        lerpCoroutine = StartCoroutine(LerpToTargetY(targetY));
    }

    private IEnumerator LerpToTargetY(float targetY)
    {
        if (floatParent == null) yield break;

        while (Mathf.Abs(floatParent.localPosition.y - targetY) > 0.01f)
        {
            Vector3 currentPos = floatParent.localPosition;
            float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * lerpSpeed);
            floatParent.localPosition = new Vector3(currentPos.x, newY, currentPos.z);
            yield return null;
        }

        Vector3 finalPos = floatParent.localPosition;
        floatParent.localPosition = new Vector3(finalPos.x, targetY, finalPos.z);
        lerpCoroutine = null;
    }

    public void onNextButtonPressed()
    {
        if(currentState == openedState)
        {
            // 托盘已打开，用户确认选择并进入烹饪阶段

            // 1. 同步数据：将 trayItems 的当前状态同步到 IngredientInventory
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.SyncIngredientsFromTray(trayItems);
            }
            else
            {
                Debug.LogError("[IngredientInventoryTray] IngredientInventory.Instance is null");
            }

            // 2. 关闭托盘
            onClose();

            // 3. 通知 FridgeManager 完成食材选择
            FridgeManager fridgeManager = GetComponentInParent<FridgeManager>();
            if (fridgeManager != null)
            {
                fridgeManager.onFridgeDone();
            }
            else
            {
                Debug.LogWarning("[IngredientInventoryTray] FridgeManager not found in parent");
            }
        }
        else
        {
            // Not Opened, open the Tray
            ChangeState(openedState);
            //GetComponent<MouseParallax>().enableParallax = false;
            GetComponent<Animator>().SetTrigger("Open");
        }
    }

    public void onClose()
    {
        GetComponent<Animator>().SetTrigger("Close");
        onIdle();
    }

    public void onClose_IngredientOnHand()
    {
        GetComponent<Animator>().SetTrigger("Close");
        onHighlight();
    }
}
