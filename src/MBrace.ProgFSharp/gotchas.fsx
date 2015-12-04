
(*** hide ***)
#load "config.fsx"



open System
open System.IO
open MBrace.Core
open MBrace.Flow

// Initialize client object to an MBrace cluster
let cluster = Config.GetCluster() 


(**

The purpose of this chapter is to investigate some interesting runtime behaviour and in the
process to understand the semantics of MBrace a little bit better.

*)


// Example 1: Closures and non-serializable objects

let example1 = 
    cloud {
        let client = new System.Net.WebClient()
        let! result = Cloud.CreateProcess(cloud { return client.DownloadString("www.fsharp.org") })
        return result.Result
    }

let proc1 = cluster.CreateProcess(example1) 
proc1.Result // BOOM!
// Task 1: Try to fix the example
// Tip: Comment out code and add Cloud.Log(Enviroment.MachineName) to investigate Machine transition points
proc1.ShowLogs()

// Example 2: Closures and object identity

let example2 = 
    cloud {
        let data = [|1..10000000|]
        let! data' = Cloud.CreateProcess(cloud { return data })
        return Object.ReferenceEquals(data, data'.Result) 
    }

let proc2 = cluster.CreateProcess(example2) 
// Task 2: Try to guess the result and explain the behaviour
proc2.Result


// Example 3: Closures and mutation

let example3 = 
    cloud {
        let data = ref 1
        let! data' = Cloud.CreateProcess(cloud { let _ = incr data in return !data })
        return !data = data'.Result
    }

let proc3 = cluster.CreateProcess(example3) 
// Task 3: Try to guess the result and explain the behaviour
proc3.Result


