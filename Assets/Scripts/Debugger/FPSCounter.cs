using UnityEngine;
using System.Collections.Generic;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime = 0.0f;

    [SerializeField] private float averageWindow = 1.0f;
    private Queue<float> frameTimeSamples = new Queue<float>();
    private Queue<float> timeStamps = new Queue<float>();

    public string FpsText { get; private set; } = "Calculating...";

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float currentFps = 1.0f / deltaTime;
        float currentFrameTimeMs = deltaTime * 1000.0f;

        float currentTime = Time.unscaledTime;
        frameTimeSamples.Enqueue(deltaTime);
        timeStamps.Enqueue(currentTime);

        while (timeStamps.Count > 0 && currentTime - timeStamps.Peek() > averageWindow)
        {
            timeStamps.Dequeue();
            frameTimeSamples.Dequeue();
        }

        float sumFrameTime = 0.0f;
        foreach (float ft in frameTimeSamples)
        {
            sumFrameTime += ft;
        }

        int sampleCount = frameTimeSamples.Count;
        float averageFrameTime = sumFrameTime / sampleCount;
        float averageFps = 1.0f / averageFrameTime;
        float averageFrameTimeMs = averageFrameTime * 1000.0f;

        // •¶š—ñ‚ğì‚Á‚Ä‚µ‚Ä•Û‚µ‚Ä‚¨‚­
        /*FpsText = string.Format("{0:0.0} FPS (avg: {1:0.0})\n{2:0.00} ms (avg: {3:0.00})",
            currentFps, averageFps, currentFrameTimeMs, averageFrameTimeMs);*/
        FpsText = string.Format("{0:0.0}/{1:0.0}", currentFps,  currentFrameTimeMs);
    }

    public void ResetAverage()
    {
        frameTimeSamples.Clear();
        timeStamps.Clear();
        FpsText = "Calculating...";
    }
}
