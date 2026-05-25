using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewSystem : MonoBehaviour
{
    private ScrollRect _scrollRect;

    [SerializeField] private ScrollButton _leftButton;
    [SerializeField] private ScrollButton _rightButton;
    [SerializeField] private ScrollButton _bottomButton;
    [SerializeField] private ScrollButton _topButton;

    [SerializeField] private float scrollSpeed = 0.01f;

    private void Start()
    {
        _scrollRect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        
        if (_leftButton != null)
        {
            if (_leftButton.isDown)
            {
                scrollLeft();
            }
        }
        if (_rightButton != null)
        {
            if (_rightButton.isDown)
            {
                scrollRight();
            }
        }
        if (_bottomButton != null)
        {
            if (_bottomButton.isDown)
            {
                scrollBottom();
            }
        }
        if (_topButton != null)
        {
            if (_topButton.isDown)
            {
                scrollTop();
            }
        }
    }

    void scrollLeft() {
        if (_scrollRect != null) {
            if (_scrollRect.horizontalNormalizedPosition >= 0f) {
                _scrollRect.horizontalNormalizedPosition -= scrollSpeed;
            }
        }
    }
    void scrollRight() {
        if (_scrollRect != null)
        {
            if (_scrollRect.horizontalNormalizedPosition <= 1f)
            {
                _scrollRect.horizontalNormalizedPosition += scrollSpeed;
            }
        }
    }
    void scrollBottom() {
        if (_scrollRect != null)
        {
            if (_scrollRect.verticalNormalizedPosition >= 0f)
            {
                _scrollRect.verticalNormalizedPosition -= scrollSpeed;
            }
        }
    }
    void scrollTop() {
        if (_scrollRect != null)
        {
            if (_scrollRect.verticalNormalizedPosition <= 1f)
            {
                _scrollRect.verticalNormalizedPosition += scrollSpeed;
            }
        }
    }
}
