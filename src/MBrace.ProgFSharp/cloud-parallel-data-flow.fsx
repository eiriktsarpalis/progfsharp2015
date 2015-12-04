
(*** hide ***)
#load "utils/utils.fsx"
#load "utils/config.fsx"
#load "lib/sieve.fsx"


// Note: Before running, choose your cluster version at the top of this script.
// If necessary, edit AzureCluster.fsx to enter your connection strings.

open System
open System.IO
open MBrace.Core
open MBrace.Flow

// Initialize client object to an MBrace cluster
let cluster = Config.GetCluster() 

(**
# Introduction to Data Parallel Cloud Flows

You now learn the CloudFlow programming model, for cloud-scheduled
parallel data flow tasks.  This model is similar to Hadoop and Spark.
 
CloudFlow.ofArray partitions the input array based on the number of 
available workers.  The parts of the array are then fed into cloud tasks
implementing the map and filter stages.  
*)

let inputs = [|1..1000|]

// Task 1: Find the sum of all the multiples of 3 or 5 below 1000.
let multiples = 
    inputs
    |> CloudFlow.OfArray
    |> CloudFlow.filter (fun num -> (!?))
    |> CloudFlow.sum
    |> cluster.Run


(** 

Data parallel cloud flows can be used for all sorts of things.
Later, you will see how to source the inputs to the data flow from
a collection of cloud files, or from a partitioned cloud vector.


## Changing the degree of parallelism

The default is to partition the input array between all available workers.

You can also use CloudFlow.withDegreeOfParallelism to specify the degree
of partitioning of the stream at any point in the pipeline.
*)
let numbers = [| for i in 1 .. 30 -> 50000000 |]

// Task 2: Collect only twin primes (A twin prime is a prime number that has a prime gap of two)
let twinPrimes (primes : int[]) : int[] = (!?)
let computePrimesTask = 
    numbers
    |> CloudFlow.OfArray
    |> CloudFlow.withDegreeOfParallelism 6
    |> CloudFlow.map (fun n -> Sieve.getPrimes n)
    |> CloudFlow.map (fun primes -> twinPrimes primes) 
    |> CloudFlow.map (fun primes -> sprintf "calculated %d twin primes: %A" primes.Length primes)
    |> CloudFlow.toArray
    |> cluster.CreateProcess 

(** Next, check if the work is done *) 
computePrimesTask.ShowInfo()

(** Next, await the result *) 
let computePrimes = computePrimesTask.Result

(**

## Persisting intermediate results to cloud storage

Results of a flow computation can be persisted to store by terminating
with a call to CloudFlow.persist/persistaCached. 
This creates a PersistedCloudFlow instance that can be reused without
performing recomputations of the original flow.

*)

let persistedCloudFlow =
    inputs
    |> CloudFlow.OfArray
    |> CloudFlow.collect(fun i -> seq {for j in 1 .. 10000 -> (i+j, string j) })
    |> CloudFlow.persist StorageLevel.Memory
    |> cluster.Run


let length = persistedCloudFlow |> CloudFlow.length |> cluster.Run
let max = persistedCloudFlow |> CloudFlow.maxBy fst |> cluster.Run

(** 
## Summary

In this tutorial, you've learned the basics of the CloudFlow programming
model, a powerful data-flow model for scalable pipelines of data. 
Continue with further samples to learn more about the
MBrace programming model. 

 *)
