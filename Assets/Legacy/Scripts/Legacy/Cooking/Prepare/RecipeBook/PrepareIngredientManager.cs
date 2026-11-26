using UnityEngine;

public class PrepareIngredientManager : MonoBehaviour
{
    public bool isFreeCooking = true;
    #region FSM
    private PreparePhaseStateBase currentState;
    public PrepareIdleState idleState = new PrepareIdleState();
    public RecipeMenuState recipeMenuState = new RecipeMenuState();
    public RecipeBookState recipeBookState = new RecipeBookState();
    public RecipeBookFridgeState bookFridgeState = new RecipeBookFridgeState();
    public RecipeDetailState recipeDetailState = new RecipeDetailState();
    public FridgeState fridgeState = new FridgeState();

    public void ChangeState(PreparePhaseStateBase newState)
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

    public PreparePhaseStateBase GetCurrentState()
    {
        return currentState;
    }

    [TextArea, SerializeField] private string currentStateIdentifier;
    #endregion

    #region Singleton
    public static PrepareIngredientManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion

    public GameObject PrepareParent;
    public FridgeManager fridgeManager;
    public GameObject RecipeInfoParent;
    public Recipe currentRecipe;
    public MouseInteractionLayer preventControlLayer;

    void Start()
    {
        //fridgeManager.gameObject.SetActive(false);
        onInitialize();
    }

    void Update()
    {
        if (currentState != null)
        {
            currentStateIdentifier = currentState.GetType().Name;
            currentState.UpdateState(this);
        }
    }

    public void onInitialize()
    {
        if(isFreeCooking)
        {
            //let player choose recipe
            ChangeState(recipeMenuState);
        }
        else
        {
            //recipe chosen by customer or forced by elsewhere
            onRecipeChosen(currentRecipe);
        }
        
    }

    public void onRecipeChosen(Recipe recipe)
    {
        currentRecipe = recipe;
        //Set RecipeInfo
        fridgeManager.onInitialize(currentRecipe);
        //PrepareParent.SetActive(true);
        ChangeState(recipeBookState);
    }

    public void onStartCook()
    {
        fridgeManager.gameObject.SetActive(true);
        ChangeState(fridgeState);
    }

    public void onFridgeFinished()
    {
        fridgeManager.gameObject.SetActive(false);
        //TODO: hand off to CookingManager scene
        SceneSwitchManager.Instance.SwitchSceneWithFullFade("CookingProcess");
        ChangeState(idleState);
    }

    public void onEnterBookFridgeState()
    {
        ChangeState(bookFridgeState);
    }

    public void onExitBookFridgeState()
    {
        ChangeState(recipeBookState);
    }

    public void onEnterFridgeState()
    {
        ChangeState(fridgeState);
        fridgeManager.ingredientTray.gameObject.SetActive(true);
    }

    public void onTempPreventControl()
    {
        preventControlLayer.OnPushLayer();
    }

    public void onRestoreTempPreventControl()
    {
        preventControlLayer.OnRemoveLayer();
    }
}
