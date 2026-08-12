using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ingredient_Interaction : MonoBehaviour
{
    public InteractionController myController;
    public Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void PlayAnim(int i)
    {
        string animName = $"Resolve_{i}";
        Debug.Log(animName);
        if (Tools.AnimatorHasState(anim, animName))
        {
            anim.speed = 1;
            anim.Play(animName);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Does not have Anim {animName}!");

            // 注释掉：边界情况不再汇报完成
            /*if (myController != null)
            {
                myController.OnIngredientAnimComplete(this);
            }*/
        }
    }

    public void AnimEnd()
    {
        anim.speed = 0;
        Debug.Log($"[Ingredient_Interaction] {gameObject.name} animation finished");

        // 注释掉：动画完成不再向上汇报
        /*if (myController != null)
        {
            myController.OnIngredientAnimComplete(this);
        }
        else
        {
            Debug.LogWarning($"[Ingredient_Interaction] {gameObject.name} has no controller reference!");
        }*/
    }

    public void ResolveAnimEnd()
    {
        anim.speed = 0;
        myController.ActionCompleted();
    }
}
