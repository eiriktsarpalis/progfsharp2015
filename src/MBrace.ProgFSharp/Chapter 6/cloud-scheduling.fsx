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
        Parallel1.map __IMPLEMENT_ME__ partitionedInputs // call the original parallel mapping operation on the partitioned inputs

(*

Testing the original example:

*)

let inputs' = [|1 .. 10000000|]

#time "on"
inputs' |> Array.map (fun x -> x + 1)
inputs' |> Parallel2.map (fun x -> x + 1)