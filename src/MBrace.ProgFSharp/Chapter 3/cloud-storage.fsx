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

# Chapter 3: Cloud Storage

In this tutorial you will learn how you can interact with Azure storage using mbrace.
Follow the instructions and complete the assignments described below.

/////////////////////////////////////////
// Task 1: Interacting with blob storage

The cluster object comes with APIs for interacting with blob storage:

*)

let fileSystem = cluster.Store.CloudFileSystem

(*

Let's use that to create a container in the cluster's blob storage account

*)

let container = fileSystem.Directory.Create "/mycontainer"


(*

And populate it with some files

*)

let (@@) x y = fileSystem.Path.Combine(x,y)

let file1 = fileSystem.File.WriteAllText(container.Path @@ "a.txt", "Lorem ipsum dolor sit amet, consectetur adipiscing elit")
let file2 = fileSystem.File.WriteAllText(container.Path @@ "b.txt", "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.")
let file3 = fileSystem.File.WriteAllText(container.Path @@ "c.txt", "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi.")

(*

We can now send a cloud computation that acts on the uploaded files

*)

let getTotalSize () : Cloud<int64> = cloud { 
    let files = fileSystem.File.Enumerate("/mycontainer") // enumerate all files in container
    let! (textSizes : int64 []) = Cloud.Parallel [for f in files -> cloud { return __IMPLEMENT_ME__  (* file size in bytes *) }]
    return __IMPLEMENT_ME__ // the total size in bytes of contained files
}

let totalSize : int64 = __IMPLEMENT_ME__ // run the computation in the cloud

(*

Finally, let's delete the created container

*)

fileSystem.Directory.Delete("/mycontainer", recursiveDelete = true)

(*

///////////////////////////////////////////////
// Task 2: Download http files to blob storage

Let's now see how we can use mbrace to download files from the web
to our blob storage account in parallel.

*)

open System.IO
open System.Text.RegularExpressions

/// download text from given http uri
let downloadText (uri : string) = cloud {
    return! Cloud.OfAsync <| (new System.Net.WebClient()).AsyncDownloadString(Uri uri)
}

/// our source uri which contains links to all the text files
let sourceUri = "http://textfiles.com/etext/AUTHORS/ARISTOTLE/"
/// Regex for locating text hyperlinks
let hrefRegex = new Regex("href=\"([^\"]*\.txt)\"", RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

let download() = cloud {
    let! (sourceText : string) = __IMPLEMENT_ME__ // download all html from source uri
    let textFiles = // find all linked text files in html
        hrefRegex.Matches sourceText
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Groups.[1].Value)
        |> Seq.map (fun file -> Path.Combine(sourceUri, file))

    let! container = CloudDirectory.Create "/aristotle" // create a container for all our files

    let downloadToBlobFile (textUri : string) = cloud {
        let! (text : string) = __IMPLEMENT_ME__ // download all text from uri
        let! path = CloudPath.Combine(container.Path, Path.GetFileName textUri) // create a path for target blob
        let! fileInfo = CloudFile.WriteAllText(path, text) // write all text to target blob
        return fileInfo.Path
    }

    let! (results : string []) = __IMPLEMENT_ME__ // use Cloud.Parallel to parallelize download of files to blob store
    return results
}

let downloadProc : CloudProcess<string []> = __IMPLEMENT_ME__ // run the computation to download the files

downloadProc.ShowInfo() // track download progress

let downloadedFiles = downloadProc.Result // get the downloaded files

(*

/////////////////////////////////////////////////////////
// Task 3: Performing wordcount on blob store text files

Let's try to put our freshly downloaded files into use.
We'll be performing a simple parallel wordcount operation.

*)

type WordFrequency = string * int // word x frequency count
type WordCount = WordFrequency []

/// compute wordcount of given text body
let computeWordCount (text : string) : WordCount =
    let words = text.Split([|' '; '\n' ; '.' ; ',' ; ':' ; ';' ; '\"'|])
    words |> Seq.filter (fun w -> w.Length > 4) 
          |> Seq.map (fun w -> w.ToLower()) 
          |> Seq.countBy id
          |> Seq.toArray

/// combine two wordcounts into one
let combine (w1 : WordCount) (w2 : WordCount) : WordCount =
    Seq.append w1 w2
    |> Seq.groupBy fst
    |> Seq.map (fun (w,freqs) -> w, freqs |> Seq.sumBy snd)
    |> Seq.toArray

/// computes wordcount on all files that we downloaded
let computeWordCountCloud () : Cloud<WordCount> = cloud {
    // find all files in blob container
    let! files = CloudFile.Enumerate "/aristotle"
    // function that computes wordcount for given file
    let getWordCount (file : CloudFileInfo) : Cloud<WordCount> = cloud {
        let! text = CloudFile.ReadAllText file.Path // read all text from file
        return __IMPLEMENT_ME__ // compute the word count on given text
    }

    let! wordCounts = Cloud.Parallel [for f in files -> getWordCount f]
    let aggregate : WordCount = __IMPLEMENT_ME__ // use 'combine' to compute the aggregate WordCount
    return __IMPLEMENT_ME__ // take only the top 10 words by frequency
}

let wordcountProc : CloudProcess<WordCount> = __IMPLEMENT_ME__ // run the computation in the cluster

(* YOU HANE NOW COMPLETED CHAPTER 3 *)