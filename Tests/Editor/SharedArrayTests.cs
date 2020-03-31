using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Stella3D.Tests
{
    public class SharedArrayTests
    {
        [Test]
        public void BothRepresentations_ReferToSameData()
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
        public unsafe void GetPinnableReference_ReturnsCorrectPointer()
        {
            var shared = new SharedArray<Vector4>(4);
            fixed (Vector4* arrayPtr = (Vector4[]) shared)
                fixed (Vector4* pinRefPtr = shared)
                    Assert.IsTrue(arrayPtr == pinRefPtr);
        }
        
        [Test]
        public void Constructor_ThrowsIf_TypesAreNotEqualSize()
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
        public void Resize_ThrowsIf_AnyJobsAreUsingData()
        {
            var shared = new SharedArray<Vector2>(4);
            AssertSafety.ThrowsIfAnyDataUsers_SingleCall(shared, () => shared.Resize(8));
        }

        [Test]
        public void Clear_BasicSuccess()
        {
            var managed = new[] { 10, 8, 30, 20 };
            var shared = new SharedArray<int>(managed);
            shared.Clear();

            for (int i = 0; i < managed.Length; i++)
                Assert.Zero(managed[i]);
        }
        
        [Test]
        public void Clear_ThrowsIf_AnyJobsAreUsingData()
        {
            var shared = new SharedArray<int>(new[] { 6, 9, 4, 2 });
            AssertSafety.ThrowsIfAnyJobsAreUsingData(shared, shared.Clear);
        }

        [Test]
        public void GetEnumerator_BasicSuccess()
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
        public void GetEnumerator_ThrowsIf_AnyJobsAreWriting()
        {
            var shared = new SharedArray<int>(4);
            TestDelegate enumerateAction = () => { foreach (var e in shared) { } };

            AssertSafety.ThrowsIfAnyScheduledWriters(shared, enumerateAction);
            // it's fine if other things are reading while we enumerate, because enumerating returns copies of structs,
            // so the source data won't be changed by the enumerator
            AssertSafety.DoesNotThrowIfAnyScheduledReaders(shared, enumerateAction);
        }

        [Test]
        public void ImplicitCastToManagedArray_ThrowsIf_AnyJobsAreUsingData()
        {
            var shared = new SharedArray<double>(4);
            AssertSafety.ThrowsIfAnyJobsAreUsingData(shared, () => { double[] asManaged = shared; });
        }
        
        [Test]
        public void Dispose_ThrowsIf_AnyJobsAreUsingData()
        {
            var shared = new SharedArray<int>(new[] { 3, 0, 3, 0 });
            AssertSafety.ThrowsIfAnyDataUsers_SingleCall(shared, shared.Dispose);
        }
    }
}
