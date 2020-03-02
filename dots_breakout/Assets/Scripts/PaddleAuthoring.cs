using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct PaddleTag : IComponentData
{
    public NativeString64 InputName;
}

public struct PaddleTagScore : IComponentData
{
    public int Value;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class PaddleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MovementSpeed;
    public string InputName;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = transform.localScale;
        var bounds = new RectangleBounds
        {
            HalfWidthHeight = new float2(scale.x / 2.0f, scale.y / 2.0f),
        };
        dstManager.AddComponentData(entity, bounds);
        dstManager.AddComponentData(entity, new PaddleTag { InputName = InputName});
        
        dstManager.AddComponentData(entity, new MovementSpeed
        {
            Speed = MovementSpeed
        });
        
        dstManager.AddComponentData(entity, new Position2D
        {
            Value = new float2(transform.position.x, transform.position.y)
        });

        dstManager.AddComponent<PaddleTagScore>(entity);
        
        dstManager.RemoveComponent<Translation>(entity);
        dstManager.RemoveComponent<Rotation>(entity);
    }
}
