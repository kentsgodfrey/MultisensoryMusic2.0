using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordCue
{
    [Tooltip("Exact name of the Blender Shape Key or target label")]
    public string word;

    [Tooltip("Start timestamp in seconds when the word is spoken/sung")]
    public float startTime;

    [Tooltip("Duration of the word sound in seconds")]
    public float duration;

    [Tooltip("Optional: Specific Transform to move physically (e.g., 3D text mesh object)")]
    public Transform targetTransform;

    [Tooltip("Upward movement displacement in units when active")]
    public float moveHeight = 1.0f;
}

public class LyricSyncManager : MonoBehaviour
{
    [Header("Audio Setup")]
    [SerializeField] private AudioSource audioSource;

    [Header("Blender Mesh Setup")]
    [Tooltip("SkinnedMeshRenderer holding your Blender Shape Keys")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Lyric Cues Timeline")]
    [SerializeField] private List<WordCue> wordCues = new List<WordCue>();

    [Header("Animation Settings")]
    [Tooltip("Maximum BlendShape weight (Unity uses 0 to 100)")]
    [SerializeField] private float maxBlendWeight = 100f;

    private Dictionary<string, int> blendShapeIndexMap = new Dictionary<string, int>();
    private Vector3[] initialPositions;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Cache BlendShape indices for high-performance lookup
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
        {
            Mesh mesh = skinnedMeshRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                blendShapeIndexMap[shapeName] = i;
            }
        }

        // Cache initial local positions for transform-based displacement
        initialPositions = new Vector3[wordCues.Count];
        for (int i = 0; i < wordCues.Count; i++)
        {
            if (wordCues[i].targetTransform != null)
            {
                initialPositions[i] = wordCues[i].targetTransform.localPosition;
            }
        }
    }

    private void Update()
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        float currentTime = audioSource.time;

        for (int i = 0; i < wordCues.Count; i++)
        {
            WordCue cue = wordCues[i];
            float progress = GetCueProgress(currentTime, cue.startTime, cue.duration);

            // Sine bell curve smoothly ramps 0 -> 1 -> 0 over the duration of the word
            float pulseIntensity = Mathf.Sin(progress * Mathf.PI);

            // 1. Drive Blender Shape Key (BlendShape) weight
            if (skinnedMeshRenderer != null && blendShapeIndexMap.TryGetValue(cue.word, out int shapeIndex))
            {
                skinnedMeshRenderer.SetBlendShapeWeight(shapeIndex, pulseIntensity * maxBlendWeight);
            }

            // 2. Drive physical Transform position (Upward move)
            if (cue.targetTransform != null)
            {
                Vector3 basePos = initialPositions[i];
                Vector3 offset = Vector3.up * (pulseIntensity * cue.moveHeight);
                cue.targetTransform.localPosition = basePos + offset;
            }
        }
    }

    private float GetCueProgress(float currentTime, float startTime, float duration)
    {
        if (duration <= 0f || currentTime < startTime || currentTime > startTime + duration)
            return 0f;

        return Mathf.Clamp01((currentTime - startTime) / duration);
    }

    [ContextMenu("Play Audio")]
    public void PlayTrack()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}