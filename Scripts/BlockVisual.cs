using UnityEngine;

public class BlockVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bevelRenderer;

    void Awake()
    {
        bevelRenderer.sprite = BevelOverlayGenerator.GetBevelSprite();
    }
}