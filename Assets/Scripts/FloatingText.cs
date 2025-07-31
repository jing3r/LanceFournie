using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Управляет поведением одного элемента всплывающего текста (анимация, цвет, самоуничтожение).
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    private TextMeshPro textMesh;

    [Header("Animation Settings")]
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private Vector3 moveVector = new Vector3(0, 1.5f, 0);

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    /// <summary>
    /// Инициирует отображение текста и запускает его анимацию.
    /// </summary>
    public void Show(string message, Color color)
    {
        textMesh.text = message;
        textMesh.color = color;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPosition = transform.position;
        Color startColor = textMesh.color;
        float timer = 0f;

        while (timer < lifetime)
        {
            transform.position = Vector3.Lerp(startPosition, startPosition + moveVector, timer / lifetime);
            
            // Плавное исчезновение в последнюю треть жизни
            if (timer > lifetime * 0.66f)
            {
                float fadeProgress = (timer - (lifetime * 0.66f)) / (lifetime * 0.34f);
                textMesh.color = Color.Lerp(startColor, Color.clear, fadeProgress);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        FeedbackManager.Instance.ReturnTextToPool(this);
    }
}