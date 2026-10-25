using UnityEngine;

public class ButtonDetector : MonoBehaviour
{
    public string tagThatWorks = "Button";
    public GameObject catDoor;
    public Sprite straightBlack, straightBlue;
    public Sprite cornerBlack, cornerBlue;
    public Sprite crossBlack, crossBlue;

    private SpriteRenderer[] wireRenderers;

    private void Start()
    {
        // Find all wire pieces in children
        wireRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void SetWireColors(bool active)
    {
        foreach (var sr in wireRenderers)
        {
            if (sr.sprite == straightBlack) sr.sprite = active ? straightBlue : straightBlack;
            else if (sr.sprite == straightBlue) sr.sprite = active ? straightBlue : straightBlack;

            else if (sr.sprite == cornerBlack) sr.sprite = active ? cornerBlue : cornerBlack;
            else if (sr.sprite == cornerBlue) sr.sprite = active ? cornerBlue : cornerBlack;

            else if (sr.sprite == crossBlack) sr.sprite = active ? crossBlue : crossBlack;
            else if (sr.sprite == crossBlue) sr.sprite = active ? crossBlue : crossBlack;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagThatWorks))
        {
            if (catDoor != null){
            catDoor.GetComponent<GateController>()?.OpenGate();
            catDoor.GetComponent<BoxCollider2D>().enabled = false;
            }
            SetWireColors(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(tagThatWorks))
        {
            if (catDoor != null){
            catDoor.GetComponent<GateController>()?.CloseGate();
            catDoor.GetComponent<BoxCollider2D>().enabled = true;
            }
            SetWireColors(false);
        }
    }
}
