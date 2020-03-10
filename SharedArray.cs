using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Stella3D
{
    public class SharedArray<T> : SharedArray<T, T> where T : unmanaged 
    {
        public SharedArray(T[] managed) { Initialize(managed); }
    }

    public class SharedArray<T, TNative> : IDisposable, IEnumerable<T> 
        where T : unmanaged 
        where TNative : unmanaged
    {
        protected GCHandle m_GcHandle;
        protected AtomicSafetyHandle m_SafetyHandle;

        protected T[] m_Managed;
        protected NativeArray<TNative> m_Native;

        public int Length => m_Managed.Length;

        protected SharedArray() { }

        public unsafe SharedArray(T[] managed)
        {
            if (sizeof(T) != sizeof(TNative))
            {
                var msg = $"size of native alias type {typeof(TNative).FullName} ({sizeof(TNative)} bytes) " +
                          $"must be equal to size of type {typeof(T).FullName} ({sizeof(T)} bytes)";

                throw new InvalidOperationException(msg);
            }
            
            Initialize(managed);
        }

        ~SharedArray() { Dispose(); }
        
        public static implicit operator T[](SharedArray<T, TNative> self)
        {
#if UNITY_EDITOR && !DISABLE_SHAREDARRAY_SAFETY
            // CheckWrite also checks for any other readers
            AtomicSafetyHandle.CheckWriteAndThrow(self.m_SafetyHandle);
#endif
            return self.m_Managed;
        }

        public static implicit operator NativeArray<TNative>(SharedArray<T, TNative> self)
        {
            return self.m_Native;
        }

        protected void Initialize(T[] managed)
        {
            m_Managed = managed;
            // Unity's garbage collector doesn't move objects around, so this should not even be necessary.
            // there's not much downside to playing it safe, though
            m_GcHandle = GCHandle.Alloc(m_Managed, GCHandleType.Pinned);
            InitializeNative();
        }

        protected unsafe void InitializeNative()
        {
            fixed (void* ptr = m_Managed)
            {
                m_Native = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TNative>
                    (ptr, m_Managed.Length, Allocator.None);
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

            // no jobs can be using the data when we resize it
            AtomicSafetyHandle.CheckWriteAndThrow(m_SafetyHandle);

            if (m_GcHandle.IsAllocated) m_GcHandle.Free();
            Array.Resize(ref m_Managed, newSize);
            m_GcHandle = GCHandle.Alloc(m_Managed, GCHandleType.Pinned);

            AtomicSafetyHandle.Release(m_SafetyHandle);
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