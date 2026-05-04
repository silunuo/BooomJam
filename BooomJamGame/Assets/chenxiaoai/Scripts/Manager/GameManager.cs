using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public enum GameMode
    {
        GamePlay,
        DialogueMoment
    }
    public GameMode gameMode;
    private PlayableDirector currentPlayableDirector;
    private double currentClipEndTime;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        DontDestroyOnLoad(gameObject);

        gameMode = GameMode.GamePlay;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (gameMode == GameMode.DialogueMoment)
            {
                if (UIManager.instance.IsTyping)
                {
                    UIManager.instance.CompleteTypewriter();
                }
                else
                {
                    ResumeTimeline();
                }
            }
            else if (gameMode == GameMode.GamePlay && UIManager.instance.IsTyping)
            {
                // 如果正在播放片段时按下空格
                UIManager.instance.CompleteTypewriter();
                // 将 Timeline 时间跳转到当前片段的末尾，触发暂停
                if (currentPlayableDirector != null)
                {
                    currentPlayableDirector.time = currentClipEndTime;
                }
            }
        }
    }

    public void OnDialogueClipStart(PlayableDirector director, double endTime)
    {
        currentPlayableDirector = director;
        currentClipEndTime = endTime;
    }

    public void PauseTimeline(PlayableDirector _playableDirector)
    {
        currentPlayableDirector = _playableDirector;
        gameMode = GameMode.DialogueMoment;
        
        // 只有在 Graph 有效时才设置速度
        if (currentPlayableDirector != null && currentPlayableDirector.playableGraph.IsValid())
        {
            currentPlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(0d);
        }

        if (UIManager.instance != null) UIManager.instance.ToggleSpaceBar(true);
    }

    public void ResumeTimeline()
    {
        if (currentPlayableDirector == null) return;

        // 检查是否已经到达或接近 Timeline 的终点
        if (currentPlayableDirector.time >= currentPlayableDirector.duration - 0.1f)
        {
            EndTimeline();
            return;
        }

        gameMode = GameMode.GamePlay;
        
        if (currentPlayableDirector.playableGraph.IsValid())
        {
            currentPlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(1d);
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.ToggleSpaceBar(false);
            UIManager.instance.ToggleDialogueBox(true);
        }
    }

    public void EndTimeline()
    {
        Debug.Log("[GameManager] EndTimeline 被调用了！");
        gameMode = GameMode.GamePlay;
        
        if (UIManager.instance != null)
        {
            Debug.Log("[GameManager] 正在调用 UIManager 关闭对话框...");
            UIManager.instance.ToggleSpaceBar(false);
            UIManager.instance.ToggleDialogueBox(false);
        }
        else
        {
            Debug.LogError("[GameManager] UIManager.instance 为空，无法关闭 UI！");
        }

        if (currentPlayableDirector != null)
        {
            currentPlayableDirector.Stop();
        }
    }
}
