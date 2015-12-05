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

# Chapter 2: Parallel workflows

In this tutorial you will learn how mbrace can be used to define parallel and distributed computation.
Follow the instructions and complete the assignments described below.


////////////////////////////////
// Task 1: Using Cloud.Parallel

Distribution in MBrace can be achieved through the use of primitive combinators.
The simplest distribution primitive is Cloud.Parallel; like Async.Parallel, it
takes a collection of cloud computations and combines them into one computation
executed in a distributed fork/join pattern.

Let's try to implement a simple parallel computation, one that downloads text
from a set of web pages and counts the total number of lines.

*)

open System.Net

/// downloads text from given http uri
let download (uri : string) : Cloud<string> = cloud {
    let uri = Uri uri
    let client = new WebClient()
    let! text = Cloud.OfAsync( __IMPLEMENT_ME__ ) // asynchronously download text from given uri using webclient
    return text
}

/// computes the total line count 
let getLineCount (uris : string list) : Cloud<int> = cloud {
    // distribute the download operations across the cluster
    let! (results : string []) = Cloud.Parallel [for uri in uris -> cloud { return! __IMPLEMENT_ME__ } ]
    return __IMPLEMENT_ME__ // compute the total number of lines for all downloaded text
}

/// Input http uris
let pages = ["http://bing.com"; "http://yahoo.com" ; "http://google.com" ; "http://msn.com" ]

let lineCountProc = cluster.CreateProcess(getLineCount pages)

(* 

/////////////////////////////////////////
// Task 2: Using Cloud.ParallelEverywhere

Let's try something different. In this example we will be using Cloud.ParallelEverywhere,
a library combinator that takes a single cloud computation and executes it across the cluster,
precisely once on each target machine. We will be using that to get a list of all executing workers.

*)

type Worker = { HostName : string ; CoreCount : int ; Is64Bit : bool }

let getCurrentWorkerInfo() =
    { HostName = Environment.MachineName ; CoreCount = Environment.ProcessorCount ; Is64Bit = Environment.Is64BitProcess }

let getClusterWorkerInfo () : Cloud<Worker []> = cloud {
    let! info = Cloud.ParallelEverywhere( __IMPLEMENT_ME__ )
    return info
}

getClusterWorkerInfo() |> cluster.Run

(*

///////////////////////////////////////////
// Task 3: Spawning nested Cloud Processes

It is possible to fork cloud computations as separate cloud processes
in an already running cloud process using the Cloud.CreateProcess() primitive.

In this example, we want to run a simple benchmark on google and bing:
for each site we download the home page 100 times and return the first
to complete succesfully.

*)

/// download text from given http uri
let downloadText (uri : string) = cloud {
    return! Cloud.OfAsync <| (new System.Net.WebClient()).AsyncDownloadString(Uri uri)
}

/// benchmark the sequentual download of the same uri 100 times
let download100 (uri : string) : Cloud<string * TimeSpan> = cloud {
    let sw = new System.Diagnostics.Stopwatch()
    sw.Start()
    for i in [1 .. 100] do
        __IMPLEMENT_ME__ // download and discard text from input uri

    sw.Stop()
    return __IMPLEMENT_ME__ // input uri and elapsed time
}

/// test which of google or bing is fastest to serve front page 100 times
let getFastest () = cloud {
    let! cts = Cloud.CreateCancellationTokenSource() // create a cancellation token for our computation
    try
        let! googleProc = Cloud.CreateProcess(download100 "http://google.com/", cancellationToken = cts.Token) // fork google worker
        let! bingProc = Cloud.CreateProcess(download100 "http://bing.com/", cancellationToken = cts.Token) // fork bing worker
        let! fastest = Cloud.WhenAny(googleProc, bingProc) // await the first computation to complete
        return __IMPLEMENT_ME__ // return the result of the fastest computation
    finally
        cts.Cancel() // ensure any leftover cloud processes are cancelled once complete
}

getFastest() |> cluster.Run

(* YOU HANE NOW COMPLETED CHAPTER 2 *)