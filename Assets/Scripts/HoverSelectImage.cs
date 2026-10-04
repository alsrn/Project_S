using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSelectImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("마우스를 올렸을 때 보여줄 오브젝트 (SelectImage)")]
    [SerializeField] private GameObject selectImage;

    private void Awake()
    {
        if (selectImage == null)
        {
            Transform found = transform.Find("SelectImage");
            if (found != null) selectImage = found.gameObject;
        }

        if (selectImage != null) selectImage.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectImage != null) selectImage.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectImage != null) selectImage.SetActive(false);
    }

    private void OnDisable()
    {
        if (selectImage != null) selectImage.SetActive(false);
    }
}