using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private float sizeOfSprite, backgroundMoveSpeed;
    private float _spriteStartPosition;
    private float _initialSpritePosition;
    private float _cameraStartPosition;

    private void Start()
    {
        _camera = Camera.main;
        sizeOfSprite = GetComponent<SpriteRenderer>().bounds.size.x;
        _spriteStartPosition = transform.position.x;
        _initialSpritePosition = _spriteStartPosition;
        _cameraStartPosition = _camera.transform.position.x;
    }

    private void Update()
    {
        var cameraPos = _camera.transform.position.x;
        var cameraOffset = cameraPos - _cameraStartPosition;
        var temp = _initialSpritePosition + cameraOffset * (1 - backgroundMoveSpeed);
        var distance = cameraOffset * backgroundMoveSpeed;

        var newPosition = new Vector3 (_spriteStartPosition + distance, transform.position.y, transform.position.z);
        
        transform.position = newPosition;

        if(temp > _spriteStartPosition + (sizeOfSprite / 2))
        {
            _spriteStartPosition += sizeOfSprite;
        }
        else if(temp < _spriteStartPosition - (sizeOfSprite / 2))
        {
            _spriteStartPosition -= sizeOfSprite;
        }
    }
}
