#load "../config.fsx"
#load "../lib/utils.fsx"
#load "../lib/dashboard.fsx"
//#load "../lib/thespian.fsx"

open System
open MBrace.Core
open MBrace.Azure
open Dashboard

let cluster = Config.GetCluster()

(*

From Async to Cloud (Progressive F# Tutorials 2015 London)
==========================================================

# Chapter 6: Cloud Scheduling

In this chapter we will explore how MBrace can be utilised for implementing
self-scheduling cloud workflows that correctly utilise our available resources.


//////////////////////////////////
// Section 1: Parallelism gotchas

Let's forget about MBrace for a moment, and focus on Async.
Async comes with an Async.Parallel combinator, which similar to Cloud.Parallel
executes its inputs in parallel by scheduling execution to the local thread pool.

Since it runs everything in parallel, it would be reasonable to assume that it 
always performs better than iterating through the inputs sequentially. Correct?

Wrong!

Consider the following example:

*)

module Parallel1 =
    /// Parallel map function
    let map (mapper : 'T -> 'S) ts =
        ts |> Seq.map (fun t -> async { return mapper t }) |> Async.Parallel |> Async.RunSynchronously

(*

This gives an implementation of map which uses Async.Parallel to execute the mapping operations.
Let's see how it performs when compared against the sequential Array.map

*)


let inputs = [|1 .. 10000000|]

#time "on"
inputs |> Array.map (fun x -> x + 1)
inputs |> Parallel1.map (fun x -> x + 1)

(*

What do you observe here? Can you explain why this behaviour is happening? [scroll down for an answer]























In our parallel map implementation each unit of execution is being enqueued as a separate work item
in the thread pool. This may negatively impact our performance dependending on the following factors:
  1. Number of CPU cores in our machine.
  2. Length of the input array.
  3. Workload induced by each of the elements in our computation,
    as compared to the total workload as well as thread pool scheduling time.
    If, for example, the workload corresponding to each element of our map operation
    is insignificant when compared to the grand total, it might be the case that the dominating factor
    in our computation is all the time taken to schedule each of those work items.

In other words, our implementation suffers because it performs fine-grained scheduling.
If however, we were to use a somewhat different example, the story would be quite different:

*)

let computation (n : int) =
    let mutable s = 0
    for i = 1 to 100000000 do if i % n = 0 then s <- s + 1
    s

#time "on"
[|1 .. 20|] |> Array.map computation
[|1 .. 20|] |> Parallel1.map computation

(*

Depending on your machine's multicore capacities, the parallel map will run faster than the sequential version.
This happens because the current example is a more 'coarse-grained' computation, the computation assigned to each
element takes up a sizeable proportion of the total workload.

Precisely the same considerations apply to the cloud in general and MBrace in particular.
And since distribution is involved, the price to pay when not correctly addressing granularity concerns
increases by orders of magnitude.

Whenever contemplating whether you should send your computation to the cloud, keep the following points in mind:
    * Does my problem really require scaling out? If your biggest machine can handle it, it should probably stay there.
    * What are the workload characteristics of my computation? Is it CPU bound? Is it IO bound? Both? Does it utilise multicore execution?



/////////////////////////////////////
// Section 2: Improving Parallel.map

In this section we will atempt to improve our async-based naive Parallel.map implementantation so that it better
addresses issues of granularity. To do this, we will follow a strategy of grouping elements together in each work item.

Let's start by implementing a function that takes an array of inputs and partitions them into chunks
according to the core capacity of the current worker:

*)

let partitionInputs (inputs : 'T []) =
    let numberOfPartitions = Environment.ProcessorCount
    Array.splitInto numberOfPartitions inputs


(*

Then implement a map implementation that performs a mapping operation in a single core.
Just use the standard map implementation here:


*)

let singleCoreMap (f : 'T -> 'S) (inputs : 'T []) = __IMPLEMENT_ME__

(*

We are now ready to put all the pieces together and implement our improved Parallel.map:

*)

module Parallel2 =
    let map (f : 'T -> 'S) (inputs : 'T []) : 'S [] = 
        let partitionedInputs : 'T[][] = __IMPLEMENT_ME__ // partition inputs according to core count
        let mappedPartitions : 'S[][] = Parallel1.map __IMPLEMENT_ME__ partitionedInputs // call the original parallel mapping operation on the partitioned inputs
        Array.concat mappedPartitions

(*

Testing the original example:

*)

let inputs' = [|1 .. 10000000|]

#time "on"
inputs' |> Array.map (fun x -> x + 1)
inputs' |> Parallel2.map (fun x -> x + 1)

(*

Which gives a much noticeable performance improvement. Can you think of ways this could be improved even more?


///////////////////////////////////////////////////////
// Section 3: WorkerRef's and targeted cloud processes

MBrace comes with the concept of a 'WorkerRef'. As the name implies, it is a reference object to worker that participates in our MBrace cluster.
To get a reference to the currently executing worker:

*)

cluster.Run (cloud { return! Cloud.CurrentWorker })

(*

Which will simply give back a reference to the worker that happened tou execute this job.

To get references to *all* workers of our current cluster, we use

*)

cluster.Run (Cloud.GetAvailableWorkers())

(*

WorkerRefs can be used to direct computations for execution by a specific worker.
For instance:

*)

let wref = cluster.Run Cloud.CurrentWorker
let wref' = cluster.Run(Cloud.CurrentWorker, target = wref) // send a computation to the given worker ref

wref = wref' // true

(*

This is a useful construct when needing to define self-scheduling cloud workflows and 
when utilising worker worker-specific state such as caching.


///////////////////////////////////////////////////////
// Section 4: Implementing a Parallel.map using MBrace

Let's now see how we can utilise the same techniques as before to implement a 
distributed Parallel.map workflow using MBrace.

Let's also define a function that gets the number of workers in our cluster

*)

let getWorkerCount() : Cloud<int> = cloud {
    let! (workers : WorkerRef []) = __IMPLEMENT_ME__ // get all available workers in the cluster
    return __IMPLEMENT_ME__ // return the total worker count
}

(*

As before, we define a sequential map workflow:

*)

let seqMap (f : 'T -> 'S) (ts : 'T []) = cloud { return Array.map f ts }

(*

Let's now move to our parallel workflow implementation

*)

module Parallel3 =
    let map (f : 'T -> 'S) (ts : 'T []) = cloud {
        let! (workerCount:int) = __IMPLEMENT_ME__// get the current worker count
        let chunks : 'T[][] = __IMPLEMENT_ME__ // use the provided 'Array.splitInto' to partition the inputs according to worker count
        let! (mappedChunks : 'S[][]) = Cloud.Parallel [for ch in chunks -> cloud { return! __IMPLEMENT_ME__ } ] // perform a sequential map in each worker
        return Array.concat mappedChunks
    }


(*

Let's now test the implementation:

*)

cluster.Run(Parallel3.map (fun i -> i + 1) [|1 .. 1000000|])


(*

//////////////////////////////////////////////////
// Section 5*: Improving Distributed Parallel.map

This is an optional section in which you will be allowed to provide an
improved implementation of Parallel.map

i) Let's begin with the observation that workers are usually multicore machines.
This can be verified by calling

*)

let worker = cluster.Run Cloud.CurrentWorker

worker.ProcessorCount // processor count declared by worker

(*

This of course means running one sequential map operation in each worker job
is far from optimal. Can you rewrite the implementation so that the 'seqMap' implementation
is replaced with the multicore ready 'Parallel2.map' ?

ii) It is not always the case that clusters are homogeneous w.r.t. processor count and CPU clock speed.
We can use the WorkerRef instance to estimate a 'performance score' for each worker:

*)

let getPerformanceScore (worker : IWorkerRef) = worker.MaxCpuClock * float worker.ProcessorCount

(*

Can you create an implementation of Parallel3.map that performs a weighted partitioning of inputs
based on each worker's performance score?

*)

(* YOU HANE NOW COMPLETED CHAPTER 5 *)