using UnityEngine;

//[ExecuteAlways]
public class AutoScaleAndPosition : MonoBehaviour
{
    public Camera cam;
    public bool isparent=true;
    private SpriteRenderer[] renderers; 

    [Range(0f, 1f)]
    public float targetScreenWidthRatio = 0.5f; // 0.5 artinya setengah dari lebar layar

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (isparent == true)
            // Ambil semua SpriteRenderer dari anak-anak objek
            renderers = GetComponentsInChildren<SpriteRenderer>();
        else
            // renderer obj itu sendiri
            renderers = GetComponents<SpriteRenderer>();
            
        


        if (renderers.Length == 0) return;

        // Hitung bounding box gabungan dari semua sprite
        Bounds combinedBounds = renderers[0].bounds;
        foreach (SpriteRenderer sr in renderers)
            combinedBounds.Encapsulate(sr.bounds);

        float spriteWidth = combinedBounds.size.x;

        // Hitung lebar layar dalam world units
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        // Hitung skala target berdasarkan rasio lebar layar
        float targetWidth = screenWidth * targetScreenWidthRatio;
        float scale = targetWidth / spriteWidth;

        // Terapkan skala proporsional
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}