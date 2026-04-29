using UnityEngine;

public class LoopingBackground : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    private float width;

    void Start()
    {
        width = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x <= -width)
        {
            transform.position += new Vector3(width * 2f, 0, 0);
        }
    }
}