using UnityEngine;

/// <summary>
/// Chạy chuỗi khung hình mặt mèo (cắt từ atlas Spine ingame_cat_face_03) trên SpriteRenderer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CatFaceAnimator : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private CatFaceAnimation _animation;
    private float _fps = 24f;
    private float _time;
    private int _frame = -1;

    public void Play(CatFaceAnimation animation, float fps, float width)
    {
        _renderer = GetComponent<SpriteRenderer>();
        _animation = animation;
        _fps = Mathf.Max(1f, fps);
        _time = Random.Range(0f, animation.frames.Count / _fps); // lệch pha để các mèo không chớp mắt cùng lúc

        float k = width / Mathf.Max(0.0001f, animation.frames[0].bounds.size.x);
        transform.localScale = new Vector3(k, k, 1f);

        ShowFrame(0);
    }

    private void Update()
    {
        if (_animation == null || _animation.frames.Count == 0) return;

        _time += Time.deltaTime;
        int frame = (int)(_time * _fps) % _animation.frames.Count;
        if (frame != _frame) ShowFrame(frame);
    }

    private void ShowFrame(int index)
    {
        _frame = index;
        _renderer.sprite = _animation.frames[index];
    }
}
