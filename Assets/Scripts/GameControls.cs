using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет глобальными игровыми действиями: пауза, скорость времени, перезапуск и выход.
/// </summary>
public class GameControls : MonoBehaviour
{
    private bool isPaused = false;
    private float prePauseTimeScale = 1f;

    /// <summary>
    /// Переключает состояние паузы в игре.
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            // Запоминаем текущую скорость времени, чтобы восстановить ее после паузы.
            prePauseTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = prePauseTimeScale;
        }
    }

    /// <summary>
    /// Устанавливает скорость течения времени в игре.
    /// </summary>
    /// <param name="scale">Множитель скорости (1.0f - нормальная, 2.0f - двойная и т.д.).</param>
    public void SetTimeScale(float scale)
    {
        // Не меняем скорость, если игра на паузе, но запоминаем выбор.
        prePauseTimeScale = scale;
        if (!isPaused)
        {
            Time.timeScale = scale;
        }
    }

    /// <summary>
    /// Перезапускает текущую сцену для нового боя.
    /// </summary>
    public void Rematch()
    {
        // Важно сбросить Time.timeScale перед перезагрузкой,
        // иначе новая сцена может начаться на паузе или в ускорении.
        Time.timeScale = 1f;
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    /// <summary>
    /// Закрывает приложение.
    /// </summary>
    public void QuitApplication()
    {
        Application.Quit();
        
        // Эта директива нужна, чтобы кнопка "Выход" работала и в редакторе Unity.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}