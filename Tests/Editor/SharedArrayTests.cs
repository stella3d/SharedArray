using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Stella3D.Tests
{
    public class SharedArrayTests
    {
        [Test]
        public void RepresentationsEqual()
        {
            var shared = new SharedArray<float>(8);
            
            float[] asManaged = shared;
            for (int i = 0; i < asManaged.Length; i++)
                asManaged[i] = i;

            NativeArray<float> asNative = shared;
            
            // does the native representation reflect changes made in the managed one ?
            for (int i = 0; i < asNative.Length; i++)
                Assert.IsTrue(asManaged[i].Equals(asNative[i]));

            for (int i = 0; i < asNative.Length; i++)
                asNative[i] = i * 2f;

            // does the managed representation reflect changes made in the native one ?
            for (int i = 0; i < asManaged.Length; i++)
            {
                var nativeElement = asNative[i];
                Assert.IsTrue((i * 2f).Equals(nativeElement));
                Assert.IsTrue(asManaged[i].Equals(nativeElement));
            }
        }
        
        [Test]
        public unsafe void ManagedAndNativeHaveSamePointers()
        {
            var shared = new SharedArray<Vector4>(4);
            fixed (void* managedPtr = (Vector4[]) shared)
            {
                var nativePtr = ((NativeArray<Vector4>) shared).GetUnsafeReadOnlyPtr();
                Assert.IsTrue(managedPtr == nativePtr);
            }
        }
        
        [Test]
        public unsafe void GetPinnableReferenceReturnsCorrectPointer()
        {
            var shared = new SharedArray<Vector4>(4);
            fixed (Vector4* arrayPtr = (Vector4[]) shared)
                fixed (Vector4* pinRefPtr = shared)
                    Assert.IsTrue(arrayPtr == pinRefPtr);
        }
        
        [Test]
        public void UnequalSizeTypesConstructorThrows()
        {
            try
            {
                var shared = new SharedArray<Vector4, float>(8);
                throw new Exception("Shouldn't get here, the above constructor should throw!");
            }
            catch (InvalidOperationException e)
            {
                Assert.NotNull(e.Message);
                Assert.IsTrue(e.Message.Contains("size"));  // is the exception message the intended one ?
            }
        }
        
        [Test]
        public void Resize()
        {
            const int initialLength = 8;
            var shared = new SharedArray<Vector3>(initialLength);

            Vector3[] asManaged = shared;
            for (int i = 0; i < asManaged.Length; i++)
                asManaged[i] = Vector3.one * i;
            
            Assert.AreEqual(initialLength, shared.Length);
            Assert.AreEqual(initialLength, ((NativeArray<Vector3>)shared).Length);
            
            const int resizedLength = 16;
            shared.Resize(resizedLength);
            
            Vector3[] managedAfterResize = shared;
            // did data from before resize get preserved ?
            for (int i = 0; i < initialLength; i++)
                Assert.AreEqual(Vector3.one * i, managedAfterResize[i]);
            
            Assert.AreEqual(resizedLength, managedAfterResize.Length);
            Assert.AreEqual(resizedLength, ((NativeArray<Vector3>)shared).Length);
        }
        
        [Test]
        public void GetEnumerator()
        {
            var shared = new SharedArray<float>(4);
            var asManaged = (float[]) shared;
            for (int i = 0; i < asManaged.Length; i++)
                asManaged[i] = i;

            // try enumerating, which is just a wrapper around normal array enumeration
            foreach (var floatEntry in shared)
                Assert.IsTrue(asManaged.Contains(floatEntry));
        }
        
        [Test]
        public void GetEnumerator_ThrowsIfReadSafetyViolated()
        {
            var shared = new SharedArray<int>(4);
            // enumerating is allowed when no jobs are writing to the data
            Assert.DoesNotThrow(() => { foreach (var e in shared) { } });
            
            var writeJobHandle = new WriteTestJob<int>(shared).Schedule();
            
            // Because there is an uncompleted job scheduled to writes to the data, enumerating the data should throw
            Assert.Throws<InvalidOperationException>(() => { foreach (var e in shared) { } });

            writeJobHandle.Complete();
            // Now that all jobs writing to the data are done, enumerating the data is allowed again
            Assert.DoesNotThrow(() => { foreach (var e in shared) { } });
            
            var readOnlyJobHandle = new ReadOnlyTestJob<int>(shared).Schedule();
            
            // enumerating structs doesn't allow mutating the data, so if a job is only reading, enumerating is fine
            Assert.DoesNotThrow(() => { foreach (var e in shared) { } });
            readOnlyJobHandle.Complete();
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
