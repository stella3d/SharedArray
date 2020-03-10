using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Stella3D
{
    public class SharedArray<T> : IDisposable, IEnumerable<T>
        where T : unmanaged
    {
        GCHandle m_GcHandle;
        AtomicSafetyHandle m_SafetyHandle;

        T[] m_Managed;
        NativeArray<T> m_Native;

        public int Length => m_Managed.Length;

        public SharedArray(T[] managed)
        {
            m_Managed = managed;

            // Unity's garbage collector doesn't move objects around, so this should not even be necessary.
            // there's not much downside to playing it safe, though
            m_GcHandle = GCHandle.Alloc(m_Managed, GCHandleType.Pinned);

            InitializeNative();
        }

        ~SharedArray() { Dispose(); }
        
        public static implicit operator T[](SharedArray<T> self)
        {
#if UNITY_EDITOR && !DISABLE_SHAREDARRAY_SAFETY
            // CheckWrite also checks for any other readers
            AtomicSafetyHandle.CheckWriteAndThrow(self.m_SafetyHandle);
#endif
            return self.m_Managed;
        }

        public static implicit operator NativeArray<T>(SharedArray<T> self)
        {
            return self.m_Native;
        }

        unsafe void InitializeNative()
        {
            fixed (void* ptr = m_Managed)
            {
                m_Native = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, m_Managed.Length,
                    Allocator.None);
            }

#if UNITY_EDITOR
            m_SafetyHandle = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref m_Native, m_SafetyHandle);
#endif
        }

        public void Resize(int newSize)
        {
            if (newSize == m_Managed.Length)
                return;

            if (m_GcHandle.IsAllocated) m_GcHandle.Free();
            Array.Resize(ref m_Managed, newSize);
            m_GcHandle = GCHandle.Alloc(m_Managed, GCHandleType.Pinned);

            InitializeNative();
        }
        
        public void Dispose()
        {
            m_Managed = null;
            if (m_GcHandle.IsAllocated) m_GcHandle.Free();
        }

        public IEnumerator<T> GetEnumerator()
        {
#if UNITY_EDITOR && !DISABLE_SHAREDARRAY_SAFETY
            // CheckWrite also checks for any other readers
            AtomicSafetyHandle.CheckWriteAndThrow(m_SafetyHandle);
#endif
            return (IEnumerator<T>) m_Managed.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}