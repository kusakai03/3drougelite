using TMPro;
using UnityEngine;

public class BlessingBuffRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text buffText;

    public void Setup(int index, string text)
    {

        if (indexText != null)
        {
            indexText.text = $"[{index}]";
        }
        if (buffText != null)
        {
            buffText.text = text;
        }
    }
}