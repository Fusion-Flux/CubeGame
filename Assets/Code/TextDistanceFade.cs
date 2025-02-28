using TMPro;
using UnityEngine;

public class TextDistanceFade : MonoBehaviour
{
    public float visibleDistance = 10f;
    private TextMeshPro tmp;
    private Camera mainCamera;

    void Start() {
        tmp = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;
    }

    void Update() {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        Color color = tmp.color;
        color.a = distance < visibleDistance ? 1f : 0f;
        tmp.color = color;
    }
}
