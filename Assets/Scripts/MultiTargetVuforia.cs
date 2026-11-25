using UnityEngine;

public class MultiTargetVuforia : MonoBehaviour
{
    [SerializeField] private GameObject startModel;

    private int modelsCount;
    private int indexCurrentModel;
    private bool ready;

    private void Start()
    {
        modelsCount = transform.childCount;

        if (modelsCount == 0 || startModel == null)
        {
            Debug.LogError("Asigna Start Model y agrega los planetas.", this);
            return;
        }

        if (startModel.transform.parent != transform)
        {
            Debug.LogError(
                "Start Model debe ser un hijo directo de ImageTarget.", this);
            return;
        }

        indexCurrentModel = startModel.transform.GetSiblingIndex();

        for (int i = 0; i < modelsCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(
                i == indexCurrentModel);
        }

        ready = true;
    }

    public void ChangeARModel(int step)
    {
        if (!ready)
            return;

        transform.GetChild(indexCurrentModel).gameObject.SetActive(false);

        indexCurrentModel =
            ((indexCurrentModel + step) % modelsCount + modelsCount)
            % modelsCount;

        transform.GetChild(indexCurrentModel).gameObject.SetActive(true);
    }
}