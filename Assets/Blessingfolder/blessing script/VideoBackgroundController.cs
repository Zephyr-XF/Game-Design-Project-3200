using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
public class VideoBackgroundController : MonoBehaviour
{
    private RawImage rawImage;
    private VideoPlayer videoPlayer;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        // Prepare the video to avoid lag
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Create a Render Texture matching the video size
        RenderTexture renderTexture = new RenderTexture((int)source.width, (int)source.height, 0);
        
        // Assign texture to both Video Player and Raw Image
        source.targetTexture = renderTexture;
        rawImage.texture = renderTexture;
        
        // Start playing
        source.Play();
    }
}
