# SharedArray
A `SharedArray` is a segment of memory that is represented both as a normal C# array `T[]`, and a Unity [`NativeArray<T>`](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html).

It's designed to reduce the overhead of communicating between C# job data in `NativeArray` and APIs that use a normal array of structs, such as [Graphics.DrawMeshInstanced()](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html), by eliminating the need to copy data.

(this is **close to ready for real use**, i will make a release when i think it's polished enough for people to use)

## Safety System

Unity's job system has a [safety system for reading & writing data](https://docs.unity3d.com/Manual/JobSystemSafetySystem.html) (in the Editor only).  This catches cases where a data race would occur and warns you about it.

SharedArray _works with this safety system_, so when you access the data on the main thread, the system knows whether it is safe to read or write, just like using a `NativeArray` allocated the normal way.

Here's all of the operations that include a check of the safety system.

```csharp
SharedArray<T> sharedArray;            // created elsewhere 

// These 4 operations will check that no jobs are using the data, in any way
T[] asNormalArray = sharedArray; 
sharedArray.Clear();
sharedArray.Resize(32);
sharedArray.Dispose();

// Enumerating in either of these ways will check if any jobs are writing to the data, but allow other readers
foreach(var element in sharedArray) { }

var enumerator = sharedArray.GetEnumerator();
```

The safest way to use SharedArray is: 
1) Manipulate data in a C# job, using the `NativeArray<T>` representation
2) Convert to a managed array `T[]` only right before you use it on the main thread.  

This is important if you want the safety system to work - if you pass around a reference to the managed representation, you won't get the safety system checks.

You can see this pattern demonstrated in the [demo project](https://github.com/stella3d/SharedArray-Demo).

## Aliasing 

It's possible to have the `NativeArray` representation of the data be of a different type than the source managed array.  

To do so, create the `SharedArray` with 2 types instead of 1 :

```csharp
Vector4[] source = new Vector4[64];
SharedArray<Vector4, float4> shared = new SharedArray<Vector4, float4>(source);
NativeArray<float4> native = shared;
Vector4[] asManaged = shared;
```

The only safety check that aliasing makes is that the types are both `unmanaged` and the same size.  

#### Why Alias Types ?

Aliasing was made to eliminate the overhead of converting between analogous types in `Unity.Mathematics` and `UnityEngine` (such as `float4` <-> `Vector4` or `float4x4` <-> `Matrix4x4`).

These `Unity.Mathematics` types have optimizations specific to the [Burst compiler](https://docs.unity3d.com/Packages/com.unity.burst@0.2/manual/index.html), and replace the existing Unity math structs and methods.
  We want to get the compiler-specific performance advantage of using those new types, without the overhead of converting back from `Unity.Mathematics` types. 
  
For types that are laid out the same in memory, we can just treat one like the other.  Since we do this for the whole array, there is never any conversion between types happening, and thus no overhead - it's just a different "view" on the same memory.

