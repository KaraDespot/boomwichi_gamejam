using UnityEngine;

public class LoadingScreenManager : MonoBehaviour
{
    private Animator _animatorComponent;
    private DemoSceneManager _demoSceneManager;

    [SerializeField] private bool hideOnStart = false;
    [SerializeField] private bool revealOnStart = true;

    private void Start()
    {
        _animatorComponent = transform.GetComponent<Animator>();
        _demoSceneManager = transform.parent != null ? transform.parent.GetComponent<DemoSceneManager>() : null;

        if (_animatorComponent != null && revealOnStart)
        {
            _animatorComponent.ResetTrigger("Hide");
            _animatorComponent.SetTrigger("Reveal");
        }

        if (hideOnStart)
        {
            HideLoadingScreen();
        }
    }

    public void RevealLoadingScreen()
    {
        _animatorComponent.SetTrigger("Reveal");
    }

    public void HideLoadingScreen()
    {
        // Call this function, if you want start hiding the loading screen
        _animatorComponent.SetTrigger("Hide");
    }

    public void OnFinishedReveal()
    {
        if (_demoSceneManager != null)
        {
            _demoSceneManager.OnLoadingScreenRevealed();
        }
    }

    public void OnFinishedHide()
    {
        if (_demoSceneManager != null)
        {
            _demoSceneManager.OnLoadingScreenHided();
        }
    }
}
