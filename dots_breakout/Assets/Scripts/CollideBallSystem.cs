using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateAfter(typeof(MoveBallSystem))]
public class CollideBallSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

    private EntityQuery m_BrickQuery;
    
    [BurstCompile]
    struct CollideBallsWithPaddleJob : IJobForEachWithEntity<RectangleBounds, MovementSpeed, Position2D, Velocity2D, BallScore>
    {
        public Entity PaddleEntity;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction] 
        public ComponentDataFromEntity<Position2D> PaddleTranslation;
        
        [ReadOnly] 
        public ComponentDataFromEntity<RectangleBounds> PaddleBounds;
        
        public PaddleTagScore PaddleScore;
        public EntityCommandBuffer.Concurrent Commandbuffer;

        public void Execute(
            Entity entity,
            int index,
            [ReadOnly]ref RectangleBounds ballBounds, 
            [ReadOnly]ref MovementSpeed speed,
            ref Position2D ballTranslation,
            ref Velocity2D ballVelocity,
            [ReadOnly]ref BallScore ballScore)
        {
            var ballPosition = ballTranslation.Value;
            var paddlePosition = PaddleTranslation[PaddleEntity].Value;
            var paddleRect = PaddleBounds[PaddleEntity];

            var velocity = ballVelocity.Velocity;
            var delta =  paddlePosition.xy - ballPosition.xy;
            var combinedHalfBounds = ballBounds.HalfWidthHeight + paddleRect.HalfWidthHeight;

            if (math.all(math.abs(delta) <= combinedHalfBounds))
            {
                velocity = math.normalize(new float2(math.sign(-delta.x), -velocity.y));

                ballPosition.y += combinedHalfBounds.y + delta.y;
                
                ballVelocity.Velocity = velocity;
                ballTranslation.Value = ballPosition;

                PaddleScore.Value += ballScore.Value;
                ballScore.Value = 0;
                Commandbuffer.SetComponent(index, PaddleEntity, PaddleScore);
            }
        }
    }

    [BurstCompile]
    struct CollideBallsWithBricksJob_Accelerated : IJobForEachWithEntity<RectangleBounds, MovementSpeed, Position2D, Velocity2D, BallScore>
    {
        public EntityCommandBuffer.Concurrent Ecb;

        [ReadOnly] public BrickHashGrid BrickGrid;

        [ReadOnly] public ComponentDataFromEntity<Position2D> BrickTranslationRO;
        [ReadOnly] public ComponentDataFromEntity<RectangleBounds> BrickRectangleBoundsRO;
        [ReadOnly] public ComponentDataFromEntity<BrickScore> BrickScore;

        public void Execute(
            Entity e,
            int ballIndex,
            [ReadOnly]ref RectangleBounds ballBounds, 
            [ReadOnly]ref MovementSpeed speed,
            [ReadOnly]ref Position2D ballTranslation,
            ref Velocity2D velocity2D,
            ref BallScore ballScore)
        {
            var ballPosition = ballTranslation.Value;
            var velocity = velocity2D.Velocity;

            var invertX = false;
            var invertY = false;

            var hashBallPosition = (int) ((math.floor(ballPosition.x / BrickGrid.GridCellSize)) +
                                          (math.floor(ballPosition.y / BrickGrid.GridCellSize)) * BrickGrid.ScreenWidthInCells);
            var bricksInCell = BrickGrid.Grid.GetValuesForKey(hashBallPosition);
            while (bricksInCell.MoveNext())
            {
                var brickEntity = bricksInCell.Current;
                
                // todo: remove bricks from hashmap when they are destroyed
                if(!BrickTranslationRO.Exists(brickEntity))
                    continue;
                
                var brickPosition = BrickTranslationRO[brickEntity].Value;
                var brickRect = BrickRectangleBoundsRO[brickEntity];

                var delta = brickPosition.xy - ballPosition.xy;
                var combinedHalfBounds = ballBounds.HalfWidthHeight + brickRect.HalfWidthHeight;

                if (math.all(math.abs(delta) <= combinedHalfBounds))
                {
                    var crossWidth = combinedHalfBounds.x * delta.y;
                    var crossHeight = combinedHalfBounds.y * delta.x;
                        
                    if(crossWidth > crossHeight)
                        if (crossWidth > -crossHeight)
                            invertY = true;
                        else
                            invertX = true;
                    else
                        if (crossWidth > -crossHeight)
                            invertX = true;
                        else
                            invertY = true;
                        
                    var brickScore = BrickScore[brickEntity];
                    ballScore.Value += brickScore.Value;
                    
                    Ecb.DestroyEntity(ballIndex, brickEntity);
                }
            }

            if (invertY)
                velocity.y = -velocity.y;
            
            if (invertX)
                velocity.x = -velocity.x;

            velocity2D.Velocity = velocity;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        JobHandle collideBrickHandle = inputDependencies;
        
        Entities.WithoutBurst().ForEach((Entity paddleEntity, ref PaddleTagScore paddleTagScore) =>
        {
            var paddleJob = new CollideBallsWithPaddleJob
            {
                PaddleEntity = paddleEntity,
                PaddleScore = paddleTagScore,
                Commandbuffer = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent(),
            
                PaddleTranslation = GetComponentDataFromEntity<Position2D>(true),
                PaddleBounds = GetComponentDataFromEntity<RectangleBounds>(true)
            };
            collideBrickHandle = paddleJob.Schedule(this, collideBrickHandle);
        }).Run();
        
        var brickCount = m_BrickQuery.CalculateEntityCount();
        if (brickCount > 0)
        {
            var hashGrid = GetSingleton<BrickHashGrid>();
            var brickJob = new CollideBallsWithBricksJob_Accelerated
            {
                Ecb = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent(),
                
                BrickGrid = hashGrid,

                BrickTranslationRO = GetComponentDataFromEntity<Position2D>(true),
                BrickRectangleBoundsRO = GetComponentDataFromEntity<RectangleBounds>(true),
                BrickScore = GetComponentDataFromEntity<BrickScore>(true)
            };
            
            collideBrickHandle = brickJob.Schedule(this, collideBrickHandle);
        }
        
        m_EndSimECBSystem.AddJobHandleForProducer(collideBrickHandle);

        return collideBrickHandle;
    }

    protected override void OnCreate()
    {
        m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BrickQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{ ComponentType.ReadOnly<Position2D>(), ComponentType.ReadOnly<RectangleBounds>(), ComponentType.ReadOnly<BrickScore>() }
        });

        RequireSingletonForUpdate<PaddleTag>();
    }
}