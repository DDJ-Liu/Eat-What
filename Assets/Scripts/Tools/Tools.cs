using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
/*using UnityEngine.Localization;
using UnityEngine.Localization.Settings;*/

public static class Tools
{
    public static Vector3 getMousePos()
    {
        //�������Ļλ��
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        //ת���������λ��
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane));
        //2D��
        mouseWorldPosition.z = 0;

        return mouseWorldPosition;
    }

    // ��̬�������������ֽ�Ϊ List<int>
    public static List<int> SplitDigits(int number)
    {
        List<int> digits = new List<int>();

        // תΪ�ַ���������ַ�����Ϊ����
        foreach (char c in Mathf.Abs(number).ToString())
        {
            digits.Add(int.Parse(c.ToString()));
        }

        // ��ת List
        digits.Reverse();

        return digits;
    }

    public static bool AnimatorHasParameter(Animator animator, string parameterName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName)
            {
                return true;
            }
        }
        return false;
    }

    public static bool AnimatorHasState(Animator animator, string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        // 检查所有层中的状态名称
        int layerCount = animator.layerCount;
        int stateHash = Animator.StringToHash(animationName);

        for (int i = 0; i < layerCount; i++)
        {
            if (animator.HasState(i, stateHash))
            {
                return true;
            }
        }

        // 如果状态名称未找到，也检查AnimationClip的名称
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return true;
            }
        }

        return false;
    }

    /* ·����
     * Resources �ļ��з�ʽ
     * �� Prefab �洢�� Assets/Resources/Prefabs �У�·��Ϊ Prefabs/MyPrefab��
    */
    public static GameObject LoadAndInstantiatePrefab(string path)
    {
        // �� Resources �ļ��м��� Prefab
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab != null)
        {
            // ʵ���� Prefab
            GameObject instance = GameObject.Instantiate(prefab);
            instance.name = prefab.name; // ��ѡ������ʵ������������һ��
            return instance;
        }
        else
        {
            Debug.LogError($"Failed to load prefab at path: {path}");
            return null;
        }
    }

    public static GameObject LoadAndInstantiatePrefab(string path, Transform parent)
    {
        // �� Resources �ļ��м��� Prefab
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab != null)
        {
            // ʵ���� Prefab
            GameObject instance = GameObject.Instantiate(prefab, parent);
            instance.name = prefab.name; // ��ѡ������ʵ������������һ��
            if(parent != null)
            {
                instance.transform.parent = parent;
                instance.transform.localPosition = Vector3.zero;
            }
            return instance;
        }
        else
        {
            Debug.LogError($"Failed to load prefab at path: {path}");
            return null;
        }
    }

    /* ·����
     * Resources �ļ��з�ʽ
     * �� Prefab �洢�� Assets/Resources/Prefabs �У�·��Ϊ Prefabs/MyPrefab��
    */
    public static GameObject LoadPrefabOnly(string path)
    {
        // �� Resources �ļ��м��� Prefab
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null) Debug.LogError($"Failed {path}");
        return prefab;
    }

    public static bool checkWithin_Square(Vector2 target, Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR)
    {
        if(target.x < TL.y || target.x > BR.y || target.y < TL.x || target.y > BR.x)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static Vector2 CalculateVectorsCenter(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
        {
            return Vector2.zero;
        }

        // �ȴ��� y ֵ�������� y ���� -1
        List<Vector2> processedPoints = new List<Vector2>();
        foreach (Vector2 point in points)
        {
            processedPoints.Add(new Vector2(point.x, point.y * -1f));
        }

        // ��ʼ����С�����ֵ
        float minX = processedPoints[0].x;
        float maxX = processedPoints[0].x;
        float minY = processedPoints[0].y;
        float maxY = processedPoints[0].y;

        // �������д�����ĵ㣬�ҵ���С������ x �� y
        foreach (Vector2 point in processedPoints)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        // ������ε����ĵ�
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        return new Vector2(centerX, centerY);
    }

    public static Vector2 Rotate90Clockwise(Vector2 v)
    {
        // ��ת�����ʵ�� (˳ʱ����ת 90 ��)
        return new Vector2(-v.y, v.x);
    }

    public static Vector2 Rotate90CounterClockwise(Vector2 v)
    {
        // ��ת�����ʵ�� (��ʱ����ת 90 ��)
        return new Vector2(v.y, -v.x);
    }

    public static Vector2 RotateVector2(Vector2 v, int rotationTime)
    {
        int i = 0;
        Vector2 tempV = v;
        while (i < rotationTime)
        {
            tempV = Rotate90Clockwise(tempV);
            i++;
        }

        return tempV;
    }

    public static List<T> RemoveDuplicateInList<T>(List<T> toCheck)
    {
        // ʹ�� HashSet ȥ���ظ���
        HashSet<T> uniqueItems = new HashSet<T>(toCheck);

        // �� HashSet ת���� List
        List<T> result = new List<T>(uniqueItems);

        return result;
    }

    public static Sprite LoadNumberSpriteSingle(string path)
    {
        string fullPath = "Numbers/" + path;
        //Debug.Log(fullPath);
        Sprite find = Resources.Load<Sprite>(fullPath);
        return find;
    }

    public static Sprite LoadSpriteSingle(string path)
    {
        Sprite find = Resources.Load<Sprite>(path);
        return find;
    }

    /// <summary>
    /// 从 Resources 文件夹中加载指定路径下的所有 Sprite，并按文件名后缀数字排序
    /// 例如：frame_0, frame_1, frame_2... 或 img0, img1, img2...
    /// </summary>
    /// <param name="folderPath">Resources 文件夹下的相对路径，如 "Transitions/FadeOut"</param>
    /// <returns>按后缀数字排序的 Sprite 数组</returns>
    public static Sprite[] LoadSpritesFromFolder(string folderPath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(folderPath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[Tools] No sprites found at path: {folderPath}");
            return new Sprite[0];
        }

        // 按文件名后缀数字排序
        System.Array.Sort(sprites, (a, b) =>
        {
            int numA = ExtractNumberSuffix(a.name);
            int numB = ExtractNumberSuffix(b.name);
            return numA.CompareTo(numB);
        });

        Debug.Log($"[Tools] Loaded {sprites.Length} sprites from {folderPath}");
        return sprites;
    }

    /// <summary>
    /// 从字符串中提取尾部的数字后缀
    /// 例如："frame_10" -> 10, "img2" -> 2, "test_abc_5" -> 5
    /// </summary>
    private static int ExtractNumberSuffix(string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        // 从字符串末尾开始，找到连续的数字
        int i = str.Length - 1;
        while (i >= 0 && char.IsDigit(str[i]))
        {
            i--;
        }

        // 如果找到了数字
        if (i < str.Length - 1)
        {
            string numberStr = str.Substring(i + 1);
            if (int.TryParse(numberStr, out int result))
            {
                return result;
            }
        }

        return 0;
    }

    public static Sprite LoadNumberSpriteMultiple(string path, int index)
    {
        string fullPath = "Numbers/" + path;
        Sprite[] find = Resources.LoadAll<Sprite>(fullPath);
        if(index < find.Length)
        {
            return find[index];
        }
        else
        {
            return null;
        }
        
    }

    public static float CalculatePercentage(float min, float max, float value)
    {
        // ���Ƿ���Χ����Сֵ���ܴ��ڵ������ֵ
        if (min >= max)
        {
            throw new ArgumentException(
                $"��Сֵ({min})����С�����ֵ({max})����Χ������Ч��");
        }

        // ����ٷֱȹ�ʽ��(value - min) / (max - min) * 100
        return (value - min) / (max - min);
    }

    /// <summary>
    /// ���ݰٷֱ�����Сֵ�����ֵ֮������Ӧ����ֵ
    /// </summary>
    /// <param name="percent">�ٷֱ�(0-1)</param>
    /// <param name="min">��Сֵ</param>
    /// <param name="max">���ֵ</param>
    /// <returns>min��max֮���percent%����ֵ</returns>
    public static float GetValueByPercent(float percent, float min, float max)
    {
        // ȷ��percent��0��1֮��
        percent = Mathf.Clamp01(percent);

        // ���㲢���ز�ֵ���
        return min + (max - min) * percent;
    }

    public static List<string> SplitString(string input, string separator)
    {
        if (string.IsNullOrEmpty(separator))
        {
            throw new ArgumentException("�ָ�������Ϊnull����ַ���", nameof(separator));
        }

        // ʹ��StringSplitOptions.Noneȷ���������ַ���Ԫ�أ������Ҫ������Ԫ�أ�
        string[] parts = input.Split(new[] { separator }, StringSplitOptions.None);

        // ֱ�ӷ��طָ�������ת���ɵ��б����ָ����в������ָ�����
        return new List<string>(parts);
    }

    /*public static string GetLocalizedStringFromTable(string tableName, string key, string languageCode = null)
    {
        // ��ȡ�ַ�����
        var table = LocalizationSettings.StringDatabase.GetTable(tableName);
        if (table == null)
        {
            Debug.LogError($"�� '{tableName}' ������");
            return null;
        }

        // ��ȡ��Ŀ
        var entry = table.GetEntry(key);
        if (entry == null)
        {
            Debug.LogError($"�� '{key}' �ڱ� '{tableName}' �в�����");
            return null;
        }

        // ��ȡ�ض����Ե�Locale
        Locale targetLocale = null;
        if (!string.IsNullOrEmpty(languageCode))
        {
            targetLocale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
        }

        if (targetLocale == null)
        {
            targetLocale = LocalizationSettings.SelectedLocale;
        }

        // ��ȡ���ػ��ַ���
        return entry.GetLocalizedString(targetLocale);
    }*/

    public static float Vector2CrossProduct(Vector2 vec1,Vector2 vec2)
    {
        return vec1.x * vec2.y - vec1.y * vec2.x;
    }

    /// <summary>
    /// 在子物体的 Canvas 下查找 Image 组件。
    /// 优先返回名称包含 "MainImage"（大小写无关）的 Image，否则返回第一个找到的。
    /// </summary>
    public static Image FindImageInChildCanvas(GameObject root)
    {
        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        if (canvases == null || canvases.Length == 0)
            return null;

        Image firstFound = null;

        foreach (Canvas canvas in canvases)
        {
            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject == canvas.gameObject)
                    continue;

                if (img.gameObject.name.IndexOf("MainImage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return img;

                if (firstFound == null)
                    firstFound = img;
            }
        }

        return firstFound;
    }

    public static bool TryRemoveFromStack<T>(Stack<T> stack, T item)
    {
        if (stack == null) throw new ArgumentNullException(nameof(stack));

        var temp = new Stack<T>();
        bool found = false;

        while (stack.Count > 0)
        {
            T current = stack.Pop();
            if (!found && EqualityComparer<T>.Default.Equals(current, item))
            {
                found = true; // �ҵ�Ŀ�꣬��ѹ�� temp
                continue;
            }
            temp.Push(current);
        }

        // �� temp �е�Ԫ������ѹ��ԭջ��ע��˳��ᷴת����Ϊ temp Ҳ��ջ��
        while (temp.Count > 0)
        {
            stack.Push(temp.Pop());
        }

        return found;
    }
}

[System.Serializable]
public class CompositeData
{
    public int Int;
    public float Float;
    public string String;
    public CompositeData(string rawData)
    {
        if(rawData == "null")
        {
            return;
        }
        int.TryParse(rawData, out Int);
        float.TryParse(rawData, out Float);
        String = rawData;
    }

    public override string ToString()
    {
        return $"Int: {this.Int}| Float�� {this.Float}| String: {this.String}";
    }
}
