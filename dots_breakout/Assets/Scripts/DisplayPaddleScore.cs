using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DefaultNamespace
{
    [UpdateBefore(typeof(CollideBallSystem))]
    public class DisplayPaddleScore : ComponentSystem
    {
        private EntityQuery m_EntityQuery;
        
        protected override void OnCreate()
        {
            m_EntityQuery = GetEntityQuery(ComponentType.ReadOnly<PaddleTagScore>());
        }

        protected override void OnUpdate()
        {
            using (var paddleEntities = m_EntityQuery.ToComponentDataArray<PaddleTagScore>(Allocator.TempJob))
            {
                Entities.ForEach((UIScore uiScore) =>
                {    
                    uiScore.SetScore(paddleEntities[uiScore.PaddleIndex].Value);
                });
            }
        }
    }
}