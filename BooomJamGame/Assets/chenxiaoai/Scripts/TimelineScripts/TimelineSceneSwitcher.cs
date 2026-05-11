using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// 监听指定的 PlayableDirector，当 Timeline 播放结束时自动跳转到目标场景。
/// </summary>
public class TimelineSceneSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("要监听的 PlayableDirector 组件")]
    public PlayableDirector director;
    
    [Tooltip("Timeline 结束后要跳转的场景名称")]
    public string targetSceneName = "Scene2";

    private void OnEnable()
    {
        if (director != null)
        {
            director.stopped += OnTimelineStopped;
        }
    }

    private void OnDisable()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
    }

    private void OnTimelineStopped(PlayableDirector obj)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("TimelineSceneSwitcher: 目标场景名称为空！");
            return;
        }

        Debug.Log($"Timeline 播放结束，正在跳转到场景: {targetSceneName}");
        
        // 如果存在转换管理器，使用瞬间黑屏跳转（跳过淡入）
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.InstantTransitionToScene(targetSceneName);
        }
        else
        {
            // 否则直接跳转
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
