using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("(client) Clicked: " + gameObject.tag);
        PlayerItemSystem.Client_ClickItem(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerItemSystem.Client_StartHoverItem(gameObject.tag);
        // todo: outline item w/shader or something
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerItemSystem.Client_StopHoverItem();
        // todo: item back to normal
    }
}
