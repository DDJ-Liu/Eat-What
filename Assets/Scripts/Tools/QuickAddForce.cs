using UnityEngine;

public class QuickAddForce : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb2D;
    public bool started = false;

    [Header("力参数")]
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private float speed = 10f;

    private void Awake()
    {
        if (rb2D == null)
        {
            rb2D = GetComponent<Rigidbody2D>();
        }
        rb2D.gravityScale = 0f;
        started = false;
    }

    private void Update()
    {
        rb2D.gravityScale = started? 6f : 0f; 
    }

    public void ApplyForce()
    {
        if (rb2D != null)
        {
            //rb2D.gravityScale = 6f;
            started = true;
            Vector2 force = direction.normalized * speed;
            rb2D.velocity = force;
        }
    }
}
