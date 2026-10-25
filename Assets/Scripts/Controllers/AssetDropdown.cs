using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AssetDropdown : MonoBehaviour
{
    public TMP_Text selectedText;
    public GameObject dropdownList;
    public GameObject optionPrefab; // Button prefab with TMP_Text
    public Transform optionContainer;

    [System.Serializable]
    public class AssetOption
    {
        public string name;
        public Object asset;
    }

    public List<AssetOption> options = new List<AssetOption>();
    public UnityEvent<Object> onAssetSelected;

    private bool isOpen = false;

    void Start()
    {
        dropdownList.SetActive(false);
        PopulateOptions();
    }

    public void ToggleDropdown()
    {
        isOpen = !isOpen;
        dropdownList.SetActive(isOpen);
    }

    private void PopulateOptions()
    {
        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

        foreach (var option in options)
        {
            var btn = Instantiate(optionPrefab, optionContainer);
            btn.GetComponentInChildren<TMP_Text>().text = option.name;

            var asset = option.asset; // Capture for lambda
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedText.text = option.name;
                dropdownList.SetActive(false);
                isOpen = false;
                onAssetSelected?.Invoke(asset);
            });
        }
    }
}
