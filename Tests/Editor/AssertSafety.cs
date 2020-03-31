using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Stella3D.Tests
{
    public static class AssertSafety
    {
        public static void ThrowsIfAnyScheduledWriters<T>(SharedArray<T> shared, TestDelegate safetyCheckedAction) where T : unmanaged
        {
            AssertCheck(new WriteTestJob<T>(shared), safetyCheckedAction);
        }
        
        public static void ThrowsIfAnyJobsAreUsingData<T>(SharedArray<T> shared, TestDelegate safetyCheckedAction) where T : unmanaged
        {
            AssertCheck(new ReadOnlyTestJob<T>(shared), safetyCheckedAction);
            AssertCheck(new WriteTestJob<T>(shared), safetyCheckedAction);
        }

        // for methods like .Dispose() where calling them more than once has undesirable side effects
        public static void ThrowsIfAnyDataUsers_SingleCall<T>(SharedArray<T> shared, TestDelegate safetyCheckedAction) 
            where T : unmanaged
        {
            SingleAssertCheck(new WriteTestJob<T>(shared), safetyCheckedAction);
        }

        public static void DoesNotThrowIfAnyScheduledReaders<T>(SharedArray<T> shared, TestDelegate safetyCheckedAction) where T : unmanaged
        {
            AssertCheck(new ReadOnlyTestJob<T>(shared), safetyCheckedAction, false);
        }

        static void SingleAssertCheck<TJob>(TJob job, TestDelegate safetyCheckedAction, bool shouldThrow = true) 
            where TJob : struct, IJob
        {
            var jobHandle = job.Schedule();
            
            if(shouldThrow)
                Assert.Throws<InvalidOperationException>(safetyCheckedAction);
            else
                Assert.DoesNotThrow(safetyCheckedAction);

            jobHandle.Complete();
        }
        
        static void AssertCheck<TJob>(TJob job, TestDelegate safetyCheckedAction, bool shouldThrow = true) 
            where TJob : struct, IJob
        {
            Assert.DoesNotThrow(safetyCheckedAction);
            var jobHandle = job.Schedule();
            
            if(shouldThrow)
                Assert.Throws<InvalidOperationException>(safetyCheckedAction);
            else
                Assert.DoesNotThrow(safetyCheckedAction);
            
            jobHandle.Complete();
            Assert.DoesNotThrow(safetyCheckedAction);
        }
        
        public struct WriteTestJob<T> : IJob where T: unmanaged
        {
            public NativeArray<T> Values;
            public WriteTestJob(NativeArray<T> values) { Values = values; }
            public void Execute() { }
        }
        
        public struct ReadOnlyTestJob<T> : IJob where T: unmanaged
        {
            [ReadOnly] public NativeArray<T> Values;
            public ReadOnlyTestJob(NativeArray<T> values) { Values = values; }
            public void Execute() { }
        }
    }
}
