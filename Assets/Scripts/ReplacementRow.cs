using UnityEngine;
using UnityEngine.UI;
using System;
using Michsky.UI.Heat;
using TMPro;

public class ReplacementRow : MonoBehaviour
{
    [Header("Поля ввода")]
    public TMP_InputField searchInput;
    public TMP_InputField replacementInput1;
    public TMP_InputField replacementInput2;
    public TMP_InputField replacementInput3;

    [Header("Кнопка удаления")]
    public ButtonManager deleteButton;
    
    public Action<ReplacementRow> OnDelete;
    
    public void SetupDeleteButton()
    {
        if(deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                if(OnDelete != null)
                    OnDelete(this);
            });
        }
    }
}