using UnityEngine;

// 这个属性可以确保Player对象上一定有一个AudioSource组件
// 我们会用到两个，一个用于一次性音效，一个用于循环的脚步声
[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("用于播放攻击、技能、冲刺等一次性音效的音源")]
    public AudioSource sfxSource;
    [Tooltip("用于播放走路、跑步等循环音效的音源")]
    public AudioSource footstepSource;

    [Header("Combat SFX")]
    public AudioClip[] basicAttackClips; // 使用数组可以每次随机播放一个，增加多样性
    public AudioClip skill1Clip;
    public AudioClip skill2Clip;
    public AudioClip skill3Clip;

    [Header("Movement SFX")]
    public AudioClip dashClip;
    public AudioClip walkingClip;
    public AudioClip runningClip;

    private void Awake()
    {
        // 如果没有在Inspector中手动拖拽，就自动获取
        if (sfxSource == null || footstepSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length >= 2)
            {
                sfxSource = sources[0];
                footstepSource = sources[1];
                Debug.LogWarning("PlayerAudio: AudioSources were not assigned. Automatically assigned the first two found. Please assign them manually in the Inspector for clarity.");
            }
            else
            {
                Debug.LogError("PlayerAudio requires two AudioSource components on the GameObject. One for SFX and one for footsteps.");
            }
        }
    }

    // --- 供其他脚本调用的公共方法 ---

    #region Combat Audio
    public void PlayBasicAttack(int comboStep)
    {
        // 安全检查：确保数组不为空，并且传入的连击数在有效范围内
        if (basicAttackClips != null && basicAttackClips.Length > 0 && comboStep > 0)
        {
            // 数组索引是从0开始的，而我们的连击数是从1开始，所以要减1
            // 使用 Mathf.Min 来防止索引越界，如果连击数超过了音效数量，就播放最后一个音效
            int clipIndex = Mathf.Min(comboStep - 1, basicAttackClips.Length - 1);

            AudioClip clipToPlay = basicAttackClips[clipIndex];

            if (clipToPlay != null)
            {
                sfxSource.PlayOneShot(clipToPlay);
            }
        }
    }

    public void PlaySkill1()
    {
        if (skill1Clip != null) sfxSource.PlayOneShot(skill1Clip);
    }

    public void PlaySkill2()
    {
        if (skill2Clip != null) sfxSource.PlayOneShot(skill2Clip);
    }

    public void PlaySkill3()
    {
        if (skill3Clip != null) sfxSource.PlayOneShot(skill3Clip);
    }
    #endregion

    #region Movement Audio
    public void PlayDash()
    {
        if (dashClip != null) sfxSource.PlayOneShot(dashClip);
    }

    // 管理脚步声的方法
    public void ManageFootsteps(bool isMoving, bool isRunning)
    {
        if (isMoving)
        {
            AudioClip targetClip = isRunning ? runningClip : walkingClip;

            // 如果当前播放的不是目标音效，或者音源没有在播放
            if (footstepSource.clip != targetClip || !footstepSource.isPlaying)
            {
                footstepSource.clip = targetClip;
                footstepSource.Play(); // 因为footstepSource设置了循环，所以会一直播放
            }
        }
        else
        {
            // 如果不在移动，就停止脚步声
            footstepSource.Stop();
        }
    }
    #endregion

    public void StopAllAudio()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (footstepSource != null) footstepSource.Stop();
    }
}

