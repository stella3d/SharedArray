using NUnit.Framework;
using Unity.Collections;
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
            for (int i = 0; i < asNative.Length; i++)
                Assert.IsTrue(asManaged[i].Equals(asNative[i]));

            for (int i = 0; i < asNative.Length; i++)
                asNative[i] = i * 2f;

            for (int i = 0; i < asManaged.Length; i++)
            {
                var nativeElement = asNative[i];
                Assert.IsTrue((i * 2f).Equals(nativeElement));
                Assert.IsTrue(asManaged[i].Equals(nativeElement));
            }
        }
        
        [Test]
        public void Resize()
        {
            const int initialLength = 8;
            var shared = new SharedArray<Vector3>(initialLength);

            Vector3[] asManaged = shared;
            for (int i = 0; i < asManaged.Length; i++)
            {
                asManaged[i] = Vector3.one * i;
            }
            
            Assert.AreEqual(initialLength, asManaged.Length);
            Assert.AreEqual(initialLength, ((NativeArray<Vector3>)shared).Length);
            
            const int resizedLength = 16;
            shared.Resize(resizedLength);
            
            Vector3[] managedAfterResize = shared;
            for (int i = initialLength; i < managedAfterResize.Length; i++)
            {
                managedAfterResize[i] = Vector3.one * i;
            }
            
            Assert.AreEqual(resizedLength, managedAfterResize.Length);
            Assert.AreEqual(resizedLength, ((NativeArray<Vector3>)shared).Length);
            
            for (int i = 0; i < managedAfterResize.Length; i++)
            {
                Assert.AreEqual(Vector3.one * i, managedAfterResize[i]);
            }
        }
    }
}
