# SharedArray-Package
Zero-copy sharing between managed and native arrays in Unity


## Why

There are a number of APIs in Unity (particularly ones that date from before modern Unity packages) that take in an array of structs such as `Matrix4x4[]`.

In 2018+, we have the C# job system & burst compiler, which allow for much more efficient CPU-side processing.

But there are two points of friction / inefficiency when it comees to bridging the gap between these two:
 
1) you have to use `NativeArray<T>` in jobs, which means if you want to pass data to a method that takes `T[]`, you need to do some wasteful copying 

2) `Unity.Mathematics` package has special new types that work with Burst, and replace the existing Unity math structs and methods.  
We want to both the compiler-specific performance advantage of using those types, without the overhead of converting back from `float4` -> `Vector4`.


## Safety

describe how it integrates with the safety system


## Aliasing

describe how the aliasing feature works

The only safety check that aliasing makes is that the types are both `unmanaged` and the same size.
