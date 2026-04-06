using System.Collections.Generic;
using UnityEngine;

namespace StoreScreenshots
{
    [CreateAssetMenu(fileName = nameof(ScreenshotConfig), menuName = "StoreScreenshots/Configuration")]
    public class ScreenshotConfig : ScriptableObject
    {
        [Header("Hotkey")]
        public KeyCode Hotkey = KeyCode.F12;
        public bool RequireShift = true;

        [Header("Output")]
        [Tooltip("Folder path relative to the project root")]
        public string OutputFolder = "Screenshots";

        [Header("Resolutions")]
        public List<ScreenshotResolution> Resolutions = new();
    }
}
