using UnityEngine;

/// <summary>
/// 这是一个“信号中转器”脚本。
/// 专门用于解决 Timeline Signal Receiver 无法在跨场景时准确引用 DontDestroyOnLoad 物体的问题。
/// 挂载在每个场景中的本地物体上，通过它来调用全局管理器的单例。
/// </summary>
public class GlobalManagerRelay : MonoBehaviour
{
    /// <summary>
    /// 在 Timeline 结束时调用，用于关闭对话框和恢复游戏状态
    /// </summary>
    public void RelayEndTimeline()
    {
        Debug.Log($"<color=cyan>[GlobalManagerRelay]</color> 收到 RelayEndTimeline 信号。当前物体: {gameObject.name}, 场景: {gameObject.scene.name}");

        // 1. 检查 GameManager
        if (GameManager.instance == null)
        {
            Debug.LogError("<color=red>[GlobalManagerRelay]</color> 严重错误：GameManager.instance 为空！信号中转失败。");
            return;
        }

        // 2. 检查 UIManager
        if (UIManager.instance == null)
        {
            Debug.LogError("<color=red>[GlobalManagerRelay]</color> 严重错误：UIManager.instance 为空！即使调用 EndTimeline 也无法关闭 UI。");
        }
        else if (UIManager.instance.dialogueBox == null)
        {
            Debug.LogWarning("<color=orange>[GlobalManagerRelay]</color> 警告：UIManager 存在，但其 dialogueBox 引用为空！请检查该场景的 UIManager 配置。");
        }

        Debug.Log("<color=green>[GlobalManagerRelay]</color> 成功触发 GameManager.EndTimeline 调用。");
        GameManager.instance.EndTimeline();
    }

    /// <summary>
    /// 在 Timeline 暂停时调用（例如对话片段开始时）
    /// </summary>
    public void RelayPauseTimeline(UnityEngine.Playables.PlayableDirector director)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.PauseTimeline(director);
        }
    }

    /// <summary>
    /// 如果你需要手动触发玩家数据保存
    /// </summary>
    public void RelaySavePlayerData(EntityCore player)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.SavePlayerData(player);
        }
    }
}
