using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("インベントリ設定")]
    [SerializeField] private int inventoryNumber;
    [SerializeField] private GameObject player;
    private GetItem getItem;
    [SerializeField] private List<Image> itemImages;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        getItem = player.GetComponent<GetItem>();
    }

    // Update is called once per frame
    void Update()
    {
        // なぜか回ってます
        if(getItem.inventory.Count == inventoryNumber)
        {
            if(getItem.canGrow)
            {
                itemImages[0].enabled = true;
                itemImages[1].enabled = false;
            }
            else if (getItem.iswhite)
            {
                itemImages[1].enabled = true;
                itemImages[0].enabled = false;
            }
            else
            {
                itemImages[0].enabled = false;
                itemImages[1].enabled = false;
            }
        }
        else if(getItem.inventory.Count == inventoryNumber - 1)
        {
            itemImages[0].enabled = false;
            itemImages[1].enabled = false;
        }
        else if(getItem.inventory.Count == 0)
        {
            itemImages[0].enabled = false;
            itemImages[1].enabled = false;
        }
    }
}
