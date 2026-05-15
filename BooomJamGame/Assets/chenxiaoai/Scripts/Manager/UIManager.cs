using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject dialogueBox;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueLineText;
    public GameObject spacebar;
    public Image characterPortrait;

    [Header("Card Info Panel")]
    public GameObject cardInfoPanel;
    public TextMeshProUGUI infoAtkText;
    public TextMeshProUGUI infoDefText;
    public TextMeshProUGUI infoHpText;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoSkillsText;

    [Header("Top Right HUD")]
    public TextMeshProUGUI goldText;

    [Header("Skill Tree Panel")]
    public GameObject skillTreePanel;

    public float typewriterSpeed = 0.05f; 
    private Coroutine typewriterCoroutine; 
    private string currentFullDialogue; 

    public bool IsTyping => typewriterCoroutine != null;

    private void Awake()
    {
        instance = this;
        if (cardInfoPanel != null) cardInfoPanel.SetActive(false);
        if (skillTreePanel != null) skillTreePanel.SetActive(false);
    }

    public void ShowCardInfo(EntityCore core)
    {
        if (cardInfoPanel == null || core == null) return;

        if (infoNameText != null) infoNameText.text = core.entityName;
        if (infoAtkText != null) infoAtkText.text = core.attack.ToString();
        if (infoDefText != null) infoDefText.text = core.defense.ToString();
        if (infoHpText != null) infoHpText.text = $"{core.currentHealth}/{core.maxHealth}";
        if (infoSkillsText != null) 
        {
            if (core.skills != null && core.skills.Count > 0)
                infoSkillsText.text = string.Join(", ", core.skills);
            else
                infoSkillsText.text = "无";
        }

        cardInfoPanel.SetActive(true);
        // 标记刚刚开启，防止同一帧的点击立刻关闭它
        StartCoroutine(ResetClickFlag());
    }

    private bool justOpened = false;
    private IEnumerator ResetClickFlag()
    {
        justOpened = true;
        yield return new WaitForEndOfFrame();
        justOpened = false;
    }

    /// <summary>
    /// UI 按钮点击事件：打开技能树界面
    /// </summary>
    public void OnSkillTreeButtonClick()
    {
        if (skillTreePanel != null)
        { 
            skillTreePanel.SetActive(true);
            // 可以在这里调用面板的刷新逻辑
            skillTreePanel.GetComponent<SkillTreePanel>()?.RefreshAllNodes();
        }
        else
        { 
            Debug.LogWarning("[UIManager] 场景中没有找到 SkillTreePanel 引用。");
        }
    }

    private void Update()
    {
        // 实时更新金钱显示
        UpdateGoldUI();

        // 如果面板开启中，且玩家点击了鼠标左键
        if (cardInfoPanel != null && cardInfoPanel.activeSelf && Input.GetMouseButtonDown(0) && !justOpened)
        {
            // 检测点击是否在面板外
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                cardInfoPanel.GetComponent<RectTransform>(), 
                Input.mousePosition, 
                null))
            {
                HideCardInfo();
            }
        }
    }

    public void UpdateGoldUI()
    {
        if (goldText == null)
        {
            Debug.LogWarning("[UIManager] Gold Text 引用为空！请检查 Inspector 中的绑定。");
            return;
        }
        
        // 遍历所有 EntityCore，找到真正的 Player
        EntityCore player = null;
        foreach (var core in FindObjectsOfType<EntityCore>())
        {
            if (core.type == EntityType.Player)
            {
                player = core;
                break;
            }
        }
        
        if (player != null)
        { 
            goldText.text = $"Gold: {player.gold}";
        }
    }

    /// <summary>
    /// UI 按钮点击事件：打开商店界面
    /// </summary>
    public void OnShopButtonClick()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.instance.OpenShop();
        }
        else
        {
            Debug.LogWarning("[UIManager] 场景中没有找到 ShopManager 实例，无法打开商店。");
        }
    }

    public void HideCardInfo()
    {
        if (cardInfoPanel != null) cardInfoPanel.SetActive(false);
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    public void StartTypewriter(string _name, string _line, int _size)
    {
        StopTypewriter();
        characterNameText.text = _name;
        dialogueLineText.fontSize = _size;
        dialogueLineText.text = "";
        currentFullDialogue = _line; 
        typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
        ToggleDialogueBox(true);
    }

    public void StartTypewriter(string _name, string _line, int _size, Sprite _portrait)
    {
        StopTypewriter();
        characterNameText.text = _name;
        dialogueLineText.fontSize = _size;
        dialogueLineText.text = "";
        currentFullDialogue = _line;
        typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
        if (characterPortrait != null)
        {
            if (_portrait != null)
            {
                characterPortrait.sprite = _portrait;
                characterPortrait.enabled = true;
                characterPortrait.preserveAspect = true;
            }
            else
            {
                characterPortrait.sprite = null;
                characterPortrait.enabled = false;
            }
        }
        ToggleDialogueBox(true);
    }

    private IEnumerator TypewriterCoroutine()
    {
        for (int i = 0; i < currentFullDialogue.Length; i++)
        {
            dialogueLineText.text = currentFullDialogue.Substring(0, i + 1);
            yield return new WaitForSeconds(typewriterSpeed);
        }
        typewriterCoroutine = null;
    }

    public void CompleteTypewriter()
    {
        StopTypewriter();
        dialogueLineText.text = currentFullDialogue;
    }

    public void ToggleDialogueBox(bool _isActive)
    {
        Debug.Log($"[UIManager] ToggleDialogueBox 试图设置为: {_isActive}");
        if (!_isActive)
        {
            StopTypewriter();
            if (characterNameText != null) characterNameText.text = "";
            if (dialogueLineText != null) dialogueLineText.text = "";
        }
        
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(_isActive);
            Debug.Log($"[UIManager] dialogueBox 当前状态已设置为: {dialogueBox.activeSelf}");
        }
        else
        {
            Debug.LogError("[UIManager] dialogueBox 引用丢失！");
        }
    }

    public void ToggleSpaceBar(bool _isActive)
    {
        spacebar.SetActive(_isActive);
    }

    public void SetupDialogue(string _name, string _line, int _size)
    {
        StartTypewriter(_name, _line, _size);
    }
}
