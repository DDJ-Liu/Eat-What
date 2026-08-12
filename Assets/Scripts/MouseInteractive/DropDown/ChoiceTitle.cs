using TMPro;
using UnityEngine;

public class ChoiceTitle : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;

    private void Awake()
    {
        if (titleText == null)
            titleText = GetComponentInChildren<TMP_Text>();

        if (titleText != null)
            titleText.text = gameObject.name;
    }
}
