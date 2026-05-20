using TMPro;
using UnityEngine;

public class Refuse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TMP_InputField InputField;
    

    // Update is called once per frame
    public void Void()
    {
        InputField.text = "";
    }
}
