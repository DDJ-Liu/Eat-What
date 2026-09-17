using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FridgeManager : MonoBehaviour
{
    private const string ArtPrototypeRootName = "PrepareArtPrototype";

    public PrepareIngredientManager myPrepareManager;
    public Recipe currentRecipe;
    public Transform RecipeInfoParent;
    public GameObject RecipeInfoObject;

    public int fridgeLevel = 1;

    public ScrollArea_Controller fridgeScrollController;
    public Transform fridgeContentArea;
    public GameObject fridgeMiddlePrefab;
    public Transform fridgeContentParent;
    public Transform fridgeTopObject;
    public Transform fridgeBottomObject;
    public List<GameObject> fridgeSegments = new List<GameObject>();

    [Header("Ingredient Tray")]
    public IngredientInventoryTray ingredientTray;

    // Start is called before the first frame update
    void Start()
    {
        GenerateFridgeVisual();
        ingredientTray.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        GenerateFridgeVisual();
    }

    public void GenerateFridgeVisual()
    {
        if (fridgeTopObject == null || fridgeBottomObject == null)
        {
            Debug.LogWarning("FridgeManager: Missing transform references for fridge generation.");
            return;
        }

        foreach (var seg in fridgeSegments)
            Destroy(seg);
        fridgeSegments.Clear();

        int middleCount = Mathf.Max(0, fridgeLevel * 2);

        SpriteRenderer topSR = fridgeTopObject.GetComponent<SpriteRenderer>();
        SpriteRenderer bottomSR = fridgeBottomObject.GetComponent<SpriteRenderer>();
        float topHeight = topSR.bounds.size.y;
        float bottomHeight = bottomSR.bounds.size.y;
        Transform artPrototypeRoot = transform.Find(ArtPrototypeRootName);
        bool showLegacyVisuals = artPrototypeRoot == null || !artPrototypeRoot.gameObject.activeSelf;
        topSR.enabled = showLegacyVisuals;
        bottomSR.enabled = showLegacyVisuals;

        float xPos = fridgeTopObject.position.x;
        float zPos = fridgeTopObject.position.z;
        float cursorY = fridgeTopObject.position.y - topHeight / 2f;

        float middleHeight = 0f;
        for (int i = 0; i < middleCount; i++)
        {
            if (fridgeMiddlePrefab == null)
            {
                Debug.LogWarning("FridgeManager: fridgeMiddlePrefab is null but middleCount > 0.");
                break;
            }

            GameObject mid = Instantiate(fridgeMiddlePrefab, fridgeContentParent);
            SpriteRenderer sr = mid.GetComponent<SpriteRenderer>();
            sr.sortingLayerID = topSR.sortingLayerID;
            sr.sortingOrder = topSR.sortingOrder;
            sr.enabled = showLegacyVisuals;

            middleHeight = sr.bounds.size.y;
            mid.transform.position = new Vector3(xPos, cursorY - middleHeight / 2f, zPos);
            cursorY -= middleHeight;
            fridgeSegments.Add(mid);
        }

        fridgeBottomObject.position = new Vector3(xPos, cursorY - bottomHeight / 2f, zPos);

        if (fridgeContentArea != null)
        {
            float topEdge = fridgeTopObject.position.y + topHeight / 2f;
            float bottomEdge = fridgeBottomObject.position.y - bottomHeight / 2f;
            float totalHeight = topEdge - bottomEdge;
            RectTransform rt = fridgeContentArea.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.rect.width, totalHeight);
            Vector3 areaPos = fridgeContentArea.position;
            //areaPos.y = topEdge;
            fridgeContentArea.position = areaPos;
            fridgeContentArea.GetComponent<DragContainer>().SyncColliderFromRect();
        }

        fridgeScrollController.Initialize();
    }

    public void onInitialize(Recipe currentRecipe)
    {
        this.currentRecipe = currentRecipe;
        //TODO: Load Fridge Inventory
        IngredientInventory.Instance.InitializeFridgeTempDepo();

        //SetRecipeInfoObject
        RecipeInfoObject = Tools.LoadAndInstantiatePrefab($"Prepare/FridgeIngredientList/{this.currentRecipe.name}", RecipeInfoParent);

        GenerateFridgeVisual();

        gameObject.SetActive(false);
    }

    public void onFridgeDone()
    {
        myPrepareManager.onFridgeFinished();
    }

    /// <summary>
    /// IngredientIcon Selected and item is on hand
    /// </summary>
    public void onIngredientSelected()
    {
        ingredientTray.onHighlight();

    }

    public void onIngredientTossed()
    {
        ingredientTray.onIdle();
    }

}
