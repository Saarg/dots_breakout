using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[UpdateBefore(typeof(MoveBallSystem))]
public class MovePaddleSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var deltaTime = Time.DeltaTime;
        var screenBounds = GetSingleton<ScreenBoundsData>();
        
        Entities.WithoutBurst().ForEach((ref PaddleTag paddleTag, ref Position2D translation, in RectangleBounds bounds, in MovementSpeed speed) =>
        {
            var MoveDirection = Input.GetAxis(paddleTag.InputName.ToString());
            
            var position = translation.Value;
            position.x += MoveDirection * speed.Speed * deltaTime;
            position = math.min(math.max(screenBounds.XYMin + bounds.HalfWidthHeight, position), screenBounds.XYMax - bounds.HalfWidthHeight);
            translation.Value = position;
        }).Run();

        return default;
    }
}