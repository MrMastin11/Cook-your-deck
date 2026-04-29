using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PackController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sin movement")]
    [Tooltip("Амплітуда у пікселях (для UI) або у одиницях світу (для world-space).")]
    public float amplitude = 10f;
    [Tooltip("Частота в Hz (ціль: цикли за секунду).")]
    public float frequency = 0.5f;

    [Header("Hover / Scale")]
    [Tooltip("Коефіцієнт масштабу при наведенні")]
    public float hoverScale = 1.1f;
    [Tooltip("Швидкість інтерполяції масштабу (секунди)")]
    public float scaleDuration = 0.12f;

    [Header("Click / Particles")]
    [Tooltip("Префаб системи частинок, яка запускається при кліці")]
    public ParticleSystem particlePrefab;
    [Tooltip("Точка спавна частинок; якщо не вказана — використовується позиція паку")]
    public Transform particleSpawnPoint;

    [Header("Behaviour")]
    [Tooltip("Об'єкт, що містить метод ThreeCards() — викликається після кліку")]
    public GameObject cardManagerObject;
    [Tooltip("Час відтворення ефекту зникнення (сек)")]
    public float disappearDuration = 0.15f;

    public DeckManager deckManager; 

    // внутрішні
    RectTransform rt;
    Vector2 startAnchoredPos;     
    Vector3 startLocalPos;      
    float timeCounter;
    bool isUI = true;
    bool paused = false;
    bool clicked = false;
    Coroutine scaleCoroutine;

    // store the original scale so we can restore exactly what existed before
    Vector3 originalScale;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        isUI = rt != null && rt.transform.parent != null;
        if (isUI)
            startAnchoredPos = rt.anchoredPosition;
        else
            startLocalPos = transform.localPosition;

        // capture the original localScale at Awake
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (paused || clicked) return;

        timeCounter += Time.deltaTime;

        float yOffset = Mathf.Sin(timeCounter * frequency * Mathf.PI * 2f) * amplitude;

        if (isUI)
        {
            rt.anchoredPosition = startAnchoredPos + new Vector2(0f, yOffset);
        }
        else
        {
            Vector3 p = startLocalPos;
            p.y += yOffset;
            transform.localPosition = p;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (clicked) return;
        paused = true;
        StartScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (clicked) return;
        paused = false;
        StartScale(originalScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked) return;
        clicked = true;
        paused = true;
        SpawnParticles();
        deckManager.ThreeCards();
        StartCoroutine(DisappearAndDestroy());
    }
    void StartScale(Vector3 target)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(target));
    }

    IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 initial = transform.localScale;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, scaleDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.localScale = Vector3.Lerp(initial, target, k);
            yield return null;
        }
        transform.localScale = target;
        scaleCoroutine = null;
    }

    void SpawnParticles()
    {
        if (particlePrefab == null) return;
        Vector3 spawnPos = (particleSpawnPoint != null) ? particleSpawnPoint.position : transform.position;
        ParticleSystem ps = Instantiate(particlePrefab, spawnPos, Quaternion.identity, null);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    IEnumerator DisappearAndDestroy()
    {
        // stop any running scale coroutine so it doesn't race with the disappear animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        Vector3 fromScale = transform.localScale;
        Vector3 toScale = Vector3.zero;
        float t = 0f;
        float dur = Mathf.Max(0.01f, disappearDuration);
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        // Fade out and scale down
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.localScale = Vector3.Lerp(fromScale, toScale, k);
            cg.alpha = 1f - k;
            yield return null;
        }
        transform.localScale = toScale;
        cg.alpha = 0f;

        // Wait a short moment (optional)
        yield return new WaitForSeconds(0.05f);

        // Restore original scale (not Vector3.one) and keep invisible
        transform.localScale = originalScale;
        cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void SetPaused(bool value)
    {
        paused = value;
    }
    public void ResetPack()
    {
        clicked = false;
        paused = false;

        transform.localScale = originalScale;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        gameObject.SetActive(true);

        timeCounter = 0f;
    }
}