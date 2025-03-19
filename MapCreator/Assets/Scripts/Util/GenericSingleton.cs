using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get 
        {
            if (_instance == null)
            {
                _instance = (T)FindObjectOfType(typeof(T));

                if (_instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    _instance = obj.AddComponent<T>();                    
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (transform.parent != null)// && transform.root != null)
        {
            DontDestroyOnLoad(this.transform.root.gameObject);
        }
        else
        {
            if (transform.name.Contains("Manager"))
            {
                GameObject temp = GameObject.Find("Managers");

                if (temp == null)
                {
                    // 존재하지 않으면 새로 생성
                    temp = new GameObject("Managers");
                }

                transform.SetParent(temp.transform);
            }
            DontDestroyOnLoad(this.transform.root.gameObject);
        }
    }
}