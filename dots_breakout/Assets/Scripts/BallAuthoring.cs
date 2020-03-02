﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct BallScore : IComponentData
{
    public int Value;
}

public struct RectangleBounds : IComponentData
{
    public float2 HalfWidthHeight;
}

public struct Velocity2D : IComponentData
{
    public float2 Velocity;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BallAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float2 InitialVelocity;
    public float MovementSpeed;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = transform.localScale;
        var bounds = new RectangleBounds
        {
            HalfWidthHeight = new float2(scale.x / 2.0f, scale.y / 2.0f),
        };
        dstManager.AddComponentData(entity, bounds);
        
        dstManager.AddComponentData(entity, new MovementSpeed
        {
            Speed = MovementSpeed
        });
        
        dstManager.AddComponent<Velocity2D>(entity);
        dstManager.AddComponent<BallScore>(entity);
        
        dstManager.AddComponent<Position2D>(entity);
        dstManager.RemoveComponent<Translation>(entity);
        dstManager.RemoveComponent<Rotation>(entity);
    }
}
