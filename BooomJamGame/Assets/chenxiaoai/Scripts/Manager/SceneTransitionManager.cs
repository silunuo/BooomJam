using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 场景切换转换管理器。
/// 负责在场景跳转时播放黑屏淡入淡出动画。
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;

    [Header("UI References")]
    public Canvas transitionCanvas;
    public CanvasGroup fadeCanvasGroup;
    public Image fadeImage;

    [Header("Settings")]
    public float fadeDuration = 1.0f;
    public Color fadeColor = Color.black;

    private void Awake()
    {
        // 单例保护并跨场景保留
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 确保初始状态是透明的
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            if (transitionCanvas != null)
            {
                transitionCanvas.sortingOrder = 999; // 确保在最上层
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 带有淡入淡出效果的场景跳转
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(PerformTransition(sceneName));
    }

    /// <summary>
    /// 瞬间变黑并开始加载新场景（跳过淡入动画）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void InstantTransitionToScene(string sceneName)
    {
        StartCoroutine(PerformInstantTransition(sceneName));
    }

    private IEnumerator PerformInstantTransition(string sceneName)
    {
        if (fadeCanvasGroup == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // 1. 瞬间变黑
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        // 2. 加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. 场景加载完成后，正常执行淡出动画
        yield return fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PerformTransition(string sceneName)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[SceneTransitionManager] 未分配 fadeCanvasGroup！直接跳转。");
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // 1. 开始黑屏淡入
        fadeCanvasGroup.blocksRaycasts = true;
        yield return fadeCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        // 2. 加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. 开始黑屏淡出
        yield return fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();
        fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 仅播放淡入（变黑）
    /// </summary>
    public Tween FadeIn()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        return fadeCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 仅播放淡出（变透明）
    /// </summary>
    public Tween FadeOut()
    {
        return fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear).OnComplete(() => {
            fadeCanvasGroup.blocksRaycasts = false;
        });
    }
}
