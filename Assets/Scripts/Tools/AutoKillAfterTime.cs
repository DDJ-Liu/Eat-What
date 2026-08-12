using System.Collections;
using UnityEngine;

public class AutoKillAfterTime : MonoBehaviour
{
    public enum KillMode
    {
        Disable,
        Destroy
    }

    [SerializeField] private float threshold = 1f;
    [SerializeField] private KillMode killMode = KillMode.Destroy;

    private void OnEnable()
    {
        StartCoroutine(KillAfterTime());
    }

    private IEnumerator KillAfterTime()
    {
        yield return new WaitForSeconds(threshold);

        switch (killMode)
        {
            case KillMode.Disable:
                gameObject.SetActive(false);
                break;
            case KillMode.Destroy:
                Destroy(gameObject);
                break;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
