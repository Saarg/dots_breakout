using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class UIScore : MonoBehaviour
    {
        [SerializeField] private Text m_Text;
        public int PaddleIndex;

        private EntityManager m_EntityManager;
        private Entity m_Entity;

        private void Start()
        {
            m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            m_Entity = m_EntityManager.CreateEntity();
            
            m_EntityManager.AddComponentObject(m_Entity, this);
        }

        public void SetScore(int i) => m_Text.text = i.ToString();
    }
}