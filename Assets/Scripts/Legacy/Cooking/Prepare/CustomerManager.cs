using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    #region FSM
    private CustomerPhaseStateBase currentState;
    public CustomerIdleState idleState = new CustomerIdleState();
    public ChooseCustomerState chooseCustomerState = new ChooseCustomerState();
    public CustomerDialogState customerDialogState = new CustomerDialogState();

    public void ChangeState(CustomerPhaseStateBase newState)
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

    public CustomerPhaseStateBase GetCurrentState()
    {
        return currentState;
    }

    [TextArea, SerializeField] private string currentStateIdentifier;
    #endregion

    #region Singleton
    public static CustomerManager Instance;
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

    public Transform CustomerParent;
    public int CustomerPoolUid;
    public List<CookCustomer> waitingCustomers = new List<CookCustomer>();
    public DialogSystem PlaceOrderDialogSystem;
    public DialogSystem ReviewDialogSystem;
    /// <summary>
    /// The Customer player clicked, should be null when intialized.
    /// </summary>
    public CookCustomer currentCustomer;
    public Recipe currentRecipe;

    void Start()
    {
        ChangeState(chooseCustomerState);
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
        #region Generate Customers
        //TODO: GenerateLogic
        //Set information / instantiate customer
        //Set waitingCustomers list
        currentCustomer = null;
        CustomerParent.gameObject.SetActive(true);
        #endregion
    }

    public void onCustomerChoosed(Recipe targetRecipe)
    {
        if (targetRecipe == null)
        {
            return;
        }
        currentRecipe = targetRecipe;
        //currentRecipe.initializeRecipe();
        Debug.Log("CustomerChoosed");
        ChangeState(customerDialogState);
    }

    public void onCustomerSelected(string dialogName)
    {
        PlaceOrderDialogSystem.gameObject.SetActive(true);
        PlaceOrderDialogSystem.Initialize(dialogName);
    }

    public void onCustomerOrderDialogFinished()
    {
        PlaceOrderDialogSystem.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void onCustomerServiceEnd()
    {
        if (waitingCustomers.Contains(currentCustomer))
        {
            waitingCustomers.Remove(currentCustomer);
        }
        currentCustomer = null;
    }
}
