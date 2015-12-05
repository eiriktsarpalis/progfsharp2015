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

# Chapter 5: Cloud Gotchas

This chapter explores more advanced topics of MBrace and the cloud.
In particular, we will look at common misconceptions and errors that occur
when programming in MBrace.

Follow the instructions and complete the assignments described below.

/////////////////////////////////////////
// Section 1: Local vs. Remote execution

MBrace makes it possible to execute cloud workflows in the local process
just as if they were asynchronous workflows: parallelism is achieved using the local threadpool.
This can be done using the cluster.Runlocally() method:

*)

cloud { return Environment.MachineName } |> cluster.Run         // local execution
cloud { return Environment.MachineName } |> cluster.RunLocally  // remote execution

(*

As demonstrated above, local versus remote execution comes with minute difference w.r.t.
to the computed result as well as observed side-effects.

Let's try a simple example. Just by looking at the example below, 
can you guess what the difference will be when run locally as opposed to remotely?

*)

cloud { let _ = printfn "I am a side-effect!" in return 42 }

(*

While the above is a mostly harmless example, what can be said about the example below?

*)

open System.IO
let currentDirectory = Directory.GetCurrentDirectory()
let getContents = cloud { return Directory.EnumerateFiles currentDirectory |> Seq.toArray }

cluster.RunLocally getContents
cluster.Run getContents

(*

Why does the error happen? Can you suggest a way the above could be fixed?



////////////////////////////////////////////////
// Section 2: Cloud workflows and serialization

It is often the case that our code relies on objects that are not serializable.
But what happens when this code happens to be running in the cloud?

*)

let downloader = cloud {
    let client = new System.Net.WebClient()
    let! downloadProc = Cloud.CreateProcess(cloud { return client.DownloadString("www.fsharp.org") })
    return downloadProc.Result
}

(*

What will happen if we attempt to execute the snippet above?

*)

cluster.Run(downloader)

(*

Assingment: can you rewrite the snippet above so that it no longer fails?
Tip: can you detect what segments of the code entail transition to a different machine?


//////////////////////////////////////////////////
// Section 2: Cloud workflows and object identity

Consider the following snippet:

*)

let example2 = cloud {
    let data = [| 1 .. 100 |]
    let! proc = Cloud.CreateProcess(cloud { return data })
    return Object.ReferenceEquals(data, proc.Result) 
}

(*

Can you guess its result?

*)

cluster.Run example2
cluster.RunLocally example2

(*

Can you explain why this behaviour happens?




///////////////////////////////////////////
// Section 2: Cloud workflows and mutation

Consider the following sample:

*)

let example3 = cloud {
    let data = [|1 .. 10|]
    let! _ = Cloud.Parallel [for i in 0 .. data.Length - 1 -> cloud { data.[i] <- 0 } ]
    return data
}

(*

Can you guess its result?

*)

cluster.Run example3
cluster.RunLocally example3

(*

Can you explain why this behaviour happens?

*)

(* YOU HANE NOW COMPLETED CHAPTER 5 *)