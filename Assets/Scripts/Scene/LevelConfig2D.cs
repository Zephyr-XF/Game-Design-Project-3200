using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "Game/LevelData")]
public class LevelConfig2D : ScriptableObject
{
    [Header("关卡名")]
    public string levelName;

    [Header("本关玩家出生点 ID")]
    [Tooltip("对应场景中 SpawnPoint2D.spawnId")]
    public int playerSpawnId;

    [Header("关卡ID")]
    public int levelID = 0;
}