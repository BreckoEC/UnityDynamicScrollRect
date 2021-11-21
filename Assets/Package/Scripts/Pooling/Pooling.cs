using System.Collections.Generic;
using UnityEngine;

#if UNITYDYNAMICSCROLLRECT_ZENJECT
using Zenject;
#endif

namespace pooling
{
    public class Pooling<T> : List<T> where T : MonoBehaviour, IPooling
    {
        public bool createMoreIfNeeded = true;

        private Transform mParent;
        private Vector3 mStartPos;
        private GameObject referenceObject;

#if UNITYDYNAMICSCROLLRECT_ZENJECT
        [Inject] IInstantiator instantiator;
#endif

        public delegate void ObjectCreationCallback(T obj);
        public event ObjectCreationCallback OnObjectCreationCallBack;

        public Pooling<T> Initialize(GameObject refObject, Transform parent)
        {
            return Initialize(0, refObject, parent);
        }

        public Pooling<T> Initialize(int amount, GameObject refObject, Transform parent, bool startState = false)
        {
			return Initialize(amount, refObject, parent, Vector3.zero, startState);
        }

        public Pooling<T> Initialize(int amount, GameObject refObject, Transform parent, Vector3 worldPos, bool startState = false)
        {
            mParent = parent;
            mStartPos = worldPos;
            referenceObject = refObject;

            Clear();

            for (var i = 0; i < amount; i++)
            {
                var obj = CreateObject();

                if(startState) obj.OnCollect();
                else obj.OnRelease();

                Add(obj);
            }

            return this;
        }
        
        public T Collect(Transform parent = null, Vector3? position = null, bool localPosition = true)
        {
            var obj = Find(x => x.isUsing == false);
            if (obj == null && createMoreIfNeeded)
            {
                obj = CreateObject(parent, position);
                Add(obj);
            }

            if (obj == null) return obj;

            obj.transform.SetParent(parent ?? mParent);
            if (localPosition)
                obj.transform.localPosition = position ?? mStartPos;
            else
                obj.transform.position = position ?? mStartPos;
            obj.OnCollect();

            return obj;
        }

        public void Release(T obj)
        {
			if(obj != null)
                obj.OnRelease();
        }

        public List<T> GetAllWithState(bool active)
        {
            return FindAll(x => x.isUsing == active);
        }

        private T CreateObject(Transform parent = null, Vector3? position = null)
        {
#if UNITYDYNAMICSCROLLRECT_ZENJECT
            var obj = instantiator.InstantiatePrefabForComponent<T>(referenceObject, position ?? mStartPos, Quaternion.identity, parent ?? mParent);
#else
            var go = GameObject.Instantiate(referenceObject, position ?? mStartPos, Quaternion.identity, parent ?? mParent);
            var obj = go.GetComponent<T>() ?? go.AddComponent<T>();
#endif
            obj.transform.localPosition = position ?? mStartPos;
            obj.name = obj.objectName + Count;

			if(OnObjectCreationCallBack != null)
                OnObjectCreationCallBack.Invoke(obj);

            return obj;
        }
    }
}