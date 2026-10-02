using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _ins;
    public static T Instance
    {
        get
        {
            if (_ins == null)
            {
                _ins = FindFirstObjectByType<T>();
            }
            return _ins;
        }
    }
    
    protected virtual void Awake()
    {
        if (_ins == null) { _ins = this.GetComponent<T>(); return;}
        if(_ins.gameObject.GetInstanceID() != this.gameObject.GetInstanceID()) Destroy(this.gameObject);
    }
}
public class DontDestroyOnLoadSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _ins;
    public static T Instance
    {
        get
        {
            if (_ins == null)
            {
                _ins = FindFirstObjectByType<T>();
            }
            return _ins;
        }
    }
    protected virtual void Awake()
    {
        if (_ins == null)
        {
            _ins = this.GetComponent<T>();
            DontDestroyOnLoad(_ins);
            return;
        }
        if(_ins.gameObject.GetInstanceID() != this.gameObject.GetInstanceID()) Destroy(this.gameObject);
    }
}