using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class switchObject : MonoBehaviour
{
    public bool startsPresent = true;
    public Sprite presentSprite;
    public Sprite pastSprite;
    public UnityEvent onSwitchPresent;
    public UnityEvent onSwitchPast;
    private SpriteRenderer spriteRenderer;
    public bool present = true;


    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (startsPresent)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = presentSprite;
            }     
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = pastSprite;
            }
        }
    }

    public void Switch()
    {
        if (present == true)
        {
            spriteRenderer.sprite = pastSprite;
            onSwitchPast?.Invoke();
            present = false;
        }
        else
        {
            spriteRenderer.sprite = presentSprite;
            onSwitchPresent?.Invoke();
            present = true;
        }
    }
}
