using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Stella3D
{
    /// <summary>
    /// An array usable as both a NativeArray and managed array
    /// </summary>
    /// <typeparam name="T">The type of the array element</typeparam>
    public class SharedArray<T> : SharedArray<T, T> where T : unmanaged 
    {
        public SharedArray(T[] managed) { Initialize(managed); }
    }

    /// <summary>
    /// An array usable as both a NativeArray and managed array
    /// </summary>
    /// <typeparam name="T">The element type in the managed representation</typeparam>
    /// <typeparam name="TNative">
    /// The element type in the NativeArray representation.  Must be the same size as T
    /// </typeparam>
    public class SharedArray<T, TNative> : IDisposable, IEnumerable<T> 
        where T : unmanaged 
        where TNative : unmanaged
    {
        protected GCHandle m_GcHandle;
#if UNITY_EDITOR
        protected AtomicSafetyHandle m_SafetyHandle;
#endif

        protected T[] m_Managed;
        protected NativeArray<TNative> m_Native;

        public int Length => m_Managed.Length;

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
        
        protected SharedArray() { }
        
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
            // this is the trick to making a NativeArray view of a managed array (or any pointer)
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
            
#if UNITY_EDITOR && !DISABLE_SHAREDARRAY_SAFETY
            // no jobs can be using the data when we resize it
            AtomicSafetyHandle.CheckWriteAndThrow(m_SafetyHandle);
#endif
            if (m_GcHandle.IsAllocated) m_GcHandle.Free();
            Array.Resize(ref m_Managed, newSize);
            m_GcHandle = GCHandle.Alloc(m_Managed, GCHandleType.Pinned);

#if UNITY_EDITOR
            AtomicSafetyHandle.Release(m_SafetyHandle);
#endif
            InitializeNative();
        }
        
        public void Dispose()
        {
#if UNITY_EDITOR && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(m_SafetyHandle);
#endif
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
