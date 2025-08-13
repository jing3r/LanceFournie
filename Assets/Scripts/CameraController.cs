using UnityEngine;
using System.Collections;

/// <summary>
/// Управляет перемещением камеры между предопределенными точками обзора.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    [Tooltip("Позиция и вращение камеры для фазы драфта (общий вид).")]
    public Transform draftView;
    [Tooltip("Позиция и вращение камеры для фазы расстановки.")]
    public Transform placementView;
    [Tooltip("Позиция и вращение камеры для фазы боя.")]
    public Transform battleView;

    [Header("Animation Settings")]
    [Tooltip("Время, за которое камера переместится из одной точки в другую.")]
    [SerializeField] private float transitionDuration = 1.5f;

    private void OnEnable()
    {
        BattleManager.OnPlacementPhaseStarted += MoveToPlacementView;
        BattleManager.OnBattleStarted += MoveToBattleView;
    }
    private void OnDisable()
    {
        BattleManager.OnPlacementPhaseStarted -= MoveToPlacementView;
        BattleManager.OnBattleStarted -= MoveToBattleView;
    }


    private void Start()
    {
        if (draftView != null)
        {
            transform.position = draftView.position;
            transform.rotation = draftView.rotation;
        }
    }
    private void MoveToPlacementView()
    {
        if (placementView != null)
        {
            StartCoroutine(TransitionTo(placementView.position, placementView.rotation));
        }
    }

    private void MoveToBattleView()
    {
        if (battleView != null)
        {
            StartCoroutine(TransitionTo(battleView.position, battleView.rotation));
        }
    }

    private IEnumerator TransitionTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        float elapsedTime = 0f;
        Vector3 startingPos = transform.position;
        Quaternion startingRot = transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, elapsedTime / transitionDuration);

            transform.position = Vector3.Lerp(startingPos, targetPosition, progress);
            transform.rotation = Quaternion.Slerp(startingRot, targetRotation, progress);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}