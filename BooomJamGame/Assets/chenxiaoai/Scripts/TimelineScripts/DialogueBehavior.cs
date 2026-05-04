using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class DialogueBehavior : PlayableBehaviour
{
    private PlayableDirector playableDirector;

    public string characterName;
    [TextArea(8, 1)] public string dialogueLine;
    public int dialogueSize;
    public Sprite portrait;

    private bool isClipPlayed;
    public bool requirePause;
    private bool pauseScheduled;

    public override void OnPlayableCreate(Playable playable)
    {
        playableDirector = playable.GetGraph().GetResolver() as PlayableDirector;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (isClipPlayed == false && info.weight > 0)
        {
            UIManager.instance.StartTypewriter(characterName, dialogueLine, dialogueSize, portrait);
            
            // 计算当前片段在整个 Timeline 中的结束时间
            if (playableDirector != null)
            {
                double clipEndTime = playableDirector.time + playable.GetDuration();
                GameManager.instance.OnDialogueClipStart(playableDirector, clipEndTime);
            }

            if (requirePause)
            {
                pauseScheduled = true;
            }
            isClipPlayed = true;
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        isClipPlayed = false;

        // 检查是否是因为时间轴播放完毕导致的暂停（即 playhead 已经到达或超过 clip 终点）
        // 或者 playable 的时间已经等于持续时间
        double duration = playable.GetDuration();
        double time = playable.GetTime();

        if (pauseScheduled || (requirePause && time >= duration && duration > 0))
        {
            pauseScheduled = false;
            UIManager.instance.CompleteTypewriter();
            GameManager.instance.PauseTimeline(playableDirector);
        }
        else
        {
            // 只有在不是因为需要暂停的情况下才隐藏对话框
            // 避免在初始化或中途暂停时意外隐藏
            if (playableDirector != null && playableDirector.state != PlayState.Playing)
            {
                 // 如果是整个 Director 停止了，可以隐藏
                 // UIManager.instance.ToggleDialogueBox(false);
            }
        }
    }
}
