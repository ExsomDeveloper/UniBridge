using System;
using System.Collections;
using UnityEngine;

namespace UniBridge
{
    public class InterstitialCountdownOverlay : MonoBehaviour
    {
        private int _remaining;
        private GUIStyle _style;

        public void Begin(int seconds, Action onComplete)
        {
            _remaining = seconds;
            StartCoroutine(CountdownCoroutine(seconds, onComplete));
        }

        private IEnumerator CountdownCoroutine(int seconds, Action onComplete)
        {
            _remaining = seconds;
            while (_remaining > 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                _remaining--;
            }
            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                };
                _style.normal.textColor = Color.white;
            }

            // Shadow for readability on any background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Label(new Rect(2, 22, Screen.width, 100), _remaining.ToString(), _style);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 20, Screen.width, 100), _remaining.ToString(), _style);
        }
    }
}
