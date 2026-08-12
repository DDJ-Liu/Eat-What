using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookCustomer : MonoBehaviour
{
    public CustomerManager myCustomerManager;
    public int uid;
    public string CustomerPlaceOrderDialogName;
    public Recipe myRecipe;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Used when Customer Button Clicked. | set CustomerManager currentCustomer -> CookingManager next state;
    /// </summary>
    public void onChoose()
    {
        myCustomerManager.currentCustomer = this;
        myCustomerManager.onCustomerSelected(CustomerPlaceOrderDialogName);
        myCustomerManager.onCustomerChoosed(myRecipe);
    }
}
