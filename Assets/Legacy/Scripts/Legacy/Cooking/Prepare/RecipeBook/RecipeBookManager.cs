using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeBookManager : MonoBehaviour
{
    #region FSM
    private RecipeBookStateBase currentState;
    public RecipeBookMenuState menuState = new RecipeBookMenuState();
    public RecipeBookRecipeState recipeState = new RecipeBookRecipeState();
    private Stack<RecipeBookStateBase> interruptedStates = new Stack<RecipeBookStateBase>();

    public void ChangeState(RecipeBookStateBase newState)
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

    public RecipeBookStateBase GetCurrentState()
    {
        return currentState;
    }

    public void InterruptState(RecipeBookStateBase newState)
    {
        if (newState == null)
        {
            Debug.LogWarning("[RecipeBookFSM] InterruptState called with null newState, ignored.");
            return;
        }

        if (currentState != null)
        {
            interruptedStates.Push(currentState);
        }

        currentState = newState;
        currentState.EnterState(this);
    }

    public void ReviveState()
    {
        if (interruptedStates.Count == 0)
        {
            Debug.LogWarning("[RecipeBookFSM] ReviveState called but no interrupted state to revive.");
            return;
        }

        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        currentState = interruptedStates.Pop();
        currentState.EnterState(this);
    }

    public bool HasInterruptedState()
    {
        return interruptedStates.Count > 0;
    }

    [TextArea, SerializeField] private string currentStateIdentifier;
    #endregion

    public PrepareIngredientManager prepareManager;
    [Header("Menu")]
    public GameObject TitlePagePrefab;
    public GameObject ListPagePrefab;
    public GameObject currentpageObject;
    [SerializeField] public List<GameObject> leftPageObjects = new List<GameObject>();
    [SerializeField] public List<GameObject> rightPageObjects = new List<GameObject>();
    [SerializeField] public List<GameObject> recipeObjects = new List<GameObject>();

    [Header("Recipe")]
    public Transform RecipeSpawnParent;
    public GameObject RecipeDataObject;

    [Header("Pages & Data")]
    public List<Recipe> recipes;
    public int recipePerPage = 4;
    public int currentPage_Menu = 0;
    public int currentPage_Recipe = 0;

    [Header("Flip Animation")]
    public GameObject flipAnim_LeftToRight;
    public GameObject flipAnim_LeftToRight_YReverse;
    public GameObject flipAnim_RightToLeft;

    public Coroutine flipCoroutine;

    #region Hand Over to State
    public void onNextPage()
    {
        currentState?.OnNextPage(this);
    }
    public void onPrevPage()
    {
        currentState?.OnPrevPage(this);
    }
    public void NextPage()
    {
        currentState.NextPage(this);
    }
    public void PreviousPage()
    {
        currentState.PreviousPage(this);
    }
    public void UpdatePage()
    {
        currentState?.OnUpdatePage(this);
    }
    public void UpdateLeftPage()
    {
        menuState.OnUpdateLeftPage(this);
    }
    public void UpdateRightPage()
    {
        menuState.OnUpdateRightPage(this);
    }
    public void nextButtonCondition(ConditionReceiver condition)
    {
        currentState?.NextButtonCondition(this, condition);
    }
    public void prevButtonCondition(ConditionReceiver condition)
    {
        currentState?.PrevButtonCondition(this, condition);
    }
    #endregion

    void Start()
    {
        ChangeState(menuState);
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentStateIdentifier = currentState.GetType().Name;
            currentState.UpdateState(this);
        }
    }

    public int MaxPage_Menu
    {
        get
        {
            int recipeCount = recipes != null ? recipes.Count : 0;
            int objectsPerPage = recipePerPage * 2;
            return Mathf.Max(1, Mathf.CeilToInt((float)recipeCount / objectsPerPage));
        }
    }

    public int MaxPage_Recipe
    {
        get
        {
            return recipes.Count;
        }
    }

    #region MenuState Support Functions
    public IEnumerator WaitFlipThenExecute(Animator anim, string stateName, bool delayUpdatePage)
    {
        yield return null;
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        while (!stateInfo.IsName(stateName))
        {
            yield return null;
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        }
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        }

        // Flip Animation done
        prepareManager.onRestoreTempPreventControl();
        if (delayUpdatePage)
        {
            UpdatePage();
        }
        currentState.nextButton.SetActive(true);
        currentState.prevButton.SetActive(true);
        flipAnim_LeftToRight.SetActive(false);
        flipAnim_RightToLeft.SetActive(false);
        flipAnim_LeftToRight_YReverse.SetActive(false);
        flipCoroutine = null;
    }

    
    #endregion

    public void onRecipeSelected(Recipe newRecipe)
    {
        currentPage_Recipe = recipes != null ? recipes.IndexOf(newRecipe) : -1;
        prepareManager.onRecipeChosen(newRecipe);
        ChangeState(recipeState);
    }
}