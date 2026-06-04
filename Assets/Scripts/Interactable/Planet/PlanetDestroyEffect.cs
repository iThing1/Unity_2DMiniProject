using System.Collections;
using UnityEngine;

// 행성 파괴 연출 전담 컴포넌트
public class PlanetDestroyEffect : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("파티클 설정")]
    [SerializeField] private int _particleCount = 20;
    [SerializeField] private float _particleSpeed = 3f;
    [SerializeField] private float _particleDuration = 1.0f;
    [SerializeField] private float _particleSize = 1.5f;
    [SerializeField] private Color _particleColorInner = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color _particleColorOuter = new Color(1f, 0.2f, 0.2f);

    private Sprite _circleSprite;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Play(System.Action onComplete)
    {
        StartCoroutine(PlayRoutine(onComplete));
    }

    // =========================================================================
    // 연출 코루틴
    // =========================================================================
    private IEnumerator PlayRoutine(System.Action onComplete)
    {
        SpawnParticles();
        yield return new WaitForSecondsRealtime(_particleDuration);
        onComplete?.Invoke();
    }

    private void SpawnParticles()
    {
        _circleSprite = CreateCircleSprite();

        for (int i = 0; i < _particleCount; i++)
        {
            GameObject particle = new GameObject("Particle");
            particle.transform.position = transform.position;

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = _circleSprite;
            renderer.color = Color.Lerp(_particleColorInner, _particleColorOuter, Random.value);
            renderer.sortingOrder = 100;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(_particleSpeed * 0.5f, _particleSpeed);
            float size = Random.Range(_particleSize * 0.5f, _particleSize);
            particle.transform.localScale = Vector3.one * size;

            StartCoroutine(MoveParticle(particle, dir * speed));
        }
    }

    private IEnumerator MoveParticle(GameObject particle, Vector2 velocity)
    {
        float elapsed = 0f;
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        Color startColor = renderer.color;

        while (elapsed < _particleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _particleDuration;

            particle.transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);

            renderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        Destroy(particle);
    }

    private Sprite CreateCircleSprite()
    {
        int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}