using System;

namespace UniBridge
{
    public interface ISaveSource
    {
        void Save(string key, string json, Action<bool> onComplete);
        void Load(string key, Action<bool, string> onComplete);
        void Delete(string key, Action<bool> onComplete);
        void HasKey(string key, Action<bool> onComplete);
    }
}
