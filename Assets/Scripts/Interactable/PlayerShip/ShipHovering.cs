using UnityEngine;

public class ShipHovering : MonoBehaviour
{
    private void OnMouseEnter()
    {
        UIManager.Instance.OpenShipStatUI();
    }

    private void OnMouseExit()
    {
        UIManager.Instance.CloseShipStatUI();
    }
}