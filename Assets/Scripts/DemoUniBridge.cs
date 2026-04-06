using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoUniBridge : MonoBehaviour
{
    [SerializeField] private Button _button;

    private void Awake()
    {
        _button.onClick.AddListener(StartReward);
    }

    private void StartReward()
    {
        UniBridge.UniBridge.ShowReward(status =>
        {
            UnityEngine.Debug.Log("Reward status: " + status);
        });
    }
}
