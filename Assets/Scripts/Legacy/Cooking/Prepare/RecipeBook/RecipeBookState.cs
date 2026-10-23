using UnityEngine;

public abstract class RecipeBookStateBase
{
    public MouseInteractionLayer myLayer;
    public Transform SpawnParent;
    public GameObject nextButton;
    public GameObject prevButton;

    public virtual void EnterState(RecipeBookManager rb)
    {
        if (myLayer != null) myLayer.OnPushLayer();
    }
    public virtual void UpdateState(RecipeBookManager rb) { }
    public virtual void ExitState(RecipeBookManager rb)
    {
        if (myLayer != null) myLayer.OnRemoveLayer();
    }

    public virtual void OnNextPage(RecipeBookManager rb) { }
    public virtual void OnPrevPage(RecipeBookManager rb) { }
    public virtual void NextPage(RecipeBookManager rb) { }
    public virtual void PreviousPage(RecipeBookManager rb) { }
    public virtual void NextButtonCondition(RecipeBookManager rb, ConditionReceiver c) { c.ReportCondition(false); }
    public virtual void PrevButtonCondition(RecipeBookManager rb, ConditionReceiver c) { c.ReportCondition(false); }

    public virtual void OnUpdatePage(RecipeBookManager rb) { }
}

[System.Serializable]
public class RecipeBookMenuState : RecipeBookStateBase
{
    public override void EnterState(RecipeBookManager rb)
    {
        base.EnterState(rb);
        rb.UpdatePage();
    }

    public override void OnNextPage(RecipeBookManager rb)
    {
        if (rb.currentPage_Menu >= rb.MaxPage_Menu) return;
        if (rb.flipCoroutine != null) return;
        rb.prepareManager.onTempPreventControl();
        nextButton.SetActive(false);
        prevButton.SetActive(false);
        rb.flipAnim_RightToLeft.SetActive(true);
        Animator anim = rb.flipAnim_RightToLeft.GetComponent<Animator>();
        anim.Play("Flip");

        if (rb.currentPage_Menu == 0)
        {
            rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
            rb.NextPage();
            rb.UpdatePage();
            rb.currentpageObject.transform.Find("LeftPaper").gameObject.SetActive(false);
        }
        else
        {
            rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
            rb.NextPage();
            rb.UpdateRightPage();
        }
    }

    public override void OnPrevPage(RecipeBookManager rb)
    {
        if (rb.currentPage_Menu <= 0) return;
        if (rb.flipCoroutine != null) return;
        rb.prepareManager.onTempPreventControl();
        nextButton.SetActive(false);
        prevButton.SetActive(false);
        rb.flipAnim_LeftToRight.SetActive(true);
        Animator anim = rb.flipAnim_LeftToRight.GetComponent<Animator>();
        anim.Play("Flip");
        if (rb.currentPage_Menu == 1)
        {
            rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
            rb.PreviousPage();
            rb.currentpageObject.transform.Find("LeftPaper").gameObject.SetActive(false);
        }
        else
        {
            rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
            rb.PreviousPage();
            rb.UpdateLeftPage();
        }
    }

    public override void NextPage(RecipeBookManager rb)
    {
        rb.currentPage_Menu++;
    }

    public override void PreviousPage(RecipeBookManager rb)
    {
        rb.currentPage_Menu--;
    }

    public override void NextButtonCondition(RecipeBookManager rb, ConditionReceiver c)
    {
        c.ReportCondition(rb.currentPage_Menu < rb.MaxPage_Menu);
    }

    public override void PrevButtonCondition(RecipeBookManager rb, ConditionReceiver c)
    {
        c.ReportCondition(rb.currentPage_Menu > 0);
    }

    public override void OnUpdatePage(RecipeBookManager rb)
    {
        Debug.Log("[RecipeBook] PageUpdate Start - page: " + rb.currentPage_Menu);

        if (rb.currentpageObject != null)
            Object.Destroy(rb.currentpageObject);

        rb.leftPageObjects.Clear();
        rb.rightPageObjects.Clear();
        rb.recipeObjects.Clear();

        if (rb.currentPage_Menu == 0)
        {
            rb.currentpageObject = Object.Instantiate(rb.TitlePagePrefab, SpawnParent);
        }
        else
        {
            SpawnListPage(rb);
            UpdateRecipeObjectVisibility(rb);
        }

        Debug.Log("[RecipeBook] PageUpdate End - page: " + rb.currentPage_Menu);
    }

    public void OnUpdateLeftPage(RecipeBookManager rb)
    {
        if (rb.currentpageObject == null) return;
        Transform leftPaper = rb.currentpageObject.transform.Find("LeftPaper");
        if (leftPaper == null) return;

        rb.leftPageObjects.Clear();
        rb.leftPageObjects.Add(leftPaper.gameObject);

        Transform leftGrid = leftPaper.Find("GridTray");
        if (leftGrid == null) return;

        int objectsPerPage = rb.recipePerPage * 2;
        int startIndex = (rb.currentPage_Menu - 1) * objectsPerPage;
        int recipeCount = rb.recipes != null ? rb.recipes.Count : 0;

        for (int i = 0; i < leftGrid.childCount; i++)
        {
            GameObject obj = leftGrid.GetChild(i).gameObject;
            int recipeIndex = startIndex + i;
            obj.SetActive(recipeIndex < recipeCount);

            RecipeBookButton button = obj.GetComponent<RecipeBookButton>();
            if (button != null && recipeIndex < recipeCount)
                button.onSetInfo(rb, rb.recipes[recipeIndex]);
        }
    }

    public void OnUpdateRightPage(RecipeBookManager rb)
    {
        if (rb.currentpageObject == null || rb.currentpageObject.transform.Find("RightPaper") == null)
        {
            if (rb.currentpageObject != null)
                Object.Destroy(rb.currentpageObject);

            rb.leftPageObjects.Clear();
            rb.rightPageObjects.Clear();
            rb.recipeObjects.Clear();

            SpawnListPage(rb);
        }

        Transform rightPaper = rb.currentpageObject.transform.Find("RightPaper");
        if (rightPaper == null) return;

        rb.rightPageObjects.Clear();
        rb.rightPageObjects.Add(rightPaper.gameObject);

        Transform rightGrid = rightPaper.Find("GridTray");
        if (rightGrid == null) return;

        int objectsPerPage = rb.recipePerPage * 2;
        int startIndex = (rb.currentPage_Menu - 1) * objectsPerPage + rb.recipePerPage;
        int recipeCount = rb.recipes != null ? rb.recipes.Count : 0;

        for (int i = 0; i < rightGrid.childCount; i++)
        {
            GameObject obj = rightGrid.GetChild(i).gameObject;
            int recipeIndex = startIndex + i;
            obj.SetActive(recipeIndex < recipeCount);

            RecipeBookButton button = obj.GetComponent<RecipeBookButton>();
            if (button != null && recipeIndex < recipeCount)
                button.onSetInfo(rb, rb.recipes[recipeIndex]);
        }
    }

    private void SpawnListPage(RecipeBookManager rb)
    {
        rb.currentpageObject = Object.Instantiate(rb.ListPagePrefab, SpawnParent);

        Transform leftPaper = rb.currentpageObject.transform.Find("LeftPaper");
        Transform rightPaper = rb.currentpageObject.transform.Find("RightPaper");

        rb.leftPageObjects.Add(leftPaper.gameObject);
        rb.rightPageObjects.Add(rightPaper.gameObject);

        Transform leftGrid = leftPaper.Find("GridTray");
        for (int i = 0; i < leftGrid.childCount; i++)
            rb.recipeObjects.Add(leftGrid.GetChild(i).gameObject);

        Transform rightGrid = rightPaper.Find("GridTray");
        for (int i = 0; i < rightGrid.childCount; i++)
            rb.recipeObjects.Add(rightGrid.GetChild(i).gameObject);

        InitRecipeButtons(rb);
    }

    private void InitRecipeButtons(RecipeBookManager rb)
    {
        int objectsPerPage = rb.recipePerPage * 2;
        int startIndex = (rb.currentPage_Menu - 1) * objectsPerPage;
        int recipeCount = rb.recipes != null ? rb.recipes.Count : 0;

        for (int i = 0; i < rb.recipeObjects.Count; i++)
        {
            RecipeBookButton button = rb.recipeObjects[i].GetComponent<RecipeBookButton>();
            if (button == null) continue;

            int recipeIndex = startIndex + i;
            if (recipeIndex < recipeCount)
                button.onSetInfo(rb, rb.recipes[recipeIndex]);
        }
    }

    private void UpdateRecipeObjectVisibility(RecipeBookManager rb)
    {
        int objectsPerPage = rb.recipePerPage * 2;
        int startIndex = (rb.currentPage_Menu - 1) * objectsPerPage;
        int recipeCount = rb.recipes != null ? rb.recipes.Count : 0;

        for (int i = 0; i < rb.recipeObjects.Count; i++)
        {
            int recipeIndex = startIndex + i;
            rb.recipeObjects[i].SetActive(recipeIndex < recipeCount);
        }
    }
}

[System.Serializable]
public class RecipeBookRecipeState : RecipeBookStateBase
{
    public override void EnterState(RecipeBookManager rb)
    {
        base.EnterState(rb);
        rb.UpdatePage();
    }

    public override void ExitState(RecipeBookManager rb)
    {
        base.ExitState(rb);
    }

    public override void OnNextPage(RecipeBookManager rb)
    {
        if (rb.currentPage_Recipe >= rb.MaxPage_Recipe) return;
        if (rb.flipCoroutine != null) return;
        //clearSpawnedObject(rb);
        rb.prepareManager.onTempPreventControl();
        nextButton.SetActive(false);
        prevButton.SetActive(false);
        rb.flipAnim_RightToLeft.SetActive(true);
        Animator anim = rb.flipAnim_RightToLeft.GetComponent<Animator>();
        anim.Play("Flip");

        rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
        rb.NextPage();
        //rb.UpdatePage();
    }

    public override void OnPrevPage(RecipeBookManager rb)
    {
        if (rb.currentPage_Recipe <= 0) return;
        if (rb.flipCoroutine != null) return;
        //clearSpawnedObject(rb);
        rb.prepareManager.onTempPreventControl();
        nextButton.SetActive(false);
        prevButton.SetActive(false);
        rb.flipAnim_LeftToRight_YReverse.SetActive(true);
        Animator anim = rb.flipAnim_LeftToRight_YReverse.GetComponent<Animator>();
        anim.Play("Flip");

        rb.flipCoroutine = rb.StartCoroutine(rb.WaitFlipThenExecute(anim, "Flip", true));
        rb.PreviousPage();
        //rb.UpdatePage();
    }

    public override void NextPage(RecipeBookManager rb)
    {
        rb.currentPage_Recipe++;
    }

    public override void PreviousPage(RecipeBookManager rb)
    {
        rb.currentPage_Recipe--;
    }

    public override void NextButtonCondition(RecipeBookManager rb, ConditionReceiver c)
    {
        c.ReportCondition(rb.currentPage_Recipe < rb.MaxPage_Recipe);
    }

    public override void PrevButtonCondition(RecipeBookManager rb, ConditionReceiver c)
    {
        c.ReportCondition(rb.currentPage_Recipe > 0);
    }

    public override void OnUpdatePage(RecipeBookManager rb)
    {
        if (rb != null && rb.RecipeSpawnParent != null)
        {
            for (int i = rb.RecipeSpawnParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(rb.RecipeSpawnParent.GetChild(i).gameObject);
            }
        }

        rb.RecipeDataObject = Tools.LoadAndInstantiatePrefab($"Prepare/RecipePage/{rb.recipes[rb.currentPage_Recipe].name}", rb.RecipeSpawnParent);
    }

    private void SpawnListPage(RecipeBookManager rb)
    {
        
    }

    private void InitRecipeButtons(RecipeBookManager rb)
    {
        
    }

    private void UpdateRecipeObjectVisibility(RecipeBookManager rb)
    {
        
    }

    public void clearSpawnedObject(RecipeBookManager rb)
    {
        if (rb != null && rb.RecipeSpawnParent != null)
        {
            for (int i = rb.RecipeSpawnParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(rb.RecipeSpawnParent.GetChild(i).gameObject);
            }
        }
    }
}
