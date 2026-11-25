using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MultiTargetVuforia : MonoBehaviour
{
    [SerializeField] private GameObject startModel;
    private int modelsCount;
    private int indexCurrentModel;
    private bool ready;

    private void Start()
    {
        modelsCount = transform.childCount;
        if (modelsCount == 0 || startModel == null || startModel.transform.parent != transform) {
            Debug.LogError("Asigna Start Model a un hijo directo del ImageTarget.", this);
            return;
        }
        indexCurrentModel = startModel.transform.GetSiblingIndex();
        for (int i = 0; i < modelsCount; i++)
            transform.GetChild(i).gameObject.SetActive(i == indexCurrentModel);
        ready = true;
    }
    private void Update()
    {
        if (!ready) return;
        bool previous = false, next = false;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null) {
            previous = keyboard.leftArrowKey.wasPressedThisFrame;
            next = keyboard.rightArrowKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        previous = Input.GetKeyDown(KeyCode.LeftArrow);
        next = Input.GetKeyDown(KeyCode.RightArrow);
#endif
        if (previous == next) return;
        ChangeARModel(previous ? -1 : 1);
    }
    public void ChangeARModel(int step)
    {
        if (!ready || modelsCount == 0) return;
        transform.GetChild(indexCurrentModel).gameObject.SetActive(false);
        indexCurrentModel = ((indexCurrentModel + step) % modelsCount + modelsCount) % modelsCount;
        transform.GetChild(indexCurrentModel).gameObject.SetActive(true);
    }
}
