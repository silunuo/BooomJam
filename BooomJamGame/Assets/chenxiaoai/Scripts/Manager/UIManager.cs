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

    public float typewriterSpeed = 0.05f; 
    private Coroutine typewriterCoroutine; 
    private string currentFullDialogue; 

    public bool IsTyping => typewriterCoroutine != null;

    private void Awake()
    {
        instance = this;
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
