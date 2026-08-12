using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tool_Interaction : MonoBehaviour
{
    public List<GameObject> children = new List<GameObject>();

    [Header("¸ú×Ù")]
    public bool followMouse;

    [Header("¶¯»­")]
    public Animator targetAnim;

    private void Awake()
    {
        children.Clear();

        foreach (Transform child in transform)
        {
            if (child != null)
            {
                children.Add(child.gameObject);
            }
        }

        Debug.Log($"[Tool_Interaction] Initialized with {children.Count} children");
    }

    private void Update()
    {
        if(followMouse)
        {
            transform.position = Tools.getMousePos();
        }
    }

    public void OpenGameObject(string name)
    {
        GameObject child = FindChildByName(name);

        if (child == null)
        {
            Debug.LogWarning($"[Tool_Interaction] OpenGameObject: child '{name}' not found");
            return;
        }

        child.SetActive(true);
    }

    public void CloseGameObject(string name)
    {
        GameObject child = FindChildByName(name);

        if (child == null)
        {
            Debug.LogWarning($"[Tool_Interaction] CloseGameObject: child '{name}' not found");
            return;
        }

        child.SetActive(false);
    }

    public void ToggleGameObject(string name)
    {
        GameObject child = FindChildByName(name);

        if (child == null)
        {
            Debug.LogWarning($"[Tool_Interaction] ToggleGameObject: child '{name}' not found");
            return;
        }

        child.SetActive(!child.activeSelf);
    }

    public void OpenOnly(string name)
    {
        GameObject targetChild = FindChildByName(name);

        if (targetChild == null)
        {
            Debug.LogWarning($"[Tool_Interaction] OpenOnly: child '{name}' not found");
            return;
        }

        foreach (GameObject child in children)
        {
            if (child != null)
            {
                child.SetActive(false);
            }
        }

        targetChild.SetActive(true);
    }

    public void OpenGameObjectAndCloseOthers(string name)
    {
        GameObject targetChild = FindChildByName(name);

        if (targetChild == null)
        {
            Debug.LogWarning($"[Tool_Interaction] OpenGameObjectAndCloseOthers: child '{name}' not found");
            return;
        }

        foreach (GameObject child in children)
        {
            if (child != null)
            {
                child.SetActive(child == targetChild);
            }
        }
    }

    public void CloseGameObjectAndOpenOthers(string name)
    {
        GameObject targetChild = FindChildByName(name);

        if (targetChild == null)
        {
            Debug.LogWarning($"[Tool_Interaction] CloseGameObjectAndOpenOthers: child '{name}' not found");
            return;
        }

        foreach (GameObject child in children)
        {
            if (child != null)
            {
                child.SetActive(child != targetChild);
            }
        }
    }

    public void CloseAll()
    {
        foreach (GameObject child in children)
        {
            if (child != null)
            {
                child.SetActive(false);
            }
        }
    }

    public void OpenAll()
    {
        foreach (GameObject child in children)
        {
            if (child != null)
            {
                child.SetActive(true);
            }
        }
    }

    private GameObject FindChildByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[Tool_Interaction] FindChildByName: name is null or empty");
            return null;
        }

        foreach (GameObject child in children)
        {
            if (child == null) continue;

            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    public void PlayAnim(string name)
    {
        if(targetAnim != null)
        {
            targetAnim.speed = 1;
            targetAnim.Play(name);
        }
    }

    public void PauseAnim()
    {
        if(targetAnim != null)
        {
            targetAnim.speed = 0;
        }
    }

    public void StopAnim(string DefaultAnimName)
    {
        if (targetAnim != null)
        {
            targetAnim.Rebind();
            targetAnim.speed = 1;
            targetAnim.Play(DefaultAnimName);
        }
    }
}
