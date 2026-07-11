using UnityEngine;

public class SaveSlotSelectionController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    public void OpenForNewGame()
    {
        StartGameAndClose();
    }

    public void OpenForContinue()
    {
        StartGameAndClose();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void StartGameAndClose()
    {
        Close();

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }
}
