using UnityEngine;

public class UIMenu : MonoBehaviour
{
    public void PlayGameSolo()
    {
        MenuFunction.Instance.PlayLocal();
    }
}
