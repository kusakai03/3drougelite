
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GlobalCanvas : MonoBehaviour
{
    public static GlobalCanvas Instance { get; private set; }

    [Header("Error UI")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button errorConfirmButton;

    private Action currentAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        errorPanel.SetActive(false);
        errorConfirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void SendErrorLog(string log, Action action)
    {
        currentAction = action;
        errorText.text = log;
        errorPanel.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        errorPanel.SetActive(false);

        Action action = currentAction;
        currentAction = null;

        action?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        errorConfirmButton.onClick.RemoveListener(OnConfirmClicked);
        Instance = null;
    }
}
