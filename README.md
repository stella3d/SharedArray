# SharedArray
Zero-copy sharing between managed and native arrays in Unity

(this is **close to ready for real use**, i will make a release when i think it's polished enough for people to use)

## Why

There are a number of APIs in Unity (particularly ones that date from before modern Unity packages) that take in an array of structs such as `Matrix4x4[]`.

In 2018+, we have the C# job system & burst compiler, which allow for much more efficient CPU-side processing.

But there are two points of friction / inefficiency when it comees to bridging the gap between these two:
 
1) you have to use `NativeArray<T>` in jobs, which means if you want to do some calculations in a job and pass that data to a method that takes `T[]`, you need to do some wasteful copying.

2) `Unity.Mathematics` package has special new types that work with Burst, and replace the existing Unity math structs and methods.  
We want to get the compiler-specific performance advantage of using those new types, without the overhead of converting back from `Unity.Mathematics` types to `UnityEngine` types (such as `float4` -> `Vector4`).


## Safety

When accessing the data as a managed array, the job safety system makes sure that no jobs are reading or writing the data, using the normal job safety system

(more detail on safety system / integration with it goes here)


## Aliasing

It's possible to have the `NativeArray` representation of the data be of a different type than the source managed array.  
This is how we can get around the overhead of converting back to `UnityEngine` types (for types that are identical in memory).

To do so, create the `SharedArray` with 2 types instead of 1 :

```csharp
Vector4[] source = new Vector4[64];
SharedArray<Vector4, float4> shared = new SharedArray<Vector4, float4>(source);
NativeArray<float4> native = shared;
Vector4[] asManaged = shared;
```

The only safety check that aliasing makes is that the types are both `unmanaged` and the same size.  
