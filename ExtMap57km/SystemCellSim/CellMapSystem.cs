using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace ExtMap57km.Systems
{
    public abstract partial class CellMapSystem<T> : GameSystemBase where T : struct, ISerializable
    {
        [BurstCompile]
        internal struct SerializeJob<TWriter> : IJob where TWriter : struct, IWriter
        {
            [ReadOnly]
            public int m_Stride;

            [ReadOnly]
            public NativeArray<T> m_Map;

            public EntityWriterData m_WriterData;

            public void Execute()
            {
                TWriter writer = m_WriterData.GetWriter<TWriter>();
                if (m_Stride != 0 && m_Map.Length != 0)
                {
                    NativeList<byte> buffer = new NativeList<byte>(1000, Allocator.Temp);
                    m_WriterData.GetWriter<TWriter>(buffer).Write(m_Map);
                    writer.Write(-m_Map.Length);
                    writer.Write(buffer.Length);
                    writer.Write(buffer.AsArray(), m_Stride);
                    buffer.Dispose();
                }
                else
                {
                    writer.Write(m_Map.Length);
                    writer.Write(m_Map);
                }
            }
        }

        [BurstCompile]
        internal struct DeserializeJob<TReader> : IJob where TReader : struct, IReader
        {
            [ReadOnly]
            public int m_Stride;

            public NativeArray<T> m_Map;

            public EntityReaderData m_ReaderData;

            public void Execute()
            {
                TReader reader = m_ReaderData.GetReader<TReader>();
                if (!(reader.context.version > Version.stormWater))
                {
                    return;
                }

                if (reader.context.version > Version.cellMapLengths)
                {
                    reader.Read(out int value);
                    if (m_Map.Length == value)
                    {
                        reader.Read(m_Map);
                    }
                    else if (m_Map.Length == -value)
                    {
                        reader.Read(out int value2);
                        NativeArray<byte> nativeArray = new NativeArray<byte>(value2, Allocator.Temp);
                        NativeReference<int> position = new NativeReference<int>(0, Allocator.Temp);
                        reader.Read(nativeArray, m_Stride);
                        m_ReaderData.GetReader<TReader>(nativeArray, position).Read(m_Map);
                        nativeArray.Dispose();
                        position.Dispose();
                    }
                }
                else
                {
                    reader.Read(m_Map);
                }
            }
        }

        [BurstCompile]
        private struct SetDefaultsJob : IJob
        {
            public NativeArray<T> m_Map;

            public void Execute()
            {
                for (int i = 0; i < m_Map.Length; i++)
                {
                    m_Map[i] = default;
                }
            }
        }

        public static readonly int kMapSize = 57344;

        protected JobHandle m_ReadDependencies;

        protected JobHandle m_WriteDependencies;

        protected NativeArray<T> m_Map;

        protected int2 m_TextureSize;

        public JobHandle Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps) where TWriter : struct, IWriter
        {
            int stride = 0;
            if ((object)default(T) is IStrideSerializable strideSerializable)
            {
                stride = strideSerializable.GetStride(writerData.GetWriter<TWriter>().context);
            }

            SerializeJob<TWriter> jobData = default;
            jobData.m_Stride = stride;
            jobData.m_Map = m_Map;
            jobData.m_WriterData = writerData;
            JobHandle jobHandle = jobData.Schedule(JobHandle.CombineDependencies(inputDeps, m_WriteDependencies));
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
            return jobHandle;
        }

        public virtual JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps) where TReader : struct, IReader
        {
            int stride = 0;
            if ((object)default(T) is IStrideSerializable strideSerializable)
            {
                stride = strideSerializable.GetStride(readerData.GetReader<TReader>().context);
            }

            DeserializeJob<TReader> deserializeJob = default;
            deserializeJob.m_Stride = stride;
            deserializeJob.m_Map = m_Map;
            deserializeJob.m_ReaderData = readerData;
            DeserializeJob<TReader> jobData = deserializeJob;
            m_WriteDependencies = jobData.Schedule(JobHandle.CombineDependencies(inputDeps, m_ReadDependencies, m_WriteDependencies));
            return m_WriteDependencies;
        }

        public virtual JobHandle SetDefaults(Context context)
        {
            SetDefaultsJob setDefaultsJob = default;
            setDefaultsJob.m_Map = m_Map;
            SetDefaultsJob jobData = setDefaultsJob;
            m_WriteDependencies = jobData.Schedule(JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
            return m_WriteDependencies;
        }

        public NativeArray<T> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies);
            return m_Map;
        }

        public CellMapData<T> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies);
            CellMapData<T> result = default;
            result.m_Buffer = m_Map;
            //result.m_CellSize = (float2)kMapSize / (float2)m_TextureSize;
            result.m_CellSize = (float2)kMapSize / (float2)m_TextureSize;//mod;
            result.m_TextureSize = m_TextureSize;
            return result;
        }

        public void AddReader(JobHandle jobHandle)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
        }

        public void AddWriter(JobHandle jobHandle)
        {
            m_WriteDependencies = jobHandle;
        }

        public static float3 GetCellCenter(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + (num + 0.5f) * num3, 0f, -0.5f * kMapSize + (num2 + 0.5f) * num3);
        }

        public static Bounds3 GetCellBounds(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new Bounds3(new float3(-0.5f * kMapSize + num * num3, -100000f, -0.5f * kMapSize + num2 * num3), new float3(-0.5f * kMapSize + (num + 1f) * num3, 100000f, -0.5f * kMapSize + (num2 + 1f) * num3));
        }

        public static float3 GetCellCenter(int2 cell, int textureSize)
        {
            int num = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + (cell.x + 0.5f) * num, 0f, -0.5f * kMapSize + (cell.y + 0.5f) * num);
        }

        public static float2 GetCellCoords(float3 position, int mapSize, int textureSize)
        {
            return (0.5f + position.xz / mapSize) * textureSize;
        }

        public static int2 GetCell(float3 position, int mapSize, int textureSize)
        {
            return (int2)math.floor(GetCellCoords(position, mapSize, textureSize));
        }

        protected void CreateTextures(int textureSize)
        {
            m_Map = new NativeArray<T>(textureSize * textureSize, Allocator.Persistent);
            m_TextureSize = new int2(textureSize, textureSize);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            m_ReadDependencies.Complete();
            m_WriteDependencies.Complete();
            m_Map.Dispose();
            base.OnDestroy();
        }

        [Preserve]
        protected CellMapSystem()
        {
        }
    }
}
