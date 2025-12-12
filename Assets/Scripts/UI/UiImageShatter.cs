using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UiImageShatter : MonoBehaviour
{
    public static UiImageShatter Instance;

    [Header("Settings")]
    public int rows = 10; // 切割行数
    public int cols = 10; // 切割列数
    public float explosionForce = 1500f; // 爆炸力度 (默认调大)
    public float explosionRadius = 100f; 
    public float gravityScale = 1.0f; // 重力大小 (值越小掉得越慢, 默认改成1.0感受下)
    public float debrisLifetime = 3.0f; // 碎片存活时间

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 粉碎目标 RawImage
    /// </summary>
    /// <param name="target">要粉碎的目标UI组件</param>
    public void Shatter(RawImage target)
    {
        if (target == null || target.texture == null) return;

        // 获取目标的大小和位置
        RectTransform targetRect = target.rectTransform;
        float width = targetRect.rect.width;
        float height = targetRect.rect.height;
        
        // 计算每个碎片的尺寸
        float chunkWidth = width / cols;
        float chunkHeight = height / rows;

        // 生成碎片父物体（保持在原来的层级）
        GameObject debrisRoot = new GameObject("Debris_Root");
        debrisRoot.transform.SetParent(targetRect.parent, false);
        debrisRoot.transform.position = targetRect.position;
        debrisRoot.transform.localScale = Vector3.one;

        // 隐藏本体
        target.gameObject.SetActive(false);

        // 循环生成碎片
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                CreatePiece(x, y, chunkWidth, chunkHeight, width, height, target.texture, debrisRoot.transform);
            }
        }

        // 销毁所有碎片
        Destroy(debrisRoot, debrisLifetime);
    }

    private void CreatePiece(int x, int y, float chunkW, float chunkH, float totalW, float totalH, Texture tex, Transform parent)
    {
        GameObject piece = new GameObject($"Piece_{x}_{y}");
        piece.transform.SetParent(parent, false);

        // 1. 设置 Image 组件
        RawImage img = piece.AddComponent<RawImage>();
        img.texture = tex;
        
        // 关键：计算 UV 矩形 (切片)
        // UV坐标是 0~1，所以要除以总行列数
        float uvW = 1.0f / cols;
        float uvH = 1.0f / rows;
        img.uvRect = new Rect(x * uvW, y * uvH, uvW, uvH);

        // 2. 设置位置和大小
        RectTransform rect = piece.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(chunkW, chunkH);
        
        // 计算其在父物体中的局部坐标 (以中心为原点)
        // x * chunkW 是左下角，要加上半个宽移到中心，再减去总宽度的一半
        float posX = (x * chunkW) + (chunkW / 2) - (totalW / 2);
        float posY = (y * chunkH) + (chunkH / 2) - (totalH / 2);
        rect.anchoredPosition = new Vector2(posX, posY);

        // 3. 添加物理组件 (2D)
        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale; // 使用配置的重力
        
        // 给一个随机的爆炸力
        Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDir * explosionForce * Random.Range(0.5f, 1.5f));
        
        // 也可以加一点旋转力
        rb.AddTorque(Random.Range(-50f, 50f));
    }
}
