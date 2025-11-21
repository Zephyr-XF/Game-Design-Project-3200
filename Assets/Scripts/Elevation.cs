using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Elevation_Entry : MonoBehaviour
{
    //表示是否进入高地
    static bool isEnterHigh = false;



    //用常量表示图层名称，避免硬编码
    static string  PlayuerHigh = "PlayerHigh";
    static string  PlayerLow = "PlayerLow";
    
    
    public Collider2D[] Mountian_Colliders;
    public Collider2D[] Mountain_AirWalls;



    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isEnterHigh)
        {
            Enter_elevation(collision);
            isEnterHigh = true;
        }else { 
            Exit_Elevation(collision);
            isEnterHigh= false;
        }
    }

    public void Enter_elevation(Collider2D player)
    {
        if (player.gameObject.tag == "Player")
        {
            foreach (Collider2D mountains in Mountian_Colliders)
            {
                mountains.enabled = false;
                Debug.Log("Mountains Disabled");
            }
            player.gameObject.GetComponent<SpriteRenderer>().sortingLayerName = PlayuerHigh;
            isEnterHigh = true;

            foreach (Collider2D airwalls in Mountain_AirWalls)
            {
                airwalls.enabled = true;
                Debug.Log("AirWalls Enabled");
            }

        }
    }

    public void Exit_Elevation(Collider2D player)
    {
        if (player.gameObject.tag == "Player")
        {
            foreach (Collider2D mountains in Mountian_Colliders)
            {
                mountains.enabled = true;
                Debug.Log("Mountains Enabled");
            }
            player.gameObject.GetComponent<SpriteRenderer>().sortingLayerName = PlayerLow;
            isEnterHigh = false;

            foreach (Collider2D airwalls in Mountain_AirWalls)
            {
                airwalls.enabled = false;
                Debug.Log("AirWalls Disabled");
            }

        }
    }
}
