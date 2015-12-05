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

# Chapter 4: Cloud Flow

In this tutorial you will learn how you can use the MBrace.Flow library to perform cloud analytics.
Follow the instructions and complete the assignments described below.

///////////////////////////////////
// Task 1: The MBrace.Flow Library

MBrace.Flow is a library built on top of the MBrace core primitives we have explored earlier.
It provides APIs for performing distributed computation using LINQ-style functional pipelines.

Let's begin by referencing the flow library

*)

#r "../../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"
open MBrace.Flow

(*

Primitives in the flow library are values of type CloudFlow<'T>.

*)

let source : CloudFlow<int> = CloudFlow.OfArray [|1 .. 1000|]

(*

This defines a value of type CloudFlow<int>. Just like when defining Cloud operations,
the declaration above has no computation effect. 

CloudFlows can be transformed:

*)

let mapped : CloudFlow<int * string> = CloudFlow.map (fun i -> (i % 57, string i)) source

(*

or filtered:

*)

let filtered : CloudFlow<int * string> = CloudFlow.filter (fun (r,_) -> r <> 0) mapped

(*

or counted by:

*)

let grouped : CloudFlow<int * int64> = CloudFlow.countBy fst filtered


(*

or sorted:

*)

let sorted : CloudFlow<int * int64> = CloudFlow.sortByDescending snd 10 grouped

(*

Finally, a cloud flow can be materialized by passing it to a consumer

*)

let result : Cloud<(int * int64) []> = CloudFlow.toArray sorted

(*

Note, the type has now changed from CloudFlow<'T> to Cloud<'T>. 
This means that we now have an expression ready to be computed by our cluster:

*)

cluster.Run result

(*

We can write the above in one line like so:

*)

CloudFlow.OfArray [|1 .. 1000|]
|> CloudFlow.map (fun i -> i % 57, string i)
|> CloudFlow.filter (fun (i,_) -> i <> 0)
|> CloudFlow.countBy fst
|> CloudFlow.sortByDescending snd 10
|> CloudFlow.toArray
|> cluster.Run

(*

MBrace.Flow will automatically distribute the above workflow across the cluster,
utilising as efficiently as possible all resources available to it.



////////////////////////////////
// Task 2: House Sale Analytics

Let's move on to a more interesting example. 
We'll be using CloudFlow and the CSV type provider to perform analytics on the UK land registry public data.

We begin by referencing FSharp.Data and define a type provider for our CSV format:

*)

#r "../../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open FSharp.Data

type HousePrices = CsvProvider< "../../../data/SampleHousePrices.csv", HasHeaders = true>

(*

Next, we define our data sources: CSV house sale data on the years 2012 up to 2015,
all hosted in the land registry's web server.

*)

let sources = 
  [ "http://publicdata.landregistry.gov.uk/market-trend-data/price-paid-data/a/pp-2012.csv"
    "http://publicdata.landregistry.gov.uk/market-trend-data/price-paid-data/a/pp-2013.csv"
    "http://publicdata.landregistry.gov.uk/market-trend-data/price-paid-data/a/pp-2014.csv"
    "http://publicdata.landregistry.gov.uk/market-trend-data/price-paid-data/a/pp-2015.csv" ]

(*

Performing a cloud computation on the data using CloudFlow is easy:

*)

CloudFlow.OfHttpFileByLine sources
|> CloudFlow.length
|> cluster.Run

(*

This starts a flow by reading all the provided URIs by line in a distributed fashion.
In the example above we simply calculate the total number of CSV rows in our data set.

To perform more meaningful computations on the dataset, we need to make use of our CSV type provider:

*)

CloudFlow.OfHttpFileByLine sources
|> CloudFlow.collect HousePrices.ParseRows
|> CloudFlow.averageByKey 
        (fun row -> row.DateOfTransfer.Year, row.DateOfTransfer.Month) 
        (fun row -> float row.Price)
|> CloudFlow.sortBy fst 100
|> CloudFlow.toArray
|> cluster.Run

(*

The example above uses the CSV provider parsing functionality to convert a line
of comma separated values to a strongly typed row based on the provided sample.

We then use the parsed data to determine which months registered the highest sale prices, on average.


///////////////////////////////
// Task 3: Caching Cloud flows

It should be pointed out that in the above workflows, 
the data set is being downloaded from the public server every time we send in a new computation.

How can we avoid this? Here's where persisted cloud flows come into play:

*)

let cachedFlow : PersistedCloudFlow<_> =
    CloudFlow.OfHttpFileByLine sources
    |> CloudFlow.collect HousePrices.ParseRows
    |> CloudFlow.persist StorageLevel.Memory
    |> cluster.Run

(*

The 'CloudFlow.persist' combinator forces evaluation of the flow it is being passed, 
persisting its results to cluster storage, either in-memory or at the blob store.

In this case, data has been partitioned and loaded in-memory across workers in our cluster.
This means that we can use this cached flow to perform ultra-fast queries on our parsed data set.

Sample 1: the most expensive property in Oxfordshire

*)

cachedFlow
|> CloudFlow.filter (fun row -> row.County = "OXFORDSHIRE")
|> CloudFlow.maxBy (fun row -> row.Price)
|> cluster.Run

(*

Sample 2: the least expensive street in Westminster, on average

*)

cachedFlow
|> CloudFlow.filter (fun row -> row.TownCity = "LONDON")
|> CloudFlow.filter (fun row -> row.District = "CITY OF WESTMINSTER")
|> CloudFlow.averageByKey
    (fun row -> row.Street)
    (fun row -> float row.Price)

|> CloudFlow.minBy snd
|> cluster.Run

(*

////////////////////////////////////
// Task 4: Writing your own Queries

In this assignment, we will be giving natural language descriptions
of what house data information needs to be extracted and you will have
to write a CloudFlow query on the cached dataset:

a) Find the most expensive property in London that is NOT located in Westminster

*)

(*

b) Find the street that contains the LEAST expensive property in Islington:

*)

(*

c) Find the month that registered the biggest number of property sales in the county of Cambridgeshire:

*)

(*

d) Find the outward code that hosts the highest -on average- property prices in London. 
   For those infamiliar with UK postcodes, here's a definition of the outward code:
   https://en.wikipedia.org/wiki/Postcodes_in_the_United_Kingdom#Outward_code

*)

(*

e) Find the most recent property sale whose outward code matches that of CodeNode.

*)


(* YOU HANE NOW COMPLETED CHAPTER 4 *)